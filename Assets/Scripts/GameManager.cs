using UnityEngine;
using TMPro; // This is required for TMP_Text
using System.Collections.Generic;
using UnityEngine.UI; // Sometimes needed if standard UI elements are referenced

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("Hero Settings")]
    public HeroData activeHero; 
    public Transform playerBoard; 
    public Transform playerHand; // NEW: Critical for CardDisplay
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

    public void SpawnUnitOnBoard(UnitData data)
    {
        if (playerBoard == null || cardPrefab == null) return;

        GameObject newCard = Instantiate(cardPrefab, playerBoard);
        CardDisplay display = newCard.GetComponent<CardDisplay>();
        if (display != null)
        {
            display.LoadUnit(data);
            display.isPurchased = true; 
            
            if (AbilityManager.instance != null)
            {
                AbilityManager.instance.TriggerAbilities(AbilityTrigger.OnPlay, display);
                AbilityManager.instance.RecalculateAuras(); // Recalc on spawn
            }

            CheckForTriples(data);
        }
    }

    public void SpawnToken(UnitData data, Transform parent)
    {
        if (cardPrefab == null || data == null || parent == null) return;

        GameObject newCard = Instantiate(cardPrefab, parent);
        CardDisplay display = newCard.GetComponent<CardDisplay>();
        
        if (display != null)
        {
            display.LoadUnit(data);
            display.isPurchased = true; 
            Destroy(newCard.GetComponent<UnityEngine.UI.Button>()); 
            
            // Recalc auras because a new unit appeared
            if (AbilityManager.instance != null) AbilityManager.instance.RecalculateAuras();
        }
    }

    // --- NEW HAND LOGIC ---

    public bool TryBuyToHand(UnitData data, CardDisplay sourceCard)
    {
        if (!TrySpendGold(data.cost)) return false;

        if (playerHand.childCount >= 7)
        {
            Debug.Log("Hand is full!");
            gold += data.cost; // Refund
            return false;
        }

        sourceCard.transform.SetParent(playerHand);
        sourceCard.isPurchased = true;
        return true;
    }

    public bool TryPlayCardToBoard(CardDisplay card)
    {
        if (playerBoard.childCount >= 7)
        {
            Debug.Log("Board is full!");
            return false;
        }

        card.transform.SetParent(playerBoard);
        
        if (AbilityManager.instance != null)
        {
            AbilityManager.instance.TriggerAbilities(AbilityTrigger.OnPlay, card);
            AbilityManager.instance.RecalculateAuras(); // Recalc on play
        }

        CheckForTriples(card.unitData);
        return true;
    }

    // ----------------------

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

        gold += 1;
        Destroy(unitToSell.gameObject);
        
        if (selectedUnit == unitToSell) DeselectUnit();
        
        if (AbilityManager.instance != null) AbilityManager.instance.RecalculateAuras(); // Recalc on sell
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

        // Only check BOARD for triples, not hand
        foreach(Transform child in playerBoard)
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
            foreach(CardDisplay card in matches) Destroy(card.gameObject);

            GameObject goldenObj = Instantiate(cardPrefab, playerBoard);
            CardDisplay goldenDisplay = goldenObj.GetComponent<CardDisplay>();
            
            goldenDisplay.LoadUnit(unitData);
            goldenDisplay.isPurchased = true;
            goldenDisplay.MakeGolden(); 
            
            if (AbilityManager.instance != null) AbilityManager.instance.RecalculateAuras(); // Recalc on merge
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