using UnityEngine;
using TMPro; 
using System.Collections.Generic;
using UnityEngine.UI; 
using System.Text; 
using UnityEngine.SceneManagement; 

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
    
    [Header("Economy Rules")]
    [Tooltip("If true, gold is not reset to MaxGold at start of turn.")]
    public bool enableGoldCarryover = false; 
    [Tooltip("If true, gain 1 gold per 10 held (max 5) at start of turn.")]
    public bool enableInterest = false;
    public int interestCap = 5;
    public int baseIncome = 5; // Used only if carryover is enabled
    public int bankBalance = 0; // Stored gold separate from active hand
    
    [Header("Phase Settings")]
    public float recruitTime = 60f;
    private float currentPhaseTimer;
    
    [Header("Player Stats")]
    public int playerHealth = 30;
    public int maxPlayerHealth = 30;
    public bool isUnconscious = false;

    [Header("UI References")]
    public TMP_Text goldText;
    public TMP_Text turnText;
    public TMP_Text healthText; 
    public TMP_Text heroText;   
    public TMP_Text tierText;
    public TMP_Text endTurnButtonText; 
    
    [Header("Managers")]
    public CombatManager combatManager;

    [Header("Targeting")]
    public bool isTargetingMode = false;
    public AbilityData pendingAbility; 
    public Texture2D targetingCursor;  
    private CursorMode cursorMode = CursorMode.Auto;
    private Vector2 hotSpot = Vector2.zero;

    [Header("Selection")]
    public CardDisplay selectedUnit; 
    public GameObject sellButton; 

    [Header("In-Game Menu")]
    public GameObject pauseMenuPanel; 
    private bool isPaused = false;

    public enum GamePhase { Recruit, Combat, Death } 
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
        
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        
        // --- LOGGING UPDATE ---
        string playerType = "Human";
        string logDetails = "";

        GameTester tester = FindFirstObjectByType<GameTester>();
        if (tester != null && tester.isAutoPlaying)
        {
            playerType = "AI";
            string dist = tester.distributeMMR ? $"opponents distribution @{tester.mmrVariance}" : "Fixed Distribution";
            logDetails = $"MMR {tester.targetMMR} w/ {dist}";
        }
        else if (PlayerProfile.instance != null)
        {
            // Human Player
            logDetails = $"MMR {PlayerProfile.instance.mmr}";
        }

        // Exact format requested: "Game start Player - AI - MMR 4591 w/ oppenents distribution @500"
        string fullLog = $"Player - {playerType} - {logDetails}";
        Debug.Log($"<color=magenta>[SESSION START]</color> {fullLog}");

        if (AnalyticsManager.instance != null && activeHero != null)
        {
            AnalyticsManager.instance.TrackGameStart($"{activeHero.heroName} | {fullLog}");
        }

        if (combatManager == null) combatManager = FindFirstObjectByType<CombatManager>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isTargetingMode) CancelTargeting();
            else TogglePauseMenu();
        }

        if (isTargetingMode && Input.GetMouseButtonDown(1))
        {
            CancelTargeting();
        }

        if (currentPhase == GamePhase.Recruit && !isUnconscious)
        {
            currentPhaseTimer -= Time.deltaTime;
            
            if (endTurnButtonText != null)
            {
                endTurnButtonText.text = $"Recruit - {Mathf.CeilToInt(currentPhaseTimer)}s";
            }

            if (currentPhaseTimer <= 0)
            {
                currentPhaseTimer = 0;
                if (combatManager != null) combatManager.StartCombat();
            }
        }
        else if (currentPhase == GamePhase.Combat)
        {
             if (endTurnButtonText != null) endTurnButtonText.text = "Combat...";
        }
    }

    // --- PAUSE MENU FUNCTIONS ---

    public void TogglePauseMenu()
    {
        isPaused = !isPaused;
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(isPaused);
    }

    public void SaveAndQuitToMenu()
    {
        Time.timeScale = 1f; 
        SceneManager.LoadScene(0); 
    }

    public void ConcedeGame()
    {
        TogglePauseMenu(); 
        playerHealth = 0;
        isUnconscious = true;
        currentPhase = GamePhase.Death; 
        UpdateUI();

        if (DeathSaveManager.instance != null)
        {
            DeathSaveManager.instance.SuccumbToDeath(); 
        }
    }

    public void QuitToDesktop()
    {
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }

    // --- LOGGING ---
    public void LogAction(string action)
    {
        Debug.Log($"<color=white>[ACTION]</color> {action}");
    }

    public void LogGameState(string context)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"<color=orange>=== GAME STATE ({context}) ===</color>");
        
        string heroName = activeHero != null ? activeHero.heroName : "None";
        sb.AppendLine($"Hero: {heroName} | Turn: {turnNumber} | Phase: {currentPhase}");
        sb.AppendLine($"HP: {playerHealth}/{maxPlayerHealth} | Gold: {gold}/{maxGold} | Unconscious: {isUnconscious}");
        
        sb.Append($"Board ({playerBoard.childCount}): ");
        int i = 0;
        foreach(Transform child in playerBoard)
        {
            CardDisplay cd = child.GetComponent<CardDisplay>();
            if (cd != null) 
            {
                sb.Append($"[{i}:{cd.unitData.unitName} {cd.currentAttack}/{cd.currentHealth} ({cd.permanentAttack}/{cd.permanentHealth})] ");
            }
            i++;
        }
        sb.AppendLine();

        sb.Append($"Hand ({playerHand.childCount}): ");
        i = 0;
        foreach(Transform child in playerHand)
        {
            CardDisplay cd = child.GetComponent<CardDisplay>();
            if (cd != null) sb.Append($"[{i}:{cd.unitData.unitName}] ");
            i++;
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

        // Note: New Economy traits (Interest/Carryover) would be checked here in a full HeroData impl
        UpdateUI();
    }

    public void StartRecruitPhase()
    {
        if (playerHealth <= 0 && !isUnconscious) 
        {
            currentPhase = GamePhase.Death;
            Debug.Log("Cannot start Recruit Phase - Player is Dying/Dead");
            return;
        }

        currentPhase = GamePhase.Recruit;
        currentPhaseTimer = recruitTime;

        // --- ECONOMY CALCULATION ---
        int standardTurnCap = Mathf.Min(3 + turnNumber, 10);
        maxGold = standardTurnCap; // Used for UI reference

        if (enableGoldCarryover)
        {
            // Accumulation Mode (PvE / Specific Heroes)
            int interest = 0;
            if (enableInterest)
            {
                interest = Mathf.Min(gold / 10, interestCap);
                if (interest > 0) LogAction($"Gained {interest} Gold from Interest.");
            }
            
            gold += baseIncome + interest;
        }
        else
        {
            // Reset Mode (Standard PvP)
            gold = maxGold;
        }

        // Hero Specific Bonuses
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
        if (currentPhase != GamePhase.Recruit) return false; 

        if (gold >= amount)
        {
            gold -= amount;
            UpdateUI();
            return true;
        }
        return false;
    }

    // --- BANKING API (For Future Traits) ---
    public bool DepositToBank(int amount)
    {
        if (gold >= amount)
        {
            gold -= amount;
            bankBalance += amount;
            LogAction($"Deposited {amount} gold. Bank Balance: {bankBalance}");
            UpdateUI();
            return true;
        }
        return false;
    }

    public void WithdrawFromBank(int amount)
    {
        if (bankBalance >= amount)
        {
            bankBalance -= amount;
            gold += amount;
            LogAction($"Withdrew {amount} gold. Bank Balance: {bankBalance}");
            UpdateUI();
        }
    }

    // --- BOARD SPAWNING LOGIC ---
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

    public void SpawnToken(UnitData data, Transform parent)
    {
        TrySpawnUnit(data, AbilitySpawnLocation.BoardOnly, parent); 
    }

    public bool TrySpawnUnit(UnitData data, AbilitySpawnLocation location, Transform specificTargetParent = null)
    {
        if (data == null || cardPrefab == null) return false;

        Transform targetParent = null;

        switch (location)
        {
            case AbilitySpawnLocation.BoardOnly:
                if (specificTargetParent != null)
                {
                    if (specificTargetParent.childCount < 7) 
                        targetParent = specificTargetParent;
                }
                else
                {
                    if (playerBoard.childCount < 7) 
                        targetParent = playerBoard;
                }
                break;

            case AbilitySpawnLocation.HandOnly:
                if (playerHand.childCount < 7) targetParent = playerHand;
                break;

            case AbilitySpawnLocation.BoardThenHand:
                if (playerBoard.childCount < 7) targetParent = playerBoard;
                else if (playerHand.childCount < 7) targetParent = playerHand;
                break;
                
            case AbilitySpawnLocation.ReplaceTarget:
                if (specificTargetParent != null)
                {
                    targetParent = specificTargetParent;
                }
                break;
        }

        if (targetParent == null) 
        {
            Debug.Log("Spawn Failed: No Space!");
            return false;
        }

        GameObject newCard = Instantiate(cardPrefab, targetParent);
        CardDisplay display = newCard.GetComponent<CardDisplay>();
        
        if (display != null)
        {
            display.LoadUnit(data);
            display.isPurchased = true; 
            
            if (targetParent == playerBoard)
            {
                if (currentPhase == GamePhase.Recruit)
                {
                    if (AbilityManager.instance != null)
                    {
                        AbilityManager.instance.TriggerAbilities(AbilityTrigger.OnPlay, display);
                    }
                }
                
                if (AbilityManager.instance != null) AbilityManager.instance.RecalculateAuras();
            }
            else if (targetParent != playerHand)
            {
                 if (AbilityManager.instance != null) AbilityManager.instance.RecalculateAuras();
            }
            
            if (currentPhase == GamePhase.Recruit)
                CheckForTriples(data);
            
            return true;
        }
        return false;
    }

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
        LogAction($"Bought {data.unitName} to Hand");
        
        if (AnalyticsManager.instance != null)
            AnalyticsManager.instance.TrackPurchase(data.unitName, data.cost);

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
        LogAction($"Played {card.unitData.unitName} to Board");
        
        if (AbilityManager.instance != null)
        {
            AbilityManager.instance.TriggerAbilities(AbilityTrigger.OnPlay, card);
            // Trigger Ally Play Abilities
            AbilityManager.instance.TriggerAllyPlayAbilities(card, playerBoard);
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

        LogAction($"Sold {unitToSell.unitData.unitName}");
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
            LogAction($"TRIPLE FOUND for {unitData.unitName}! Merging...");
            
            int totalBonusAttack = 0;
            int totalBonusHealth = 0;

            for(int i=0; i<3; i++)
            {
                CardDisplay c = matches[i];
                int atkBonus = Mathf.Max(0, c.permanentAttack - c.unitData.baseAttack);
                int hpBonus = Mathf.Max(0, c.permanentHealth - c.unitData.baseHealth);
                
                Debug.Log($"Consuming {c.unitData.unitName}: Perm({c.permanentAttack}/{c.permanentHealth}) - Base({c.unitData.baseAttack}/{c.unitData.baseHealth}) = Bonus({atkBonus}/{hpBonus})");

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
            
            Debug.Log($"Created Golden {unitData.unitName} with {goldenDisplay.permanentAttack}/{goldenDisplay.permanentHealth} (Base*2 + {totalBonusAttack}/{totalBonusHealth})");

            goldenDisplay.ResetToPermanent();
            goldenDisplay.UpdateVisuals();
            
            if (AbilityManager.instance != null) AbilityManager.instance.RecalculateAuras();
        }
    }

    public void ModifyHealth(int amount)
    {
        playerHealth += amount;
        if (playerHealth > maxPlayerHealth) playerHealth = maxPlayerHealth;
        
        if (playerHealth <= 0 && amount < 0)
        {
            if (AnalyticsManager.instance != null) 
                AnalyticsManager.instance.TrackDeath(turnNumber);
        }

        UpdateUI();
    }

    public void UpdateUI()
    {
        if (goldText != null)
        {
            // If in Carryover mode, show total gold. Otherwise show X/Max
            if (enableGoldCarryover)
            {
                string bankText = bankBalance > 0 ? $" (+{bankBalance} Bank)" : "";
                goldText.text = $"Gold: {gold}{bankText}";
            }
            else
            {
                goldText.text = $"Gold: {gold}/{maxGold}";
            }
        }

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

        if (tierText != null)
        {
             ShopManager shop = FindFirstObjectByType<ShopManager>();
             if (shop != null) tierText.text = $"Tier: {shop.tavernTier}";
        }
    }
}