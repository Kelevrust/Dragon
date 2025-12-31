using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class PlayerProfile : MonoBehaviour
{
    public static PlayerProfile instance;

    [Header("Player Data")]
    public int mmr = 1000;
    public int lossStreak = 0;
    public List<bool> last15Games = new List<bool>(); // True = Win (Top 4), False = Loss

    [Header("Rank Thresholds")]
    public int[] rankFloors = new int[] { 0, 2000, 4000, 6000, 8000, 10000 };
    
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void RecordMatchResult(int placement)
    {
        // 1 = 1st place, 8 = 8th place
        bool isWin = placement <= 4;
        
        // Update History
        last15Games.Add(isWin);
        if (last15Games.Count > 15) last15Games.RemoveAt(0);

        // Update Streak
        if (isWin) lossStreak = 0;
        else lossStreak++;

        // Calculate MMR Change
        int mmrChange = CalculateMMRChange(placement);
        
        // Apply with Sticky Logic
        int newMMR = mmr + mmrChange;
        
        // Check Sticky Floor
        int currentFloor = GetRankFloor(mmr);
        
        // If we are dropping below a floor...
        if (newMMR < currentFloor)
        {
            bool canDrop = CheckDropCondition();
            if (!canDrop)
            {
                Debug.Log($"<color=green>Rank Protection Active!</color> Saved at {currentFloor}.");
                newMMR = currentFloor; // Prevent drop
            }
            else
            {
                Debug.Log($"<color=red>Rank Protection Failed.</color> Dropping tier.");
            }
        }

        mmr = newMMR;
        Debug.Log($"Match Ended. Place: {placement}. MMR: {mmr} ({mmrChange})");
    }

    // Logic: Can drop if Lost 10 straight OR Won < 3 of last 15
    private bool CheckDropCondition()
    {
        if (lossStreak >= 10) return true;
        
        int recentWins = last15Games.Count(w => w == true);
        if (recentWins < 3) return true;

        return false; // Protected
    }

    private int CalculateMMRChange(int placement)
    {
        // Simple linear map: 1st (+100) ... 8th (-100)
        // 4th = +10, 5th = -10
        int[] deltas = new int[] { 100, 60, 40, 10, -10, -40, -60, -100 };
        return deltas[Mathf.Clamp(placement - 1, 0, 7)];
    }

    private int GetRankFloor(int currentMMR)
    {
        for (int i = rankFloors.Length - 1; i >= 0; i--)
        {
            if (currentMMR >= rankFloors[i]) return rankFloors[i];
        }
        return 0;
    }
}