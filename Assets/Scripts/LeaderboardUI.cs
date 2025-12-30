using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class LeaderboardUI : MonoBehaviour
{
    [Header("Visuals")]
    public GameObject rowPrefab; 
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
            portrait = GameManager.instance.activeHero != null ? GameManager.instance.activeHero.heroPortrait : null
        });

        // Add Bots
        foreach (var bot in LobbyManager.instance.opponents)
        {
            allEntries.Add(new LeaderboardEntry { 
                name = bot.name, 
                health = bot.health, 
                maxHealth = 30, // Assuming fixed max for bots for now
                isDead = bot.isDead,
                isPlayer = false,
                isCurrentOpponent = (bot == LobbyManager.instance.currentOpponent),
                portrait = bot.heroPortrait
            });
        }

        var sorted = allEntries.OrderBy(x => x.isDead).ThenByDescending(x => x.health).ToList();

        foreach (var entry in sorted)
        {
            GameObject row = Instantiate(rowPrefab, contentContainer);
            
            // 1. Set Text
            TMP_Text text = row.GetComponentInChildren<TMP_Text>();
            if (text != null)
            {
                text.text = entry.name;
                if (entry.isPlayer) text.color = Color.green;
                else if (entry.isDead) text.color = Color.gray;
                else if (entry.isCurrentOpponent) text.color = new Color(1f, 0.5f, 0f); // Orange
                else text.color = Color.white;
            }

            // 2. Set Portrait (Look for Image named "Portrait")
            // Simple way: Find child by name
            Transform portraitTrans = row.transform.Find("Portrait");
            if (portraitTrans != null)
            {
                Image pImg = portraitTrans.GetComponent<Image>();
                if (pImg != null) pImg.sprite = entry.portrait;
            }

            // 3. Set Health Bar (Look for Slider)
            Slider hpBar = row.GetComponentInChildren<Slider>();
            if (hpBar != null)
            {
                hpBar.maxValue = entry.maxHealth;
                hpBar.value = entry.health;
            }
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