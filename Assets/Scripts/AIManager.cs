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
    
    // Tier costs matching ShopManager
    private int[] tierCosts = new int[] { 0, 0, 5, 7, 8, 9, 10 }; 

    void Awake() { instance = this; }

    // Fallback for procedural gen
    public List<UnitData> GenerateEnemyBoard(int turnNumber)
    {
        // ... (Keep existing procedural logic as fallback if needed, or remove) ...
        // Keeping it for safety:
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

    // NEW: Persistent Simulation
    public void SimulateOpponentTurn(LobbyManager.Opponent bot, int turnNumber)
    {
        // 1. Income
        int income = Mathf.Min(3 + turnNumber, 10);
        bot.gold = income; // Reset gold (or add saved gold if we want banking later)

        // 2. Decide Upgrade
        // Heuristic: If we have enough gold to upgrade AND buy at least 1 unit, do it.
        // Or if we have a full board and plenty gold.
        int upgradeCost = GetUpgradeCost(bot.tavernTier);
        if (bot.tavernTier < 6 && bot.gold >= upgradeCost + 3)
        {
            if (Random.value > 0.3f) // 70% chance to upgrade if affordable
            {
                bot.gold -= upgradeCost;
                bot.tavernTier++;
                // Debug.Log($"{bot.name} upgraded to Tier {bot.tavernTier}");
            }
        }

        // 3. Buy Units
        // Get units available at this tier
        List<UnitData> shopOptions = allUnits.Where(u => u.tier <= bot.tavernTier).ToList();
        
        // AI Rerolls/Buys until out of gold or board full
        int rerolls = 0;
        while (bot.gold >= 3 && rerolls < 5)
        {
            if (shopOptions.Count == 0) break;

            UnitData pick = shopOptions[Random.Range(0, shopOptions.Count)];

            // Logic: Do we buy?
            // If board not full -> Buy
            if (bot.roster.Count < 7)
            {
                bot.gold -= pick.cost;
                bot.roster.Add(pick);
            }
            // If board full -> Check if this unit is better than our worst
            else
            {
                // Simple heuristic: Compare Tiers
                UnitData worstUnit = bot.roster.OrderBy(u => u.tier).First();
                if (pick.tier > worstUnit.tier)
                {
                    // Sell worst (+1g), Buy new (-3g) -> Net -2g
                    if (bot.gold >= 2) 
                    {
                        bot.roster.Remove(worstUnit);
                        bot.gold -= 2; // (3 cost - 1 refund)
                        bot.roster.Add(pick);
                    }
                }
            }
            
            // Simulate "Reroll" cost occasionally if didn't buy
            if (Random.value > 0.7f) 
            {
                bot.gold -= 1;
                rerolls++;
            }
        }
    }

    int GetUpgradeCost(int currentTier)
    {
        if (currentTier + 1 >= tierCosts.Length) return 99;
        return tierCosts[currentTier + 1];
    }
}