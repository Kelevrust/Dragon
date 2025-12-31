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
        public string heroName; 
        public int health;
        public bool isDead;
        public int lastDamageTaken;
        
        // MMR System
        public int mmr;
        public string rank;

        public Sprite heroPortrait;
        
        public int gold;
        public int tavernTier;
        public List<UnitData> roster; 
    }

    [Header("Configuration")]
    public int totalPlayers = 8;
    public int startingHealth = 30; 
    public int playerStartingMMR = 3000; // You start in Bronze/Silver

    [Header("Visual Assets")]
    public Sprite[] botPortraits; 

    [Header("State")]
    public List<Opponent> opponents = new List<Opponent>();
    public Opponent currentOpponent; 

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
        List<string> names = botNames.OrderBy(x => Random.value).Take(totalPlayers - 1).ToList();
        
        foreach(string name in names)
        {
            Opponent bot = new Opponent();
            bot.id = System.Guid.NewGuid().ToString();
            bot.name = name;
            bot.heroName = "Unknown"; 
            bot.health = startingHealth;
            bot.isDead = false;
            
            // Generate MMR based on Player +/- 1000
            bot.mmr = Mathf.Max(0, playerStartingMMR + Random.Range(-500, 500));
            bot.rank = GetRankFromMMR(bot.mmr);

            if (botPortraits != null && botPortraits.Length > 0)
            {
                bot.heroPortrait = botPortraits[Random.Range(0, botPortraits.Length)];
            }
            
            bot.gold = 3;
            bot.tavernTier = 1;
            bot.roster = new List<UnitData>();
            
            // Pass MMR to AI for initial setup
            if (AIManager.instance != null) AIManager.instance.SimulateOpponentTurn(bot, 0, bot.mmr);

            opponents.Add(bot);
        }
        
        Debug.Log($"Lobby Initialized with {opponents.Count} opponents.");
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
        List<Opponent> alive = opponents.Where(x => !x.isDead).ToList();
        
        if (alive.Count == 0) 
        {
            currentOpponent = null; 
            return null;
        }
        
        // In the future: Matchmake based on closest MMR
        // For MVP: Random alive
        currentOpponent = alive[Random.Range(0, alive.Count)];
        return currentOpponent;
    }

    public void SimulateRoundForBots(int turnNumber)
    {
        foreach(var bot in opponents)
        {
            if (bot.isDead) continue;
            
            // Pass MMR to AI to adjust difficulty logic
            if (AIManager.instance != null) AIManager.instance.SimulateOpponentTurn(bot, turnNumber, bot.mmr);
        }

        foreach(var bot in opponents)
        {
            if (bot.isDead) continue;
            if (bot == currentOpponent) continue; 
            
            // MMR-based win chance?
            int power = bot.roster.Sum(u => u.tier) + bot.roster.Count;
            int avgPower = turnNumber * 2; 
            
            bool won = power >= avgPower + Random.Range(-2, 3); 
            
            if (!won)
            {
                int damage = Random.Range(2, 3 + (turnNumber/2));
                bot.health -= damage;
                bot.lastDamageTaken = damage;
                
                if (bot.health <= 0)
                {
                    bot.health = 0;
                    bot.isDead = true;
                }
            }
            else
            {
                bot.lastDamageTaken = 0;
            }
        }

        UpdateLeaderboardUI();
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
            }
        }
        
        UpdateLeaderboardUI();
    }

    void UpdateLeaderboardUI()
    {
        LeaderboardUI ui = FindFirstObjectByType<LeaderboardUI>();
        if (ui != null) ui.UpdateDisplay();
    }
}