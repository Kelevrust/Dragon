using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager instance;

    [System.Serializable]
    public struct SavedAIUnit
    {
        public UnitData template;
        public bool isGolden;
        public int permAttack;
        public int permHealth;
    }

    [System.Serializable]
    public class Opponent
    {
        public string id;
        public string name;
        public string heroName;
        public int health;
        public bool isDead;
        public bool isGhost; // Dead but still fighting as a zombie/ghost
        public int lastDamageTaken;

        // MMR System
        public int mmr;
        public string rank;

        public Sprite heroPortrait;

        // Economy & State
        public int gold;
        public int tavernTier;
        public List<SavedAIUnit> roster = new List<SavedAIUnit>(); // FIX: Now stores buffed stats
        public List<UnitData> hand = new List<UnitData>(); // AI now has a hand
    }

    [Header("Configuration")]
    public int totalPlayers = 8;
    public int startingHealth = 30; 
    public int playerStartingMMR = 3000; 

    [Header("Visual Assets")]
    public Sprite[] botPortraits; 

    [Header("State")]
    public List<Opponent> opponents = new List<Opponent>();
    public Opponent currentOpponent; 

    // --- POOL TRACKING (Soft Pool Logic) ---
    // Key: UnitID, Value: Count held by AIs
    public Dictionary<string, float> aiPoolUsage = new Dictionary<string, float>();

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
        aiPoolUsage.Clear();

        List<string> names = botNames.OrderBy(x => Random.value).Take(totalPlayers - 1).ToList();
        
        foreach(string name in names)
        {
            Opponent bot = new Opponent();
            bot.id = System.Guid.NewGuid().ToString();
            bot.name = name;
            bot.heroName = "Unknown"; 
            bot.health = startingHealth;
            bot.isDead = false;
            bot.isGhost = false;
            
            bot.mmr = Mathf.Max(0, playerStartingMMR + Random.Range(-500, 500));
            bot.rank = GetRankFromMMR(bot.mmr);

            if (botPortraits != null && botPortraits.Length > 0)
            {
                bot.heroPortrait = botPortraits[Random.Range(0, botPortraits.Length)];
            }
            
            bot.gold = 3;
            bot.tavernTier = 1;
            
            // Initial AI Setup
            if (AIManager.instance != null) AIManager.instance.SimulateBotTurn(bot, 0);

            opponents.Add(bot);
        }
        
        Debug.Log($"Lobby Initialized with {opponents.Count} active bots.");
        UpdateLeaderboardUI();
    }

    string GetRankFromMMR(int mmr)
    {
        if (mmr < 2000) return "Iron";
        if (mmr < 4000) return "Bronze";
        if (mmr < 6000) return "Silver";
        if (mmr < 8000) return "Gold";
        if (mmr < 10000) return "Platinum";
        return "Diamond";
    }

    public Opponent GetNextOpponent()
    {
        // 1. Prioritize Alive opponents
        List<Opponent> alive = opponents.Where(x => !x.isDead).ToList();
        
        // 2. If odd number of players or low pop, include "Ghosts" (Dead players kept for matchmaking)
        if (alive.Count == 0) return null; // Should imply victory

        // Matchmaking Logic: Random for now, but should ideally pair based on ranking or recent opponents
        currentOpponent = alive[Random.Range(0, alive.Count)];
        
        // If we defeated everyone, maybe we fight a Ghost of the 2nd place player?
        if (alive.Count == 0 && opponents.Count > 0)
        {
             // Fallback to ghosts
             var ghosts = opponents.Where(x => x.isDead).OrderByDescending(x => x.roster.Count).ToList();
             if (ghosts.Count > 0) currentOpponent = ghosts[0];
        }

        return currentOpponent;
    }

    public void SimulateRoundForBots(int turnNumber)
    {
        // 1. Simulate Decisions (Buying/Selling)
        foreach(var bot in opponents)
        {
            // Even Dead bots ("Ghosts") can simulate turns if we want them to scale slightly 
            // so the player doesn't get a "Free Win" late game.
            // But typically Ghosts freeze their state.
            if (bot.isDead && !bot.isGhost) continue; 
            
            if (AIManager.instance != null) AIManager.instance.SimulateBotTurn(bot, turnNumber);
        }

        // 2. Simulate Combat (Bot vs Bot)
        // In a real server, we'd pair them up. For local simulation, we estimate health loss.
        foreach(var bot in opponents)
        {
            if (bot.isDead) continue;
            if (bot == currentOpponent) continue; // Don't simulate the one fighting the player yet
            
            // Simple auto-resolve for background battles
            int power = CalculateBoardPower(bot.roster);
            int avgPower = turnNumber * 3 + 2; 
            
            bool won = power >= avgPower + Random.Range(-5, 5);
            
            if (!won)
            {
                int damage = bot.tavernTier + Random.Range(1, 4);
                bot.health -= damage;
                bot.lastDamageTaken = damage;
                
                if (bot.health <= 0)
                {
                    bot.health = 0;
                    bot.isDead = true;
                    // They become a ghost for matchmaking purposes if needed
                    bot.isGhost = true; 
                }
            }
            else
            {
                bot.lastDamageTaken = 0;
            }
        }

        UpdateLeaderboardUI();
    }
    
    public int CalculateBoardPower(List<SavedAIUnit> board)
    {
        if (board == null) return 0;
        int score = 0;
        foreach(var u in board)
        {
            score += u.template.tier;
            // FIX: Use buffed stats (permAttack/permHealth) instead of base stats
            score += (u.permAttack + u.permHealth) / 2;
        }
        return score;
    }

    public void ReportPlayerVsBotResult(bool playerWon, int damageToLoser)
    {
        if (currentOpponent == null) return;

        if (playerWon)
        {
            currentOpponent.health -= damageToLoser;
            currentOpponent.lastDamageTaken = damageToLoser;
            if (currentOpponent.health <= 0)
            {
                currentOpponent.health = 0;
                currentOpponent.isDead = true;
                currentOpponent.isGhost = true;
            }
        }
        
        UpdateLeaderboardUI();
    }

    // --- POOL API ---
    public void RegisterAIUnitObtained(UnitData unit)
    {
        if (unit == null) return;
        if (!aiPoolUsage.ContainsKey(unit.id)) aiPoolUsage[unit.id] = 0;
        
        // SOFT POOL: AI taking a card only counts as "0.5" or "0.3" of a card against the human pool
        aiPoolUsage[unit.id] += 0.5f; 
    }

    public void RegisterAIUnitReleased(UnitData unit)
    {
        if (unit == null) return;
        if (aiPoolUsage.ContainsKey(unit.id))
        {
            aiPoolUsage[unit.id] = Mathf.Max(0, aiPoolUsage[unit.id] - 0.5f);
        }
    }

    void UpdateLeaderboardUI()
    {
        LeaderboardUI ui = FindFirstObjectByType<LeaderboardUI>();
        if (ui != null) ui.UpdateDisplay();
    }
}