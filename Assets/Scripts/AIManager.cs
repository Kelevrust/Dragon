using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class AIManager : MonoBehaviour
{
    public static AIManager instance;

    [Header("AI Settings")]
    [Tooltip("Multiplier for AI Gold. 1.0 = Normal.")]
    public float difficultyMultiplier = 1.0f; 

    [Header("Data Source")]
    [Tooltip("DRAG ALL UNIT FILES HERE (Tier 1-6)")]
    public UnitData[] allUnits; 
    
    private int[] tierCosts = new int[] { 0, 0, 5, 7, 8, 9, 10 }; 

    void Awake() { instance = this; }

    public List<UnitData> GenerateEnemyBoard(int turnNumber)
    {
        List<UnitData> board = new List<UnitData>();
        int budget = Mathf.RoundToInt((3 + (turnNumber * 3.0f)) * difficultyMultiplier);
        int maxTier = Mathf.Clamp((turnNumber / 2) + 1, 1, 6);
        List<UnitData> validUnits = allUnits.Where(u => u.tier <= maxTier).ToList();
        if (validUnits.Count == 0) return board;

        int currentCost = 0;
        int attempts = 0;
        while (currentCost < budget && board.Count < 7 && attempts < 50)
        {
            attempts++;
            UnitData pick = validUnits[Random.Range(0, validUnits.Count)];
            if (currentCost + pick.cost <= budget) { board.Add(pick); currentCost += pick.cost; }
        }
        return board;
    }

    public void SimulateOpponentTurn(LobbyManager.Opponent bot, int turnNumber, int mmr)
    {
        // 1. Income
        int income = Mathf.Min(3 + turnNumber, 10);
        bot.gold = income; 

        // 2. Decide Upgrade
        int upgradeCost = GetUpgradeCost(bot.tavernTier);
        bool shouldUpgrade = false;

        if (mmr > 6000) 
        {
            if (bot.tavernTier < 6 && bot.gold >= upgradeCost) shouldUpgrade = true;
        }
        else
        {
            if (bot.tavernTier < 6 && bot.gold >= upgradeCost + 3) shouldUpgrade = true;
        }

        if (shouldUpgrade)
        {
            bot.gold -= upgradeCost;
            bot.tavernTier++;
        }

        // 3. Shop Phase
        List<UnitData> shopOptions = allUnits.Where(u => u.tier <= bot.tavernTier).ToList();
        int rerolls = 0;
        int maxRerolls = (mmr > 4000) ? 5 : 0; 

        while (bot.gold >= 3)
        {
            if (shopOptions.Count == 0) break;

            List<UnitData> currentShop = new List<UnitData>();
            for(int i=0; i<3; i++) currentShop.Add(shopOptions[Random.Range(0, shopOptions.Count)]);

            // EVALUATE SHOP
            UnitData bestPick = null;
            int bestScore = -1;

            foreach(var unit in currentShop)
            {
                int score = EvaluateUnit(unit, bot.roster, mmr);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestPick = unit;
                }
            }

            // BUY LOGIC
            if (bestPick != null && bot.gold >= bestPick.cost)
            {
                // If board full, check if we should sell
                if (bot.roster.Count >= 7)
                {
                    // Find the unit we would least like to keep
                    // We calculate the score of existing units to find the weak link
                    UnitData worstUnit = null;
                    int lowestScore = 9999;

                    foreach(var unit in bot.roster)
                    {
                        // Use the same evaluation logic
                        int score = EvaluateUnit(unit, bot.roster, mmr);
                        // Penalize existing unit score slightly to bias towards change if equal? 
                        // Or just use raw score.
                        if (score < lowestScore)
                        {
                            lowestScore = score;
                            worstUnit = unit;
                        }
                    }

                    // FIX: Compare SCORES, not Tiers. 
                    // If the shop unit (potentially a Triple worth 50+ pts) is better than the worst unit (worth ~5 pts)
                    if (bestScore > lowestScore + 5) 
                    {
                        bot.roster.Remove(worstUnit);
                        bot.gold += 1; 
                        bot.gold -= bestPick.cost;
                        bot.roster.Add(bestPick);
                        // Debug.Log($"{bot.name} sold {worstUnit.unitName} for {bestPick.unitName} (Score {bestScore} vs {lowestScore})");
                        continue; 
                    }
                }
                else
                {
                    bot.gold -= bestPick.cost;
                    bot.roster.Add(bestPick);
                    continue; 
                }
            }

            if (rerolls < maxRerolls && bot.gold >= 1)
            {
                bot.gold -= 1;
                rerolls++;
            }
            else
            {
                break; 
            }
        }
    }

    int EvaluateUnit(UnitData unit, List<UnitData> roster, int mmr)
    {
        if (mmr < 2000) return Random.Range(0, 10);

        int score = 0;

        // Base Value
        score += unit.tier * 2;

        if (mmr >= 2000)
        {
            int tribeCount = roster.Count(u => u.tribe == unit.tribe);
            if (tribeCount > 0) score += 5 * tribeCount;
        }

        if (mmr >= 6000)
        {
            // Check for copies to form Triples/Pairs
            int copies = roster.Count(u => u.id == unit.id); // Ensure ID matches
            if (copies == 1) score += 15; // Pair
            if (copies == 2) score += 50; // Triple (Huge priority)
        }

        return score;
    }

    int GetUpgradeCost(int currentTier)
    {
        if (currentTier + 1 >= tierCosts.Length) return 99;
        return tierCosts[currentTier + 1];
    }
}