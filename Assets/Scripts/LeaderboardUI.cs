using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class LeaderboardUI : MonoBehaviour
{
    [Header("Visuals")]
    public GameObject rowPrefab; // Assign a prefab with a Text component
    public Transform contentContainer; // Assign a Vertical Layout Group

    public void UpdateDisplay()
    {
        if (LobbyManager.instance == null) return;

        // Clear existing
        foreach (Transform child in contentContainer)
        {
            Destroy(child.gameObject);
        }

        // Combine Player + Bots for sorting
        List<LeaderboardEntry> allEntries = new List<LeaderboardEntry>();

        // Add Player
        allEntries.Add(new LeaderboardEntry { 
            name = "YOU", 
            health = GameManager.instance.playerHealth, 
            isDead = GameManager.instance.playerHealth <= 0,
            isPlayer = true
        });

        // Add Bots
        foreach (var bot in LobbyManager.instance.opponents)
        {
            allEntries.Add(new LeaderboardEntry { 
                name = bot.name, 
                health = bot.health, 
                isDead = bot.isDead,
                isPlayer = false,
                isCurrentOpponent = (bot == LobbyManager.instance.currentOpponent)
            });
        }

        // Sort: Dead at bottom, then by Health descending
        var sorted = allEntries.OrderBy(x => x.isDead).ThenByDescending(x => x.health).ToList();

        // Render
        foreach (var entry in sorted)
        {
            GameObject row = Instantiate(rowPrefab, contentContainer);
            TMP_Text text = row.GetComponentInChildren<TMP_Text>();
            
            if (text != null)
            {
                string status = entry.isDead ? "(Dead)" : $"{entry.health}";
                text.text = $"{entry.name} : {status}";

                if (entry.isPlayer) text.color = Color.green;
                else if (entry.isDead) text.color = Color.gray;
                else if (entry.isCurrentOpponent) text.color = new Color(1f, 0.5f, 0f); // Orange
                else text.color = Color.white;
            }
        }
    }

    struct LeaderboardEntry
    {
        public string name;
        public int health;
        public bool isDead;
        public bool isPlayer;
        public bool isCurrentOpponent;
    }
}