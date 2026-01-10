using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq; 

public class GameTester : MonoBehaviour
{
    [Header("Automation Settings")]
    public bool isAutoPlaying = false;
    [Range(1f, 50f)] public float simulationSpeed = 1f;
    
    [Header("Stop Conditions")]
    public int stopOnTurn = 30; 
    public bool stopOnDefeat = false; 

    [Header("AI Difficulty Control")]
    [Range(0.5f, 5.0f)] public float aiDifficulty = 1.0f;

    [Header("MMR Controls")]
    public bool overrideMMR = false;
    [Range(0, 12000)] public int targetMMR = 3000;
    
    public bool distributeMMR = false;
    [Range(0, 2000)] public int mmrVariance = 500;

    [Header("--- DEBUG MENU ---")]
    [Header("Economy Overrides")]
    public bool toggleEconomyMode = false; 
    public bool applyPoolOverrides = false;
    [Tooltip("Copies of each unit per Tier (Index 1 = Tier 1)")]
    public int[] customPoolSizes = new int[] { 0, 16, 15, 13, 11, 9, 7 };
    
    [Header("Live Cheats")]
    public int setGoldTo = -1; 
    public int setTavernTierTo = -1; 
    
    [Header("Tribal Manipulation")]
    public Tribe tribeToAdd = Tribe.None;
    public Tribe tribeToRemove = Tribe.None;
    public bool triggerTribeUpdate = false; 
    
    [Header("Hero Swap")]
    public HeroData heroToSwapIn;
    public bool triggerHeroSwap = false;

    [Header("--- LIVE UNIT SPAWNER ---")]
    [Tooltip("Drag a UnitData here to spawn it manually")]
    public UnitData unitToSpawn;
    public bool spawnOnPlayerBoard;
    public bool spawnOnEnemyBoard;
    
    [Header("Test Dummies")]
    [Tooltip("Spawns a 0/10 Unit with no abilities")]
    public bool spawnTestDummyAlly;
    [Tooltip("Spawns a 0/10 Unit with no abilities")]
    public bool spawnTestDummyEnemy;

    [Header("--- CUSTOM DUMMY CREATOR ---")]
    public string dummyName = "Custom Test";
    public int dummyAttack = 1;
    public int dummyHealth = 1;
    [Header("Custom Keywords")]
    public bool dummyTaunt;
    public bool dummyDivineShield;
    public bool dummyPoison;
    public bool dummyVenomous;
    public bool dummyReborn;
    public bool dummyStealth;
    
    [Header("Spawn Actions")]
    [Tooltip("Spawns the custom configured unit on Player Board")]
    public bool spawnCustomAlly;
    [Tooltip("Spawns the custom configured unit on Enemy Board")]
    public bool spawnCustomEnemy;

    [Header("Logic Delays")]
    public float actionInterval = 0.2f; 
    private float timer;
    private int lastHealth;
    private bool wasAutoPlaying = false;

    void Start()
    {
        if (GameManager.instance != null)
        {
            lastHealth = GameManager.instance.playerHealth;
        }
        wasAutoPlaying = isAutoPlaying;
        
        if (applyPoolOverrides && ShopManager.instance != null)
        {
            ApplyPoolOverrides();
        }
    }

    void Update()
    {
        HandleDebugInputs();

        if (LobbyManager.instance != null && (overrideMMR || distributeMMR))
        {
            ApplyMMRSettings();
        }

        if (AIManager.instance != null) 
        {
            AIManager.instance.difficultyMultiplier = aiDifficulty;
        }

        Time.timeScale = simulationSpeed;

        if (wasAutoPlaying && !isAutoPlaying)
        {
            Debug.Log($"<color=yellow>Tester: AI Interrupted by User in Round {GameManager.instance.turnNumber} - {GameManager.instance.currentPhase}</color>");
        }
        wasAutoPlaying = isAutoPlaying;

        if (!isAutoPlaying) return;
        if (GameManager.instance == null) return;

        if (GameManager.instance.turnNumber >= stopOnTurn)
        {
            Debug.Log($"<color=orange>Tester: Reached Turn {stopOnTurn}. Stopping.</color>");
            isAutoPlaying = false;
            return;
        }

        if (stopOnDefeat && GameManager.instance.playerHealth < lastHealth)
        {
            Debug.Log("<color=red>Tester: Player took damage. Stopping.</color>");
            isAutoPlaying = false;
            return;
        }
        lastHealth = GameManager.instance.playerHealth; 

        timer += Time.unscaledDeltaTime; 
        if (timer >= actionInterval)
        {
            timer = 0;
            RunBotDecision();
        }
    }

    void HandleDebugInputs()
    {
        if (GameManager.instance == null) return;

        // --- ECONOMY ---
        if (toggleEconomyMode)
        {
            bool newState = !GameManager.instance.enableGoldCarryover;
            GameManager.instance.ToggleBankingMode(newState);
            GameManager.instance.enableInterest = newState;
            Debug.Log($"Tester: Toggled Economy Mode to {(newState ? "Accumulation" : "Standard")}");
            toggleEconomyMode = false;
        }

        if (setGoldTo >= 0)
        {
            GameManager.instance.gold = setGoldTo;
            GameManager.instance.UpdateUI();
            Debug.Log($"Tester: Cheat Gold set to {setGoldTo}");
            setGoldTo = -1; 
        }

        if (setTavernTierTo >= 1 && ShopManager.instance != null)
        {
            ShopManager.instance.ForceSetTier(setTavernTierTo);
            setTavernTierTo = -1;
        }

        if (triggerTribeUpdate && ShopManager.instance != null)
        {
            if (tribeToAdd != Tribe.None) ShopManager.instance.ModifyTribes(tribeToAdd, true);
            if (tribeToRemove != Tribe.None) ShopManager.instance.ModifyTribes(tribeToRemove, false);
            triggerTribeUpdate = false;
        }

        if (triggerHeroSwap && heroToSwapIn != null)
        {
            Debug.Log($"Tester: Swapping Hero to {heroToSwapIn.heroName}");
            GameManager.instance.activeHero = heroToSwapIn;
            GameManager.instance.UpdateUI();
            triggerHeroSwap = false;
        }

        // --- SPAWNER ---
        if (spawnOnPlayerBoard && unitToSpawn != null)
        {
            SpawnDebugUnit(unitToSpawn, true);
            spawnOnPlayerBoard = false;
        }
        
        if (spawnOnEnemyBoard && unitToSpawn != null)
        {
            SpawnDebugUnit(unitToSpawn, false);
            spawnOnEnemyBoard = false;
        }
        
        if (spawnTestDummyAlly)
        {
            SpawnDummy(true);
            spawnTestDummyAlly = false;
        }
        
        if (spawnTestDummyEnemy)
        {
            SpawnDummy(false);
            spawnTestDummyEnemy = false;
        }

        // --- CUSTOM DUMMY ---
        if (spawnCustomAlly)
        {
            SpawnCustomUnit(true);
            spawnCustomAlly = false;
        }
        if (spawnCustomEnemy)
        {
            SpawnCustomUnit(false);
            spawnCustomEnemy = false;
        }
    }

    void SpawnDebugUnit(UnitData data, bool isPlayer)
    {
        if (GameManager.instance == null) return;
        
        // Determine correct parent
        Transform parent = null;
        if (isPlayer) 
        {
            parent = GameManager.instance.playerBoard;
        }
        else
        {
            if (CombatManager.instance != null) parent = CombatManager.instance.enemyBoard;
        }
        
        if (parent == null || GameManager.instance.cardPrefab == null) return;
        
        GameObject obj = Instantiate(GameManager.instance.cardPrefab, parent);
        CardDisplay cd = obj.GetComponent<CardDisplay>();
        
        if (cd != null)
        {
            cd.LoadUnit(data);
            cd.isPurchased = true; 
            
            // If enemy, usually we remove buttons to prevent dragging them
            if (!isPlayer) Destroy(obj.GetComponent<Button>());
            
            // Refresh Auras to catch new unit
            if (AbilityManager.instance != null) AbilityManager.instance.RecalculateAuras();
        }
        
        Debug.Log($"Tester: Spawned {data.unitName} on {(isPlayer ? "Player" : "Enemy")} board.");
    }
    
    void SpawnDummy(bool isPlayer)
    {
        // Create a temporary ScriptableObject in memory
        UnitData dummy = ScriptableObject.CreateInstance<UnitData>();
        dummy.name = "Test Subject";
        dummy.unitName = "Test Subject";
        dummy.description = "A generic target for testing interactions.";
        dummy.baseAttack = 0;
        dummy.baseHealth = 10;
        dummy.cost = 0;
        dummy.tier = 1;
        dummy.abilities = new List<AbilityData>(); // Empty list to prevent null errors
        
        SpawnDebugUnit(dummy, isPlayer);
    }

    void SpawnCustomUnit(bool isPlayer)
    {
        UnitData custom = ScriptableObject.CreateInstance<UnitData>();
        custom.name = dummyName;
        custom.unitName = dummyName;
        custom.description = "Custom Debug Unit created at Runtime.";
        custom.baseAttack = dummyAttack;
        custom.baseHealth = dummyHealth;
        custom.cost = 0;
        custom.tier = 1;
        custom.hasTaunt = dummyTaunt;
        custom.hasDivineShield = dummyDivineShield;
        custom.hasPoison = dummyPoison;
        custom.hasVenomous = dummyVenomous;
        custom.hasReborn = dummyReborn;
        custom.hasStealth = dummyStealth;
        
        custom.abilities = new List<AbilityData>();

        SpawnDebugUnit(custom, isPlayer);
    }

    void ApplyPoolOverrides()
    {
        if (ShopManager.instance == null) return;
        Debug.Log("<color=cyan>Tester: Overriding Shop Pool Sizes.</color>");
        ShopManager.instance.SetPoolSizes(customPoolSizes);
    }
    
    void ApplyMMRSettings()
    {
        var bots = LobbyManager.instance.opponents;
        if (bots == null || bots.Count == 0) return;

        for (int i = 0; i < bots.Count; i++)
        {
            int newMMR = targetMMR;

            if (distributeMMR)
            {
                float t = (float)i / (Mathf.Max(1, bots.Count - 1)); 
                float offset = Mathf.Lerp(-mmrVariance, mmrVariance, t);
                newMMR = Mathf.RoundToInt(targetMMR + offset);
            }

            bots[i].mmr = Mathf.Clamp(newMMR, 0, 12000);
            
            if (bots[i].mmr < 2000) bots[i].rank = "Iron";
            else if (bots[i].mmr < 4000) bots[i].rank = "Bronze";
            else if (bots[i].mmr < 6000) bots[i].rank = "Silver";
            else if (bots[i].mmr < 8000) bots[i].rank = "Gold";
            else if (bots[i].mmr < 10000) bots[i].rank = "Platinum";
            else bots[i].rank = "Diamond";
        }
    }

    void RunBotDecision()
    {
        if (DeathSaveManager.instance != null && DeathSaveManager.instance.deathScreenPanel.activeSelf)
        {
            HandleDeathScreen();
            return;
        }

        if (GameManager.instance.currentPhase == GameManager.GamePhase.Recruit)
        {
            if (GameManager.instance.isUnconscious)
            {
                Debug.Log("Tester: Unconscious. Skipping turn.");
                CombatManager.instance.StartCombat();
                return;
            }
            
            if (TryUpgradeTavern()) return;
            if (TryPlayFromHand()) return;
            if (TryBuyTriple()) return;
            if (TryBuyAny()) return;
            if (TryImproveBoard()) return;
            if (TryReroll()) return;

            Debug.Log("Tester: Turn complete. Starting Combat.");
            CombatManager.instance.StartCombat();
        }
    }

    bool TryPlayFromHand()
    {
        if (GameManager.instance.playerHand.childCount == 0) return false;
        
        foreach(Transform child in GameManager.instance.playerHand)
        {
            CardDisplay card = child.GetComponent<CardDisplay>();
            if (card != null)
            {
                if (GameManager.instance.TryPlayCardToBoard(card))
                {
                    Debug.Log($"Tester: Playing {card.unitData.unitName} from Hand.");
                    return true;
                }
                
                GameObject playerBoard = GameObject.Find("PlayerBoard");
                if (playerBoard.transform.childCount >= 7)
                {
                    var boardCards = GetMyCardsOnBoard();
                    var worstUnit = boardCards.OrderBy(c => c.isGolden).ThenBy(c => c.unitData.tier).First();

                    bool isUpgrade = card.isGolden || card.unitData.tier > worstUnit.unitData.tier;
                    if (isUpgrade && !worstUnit.isGolden)
                    {
                        Debug.Log($"Tester: Board Full. Selling {worstUnit.unitData.unitName} to play {card.unitData.unitName}.");
                        GameManager.instance.SellUnit(worstUnit);
                        return true; 
                    }
                }
            }
        }
        return false;
    }

    bool TryUpgradeTavern()
    {
        ShopManager shop = FindFirstObjectByType<ShopManager>();
        if (shop == null) return false;

        int cost = GetUpgradeCost(shop);
        bool canAfford = GameManager.instance.gold >= cost;
        bool safeToUpgrade = GameManager.instance.gold >= cost + 3;
        bool cheapUpgrade = cost <= 4;

        if (canAfford && (safeToUpgrade || cheapUpgrade) && shop.tavernTier < shop.maxTier)
        {
            Debug.Log($"Tester: Upgrading Tavern to Tier {shop.tavernTier + 1}");
            shop.OnUpgradeClick();
            return true;
        }
        return false;
    }

    bool TryBuyTriple()
    {
        List<CardDisplay> shopCards = GetShopCards();
        List<CardDisplay> myCards = GetMyCards();

        foreach (var shopCard in shopCards)
        {
            if (!CanAfford(shopCard)) continue;

            int count = myCards.Count(c => c.unitData == shopCard.unitData && !c.isGolden);
            int shopCount = shopCards.Count(c => c.unitData == shopCard.unitData);
            
            if (count + shopCount >= 3 && count < 3) 
            {
                Debug.Log($"Tester: Found Triple path for {shopCard.unitData.unitName} (Owned: {count}, Shop: {shopCount})!");
                
                if (GetMyCardsOnBoard().Count >= 7 && !HasTripleComponents(shopCard))
                {
                    var worstUnit = GetMyCardsOnBoard().OrderBy(c => c.isGolden).ThenBy(c => c.unitData.tier).First();
                    
                    if (worstUnit.unitData != shopCard.unitData)
                    {
                        Debug.Log($"Tester: Selling {worstUnit.unitData.unitName} to make room for Triple.");
                        GameManager.instance.SellUnit(worstUnit);
                    }
                }

                return BuyCardSmart(shopCard);
            }
        }
        return false;
    }

    bool TryBuyAny()
    {
        if (GetMyCardsOnBoard().Count >= 7) return false; 
        List<CardDisplay> shopCards = GetShopCards();
        foreach (var card in shopCards)
        {
            if (CanAfford(card))
            {
                return BuyCardSmart(card);
            }
        }
        return false;
    }

    bool TryImproveBoard()
    {
        List<CardDisplay> myCards = GetMyCardsOnBoard();
        if (myCards.Count < 7) return false;

        List<CardDisplay> shopCards = GetShopCards();
        
        var worstUnit = myCards.OrderBy(c => c.isGolden).ThenBy(c => c.unitData.tier).First();
        var bestShop = shopCards.OrderByDescending(c => c.unitData.tier).FirstOrDefault();

        if (bestShop != null && CanAfford(bestShop))
        {
            if (bestShop.unitData.tier > worstUnit.unitData.tier)
            {
                Debug.Log($"Tester: Upgrading Army. Selling {worstUnit.unitData.unitName} for {bestShop.unitData.unitName}");
                GameManager.instance.SellUnit(worstUnit);
                return BuyCardSmart(bestShop); 
            }
        }
        return false;
    }

    bool TryReroll()
    {
        ShopManager shop = FindFirstObjectByType<ShopManager>();
        if (shop != null && GameManager.instance.gold >= shop.rerollCost + 3) 
        {
            shop.OnRerollClick();
            return true;
        }
        return false;
    }

    bool BuyCardSmart(CardDisplay card)
    {
        if (GameManager.instance.TryBuyToHand(card.unitData, card))
        {
            Debug.Log($"Tester: Bought {card.unitData.unitName} to Hand.");
            return true;
        }
        return false;
    }

    bool CanAfford(CardDisplay card) => GameManager.instance.gold >= card.unitData.cost;

    List<CardDisplay> GetShopCards()
    {
        List<CardDisplay> list = new List<CardDisplay>();
        ShopManager shop = FindFirstObjectByType<ShopManager>();
        if (shop != null) foreach (Transform t in shop.shopContainer) { CardDisplay cd = t.GetComponent<CardDisplay>(); if (cd != null) list.Add(cd); }
        return list;
    }

    List<CardDisplay> GetMyCards()
    {
        List<CardDisplay> list = new List<CardDisplay>();
        if (GameManager.instance.playerBoard != null) foreach (Transform t in GameManager.instance.playerBoard) { CardDisplay cd = t.GetComponent<CardDisplay>(); if (cd != null) list.Add(cd); }
        if (GameManager.instance.playerHand != null) foreach (Transform t in GameManager.instance.playerHand) { CardDisplay cd = t.GetComponent<CardDisplay>(); if (cd != null) list.Add(cd); }
        return list;
    }
    
    List<CardDisplay> GetMyCardsOnBoard()
    {
        List<CardDisplay> list = new List<CardDisplay>();
        if (GameManager.instance.playerBoard != null) foreach (Transform t in GameManager.instance.playerBoard) { CardDisplay cd = t.GetComponent<CardDisplay>(); if (cd != null) list.Add(cd); }
        return list;
    }
    
    bool HasTripleComponents(CardDisplay shopCard)
    {
        int count = GetMyCards().Count(c => c.unitData == shopCard.unitData && !c.isGolden);
        return count >= 2;
    }
    
    int GetUpgradeCost(ShopManager shop)
    {
        int nextTier = shop.tavernTier + 1;
        if (nextTier < shop.tierCosts.Length) return Mathf.Max(0, shop.tierCosts[nextTier] - shop.currentDiscount);
        return 99;
    }

    void HandleDeathScreen()
    {
        DeathSaveManager dsm = DeathSaveManager.instance;
        if (dsm.playAgainButton != null && dsm.playAgainButton.gameObject.activeInHierarchy)
        {
             Debug.Log("Tester: Game Over. Restarting...");
             dsm.playAgainButton.onClick.Invoke();
             return;
        }
        
        if (dsm.isPvPMode)
        {
            if (dsm.wagerButton != null && dsm.wagerButton.interactable)
            {
                Debug.Log("Tester: Taking the Wager (Attempt Cheat Death)!");
                dsm.wagerButton.onClick.Invoke();
            }
        }
        else
        {
            if (dsm.rollSaveButton != null && dsm.rollSaveButton.interactable) 
            {
                dsm.rollSaveButton.onClick.Invoke();
            }
        }
    }
}