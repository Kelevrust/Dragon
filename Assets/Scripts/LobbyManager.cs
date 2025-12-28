using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager instance;

    [System.Serializable]
    public class Opponent
    {
        public string id;
        public string name;
        public string heroName; // e.g. "The Woodsman"
        public int health;
        public bool isDead;
        public int lastDamageTaken;
    }

    [Header("Configuration")]
    public int totalPlayers = 8;
    public int startingHealth = 30; // Should match GameManager

    [Header("State")]
    public List<Opponent> opponents = new List<Opponent>();
    public Opponent currentOpponent; // The bot the player is currently fighting

    // Thematic Names for Project Grimm
    private string[] botNames = new string[] {
        "The Woodsman", "Baba Yaga", "Big Bad Wolf", "Cinderella", 
        "Hansel", "Gretel", "Snow White", "Prince Charming", 
        "Rumpelstiltskin", "Red Riding Hood", "The Huntsman", "Geppetto",
        "Jack (Beanstalk)", "Alice", "The Hatter", "Cheshire"
    };

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        InitializeLobby();
    }

    public void InitializeLobby()
    {
        opponents.Clear();
        
        // Randomly pick names
        List<string> names = botNames.OrderBy(x => Random.value).Take(totalPlayers - 1).ToList();
        
        foreach(string name in names)
        {
            Opponent bot = new Opponent();
            bot.id = System.Guid.NewGuid().ToString();
            bot.name = name;
            bot.heroName = "Unknown Hero"; // We can expand this later
            bot.health = startingHealth;
            bot.isDead = false;
            opponents.Add(bot);
        }
        
        Debug.Log($"Lobby Initialized with {opponents.Count} opponents.");
        
        // Initial Sort/UI Update
        UpdateLeaderboardUI();
    }

    public Opponent GetNextOpponent()
    {
        List<Opponent> alive = opponents.Where(x => !x.isDead).ToList();
        
        if (alive.Count == 0) 
        {
            currentOpponent = null; // Everyone else is dead! Player wins lobby?
            return null;
        }
        
        // Simple random matchmaking for MVP
        // In real auto-battlers, you avoid the person you just played
        currentOpponent = alive[Random.Range(0, alive.Count)];
        
        Debug.Log($"Next Opponent: {currentOpponent.name} ({currentOpponent.health} HP)");
        return currentOpponent;
    }

    // Call this when Player's combat phase ends
    public void SimulateRoundForBots(int turnNumber)
    {
        foreach(var bot in opponents)
        {
            if (bot.isDead) continue;
            if (bot == currentOpponent) continue; // Skip the one fighting the player (handled by result)
            
            // 50/50 Win/Loss Simulation for other bots
            bool won = Random.value > 0.5f;
            
            if (!won)
            {
                // Damage scaling: 2 base + turn number roughly
                int damage = Random.Range(2, 3 + turnNumber);
                bot.health -= damage;
                bot.lastDamageTaken = damage;
                
                if (bot.health <= 0)
                {
                    bot.health = 0;
                    bot.isDead = true;
                    Debug.Log($"<color=red>{bot.name} has been eliminated!</color>");
                }
            }
            else
            {
                bot.lastDamageTaken = 0;
            }
        }

        UpdateLeaderboardUI();
    }
    
    // Handle the result of the Player vs Bot match specifically
    public void ReportPlayerVsBotResult(bool playerWon, int damageToLoser)
    {
        if (currentOpponent == null) return;

        // If Player Won, Bot takes damage
        if (playerWon)
        {
            currentOpponent.health -= damageToLoser;
            if (currentOpponent.health <= 0)
            {
                currentOpponent.health = 0;
                currentOpponent.isDead = true;
                Debug.Log($"Player eliminated {currentOpponent.name}!");
            }
        }
        // If Player Lost, Player takes damage (Handled by GameManager), Bot takes 0.
        
        UpdateLeaderboardUI();
    }

    void UpdateLeaderboardUI()
    {
        LeaderboardUI ui = FindFirstObjectByType<LeaderboardUI>();
        if (ui != null) ui.UpdateDisplay();
    }
}