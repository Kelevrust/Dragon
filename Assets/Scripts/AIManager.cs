using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class AIManager : MonoBehaviour
{
    public static AIManager instance;

    [Header("AI Settings")]
    [Tooltip("Multiplier for AI Gold/Decisions.")]
    public float difficultyMultiplier = 1.0f; 

    [Header("Data Source")]
    [Tooltip("All Units available in the game.")]
    public UnitData[] allUnits; 
    
    private int[] tierCosts = new int[] { 0, 0, 5, 7, 8, 9, 10 }; 

    void Awake() { instance = this; }

    void Start()
    {
        // FIX: Auto-fill Unit list from ShopManager if empty to prevent AI starvation
        if (allUnits == null || allUnits.Length == 0)
        {
            if (ShopManager.instance != null && ShopManager.instance.availableUnits != null)
            {
                allUnits = ShopManager.instance.availableUnits;
                Debug.Log($"AIManager: Auto-filled {allUnits.Length} units from ShopManager.");
            }
        }
    }

    public void SimulateBotTurn(LobbyManager.Opponent bot, int turnNumber)
    {
        if (bot.isDead && !bot.isGhost) return;

        // 1. Income
        int income = Mathf.Min(3 + turnNumber, 10);
        if (turnNumber == 0) income = 3; // Turn 1
        bot.gold = income; 

        // 2. Upgrade Logic (Simple Heuristic)
        int upgradeCost = GetUpgradeCost(bot.tavernTier);
        // Reduce upgrade cost by 1 per turn logic (simplified here as -1 per turn)
        upgradeCost = Mathf.Max(0, upgradeCost - (turnNumber % 3)); 

        if (bot.tavernTier < 6)
        {
            // If we can afford upgrade and still buy a unit (or it's cheap), do it
            if (bot.gold >= upgradeCost + 3 || (bot.roster.Count >= 5 && bot.gold >= upgradeCost))
            {
                bot.gold -= upgradeCost;
                bot.tavernTier++;
                // Debug.Log($"{bot.name} upgraded to Tier {bot.tavernTier}");
            }
        }

        // 3. Buying Phase (Loop until out of gold)
        int rerolls = 0;
        int maxRerolls = 10;

        while (bot.gold >= 3) // Standard unit cost
        {
            // Generate a virtual shop for the bot
            List<UnitData> shop = GenerateAIShop(bot.tavernTier);
            
            // Pick best unit
            UnitData bestPick = null;
            int bestScore = -1;

            foreach(var unit in shop)
            {
                int score = EvaluateUnit(unit, bot);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestPick = unit;
                }
            }

            // Decision: Buy or Reroll?
            if (bestPick != null && bestScore > 10) // Threshold for "Worth buying"
            {
                // Buy
                if (bot.gold >= bestPick.cost)
                {
                    // Check for Board Space
                    if (bot.roster.Count < 7)
                    {
                        bot.gold -= bestPick.cost;
                        // FIX: Store as SavedAIUnit with base stats (buffs apply during combat simulation)
                        LobbyManager.SavedAIUnit newUnit = new LobbyManager.SavedAIUnit
                        {
                            template = bestPick,
                            isGolden = false,
                            permAttack = bestPick.baseAttack,
                            permHealth = bestPick.baseHealth
                        };
                        bot.roster.Add(newUnit);
                        LobbyManager.instance.RegisterAIUnitObtained(bestPick);
                        CheckTriple(bot, bestPick);
                    }
                    else
                    {
                        // Board full: Check if we should swap (Sell worst, buy best)
                        LobbyManager.SavedAIUnit worst = GetWorstUnit(bot);
                        int worstScore = EvaluateUnit(worst.template, bot);

                        if (bestScore > worstScore + 5) // Improvement threshold
                        {
                            bot.gold -= bestPick.cost;
                            bot.gold += 1; // Sell value

                            bot.roster.Remove(worst);
                            LobbyManager.instance.RegisterAIUnitReleased(worst.template);

                            LobbyManager.SavedAIUnit newUnit = new LobbyManager.SavedAIUnit
                            {
                                template = bestPick,
                                isGolden = false,
                                permAttack = bestPick.baseAttack,
                                permHealth = bestPick.baseHealth
                            };
                            bot.roster.Add(newUnit);
                            LobbyManager.instance.RegisterAIUnitObtained(bestPick);

                            CheckTriple(bot, bestPick);
                        }
                    }
                }
            }
            else
            {
                // Reroll if we have gold and haven't hit limit
                if (bot.gold >= 1 && rerolls < maxRerolls)
                {
                    bot.gold -= 1;
                    rerolls++;
                }
                else
                {
                    break; // Save gold
                }
            }
        }
    }

    // "Soft Pool" Shop Generation
    List<UnitData> GenerateAIShop(int tier)
    {
        List<UnitData> shop = new List<UnitData>();
        int shopSize = 3 + (tier / 2); // 3, 4, 5, 6 slots

        // Get valid units for this tier
        if (allUnits == null || allUnits.Length == 0) return shop;

        List<UnitData> validUnits = allUnits.Where(u => u.tier <= tier).ToList();
        if (validUnits.Count == 0) return shop;

        for (int i = 0; i < shopSize; i++)
        {
            // Simple Random weighted by tier could go here
            // For now, uniform random from valid units
            UnitData candidate = validUnits[Random.Range(0, validUnits.Count)];
            shop.Add(candidate);
        }
        return shop;
    }

    int EvaluateUnit(UnitData unit, LobbyManager.Opponent bot)
    {
        if (unit == null) return 0;
        int score = unit.tier * 5 + unit.baseAttack + unit.baseHealth;

        // Synergy: Bonus if Tribe matches existing majority
        var majorityTribe = bot.roster.GroupBy(u => u.template.tribe)
                                      .OrderByDescending(g => g.Count())
                                      .FirstOrDefault()?.Key;

        if (majorityTribe != null && majorityTribe != Tribe.None && unit.tribe == majorityTribe)
        {
            score += 10;
        }

        // Pair Logic: Huge bonus if we already have one (to make a triple)
        if (bot.roster.Any(u => u.template.id == unit.id))
        {
            score += 20;
        }

        return score;
    }

    LobbyManager.SavedAIUnit GetWorstUnit(LobbyManager.Opponent bot)
    {
        // Simple heuristic: Lowest Tier -> Lowest Stats (using buffed stats)
        return bot.roster.OrderBy(u => u.template.tier)
                         .ThenBy(u => u.permAttack + u.permHealth)
                         .FirstOrDefault();
    }

    void CheckTriple(LobbyManager.Opponent bot, UnitData newItem)
    {
        // Check if we have 3 of newItem
        int count = bot.roster.Count(u => u.template.id == newItem.id);
        if (count >= 3)
        {
            // Merge into a Golden!
            // Remove 3 copies and calculate combined stats
            int totalAttack = 0;
            int totalHealth = 0;

            for(int i=0; i<3; i++)
            {
                LobbyManager.SavedAIUnit toRemove = bot.roster.First(u => u.template.id == newItem.id);
                totalAttack += toRemove.permAttack;
                totalHealth += toRemove.permHealth;
                bot.roster.Remove(toRemove);
                LobbyManager.instance.RegisterAIUnitReleased(toRemove.template);
            }

            // Add Golden version with doubled base stats
            LobbyManager.SavedAIUnit goldenUnit = new LobbyManager.SavedAIUnit
            {
                template = newItem,
                isGolden = true,
                permAttack = newItem.baseAttack * 2,
                permHealth = newItem.baseHealth * 2
            };
            bot.roster.Add(goldenUnit);
            LobbyManager.instance.RegisterAIUnitObtained(newItem);

            // Reward: Discover a higher tier unit (if possible)
            int rewardTier = Mathf.Min(bot.tavernTier + 1, 6);
            UnitData reward = allUnits.FirstOrDefault(u => u.tier == rewardTier);
            if (reward != null)
            {
                LobbyManager.SavedAIUnit rewardUnit = new LobbyManager.SavedAIUnit
                {
                    template = reward,
                    isGolden = false,
                    permAttack = reward.baseAttack,
                    permHealth = reward.baseHealth
                };
                bot.roster.Add(rewardUnit);
                LobbyManager.instance.RegisterAIUnitObtained(reward);
            }
        }
    }

    public List<UnitData> GenerateEnemyBoard(int turnNumber)
    {
        // Fallback for generating random boards if no bot is available
        // Used in old logic, but keeping for compatibility if needed or redirecting to active bot logic
        return new List<UnitData>(); 
    }

    int GetUpgradeCost(int currentTier)
{
    // FIX: Return the cost of the NEXT tier, not the current tier
    int nextTier = currentTier + 1;
    if (nextTier < tierCosts.Length) return tierCosts[nextTier];
    return 99;
}