using UnityEngine;
using TMPro; 
using System.Collections.Generic;
using UnityEngine.UI; 
using System.Text; 

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("Hero Settings")]
    public HeroData activeHero; 
    public Transform playerBoard; 
    public Transform playerHand; 
    public GameObject cardPrefab; 

    [Header("Resources")]
    public int gold = 0;
    public int maxGold = 3;
    public int turnNumber = 1;
    public bool heroPowerUsed = false;
    
    [Header("Player Stats")]
    public int playerHealth = 30;
    public int maxPlayerHealth = 30;
    public bool isUnconscious = false;

    [Header("UI References")]
    public TMP_Text goldText;
    public TMP_Text turnText;
    public TMP_Text healthText; 
    public TMP_Text heroText;   
    
    [Header("Targeting")]
    public bool isTargetingMode = false;
    public AbilityData pendingAbility; 
    public Texture2D targetingCursor;  
    private CursorMode cursorMode = CursorMode.Auto;
    private Vector2 hotSpot = Vector2.zero;

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

    void Update()
    {
        if (isTargetingMode)
        {
            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
            {
                CancelTargeting();
            }
        }
    }

    // --- SMART SUMMONING LOGIC ---
    public bool TrySpawnUnit(UnitData data, AbilitySpawnLocation location, Transform specificTargetParent = null)
    {
        if (data == null || cardPrefab == null) return false;

        Transform targetParent = null;

        // Determine where to put it
        switch (location)
        {
            case AbilitySpawnLocation.BoardOnly:
                if (playerBoard.childCount < 7) targetParent = playerBoard;
                break;

            case AbilitySpawnLocation.HandOnly:
                if (playerHand.childCount < 7) targetParent = playerHand;
                break;

            case AbilitySpawnLocation.BoardThenHand:
                if (playerBoard.childCount < 7) targetParent = playerBoard;
                else if (playerHand.childCount < 7) targetParent = playerHand;
                break;
                
            case AbilitySpawnLocation.ReplaceTarget:
                // Only used if we selected a specific target to destroy
                if (specificTargetParent != null)
                {
                    targetParent = specificTargetParent;
                }
                break;
        }

        // If no valid parent found (e.g. board full), fail
        if (targetParent == null) 
        {
            Debug.Log("Spawn Failed: No Space!");
            return false;
        }

        // Create the Unit
        GameObject newCard = Instantiate(cardPrefab, targetParent);
        CardDisplay display = newCard.GetComponent<CardDisplay>();
        
        if (display != null)
        {
            display.LoadUnit(data);
            display.isPurchased = true; // Tokens are owned
            
            // If spawned directly to board, trigger OnPlay
            if (targetParent == playerBoard)
            {
                if (AbilityManager.instance != null)
                {
                    AbilityManager.instance.TriggerAbilities(AbilityTrigger.OnPlay, display);
                    AbilityManager.instance.RecalculateAuras();
                }
            }
            
            CheckForTriples(data);
            return true;
        }
        return false;
    }

    public void SpawnToken(UnitData data, Transform parent)
    {
        // Wrapper for compatibility with old AbilityManager calls
        // Assumes "Force Spawn" logic
        TrySpawnUnit(data, AbilitySpawnLocation.BoardOnly, parent); // Hacky fallback
    }

    // --- TARGETING SYSTEM ---

    public void StartAbilityTargeting(AbilityData ability)
    {
        if (ability == null) return;

        isTargetingMode = true;
        pendingAbility = ability;
        
        Debug.Log($"<color=cyan>Select a target for {ability.name}...</color>");
        
        if (targetingCursor != null)
        {
            Cursor.SetCursor(targetingCursor, hotSpot, cursorMode);
        }
    }

    public void OnUnitClicked(CardDisplay targetUnit)
    {
        if (!isTargetingMode || pendingAbility == null) return;

        AbilityManager.instance.CastTargetedAbility(pendingAbility, targetUnit);
        
        if (pendingAbility == activeHero.powerAbility)
        {
            heroPowerUsed = true;
            UpdateUI();
        }

        CancelTargeting();
    }

    public void CancelTargeting()
    {
        isTargetingMode = false;
        pendingAbility = null;
        Cursor.SetCursor(null, Vector2.zero, cursorMode); 
    }

    public void OnHeroPowerClick()
    {
        if (currentPhase != GamePhase.Recruit || isUnconscious) return;
        if (heroPowerUsed) 
        {
            Debug.Log("Hero Power already used this turn!");
            return;
        }
        if (activeHero == null || activeHero.powerAbility == null) return;

        if (gold < activeHero.powerCost)
        {
            Debug.Log("Not enough Gold!");
            return;
        }

        // Logic: If target is "Select", start mode. Else cast immediately.
        if (activeHero.powerAbility.targetType == AbilityTarget.SelectTarget)
        {
            StartAbilityTargeting(activeHero.powerAbility);
        }
        else
        {
            if (TrySpendGold(activeHero.powerCost))
            {
                heroPowerUsed = true;
                AbilityManager.instance.CastHeroPower(activeHero.powerAbility);
                UpdateUI();
            }
        }
    }

    // --- EXISTING LOGIC ---

    public void LogGameState(string context)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"<color=orange>=== GAME STATE ({context}) ===</color>");
        
        string heroName = activeHero != null ? activeHero.heroName : "None";
        sb.AppendLine($"Hero: {heroName} | Turn: {turnNumber} | Phase: {currentPhase}");
        sb.AppendLine($"HP: {playerHealth}/{maxPlayerHealth} | Gold: {gold}/{maxGold} | Unconscious: {isUnconscious}");
        
        sb.Append($"Board ({playerBoard.childCount}): ");
        foreach(Transform child in playerBoard)
        {
            CardDisplay cd = child.GetComponent<CardDisplay>();
            if (cd != null) 
            {
                sb.Append($"[{cd.unitData.unitName} {cd.currentAttack}/{cd.currentHealth} ({cd.permanentAttack}/{cd.permanentHealth})] ");
            }
        }
        sb.AppendLine();

        sb.Append($"Hand ({playerHand.childCount}): ");
        foreach(Transform child in playerHand)
        {
            CardDisplay cd = child.GetComponent<CardDisplay>();
            if (cd != null) sb.Append($"[{cd.unitData.unitName}] ");
        }
        sb.AppendLine();

        sb.AppendLine("============================");
        Debug.Log(sb.ToString());
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
        heroPowerUsed = false; 

        if (activeHero != null && 
            activeHero.bonusType == HeroBonusType.ExtraGold && 
            turnNumber == 1)
        {
            gold += activeHero.bonusValue;
        }

        DeselectUnit();
        UpdateUI();
        
        LogGameState("Start Recruit");
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
                AbilityManager.instance.TriggerAbilities(AbilityTrigger.OnPlay, display);

            CheckForTriples(data);
        }
    }

    public bool TryBuyToHand(UnitData data, CardDisplay sourceCard)
    {
        if (!TrySpendGold(data.cost)) return false;

        if (playerHand.childCount >= 7)
        {
            Debug.Log("Hand is full!");
            gold += data.cost; 
            return false;
        }

        sourceCard.transform.SetParent(playerHand);
        sourceCard.isPurchased = true;
        CheckForTriples(data); 
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
            AbilityManager.instance.RecalculateAuras(); 
        }

        CheckForTriples(card.unitData);
        return true;
    }

    public void SelectUnit(CardDisplay unit)
    {
        if (isTargetingMode)
        {
            OnUnitClicked(unit);
            return;
        }

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
        
        if (AbilityManager.instance != null) AbilityManager.instance.RecalculateAuras(); 
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
        List<Transform> searchZones = new List<Transform> { playerBoard, playerHand };

        foreach (Transform zone in searchZones)
        {
            foreach (Transform child in zone)
            {
                CardDisplay card = child.GetComponent<CardDisplay>();
                if (card != null && card.unitData == unitData && !card.isGolden)
                {
                    matches.Add(card);
                }
            }
        }

        if (matches.Count >= 3)
        {
            Debug.Log($"TRIPLE FOUND for {unitData.unitName}! Merging...");
            
            int totalBonusAttack = 0;
            int totalBonusHealth = 0;

            for(int i=0; i<3; i++)
            {
                CardDisplay c = matches[i];
                int atkBonus = c.permanentAttack - c.unitData.baseAttack;
                int hpBonus = c.permanentHealth - c.unitData.baseHealth;
                totalBonusAttack += atkBonus;
                totalBonusHealth += hpBonus;
                Destroy(c.gameObject);
            }

            Transform spawnTarget = playerHand.childCount < 7 ? playerHand : playerBoard;
            GameObject goldenObj = Instantiate(cardPrefab, spawnTarget);
            CardDisplay goldenDisplay = goldenObj.GetComponent<CardDisplay>();
            
            goldenDisplay.LoadUnit(unitData);
            goldenDisplay.isPurchased = true;
            goldenDisplay.MakeGolden(); 

            goldenDisplay.permanentAttack += totalBonusAttack;
            goldenDisplay.permanentHealth += totalBonusHealth;
            
            goldenDisplay.ResetToPermanent();
            goldenDisplay.UpdateVisuals();
            
            if (AbilityManager.instance != null) AbilityManager.instance.RecalculateAuras();
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
        if (heroText != null && activeHero != null) 
        {
            string status = heroPowerUsed ? "(Used)" : $"(2g)";
            heroText.text = $"{activeHero.heroName} Power: {activeHero.powerName} {status}";
        }
    }
}