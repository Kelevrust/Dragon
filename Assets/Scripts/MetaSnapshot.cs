using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Meta-game knowledge pulled from aggregated player data
/// Updated daily via PlayFab TitleData
/// </summary>
[Serializable]
public class MetaSnapshot
{
    public string version = "1.0";
    public DateTime lastUpdated;

    // Leveling Benchmarks (from aggregate data)
    public Dictionary<int, float> avgLevelByTurn;  // Turn -> Avg Tavern Tier
    public Dictionary<int, float> avgGoldByTurn;   // Turn -> Avg Gold Held

    // Unit Performance
    public Dictionary<string, float> unitWinRates; // Unit Name -> Win Rate %
    public Dictionary<string, int> unitPlayRates;  // Unit Name -> Pick Count

    // Synergy Performance
    public Dictionary<string, float> synergyWinRates; // "Dragons" -> Win Rate

    public MetaSnapshot()
    {
        avgLevelByTurn = new Dictionary<int, float>();
        avgGoldByTurn = new Dictionary<int, float>();
        unitWinRates = new Dictionary<string, float>();
        unitPlayRates = new Dictionary<string, int>();
        synergyWinRates = new Dictionary<string, float>();

        InitializeDefaults();
    }

    private void InitializeDefaults()
    {
        // Default benchmarks (will be replaced by real data)
        avgLevelByTurn[3] = 1.0f;
        avgLevelByTurn[5] = 2.0f;
        avgLevelByTurn[7] = 3.0f;
        avgLevelByTurn[9] = 4.0f;
        avgLevelByTurn[11] = 5.0f;

        lastUpdated = DateTime.Now;
    }

    public float GetExpectedLevel(int turn)
    {
        if (avgLevelByTurn.ContainsKey(turn))
            return avgLevelByTurn[turn];

        // Linear interpolation for missing turns
        int closestLower = 0;
        int closestHigher = 999;

        foreach (var kvp in avgLevelByTurn)
        {
            if (kvp.Key <= turn && kvp.Key > closestLower) closestLower = kvp.Key;
            if (kvp.Key >= turn && kvp.Key < closestHigher) closestHigher = kvp.Key;
        }

        if (closestLower == 0) return avgLevelByTurn[closestHigher];
        if (closestHigher == 999) return avgLevelByTurn[closestLower];

        float t = (turn - closestLower) / (float)(closestHigher - closestLower);
        return Mathf.Lerp(avgLevelByTurn[closestLower], avgLevelByTurn[closestHigher], t);
    }

    public float GetUnitWinRate(string unitName)
    {
        return unitWinRates.ContainsKey(unitName) ? unitWinRates[unitName] : 50f;
    }

    public bool IsUnitMeta(string unitName)
    {
        return GetUnitWinRate(unitName) > 52f; // Above average
    }

    public string ToJson()
    {
        return JsonUtility.ToJson(this);
    }

    public static MetaSnapshot FromJson(string json)
    {
        try
        {
            return JsonUtility.FromJson<MetaSnapshot>(json);
        }
        catch
        {
            Debug.LogWarning("Failed to parse MetaSnapshot. Using defaults.");
            return new MetaSnapshot();
        }
    }
}
