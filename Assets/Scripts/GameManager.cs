using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("Hero Settings")]
    public HeroData activeHero; // Drag a hero here to "select" them for now
    public Transform playerBoard; // Needed for Necromancer spawn
    public GameObject cardPrefab; // Needed for Necromancer spawn

    [Header("Resources")]
    public int gold = 0;
    public int maxGold = 3;
    public int turnNumber = 1;

    [Header("Player Stats")]
    public int playerHealth = 30;
    public int maxPlayerHealth = 30;

    [Header("UI References")]
    public TMP_Text goldText;
    public TMP_Text turnText;
    public TMP_Text healthText; // NEW
    public TMP_Text heroText;   // NEW (To show who we are playing)

    public enum GamePhase { Recruit, Combat }
    public GamePhase currentPhase;

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // 1. Apply Hero Passive immediately
        ApplyHeroBonuses();

        // 2. Start the first turn
        StartRecruitPhase();
    }

    void ApplyHeroBonuses()
    {
        if (activeHero == null) return;

        Debug.Log($"Applying Hero Bonus: {activeHero.heroName}");

        // Paladin Bonus: Extra Health
        if (activeHero.bonusType == HeroBonusType.ExtraHealth)
        {
            maxPlayerHealth += activeHero.bonusValue;
            playerHealth = maxPlayerHealth;
        }

        // Necromancer Bonus: Starting Unit
        if (activeHero.bonusType == HeroBonusType.StartingUnit && activeHero.startingUnit != null)
        {
            SpawnUnitOnBoard(activeHero.startingUnit);
        }

        UpdateUI();
    }

    public void StartRecruitPhase()
    {
        currentPhase = GamePhase.Recruit;

        // Base Gold Calculation (3 + Turn, cap at 10)
        maxGold = Mathf.Min(3 + turnNumber, 10);
        gold = maxGold;

        // Ranger Bonus: Extra Gold on Turn 1
        if (activeHero != null &&
            activeHero.bonusType == HeroBonusType.ExtraGold &&
            turnNumber == 1)
        {
            gold += activeHero.bonusValue;
        }

        UpdateUI();
    }

    public bool TrySpendGold(int amount)
    {
        if (gold >= amount)
        {
            gold -= amount;
            UpdateUI();
            return true;
        }
        return false;
    }

    // Helper to spawn units (used by Necromancer bonus and Buying)
    public void SpawnUnitOnBoard(UnitData data)
    {
        if (playerBoard == null || cardPrefab == null) return;

        GameObject newCard = Instantiate(cardPrefab, playerBoard);
        CardDisplay display = newCard.GetComponent<CardDisplay>();
        if (display != null)
        {
            display.LoadUnit(data);
            display.isPurchased = true; // It's owned, so we can't buy it again
        }
    }

    public void ModifyHealth(int amount)
    {
        playerHealth += amount;
        if (playerHealth > maxPlayerHealth) playerHealth = maxPlayerHealth;

        // Death Logic will go here later (Death Saves)
        if (playerHealth <= 0)
        {
            Debug.Log("Player is Down!");
        }

        UpdateUI();
    }

    void UpdateUI()
    {
        if (goldText != null) goldText.text = $"Gold: {gold}/{maxGold}";
        if (turnText != null) turnText.text = $"Turn: {turnNumber}";
        if (healthText != null) healthText.text = $"HP: {playerHealth}/{maxPlayerHealth}";
        if (heroText != null && activeHero != null) heroText.text = activeHero.heroName;
    }
}