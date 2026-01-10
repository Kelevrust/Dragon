using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class LeaderboardUI : MonoBehaviour
{
    [Header("Visuals")]
    public LeaderboardRow rowPrefab; 
    public Transform contentContainer; 

    public void UpdateDisplay()
    {
        if (LobbyManager.instance == null) return;

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
            portrait = GameManager.instance.activeHero != null ? GameManager.instance.activeHero.heroPortrait : null,
            tooltip = "You are here."
        });

        // Add Bots
        foreach (var bot in LobbyManager.instance.opponents)
        {
            string tribeInfo = GetPrimaryTribeInfo(bot);

            allEntries.Add(new LeaderboardEntry { 
                name = bot.name, 
                health = bot.health, 
                maxHealth = 30, 
                isDead = bot.isDead,
                isPlayer = false,
                isCurrentOpponent = (bot == LobbyManager.instance.currentOpponent),
                portrait = bot.heroPortrait,
                tooltip = $"{bot.heroName}\nRank: {bot.rank}\n{tribeInfo}"
            });
        }

        var sorted = allEntries.OrderBy(x => x.isDead).ThenByDescending(x => x.health).ToList();

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
                entry.portrait,
                entry.tooltip // Pass the generated string
            );
        }
    }

    string GetPrimaryTribeInfo(LobbyManager.Opponent bot)
    {
        if (bot.roster == null || bot.roster.Count == 0) return "Empty Board";

        // Group units by Tribe, filter out None, sort by count descending
        var tribeGroup = bot.roster
            .Where(u => u.template != null && u.template.tribe != Tribe.None)
            .GroupBy(u => u.template.tribe)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault();

        if (tribeGroup != null)
        {
            return $"Build: {tribeGroup.Key} ({tribeGroup.Count()})";
        }

        return "Build: Mixed";
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
        public string tooltip;
    }
}