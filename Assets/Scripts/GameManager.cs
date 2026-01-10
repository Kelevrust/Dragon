using UnityEngine;
using TMPro; 
using System.Collections.Generic;
using UnityEngine.UI; 
using System.Text; 
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem; 
using System.Linq;

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
    public int baseIncome = 5; 
    public int bankBalance = 0; 
    
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
    
    [Header("Bank UI References")]
    public GameObject bankPanel; 
    public TMP_Text bankBalanceText;
    
    [Header("Managers")]
    public CombatManager combatManager;

    [Header("Targeting")]
    public bool isTargetingMode = false;
    public AbilityData pendingAbility;
    public CardDisplay pendingCardSource; 
    public Texture2D targetingCursor;  
    private CursorMode cursorMode = CursorMode.Auto;
    private Vector2 hotSpot = Vector2.zero;

    [Header("Selection")]
    public CardDisplay selectedUnit; 
    public GameObject sellButton; 
    public GameObject sellZone; 

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
        if (bankPanel != null) bankPanel.SetActive(enableGoldCarryover);
        if (sellZone != null) sellZone.SetActive(false);

        string playerType = "Human";
        string logDetails = "";

        GameTester tester = FindFirstObjectByType<GameTester>();
        if (tester != null && tester.isAutoPlaying)
        {
            playerType = "AI";
            string dist = tester.distributeMMR ? $"opponents distribution @{tester.mmrVariance}" : "Fixed Distribution";
            logDetails = $"MMR {tester.targetMMR} w/ {dist}";
        }
        
        // Removed PlayerProfile check to avoid compilation errors if missing
        // else if (PlayerProfile.instance != null) { logDetails = $"MMR {PlayerProfile.instance.mmr}"; }

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
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (isTargetingMode) CancelTargeting();
            else TogglePauseMenu();
        }

        if (isTargetingMode && Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
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
                CleanUpFloatingCards();
                if (combatManager != null) combatManager.StartCombat();
            }
        }
        else if (currentPhase == GamePhase.Combat)
        {
             if (endTurnButtonText != null) endTurnButtonText.text = "Combat...";
        }
    }

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

        int standardTurnCap = Mathf.Min(3 + turnNumber, 10);
        maxGold = standardTurnCap; 

        if (enableGoldCarryover)
        {
            int interest = 0;
            if (enableInterest)
            {
                int totalWealth = gold + bankBalance;
                interest = Mathf.Min(totalWealth / 10, interestCap);
                if (interest > 0) LogAction($"Gained {interest} Gold from Interest (Total Wealth: {totalWealth}).");
            }
            gold += baseIncome + interest;
        }
        else
        {
            gold = maxGold;
        }

        if (activeHero != null && 
            activeHero.bonusType == HeroBonusType.ExtraGold && 
            turnNumber == 1)
        {
            gold += activeHero.bonusValue;
        }

        if (AbilityManager.instance != null)
        {
            AbilityManager.instance.TriggerTurnStartAbilities();
            AbilityManager.instance.RecalculateAuras();
        }
        
        // FIX: Removed duplicate ReduceUpgradeCost() call. 
        // HandleTurnStartRefresh() handles this logic internally for ShopManager.
        ShopManager shop = FindFirstObjectByType<ShopManager>();
        if (shop != null) 
        {
            shop.HandleTurnStartRefresh(); 
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

    public void ToggleBankingMode(bool active)
    {
        enableGoldCarryover = active;
        if (bankPanel != null) bankPanel.SetActive(active);
        UpdateUI();
    }

    public void DepositToBank(int amount)
    {
        if (gold >= amount)
        {
            gold -= amount;
            bankBalance += amount;
            LogAction($"Deposited {amount} gold. Bank Balance: {bankBalance}");
            UpdateUI();
        }
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
    
    public void ToggleSellZone(bool active)
    {
        if (sellZone != null)
        {
            sellZone.SetActive(active);
        }
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

    public void SpawnToken(UnitData data, Transform parent)
    {
        TrySpawnUnit(data, AbilitySpawnLocation.BoardOnly, parent); 
    }

    public bool TryPlaySpell(CardDisplay spellCard, CardDisplay target)
    {
        if (spellCard == null) return false;

        if (!TrySpendGold(spellCard.unitData.cost)) 
        {
            Debug.Log("Not enough gold to cast spell!");
            return false;
        }

        LogAction($"Cast Spell: {spellCard.unitData.unitName} on {target.unitData.unitName}");

        Destroy(spellCard.gameObject);
        
        if (AbilityManager.instance != null) AbilityManager.instance.RecalculateAuras();
        
        // Reset targeting references
        pendingCardSource = null;
        pendingAbility = null;
        
        return true;
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
                if (AbilityManager.instance != null)
                {
                    AbilityManager.instance.TriggerAllySummonAbilities(display, playerBoard);
                    AbilityManager.instance.RecalculateAuras();
                }
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

    public void StartAbilityTargeting(AbilityData ability, CardDisplay sourceCard = null)
    {
        if (ability == null) return;

        DeselectUnit(); // Clear selection first

        isTargetingMode = true;
        pendingAbility = ability;
        pendingCardSource = sourceCard;
        
        string sourceName = sourceCard != null ? sourceCard.unitData.unitName : ability.name;
        string prompt = $"Select target for {sourceName} (Right-Click to Cancel)";
        
        Debug.Log($"<color=cyan>{prompt}</color>");
        
        if (heroText != null)
        {
             heroText.text = prompt;
        }
        
        if (targetingCursor != null)
        {
            Cursor.SetCursor(targetingCursor, hotSpot, cursorMode);
        }
    }

    public void OnUnitClicked(CardDisplay targetUnit)
    {
        if (!isTargetingMode || pendingAbility == null) return;

        // Visual feedback
        if (targetUnit != null)
        {
            targetUnit.transform.DOPunchScale(Vector3.one * 0.2f, 0.2f);
        }

        if (pendingCardSource != null && pendingCardSource.unitData.cardType == CardType.Spell)
        {
             // It's a Spell Card cast
             TryPlaySpell(pendingCardSource, targetUnit);
        }
        else
        {
             // It's a Unit Ability or Hero Power
             AbilityManager.instance.CastTargetedAbility(pendingAbility, targetUnit);
             
             if (pendingAbility == activeHero.powerAbility)
             {
                 heroPowerUsed = true;
             }
        }

        UpdateUI();
        CancelTargeting();
    }

    public void CancelTargeting()
    {
        isTargetingMode = false;
        pendingAbility = null;
        pendingCardSource = null;
        
        Cursor.SetCursor(null, Vector2.zero, cursorMode); 
        UpdateUI(); 
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

        if (activeHero.powerAbility.targetType == AbilityTarget.SelectTarget || 
            activeHero.powerAbility.targetType == AbilityTarget.AllFriendly || 
            activeHero.powerAbility.targetType == AbilityTarget.RandomFriendly) 
        {
            if (activeHero.powerAbility.targetType == AbilityTarget.SelectTarget)
            {
                StartAbilityTargeting(activeHero.powerAbility);
                return;
            }
        }
        
        if (TrySpendGold(activeHero.powerCost))
        {
            heroPowerUsed = true;
            AbilityManager.instance.CastHeroPower(activeHero.powerAbility);
            UpdateUI();
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

    public bool TryBuyToBoard(UnitData data, CardDisplay sourceCard, int targetIndex = -1)
    {
        if (!TrySpendGold(data.cost)) return false;

        if (playerBoard.childCount >= 7)
        {
            Debug.Log("Board is full!");
            gold += data.cost; 
            return false;
        }

        sourceCard.transform.SetParent(playerBoard);
        if (targetIndex >= 0) sourceCard.transform.SetSiblingIndex(targetIndex);
        
        sourceCard.isPurchased = true;
        LogAction($"Bought {data.unitName} directly to Board");
        
        if (AnalyticsManager.instance != null)
            AnalyticsManager.instance.TrackPurchase(data.unitName, data.cost);

        if (AbilityManager.instance != null)
        {
            AbilityManager.instance.TriggerAbilities(AbilityTrigger.OnPlay, sourceCard);
            AbilityManager.instance.TriggerAllyPlayAbilities(sourceCard, playerBoard);
            AbilityManager.instance.TriggerAllySummonAbilities(sourceCard, playerBoard);
            AbilityManager.instance.RecalculateAuras();
        }

        CheckForTriples(data);
        return true;
    }

    public bool TryPlayCardToBoard(CardDisplay card, int targetIndex = -1)
    {
        if (card.unitData.cardType == CardType.Spell)
        {
            AbilityData spellAbility = card.unitData.abilities.FirstOrDefault(a => a.triggerType == AbilityTrigger.OnPlay);
            
            if (spellAbility != null && IsTargetedAbility(spellAbility))
            {
                DeselectUnit(); // Fix: Clear previous selection to avoid confusion
                StartAbilityTargeting(spellAbility, card);
                return false; 
            }
            
            LogAction($"Cast Spell: {card.unitData.unitName}");
            if (AbilityManager.instance != null)
            {
                AbilityManager.instance.TriggerAbilities(AbilityTrigger.OnPlay, card);
                AbilityManager.instance.RecalculateAuras();
            }
            Destroy(card.gameObject);
            return true;
        }

        if (playerBoard.childCount >= 7)
        {
            Debug.Log("Board is full!");
            return false;
        }

        card.transform.SetParent(playerBoard);
        if (targetIndex >= 0) card.transform.SetSiblingIndex(targetIndex);
        
        LogAction($"Played {card.unitData.unitName} to Board");
        
        if (AbilityManager.instance != null)
        {
            AbilityManager.instance.TriggerAbilities(AbilityTrigger.OnPlay, card);
            AbilityManager.instance.TriggerAllyPlayAbilities(card, playerBoard);
            AbilityManager.instance.TriggerAllySummonAbilities(card, playerBoard);
            AbilityManager.instance.RecalculateAuras(); 
        }

        CheckForTriples(card.unitData);
        return true;
    }

    private bool IsTargetedAbility(AbilityData ability)
    {
        if (ability == null) return false;
        return ability.targetType == AbilityTarget.SelectTarget || 
               ability.targetType == AbilityTarget.AnyUnit ||
               ability.targetType == AbilityTarget.FriendlyUnit ||
               ability.targetType == AbilityTarget.EnemyUnit;
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
        
        if (AbilityManager.instance != null)
        {
             // Potential logic for sell triggers here
        }

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
        Debug.Log($"<color=red>ModifyHealth called! Amount: {amount}. Previous HP: {playerHealth}</color>");

        playerHealth += amount;
        if (playerHealth > maxPlayerHealth) playerHealth = maxPlayerHealth;
        
        if (playerHealth <= 0 && amount < 0)
        {
            if (AnalyticsManager.instance != null) 
                AnalyticsManager.instance.TrackDeath(turnNumber);
        }

        UpdateUI();
        
        LeaderboardUI leaderboard = FindFirstObjectByType<LeaderboardUI>();
        if (leaderboard != null) leaderboard.UpdateDisplay();
    }
    
    public void CleanUpFloatingCards()
    {
        CardDisplay[] allCards = FindObjectsByType<CardDisplay>(FindObjectsSortMode.None);
        foreach(var card in allCards)
        {
            if (card.gameObject.activeInHierarchy && 
                card.transform.parent != playerBoard && 
                card.transform.parent != playerHand && 
                (combatManager == null || card.transform.parent != combatManager.shopContainer))
            {
                if (card.GetComponentInParent<Canvas>() != null && card.transform.parent.GetComponent<Canvas>() != null)
                {
                    Debug.Log($"Cleaning up floating card: {card.name}");
                    
                    if (card.isPurchased)
                    {
                        card.transform.SetParent(playerHand);
                        card.transform.localPosition = Vector3.zero;
                        card.transform.localScale = Vector3.one;
                    }
                    else
                    {
                        Destroy(card.gameObject);
                    }
                }
            }
        }
    }

    public void UpdateUI()
    {
        if (goldText != null)
        {
            if (enableGoldCarryover) goldText.text = $"Gold: {gold}";
            else goldText.text = $"Gold: {gold}/{maxGold}";
        }
        
        if (bankBalanceText != null)
        {
            string interestText = "";
            if (enableInterest && enableGoldCarryover)
            {
                int totalWealth = gold + bankBalance;
                int projectedInterest = Mathf.Min(totalWealth / 10, interestCap);
                if (projectedInterest > 0) interestText = $" <color=yellow>(+{projectedInterest})</color>";
            }
            bankBalanceText.text = $"Bank: {bankBalance}{interestText}";
        }

        if (turnText != null) turnText.text = $"Turn: {turnNumber}";

        if (healthText != null) 
        {
            healthText.text = $"HP: {playerHealth}/{maxPlayerHealth}";
            if (isUnconscious) healthText.text += " (DOWN)";
        }
        if (heroText != null && activeHero != null) 
        {
            string prompt = isTargetingMode ? heroText.text : $"{activeHero.heroName} Power: {activeHero.powerName} {(heroPowerUsed ? "(Used)" : "(2g)")}";
            if (!isTargetingMode) heroText.text = prompt;
        }

        if (tierText != null && ShopManager.instance != null)
        {
             tierText.text = $"Tier: {ShopManager.instance.tavernTier}";
        }
    }
}