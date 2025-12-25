using UnityEngine;
using System.Collections.Generic;

public class AIManager : MonoBehaviour
{
    public static AIManager instance;

    [Header("AI Settings")]
    [Tooltip("Multiplier for AI Gold. 1.0 = Normal, 1.2 = Harder.")]
    public float difficultyMultiplier = 1.0f;

    [Header("Data Source")]
    [Tooltip("DRAG ALL UNIT FILES HERE (Tier 1-6)")]
    public UnitData[] allUnits;

    void Awake() { instance = this; }

    public List<UnitData> GenerateEnemyBoard(int turnNumber)
    {
        List<UnitData> board = new List<UnitData>();

        // 1. Calculate Economy
        // Logic: Player gains roughly 1 unit worth of value (3g) per turn.
        // Base: 3g. Growth: +3g per turn.
        // Example Turn 1: 3g + 3g = 6g (2 Units)
        // Example Turn 3: 3g + 9g = 12g (4 Units)
        float rawBudget = (3 + (turnNumber * 3.0f)) * difficultyMultiplier;
        int budget = Mathf.RoundToInt(rawBudget);

        // 2. Determine Max Tier (Unlocks every 2 turns)
        int maxTier = Mathf.Clamp((turnNumber / 2) + 1, 1, 6);

        // 3. Filter valid units
        List<UnitData> validUnits = new List<UnitData>();
        foreach (var u in allUnits)
        {
            if (u != null && u.tier <= maxTier) validUnits.Add(u);
        }

        // Safety Check
        if (validUnits.Count == 0)
        {
            Debug.LogError("AIManager: No valid units found! Did you populate the 'All Units' list in the Inspector?");
            return board;
        }

        // 4. Spend Budget
        int currentCost = 0;
        int attempts = 0; // Prevent infinite loops

        while (currentCost < budget && board.Count < 7 && attempts < 50)
        {
            attempts++;

            // Pick a random unit
            UnitData pick = validUnits[Random.Range(0, validUnits.Count)];

            // Check if we can afford it
            if (currentCost + pick.cost <= budget)
            {
                board.Add(pick);
                currentCost += pick.cost;
            }
        }

        Debug.Log($"<color=cyan>AI (Turn {turnNumber})</color> | Budget: {budget} | Spawned: {board.Count} units | Tier Cap: {maxTier}");
        return board;
    }
}