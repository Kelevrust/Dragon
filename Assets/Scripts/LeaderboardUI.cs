using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class LeaderboardUI : MonoBehaviour
{
    [Header("Visuals")]
    public LeaderboardRow rowPrefab; // CHANGED: Now expects the script component
    public Transform contentContainer; 

    public void UpdateDisplay()
    {
        if (LobbyManager.instance == null) return;

        // Clear existing
        foreach (Transform child in contentContainer)
        {
            Destroy(child.gameObject);
        }

        List<LeaderboardEntry> allEntries = new List<LeaderboardEntry>();

        // Add Player
        allEntries.Add(new LeaderboardEntry { 
            name = "YOU", 
            health = GameManager.instance.playerHealth, 
            maxHealth = GameManager.instance.maxPlayerHealth,
            isDead = GameManager.instance.playerHealth <= 0,
            isPlayer = true,
            portrait = GameManager.instance.activeHero != null ? GameManager.instance.activeHero.heroPortrait : null
        });

        // Add Bots
        foreach (var bot in LobbyManager.instance.opponents)
        {
            allEntries.Add(new LeaderboardEntry { 
                name = bot.name, 
                health = bot.health, 
                maxHealth = 30, // Bots default max
                isDead = bot.isDead,
                isPlayer = false,
                isCurrentOpponent = (bot == LobbyManager.instance.currentOpponent),
                portrait = bot.heroPortrait
            });
        }

        // Sort: Dead at bottom, then by Health descending
        var sorted = allEntries.OrderBy(x => x.isDead).ThenByDescending(x => x.health).ToList();

        // Render
        foreach (var entry in sorted)
        {
            LeaderboardRow row = Instantiate(rowPrefab, contentContainer);
            
            row.Setup(
                entry.name, 
                entry.health, 
                entry.maxHealth, 
                entry.isDead, 
                entry.isPlayer, 
                entry.isCurrentOpponent,
                entry.portrait
            );
        }
    }

    struct LeaderboardEntry
    {
        public string name;
        public int health;
        public int maxHealth;
        public bool isDead;
        public bool isPlayer;
        public bool isCurrentOpponent;
        public Sprite portrait;
    }
}