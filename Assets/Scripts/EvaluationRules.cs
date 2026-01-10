using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Static rules and heuristics for evaluating player decisions
/// Layer 1: Hard-coded rules of thumb
/// Layer 2: Dynamic adjustments from MetaSnapshot
/// </summary>
public static class EvaluationRules
{
    // ============================================================================
    // LEVELING RULES
    // ============================================================================

    public static PlayQuality EvaluateLevelingDecision(int currentTurn, int currentLevel, int targetLevel, int gold, MetaSnapshot meta)
    {
        float expectedLevel = meta.GetExpectedLevel(currentTurn);
        int levelCost = GetLevelUpCost(currentLevel);

        // Critical: Leveling when it costs all gold and leaves you broke
        if (gold - levelCost < 3)
        {
            return PlayQuality.Critical; // "You'll have no gold to buy units!"
        }

        // Check if we're behind, on pace, or ahead
        float levelDelta = targetLevel - expectedLevel;

        if (levelDelta < -1.5f)
        {
            // Significantly behind - good to level
            return PlayQuality.Optimal;
        }
        else if (levelDelta < -0.5f)
        {
            // Slightly behind - good
            return PlayQuality.Good;
        }
        else if (levelDelta < 0.5f)
        {
            // On pace - acceptable
            return PlayQuality.Acceptable;
        }
        else if (levelDelta < 1.5f)
        {
            // Slightly ahead - questionable unless board is weak
            return PlayQuality.Questionable;
        }
        else
        {
            // Way ahead - poor, you're wasting stats
            return PlayQuality.Poor;
        }
    }

    public static int GetLevelUpCost(int currentLevel)
    {
        int[] costs = { 0, 5, 7, 8, 9, 10, 11 };
        return currentLevel < costs.Length ? costs[currentLevel] : 11;
    }

    // ============================================================================
    // ECONOMY RULES
    // ============================================================================

    public static PlayQuality EvaluateReroll(int gold, int tavernTier, int turnNumber, int boardStrength)
    {
        // Critical: Rerolling on turn 1-2
        if (turnNumber <= 2)
        {
            return PlayQuality.Critical; // "Never reroll early!"
        }

        // Poor: Rerolling with low gold
        if (gold < 4)
        {
            return PlayQuality.Poor; // "You can't afford anything even if you find it"
        }

        // Context-dependent: Rerolling when health is low
        if (boardStrength < 3)
        {
            return PlayQuality.Questionable; // "Your board is weak, but rerolling wastes tempo"
        }

        // Acceptable in mid-game with spare gold
        if (turnNumber >= 5 && gold >= 6)
        {
            return PlayQuality.Acceptable;
        }

        return PlayQuality.Good;
    }

    public static PlayQuality EvaluatePurchase(UnitData unit, int cost, int gold, int tavernTier, List<UnitData> boardUnits)
    {
        // Critical: Buying a unit you can't afford
        if (cost > gold)
        {
            return PlayQuality.Critical; // This shouldn't happen if UI is correct
        }

        // Check synergy with existing board
        int synergyCount = CountSynergies(unit, boardUnits);

        // Poor: Buying off-tribe with no synergies
        if (synergyCount == 0 && boardUnits.Count >= 3)
        {
            return PlayQuality.Poor; // "This doesn't fit your board"
        }

        // Good: Strong synergy
        if (synergyCount >= 2)
        {
            return PlayQuality.Good;
        }

        // Check unit tier vs tavern tier
        if (unit.tier > tavernTier)
        {
            return PlayQuality.Optimal; // "High tier unit from discovery/random!"
        }

        return PlayQuality.Acceptable;
    }

    private static int CountSynergies(UnitData newUnit, List<UnitData> boardUnits)
    {
        int count = 0;

        foreach (var existing in boardUnits)
        {
            // Tribe synergy
            if (existing.tribe == newUnit.tribe && newUnit.tribe != Tribe.None)
                count++;

            // TODO: Add keyword synergies (e.g., Deathrattle synergy)
        }

        return count;
    }

    // ============================================================================
    // SELL DECISIONS
    // ============================================================================

    public static PlayQuality EvaluateSell(UnitData unit, List<UnitData> boardUnits, int turnNumber)
    {
        // Selling a buffed/golden unit
        if (unit.baseAttack > unit.baseAttack + 2) // Heuristic: likely buffed
        {
            return PlayQuality.Poor; // "You're selling stats!"
        }

        // Selling early for gold is usually bad
        if (turnNumber <= 3)
        {
            return PlayQuality.Questionable; // "Early sells weaken your board"
        }

        // Check if unit has synergy
        int synergyCount = CountSynergies(unit, boardUnits);
        if (synergyCount >= 2)
        {
            return PlayQuality.Questionable; // "This has synergy with your board"
        }

        // Selling to make space for better units
        return PlayQuality.Acceptable;
    }

    // ============================================================================
    // TRIPLE DECISIONS
    // ============================================================================

    public static PlayQuality EvaluateTripleAcceptance(int tavernTier, int turnNumber, bool hasDiscoveryChoice)
    {
        // Always accept triples early
        if (turnNumber <= 4)
        {
            return PlayQuality.Optimal;
        }

        // Good if it upgrades your tier
        if (hasDiscoveryChoice && tavernTier < 6)
        {
            return PlayQuality.Good;
        }

        // Acceptable in general
        return PlayQuality.Acceptable;
    }

    // ============================================================================
    // COMBAT TIMING
    // ============================================================================

    public static PlayQuality EvaluateEndTurn(int goldLeft, int turnNumber, int boardCount)
    {
        // Critical: Ending turn with lots of gold unspent
        if (goldLeft >= 5 && turnNumber <= 10)
        {
            return PlayQuality.Critical; // "You're wasting resources!"
        }

        // Poor: Weak board
        if (boardCount < 3)
        {
            return PlayQuality.Poor; // "Your board is too weak!"
        }

        // Acceptable: Some gold left for interest/banking
        if (goldLeft <= 2)
        {
            return PlayQuality.Acceptable;
        }

        return PlayQuality.Good;
    }

    // ============================================================================
    // BOARD STRENGTH ESTIMATION
    // ============================================================================

    public static int EstimateBoardStrength(List<CardDisplay> board)
    {
        int strength = 0;

        foreach (var unit in board)
        {
            if (unit == null || unit.unitData == null) continue;

            // Base stats
            strength += unit.currentAttack + unit.currentHealth;

            // Tier bonus
            strength += unit.unitData.tier * 2;

            // Golden bonus
            if (unit.isGolden) strength += 5;
        }

        return strength;
    }

    // ============================================================================
    // ALTERNATIVE SUGGESTIONS
    // ============================================================================

    public static List<string> GenerateAlternatives(DecisionType type, DecisionEvaluation decision)
    {
        var alternatives = new List<string>();

        switch (type)
        {
            case DecisionType.Reroll:
                alternatives.Add("Save gold for next turn");
                alternatives.Add("Level up tavern instead");
                break;

            case DecisionType.UpgradeTavern:
                alternatives.Add("Strengthen current board first");
                alternatives.Add("Save gold for better units");
                break;

            case DecisionType.BuyUnit:
                alternatives.Add("Look for synergy units instead");
                alternatives.Add("Save gold for tavern upgrade");
                break;

            case DecisionType.EndTurn:
                alternatives.Add("Spend remaining gold on rerolls");
                alternatives.Add("Upgrade tavern tier");
                break;
        }

        return alternatives;
    }
}
