using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Core evaluation engine that tracks and evaluates player decisions
/// Integrates with GameManager hooks and provides coaching feedback
/// </summary>
public class DecisionEvaluator : MonoBehaviour
{
    public static DecisionEvaluator instance;

    [Header("Settings")]
    public CoachingSettings coachingSettings = new CoachingSettings();

    [Header("Meta Knowledge")]
    public MetaSnapshot currentMeta;

    [Header("Current Session")]
    public GameLog currentGameLog;
    private PhaseEvaluation currentPhase;

    [Header("UI References")]
    public CoachingHintUI hintUI;
    public PostMortemUI postMortemUI;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            LoadMetaSnapshot();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ============================================================================
    // SESSION MANAGEMENT
    // ============================================================================

    public void StartNewGame(string heroName)
    {
        currentGameLog = new GameLog(heroName);

        if (PlayerProfile.instance != null)
            currentGameLog.startingMMR = PlayerProfile.instance.mmr;

        currentPhase = new PhaseEvaluation(1);
        currentGameLog.phases.Add(currentPhase);

        Debug.Log($"<color=cyan>[EVALUATOR]</color> Started tracking game with {heroName}");
    }

    public void StartNewPhase(int turnNumber)
    {
        if (currentGameLog == null) return;

        // Finalize previous phase
        if (currentPhase != null)
        {
            currentPhase.CalculateOverallScore();
        }

        // Create new phase
        currentPhase = new PhaseEvaluation(turnNumber);
        currentGameLog.phases.Add(currentPhase);
    }

    public void EndGame(int placement)
    {
        if (currentGameLog == null) return;

        currentGameLog.finalPlacement = placement;
        currentGameLog.finalTurn = GameManager.instance.turnNumber;

        if (PlayerProfile.instance != null)
            currentGameLog.finalMMR = PlayerProfile.instance.mmr;

        currentGameLog.FinalizeLog();

        Debug.Log($"<color=cyan>[EVALUATOR]</color> Game ended. Placement: {placement}. Avg Score: {currentGameLog.averageScore:F1}");

        // Show post-mortem if enabled
        if (coachingSettings.showPostMortem && postMortemUI != null)
        {
            postMortemUI.DisplayPostMortem(currentGameLog);
        }

        // Upload to PlayFab
        if (PlayFabManager.instance != null)
        {
            PlayFabManager.instance.UploadGameLog(currentGameLog);
        }
    }

    // ============================================================================
    // DECISION TRACKING HOOKS
    // ============================================================================

    public void EvaluateBuyDecision(UnitData unit, int cost)
    {
        if (currentPhase == null || GameManager.instance == null) return;

        var evaluation = new DecisionEvaluation
        {
            type = DecisionType.BuyUnit,
            turnNumber = GameManager.instance.turnNumber,
            goldBefore = GameManager.instance.gold + cost,
            goldAfter = GameManager.instance.gold,
            tavernTier = GetCurrentTavernTier(),
            playerHealth = GameManager.instance.playerHealth,
            heroName = GameManager.instance.activeHero?.heroName ?? "Unknown",
            actionTaken = $"Bought {unit.unitName} (Tier {unit.tier}) for {cost}g"
        };

        // Get board state
        List<UnitData> boardUnits = GetBoardUnits();

        // Evaluate quality
        evaluation.quality = EvaluationRules.EvaluatePurchase(unit, cost, evaluation.goldBefore, evaluation.tavernTier, boardUnits);

        // Generate reasoning
        evaluation.reasoning = GeneratePurchaseReasoning(evaluation.quality, unit, boardUnits);

        // Estimate impact
        evaluation.impactScore = EstimatePurchaseImpact(unit, evaluation.quality);

        // Add alternatives if needed
        if (evaluation.quality <= PlayQuality.Questionable)
        {
            evaluation.alternatives = EvaluationRules.GenerateAlternatives(DecisionType.BuyUnit, evaluation);
        }

        RecordDecision(evaluation);
    }

    public void EvaluateSellDecision(UnitData unit)
    {
        if (currentPhase == null || GameManager.instance == null) return;

        var evaluation = new DecisionEvaluation
        {
            type = DecisionType.SellUnit,
            turnNumber = GameManager.instance.turnNumber,
            goldBefore = GameManager.instance.gold - 1,
            goldAfter = GameManager.instance.gold,
            tavernTier = GetCurrentTavernTier(),
            playerHealth = GameManager.instance.playerHealth,
            heroName = GameManager.instance.activeHero?.heroName ?? "Unknown",
            actionTaken = $"Sold {unit.unitName} for 1g"
        };

        List<UnitData> boardUnits = GetBoardUnits();
        evaluation.quality = EvaluationRules.EvaluateSell(unit, boardUnits, evaluation.turnNumber);
        evaluation.reasoning = $"Sold {unit.unitName} - {GetQualityDescription(evaluation.quality)}";
        evaluation.impactScore = -10f; // Selling is usually small negative impact

        RecordDecision(evaluation);
    }

    public void EvaluateRerollDecision(int cost)
    {
        if (currentPhase == null || GameManager.instance == null) return;

        var evaluation = new DecisionEvaluation
        {
            type = DecisionType.Reroll,
            turnNumber = GameManager.instance.turnNumber,
            goldBefore = GameManager.instance.gold + cost,
            goldAfter = GameManager.instance.gold,
            tavernTier = GetCurrentTavernTier(),
            playerHealth = GameManager.instance.playerHealth,
            heroName = GameManager.instance.activeHero?.heroName ?? "Unknown",
            actionTaken = $"Rerolled shop for {cost}g"
        };

        int boardStrength = EvaluationRules.EstimateBoardStrength(GetBoardCards());
        evaluation.quality = EvaluationRules.EvaluateReroll(
            evaluation.goldBefore,
            evaluation.tavernTier,
            evaluation.turnNumber,
            boardStrength
        );

        evaluation.reasoning = GenerateRerollReasoning(evaluation.quality, evaluation.turnNumber);
        evaluation.impactScore = evaluation.quality <= PlayQuality.Poor ? -20f : -5f;

        if (evaluation.quality <= PlayQuality.Questionable)
        {
            evaluation.alternatives = EvaluationRules.GenerateAlternatives(DecisionType.Reroll, evaluation);
        }

        RecordDecision(evaluation);
    }

    public void EvaluateLevelUpDecision(int fromLevel, int toLevel, int cost)
    {
        if (currentPhase == null || GameManager.instance == null) return;

        var evaluation = new DecisionEvaluation
        {
            type = DecisionType.UpgradeTavern,
            turnNumber = GameManager.instance.turnNumber,
            goldBefore = GameManager.instance.gold + cost,
            goldAfter = GameManager.instance.gold,
            tavernTier = toLevel,
            playerHealth = GameManager.instance.playerHealth,
            heroName = GameManager.instance.activeHero?.heroName ?? "Unknown",
            actionTaken = $"Upgraded tavern from {fromLevel} to {toLevel} for {cost}g"
        };

        evaluation.quality = EvaluationRules.EvaluateLevelingDecision(
            evaluation.turnNumber,
            fromLevel,
            toLevel,
            evaluation.goldBefore,
            currentMeta
        );

        evaluation.reasoning = GenerateLevelingReasoning(evaluation.quality, evaluation.turnNumber, toLevel);
        evaluation.impactScore = EstimateLevelingImpact(evaluation.quality, fromLevel, toLevel);

        if (evaluation.quality <= PlayQuality.Questionable)
        {
            evaluation.alternatives = EvaluationRules.GenerateAlternatives(DecisionType.UpgradeTavern, evaluation);
        }

        RecordDecision(evaluation);
    }

    public void EvaluateEndTurnDecision()
    {
        if (currentPhase == null || GameManager.instance == null) return;

        int goldLeft = GameManager.instance.gold;
        int boardCount = GameManager.instance.playerBoard.childCount;

        var evaluation = new DecisionEvaluation
        {
            type = DecisionType.EndTurn,
            turnNumber = GameManager.instance.turnNumber,
            goldBefore = goldLeft,
            goldAfter = goldLeft,
            tavernTier = GetCurrentTavernTier(),
            playerHealth = GameManager.instance.playerHealth,
            heroName = GameManager.instance.activeHero?.heroName ?? "Unknown",
            actionTaken = $"Ended turn with {goldLeft}g remaining, {boardCount} units"
        };

        evaluation.quality = EvaluationRules.EvaluateEndTurn(goldLeft, evaluation.turnNumber, boardCount);
        evaluation.reasoning = GenerateEndTurnReasoning(evaluation.quality, goldLeft, boardCount);
        evaluation.impactScore = evaluation.quality <= PlayQuality.Poor ? -30f : 0f;

        if (evaluation.quality <= PlayQuality.Questionable)
        {
            evaluation.alternatives = EvaluationRules.GenerateAlternatives(DecisionType.EndTurn, evaluation);
        }

        RecordDecision(evaluation);
    }

    // ============================================================================
    // DECISION RECORDING & FEEDBACK
    // ============================================================================

    private void RecordDecision(DecisionEvaluation evaluation)
    {
        if (currentPhase == null) return;

        currentPhase.decisions.Add(evaluation);

        // Real-time hint if appropriate
        if (ShouldShowHint(evaluation))
        {
            ShowHint(evaluation);
        }

        // Log for debugging
        string qualityColor = GetQualityColor(evaluation.quality);
        Debug.Log($"<color={qualityColor}>[DECISION]</color> {evaluation.actionTaken} - {evaluation.quality} - {evaluation.reasoning}");
    }

    private bool ShouldShowHint(DecisionEvaluation evaluation)
    {
        if (!coachingSettings.showRealTimeHints) return false;
        if (evaluation.quality > coachingSettings.minimumHintQuality) return false;

        // Only show hints to players below MMR threshold
        if (PlayerProfile.instance != null && PlayerProfile.instance.mmr > coachingSettings.maxMMRForHints)
            return false;

        return true;
    }

    private void ShowHint(DecisionEvaluation evaluation)
    {
        if (hintUI != null)
        {
            hintUI.ShowHint(evaluation, coachingSettings.pauseOnTeachingMoments);
        }
    }

    // ============================================================================
    // HELPER METHODS
    // ============================================================================

    private List<UnitData> GetBoardUnits()
    {
        var units = new List<UnitData>();
        if (GameManager.instance?.playerBoard == null) return units;

        foreach (Transform child in GameManager.instance.playerBoard)
        {
            var card = child.GetComponent<CardDisplay>();
            if (card != null && card.unitData != null)
                units.Add(card.unitData);
        }

        return units;
    }

    private List<CardDisplay> GetBoardCards()
    {
        var cards = new List<CardDisplay>();
        if (GameManager.instance?.playerBoard == null) return cards;

        foreach (Transform child in GameManager.instance.playerBoard)
        {
            var card = child.GetComponent<CardDisplay>();
            if (card != null)
                cards.Add(card);
        }

        return cards;
    }

    private int GetCurrentTavernTier()
    {
        if (ShopManager.instance != null)
            return ShopManager.instance.tavernTier;
        return 1;
    }

    // ============================================================================
    // REASONING GENERATION
    // ============================================================================

    private string GeneratePurchaseReasoning(PlayQuality quality, UnitData unit, List<UnitData> board)
    {
        switch (quality)
        {
            case PlayQuality.Optimal:
                return $"{unit.unitName} is a great pick - strong tier and synergy!";
            case PlayQuality.Good:
                return $"{unit.unitName} fits your board well.";
            case PlayQuality.Acceptable:
                return $"{unit.unitName} is okay, but watch for better synergies.";
            case PlayQuality.Questionable:
                return $"{unit.unitName} doesn't synergize with your board. Consider alternatives.";
            case PlayQuality.Poor:
                return $"{unit.unitName} is off-tribe with no synergies. You should focus your build.";
            case PlayQuality.Critical:
                return $"Critical mistake! You can't afford this or it's terrible value.";
            default:
                return "Purchase evaluated.";
        }
    }

    private string GenerateRerollReasoning(PlayQuality quality, int turn)
    {
        switch (quality)
        {
            case PlayQuality.Critical:
                return $"Never reroll on turn {turn}! You need that gold.";
            case PlayQuality.Poor:
                return "Rerolling with low gold leaves you unable to buy units.";
            case PlayQuality.Questionable:
                return "Rerolling is risky here - consider saving for levels.";
            default:
                return "Reroll is acceptable in this situation.";
        }
    }

    private string GenerateLevelingReasoning(PlayQuality quality, int turn, int newLevel)
    {
        float expected = currentMeta.GetExpectedLevel(turn);
        switch (quality)
        {
            case PlayQuality.Optimal:
                return $"Great leveling! You're catching up to meta pace (Avg: {expected:F1}).";
            case PlayQuality.Good:
                return $"Good level timing. Close to average pace.";
            case PlayQuality.Acceptable:
                return "Acceptable level timing.";
            case PlayQuality.Questionable:
                return $"You're leveling ahead of curve. Make sure your board is strong enough.";
            case PlayQuality.Poor:
                return "Leveling too fast! You're sacrificing board strength.";
            case PlayQuality.Critical:
                return "Critical error! This level costs all your gold - you can't buy units!";
            default:
                return "Level evaluated.";
        }
    }

    private string GenerateEndTurnReasoning(PlayQuality quality, int goldLeft, int boardCount)
    {
        switch (quality)
        {
            case PlayQuality.Critical:
                return $"You're ending turn with {goldLeft}g unspent! This is wasted tempo.";
            case PlayQuality.Poor:
                return $"Your board only has {boardCount} units. You need to fill it!";
            case PlayQuality.Questionable:
                return "Consider spending more gold before ending turn.";
            default:
                return "Turn end looks good.";
        }
    }

    private float EstimatePurchaseImpact(UnitData unit, PlayQuality quality)
    {
        float baseImpact = unit.tier * 5f;

        switch (quality)
        {
            case PlayQuality.Optimal: return baseImpact * 1.5f;
            case PlayQuality.Good: return baseImpact;
            case PlayQuality.Acceptable: return baseImpact * 0.7f;
            case PlayQuality.Questionable: return baseImpact * 0.3f;
            case PlayQuality.Poor: return -baseImpact * 0.5f;
            case PlayQuality.Critical: return -baseImpact;
            default: return 0f;
        }
    }

    private float EstimateLevelingImpact(PlayQuality quality, int fromLevel, int toLevel)
    {
        float baseImpact = (toLevel - fromLevel) * 15f;

        switch (quality)
        {
            case PlayQuality.Optimal: return baseImpact;
            case PlayQuality.Good: return baseImpact * 0.8f;
            case PlayQuality.Acceptable: return baseImpact * 0.6f;
            case PlayQuality.Questionable: return -baseImpact * 0.3f;
            case PlayQuality.Poor: return -baseImpact * 0.7f;
            case PlayQuality.Critical: return -baseImpact * 1.5f;
            default: return 0f;
        }
    }

    private string GetQualityDescription(PlayQuality quality)
    {
        switch (quality)
        {
            case PlayQuality.Optimal: return "Optimal play";
            case PlayQuality.Good: return "Good decision";
            case PlayQuality.Acceptable: return "Acceptable";
            case PlayQuality.Questionable: return "Questionable choice";
            case PlayQuality.Poor: return "Poor decision";
            case PlayQuality.Critical: return "Critical mistake";
            default: return "Evaluated";
        }
    }

    private string GetQualityColor(PlayQuality quality)
    {
        switch (quality)
        {
            case PlayQuality.Optimal: return "green";
            case PlayQuality.Good: return "lime";
            case PlayQuality.Acceptable: return "white";
            case PlayQuality.Questionable: return "yellow";
            case PlayQuality.Poor: return "orange";
            case PlayQuality.Critical: return "red";
            default: return "white";
        }
    }

    // ============================================================================
    // META SNAPSHOT MANAGEMENT
    // ============================================================================

    private void LoadMetaSnapshot()
    {
        // Try to load from PlayFab TitleData
        if (PlayFabManager.instance != null)
        {
            PlayFabManager.instance.LoadMetaSnapshot(OnMetaLoaded);
        }
        else
        {
            // Use defaults
            currentMeta = new MetaSnapshot();
        }
    }

    private void OnMetaLoaded(MetaSnapshot meta)
    {
        currentMeta = meta;
        Debug.Log($"<color=cyan>[EVALUATOR]</color> Loaded MetaSnapshot v{meta.version} from {meta.lastUpdated}");
    }
}
