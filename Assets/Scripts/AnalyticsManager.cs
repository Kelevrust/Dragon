using UnityEngine;
using System.Collections.Generic;
using System.Text;

public class AnalyticsManager : MonoBehaviour
{
    public static AnalyticsManager instance;

    // A session ID helps us group events from a single play session
    private string sessionID;

    void Awake()
    {
        if (instance == null) 
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // Keep alive across scenes
            sessionID = System.Guid.NewGuid().ToString().Substring(0, 8); // Short unique ID
        }
        else 
        {
            Destroy(gameObject);
        }
    }

    public void TrackGameStart(string heroName)
    {
        // EVENT: game_start
        LogEvent($"GAME START | Session: {sessionID} | Hero: {heroName}");
    }

    public void TrackRoundResult(int turnNumber, bool won, int damageTaken, int remainingHp)
    {
        // EVENT: round_end
        string result = won ? "WIN" : "LOSS";
        LogEvent($"ROUND END | Turn: {turnNumber} | Result: {result} | Dmg: {damageTaken} | HP: {remainingHp}");
    }

    public void TrackPurchase(string unitName, int cost)
    {
        // EVENT: item_purchased
        LogEvent($"PURCHASE | Turn: {GameManager.instance.turnNumber} | Unit: {unitName} | Cost: {cost}");
    }

    public void TrackReroll(int tier)
    {
        // EVENT: shop_reroll
        LogEvent($"REROLL | Turn: {GameManager.instance.turnNumber} | Tier: {tier}");
    }

    public void TrackDeath(int finalTurn)
    {
        // EVENT: game_over
        LogEvent($"GAME OVER | Session: {sessionID} | Survived: {finalTurn} Turns");
    }

    // Internal Logger - Replace this with Unity Analytics / Firebase later
    private void LogEvent(string eventData)
    {
        Debug.Log($"<color=magenta>[ANALYTICS]</color> {eventData}");
        
        // Optional: Append to a local text file for testers to email you?
        // System.IO.File.AppendAllText(Application.persistentDataPath + "/playtest_log.txt", eventData + "\n");
    }
}