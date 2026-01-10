using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Core data structures for the Decision Evaluator System
/// </summary>

// ============================================================================
// ENUMS
// ============================================================================

public enum PlayQuality
{
    Optimal,      // Best possible play given the situation
    Good,         // Solid play, minor inefficiencies
    Acceptable,   // Reasonable but suboptimal
    Questionable, // Likely a mistake but context-dependent
    Poor,         // Clear mistake with better alternatives
    Critical      // Game-losing mistake
}

public enum DecisionType
{
    // Economy
    BuyUnit,
    SellUnit,
    Reroll,
    UpgradeTavern,
    FreezeShop,

    // Board Management
    PlayUnit,
    PositionUnit,
    TripleDecision, // Accept or delay triple

    // Combat
    EndTurn,        // When to commit to combat

    // Hero Power
    UseHeroPower,

    // Advanced
    GoldManagement, // Banking/interest decisions
    LevelTiming     // When to level vs strengthen board
}

public enum CoachingGoal
{
    JustHaveFun,        // No interruptions, analytics only
    ImproveGradually,   // Post-game feedback only
    CompetitiveClimb,   // Occasional hints for critical mistakes
    Tryhard             // Real-time coaching with pauses
}

// ============================================================================
// DATA STRUCTURES
// ============================================================================

[Serializable]
public class DecisionEvaluation
{
    public DecisionType type;
    public PlayQuality quality;
    public int turnNumber;
    public float timestamp;

    // Context
    public int goldBefore;
    public int goldAfter;
    public int tavernTier;
    public int playerHealth;
    public string heroName;

    // Specific Decision Data
    public string actionTaken;          // e.g., "Bought Tier 2 Dragon"
    public List<string> alternatives;   // Better plays that were available
    public string reasoning;            // Why this was rated this way

    // Scoring
    public float impactScore;           // -100 to +100, estimated impact on game outcome

    public DecisionEvaluation()
    {
        alternatives = new List<string>();
        timestamp = Time.time;
    }
}

[Serializable]
public class PhaseEvaluation
{
    public int turnNumber;
    public float overallScore;          // 0-100 rating for this turn
    public List<DecisionEvaluation> decisions;

    // Turn Summary
    public int goldSpent;
    public int goldGained;
    public int unitsGained;
    public int unitsSold;
    public bool leveledUp;
    public int combatDamage;            // Negative if loss, positive if win

    public PhaseEvaluation(int turn)
    {
        turnNumber = turn;
        decisions = new List<DecisionEvaluation>();
    }

    public void CalculateOverallScore()
    {
        if (decisions.Count == 0)
        {
            overallScore = 50f; // Neutral
            return;
        }

        float totalScore = 0f;
        float totalWeight = 0f;

        foreach (var decision in decisions)
        {
            float weight = Mathf.Abs(decision.impactScore);
            float qualityScore = QualityToScore(decision.quality);

            totalScore += qualityScore * weight;
            totalWeight += weight;
        }

        overallScore = totalWeight > 0 ? (totalScore / totalWeight) : 50f;
    }

    private float QualityToScore(PlayQuality quality)
    {
        switch (quality)
        {
            case PlayQuality.Optimal: return 100f;
            case PlayQuality.Good: return 80f;
            case PlayQuality.Acceptable: return 60f;
            case PlayQuality.Questionable: return 40f;
            case PlayQuality.Poor: return 20f;
            case PlayQuality.Critical: return 0f;
            default: return 50f;
        }
    }
}

[Serializable]
public class GameLog
{
    public string sessionId;
    public string heroName;
    public DateTime startTime;
    public DateTime endTime;

    public int finalPlacement;  // 1-8 or 0 if incomplete
    public int finalTurn;
    public int startingMMR;
    public int finalMMR;

    public List<PhaseEvaluation> phases;

    public float averageScore;
    public int criticalMistakes;
    public int optimalPlays;

    public GameLog(string hero)
    {
        sessionId = Guid.NewGuid().ToString().Substring(0, 8);
        heroName = hero;
        startTime = DateTime.Now;
        phases = new List<PhaseEvaluation>();
    }

    public void FinalizeLog()
    {
        endTime = DateTime.Now;
        CalculateStats();
    }

    private void CalculateStats()
    {
        if (phases.Count == 0) return;

        float totalScore = 0f;
        criticalMistakes = 0;
        optimalPlays = 0;

        foreach (var phase in phases)
        {
            totalScore += phase.overallScore;

            foreach (var decision in phase.decisions)
            {
                if (decision.quality == PlayQuality.Critical || decision.quality == PlayQuality.Poor)
                    criticalMistakes++;
                if (decision.quality == PlayQuality.Optimal)
                    optimalPlays++;
            }
        }

        averageScore = totalScore / phases.Count;
    }
}

// ============================================================================
// HERO ECONOMY PROFILES
// ============================================================================

[Serializable]
public class HeroEconomyProfile
{
    public string heroName;
    public bool hasInterestMechanic;
    public bool hasBankingMechanic;
    public bool hasGoldGeneration;      // e.g., passive gold income
    public float economyWeight;         // How much to prioritize economy (0-1)

    public static HeroEconomyProfile GetProfile(string heroName)
    {
        // Default profile
        var profile = new HeroEconomyProfile
        {
            heroName = heroName,
            hasInterestMechanic = false,
            hasBankingMechanic = false,
            hasGoldGeneration = false,
            economyWeight = 0.5f
        };

        // TODO: Customize per hero when you add economy-focused heroes
        // Example:
        // if (heroName == "Banker Dragon")
        // {
        //     profile.hasBankingMechanic = true;
        //     profile.economyWeight = 0.8f;
        // }

        return profile;
    }
}

// ============================================================================
// COACHING SETTINGS
// ============================================================================

[Serializable]
public class CoachingSettings
{
    public CoachingGoal goal = CoachingGoal.ImproveGradually;
    public int maxMMRForHints = 2000;  // Only show hints below this MMR

    public bool showPostMortem = true;
    public bool showRealTimeHints = false;
    public bool pauseOnTeachingMoments = false;

    public PlayQuality minimumHintQuality = PlayQuality.Poor; // Only hint on Poor or Critical

    public void ApplyGoal(CoachingGoal newGoal)
    {
        goal = newGoal;

        switch (goal)
        {
            case CoachingGoal.JustHaveFun:
                showPostMortem = false;
                showRealTimeHints = false;
                pauseOnTeachingMoments = false;
                break;

            case CoachingGoal.ImproveGradually:
                showPostMortem = true;
                showRealTimeHints = false;
                pauseOnTeachingMoments = false;
                break;

            case CoachingGoal.CompetitiveClimb:
                showPostMortem = true;
                showRealTimeHints = true;
                pauseOnTeachingMoments = false;
                minimumHintQuality = PlayQuality.Poor;
                break;

            case CoachingGoal.Tryhard:
                showPostMortem = true;
                showRealTimeHints = true;
                pauseOnTeachingMoments = true;
                minimumHintQuality = PlayQuality.Questionable;
                break;
        }
    }
}
