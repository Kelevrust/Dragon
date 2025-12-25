using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("Hero Settings")]
    public HeroData activeHero;
    public Transform playerBoard;
    public GameObject cardPrefab;

    [Header("Resources")]
    public int gold = 0;
    public int maxGold = 3;
    public int turnNumber = 1;

    [Header("Player Stats")]
    public int playerHealth = 30;
    public int maxPlayerHealth = 30;
    public bool isUnconscious = false;

    [Header("UI References")]
    public TMP_Text goldText;
    public TMP_Text turnText;
    public TMP_Text healthText;
    public TMP_Text heroText;

    [Header("Selection")]
    public CardDisplay selectedUnit;
    public GameObject sellButton;

    public enum GamePhase { Recruit, Combat }
    public GamePhase currentPhase;

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        ApplyHeroBonuses();
        StartRecruitPhase();
    }

    void ApplyHeroBonuses()
    {
        if (activeHero == null) return;

        if (activeHero.bonusType == HeroBonusType.ExtraHealth)
        {
            maxPlayerHealth += activeHero.bonusValue;
            playerHealth = maxPlayerHealth;
        }

        if (activeHero.bonusType == HeroBonusType.StartingUnit && activeHero.startingUnit != null)
        {
            SpawnUnitOnBoard(activeHero.startingUnit);
        }

        UpdateUI();
    }

    public void StartRecruitPhase()
    {
        currentPhase = GamePhase.Recruit;
        maxGold = Mathf.Min(3 + turnNumber, 10);
        gold = maxGold;

        if (activeHero != null &&
            activeHero.bonusType == HeroBonusType.ExtraGold &&
            turnNumber == 1)
        {
            gold += activeHero.bonusValue;
        }

        DeselectUnit();
        UpdateUI();
    }

    public bool TrySpendGold(int amount)
    {
        if (isUnconscious) return false;

        if (gold >= amount)
        {
            gold -= amount;
            UpdateUI();
            return true;
        }
        return false;
    }

    // Used for Buying / Starting Unit (Uses PlayerBoard)
    public void SpawnUnitOnBoard(UnitData data)
    {
        if (playerBoard == null || cardPrefab == null || data == null) return;

        GameObject newCard = Instantiate(cardPrefab, playerBoard);
        CardDisplay display = newCard.GetComponent<CardDisplay>();
        if (display != null)
        {
            display.LoadUnit(data);
            display.isPurchased = true;
            CheckForTriples(data);
        }
    }

    // NEW: Used for Abilities/Summons (Specific Parent, No Triple Check)
    public void SpawnToken(UnitData data, Transform parent)
    {
        if (cardPrefab == null || data == null || parent == null) return;

        GameObject newCard = Instantiate(cardPrefab, parent);
        CardDisplay display = newCard.GetComponent<CardDisplay>();

        if (display != null)
        {
            display.LoadUnit(data);
            display.isPurchased = true; // Tokens are owned

            // Disable interaction components so tokens can't be clicked/dragged in combat
            Destroy(newCard.GetComponent<UnityEngine.UI.Button>());
        }
    }

    public void SelectUnit(CardDisplay unit)
    {
        if (currentPhase != GamePhase.Recruit || isUnconscious) return;
        selectedUnit = unit;
        Debug.Log($"Selected {unit.unitData.unitName}");
    }

    public void DeselectUnit()
    {
        selectedUnit = null;
    }

    public void SellUnit(CardDisplay unitToSell)
    {
        if (unitToSell == null || currentPhase != GamePhase.Recruit) return;

        Debug.Log($"Selling {unitToSell.unitData.unitName}");
        gold += 1;
        Destroy(unitToSell.gameObject);

        if (selectedUnit == unitToSell) DeselectUnit();
        UpdateUI();
    }

    public void SellSelectedUnit()
    {
        SellUnit(selectedUnit);
    }

    public void CheckForTriples(UnitData unitData)
    {
        if (currentPhase != GamePhase.Recruit) return;

        List<CardDisplay> matches = new List<CardDisplay>();

        foreach (Transform child in playerBoard)
        {
            CardDisplay card = child.GetComponent<CardDisplay>();
            if (card != null && card.unitData == unitData && !card.isGolden)
            {
                matches.Add(card);
            }
        }

        if (matches.Count >= 3)
        {
            Debug.Log("TRIPLE FOUND! Merging...");
            foreach (CardDisplay card in matches) Destroy(card.gameObject);

            GameObject goldenObj = Instantiate(cardPrefab, playerBoard);
            CardDisplay goldenDisplay = goldenObj.GetComponent<CardDisplay>();

            goldenDisplay.LoadUnit(unitData);
            goldenDisplay.isPurchased = true;
            goldenDisplay.MakeGolden();
        }
    }

    public void ModifyHealth(int amount)
    {
        playerHealth += amount;
        if (playerHealth > maxPlayerHealth) playerHealth = maxPlayerHealth;
        UpdateUI();
    }

    public void UpdateUI()
    {
        if (goldText != null) goldText.text = $"Gold: {gold}/{maxGold}";
        if (turnText != null) turnText.text = $"Turn: {turnNumber}";
        if (healthText != null)
        {
            healthText.text = $"HP: {playerHealth}/{maxPlayerHealth}";
            if (isUnconscious) healthText.text += " (DOWN)";
        }
        if (heroText != null && activeHero != null) heroText.text = activeHero.heroName;
    }
}