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
    }

    void Update()
    {
        if (LobbyManager.instance != null && (overrideMMR || distributeMMR))
        {
            ApplyMMRSettings();
        }

        if (AIManager.instance != null) 
        {
            AIManager.instance.difficultyMultiplier = aiDifficulty;
        }

        Time.timeScale = simulationSpeed;

        // --- FIXED INTERRUPTION LOGIC ---
        // Only log interruption if we were auto-playing, it's now false, 
        // AND it wasn't caused by a game-over condition we just handled.
        if (wasAutoPlaying && !isAutoPlaying)
        {
            // If the game is still running (recruit/combat) and we stopped, it's a manual user stop
            bool isGameRunning = GameManager.instance != null && !GameManager.instance.isUnconscious;
            if (isGameRunning)
            {
                Debug.Log($"<color=yellow>Tester: AI Interrupted by User in Round {GameManager.instance.turnNumber} - {GameManager.instance.currentPhase}</color>");
            }
        }

        if (!isAutoPlaying) 
        {
            wasAutoPlaying = false;
            return;
        }
        
        if (GameManager.instance == null) return;

        // Check Stop Conditions (Set wasAutoPlaying to false to prevent double logging)
        if (GameManager.instance.turnNumber >= stopOnTurn)
        {
            Debug.Log($"<color=orange>Tester: Reached Turn {stopOnTurn}. Auto-Stop.</color>");
            isAutoPlaying = false;
            wasAutoPlaying = false; // Prevent "Interrupted" log
            return;
        }

        if (stopOnDefeat && GameManager.instance.playerHealth < lastHealth)
        {
            Debug.Log("<color=red>Tester: Player took damage. Auto-Stop.</color>");
            isAutoPlaying = false;
            wasAutoPlaying = false; // Prevent "Interrupted" log
            return;
        }
        
        // Track state for next frame
        wasAutoPlaying = true;
        lastHealth = GameManager.instance.playerHealth; 

        timer += Time.unscaledDeltaTime; 
        if (timer >= actionInterval)
        {
            timer = 0;
            RunBotDecision();
        }
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

            int myCount = myCards.Count(c => c.unitData == shopCard.unitData && !c.isGolden);
            int shopCount = shopCards.Count(c => c.unitData == shopCard.unitData);
            
            if (myCount + shopCount >= 3 && myCount < 3) 
            {
                Debug.Log($"Tester: Found Triple path for {shopCard.unitData.unitName} (Owned: {myCount}, Shop: {shopCount})!");
                
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
                Debug.Log($"Tester: Upgrading Army. Selling {worstUnit.unitData.unitName} (T{worstUnit.unitData.tier}) for {bestShop.unitData.unitName} (T{bestShop.unitData.tier})");
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

    bool CanAfford(CardDisplay card)
    {
        return GameManager.instance.gold >= card.unitData.cost;
    }

    List<CardDisplay> GetShopCards()
    {
        List<CardDisplay> list = new List<CardDisplay>();
        ShopManager shop = FindFirstObjectByType<ShopManager>();
        if (shop != null)
        {
            foreach (Transform t in shop.shopContainer)
            {
                CardDisplay cd = t.GetComponent<CardDisplay>();
                if (cd != null) list.Add(cd);
            }
        }
        return list;
    }

    List<CardDisplay> GetMyCards()
    {
        List<CardDisplay> list = new List<CardDisplay>();
        if (GameManager.instance.playerBoard != null)
        {
            foreach (Transform t in GameManager.instance.playerBoard)
            {
                CardDisplay cd = t.GetComponent<CardDisplay>();
                if (cd != null) list.Add(cd);
            }
        }
        if (GameManager.instance.playerHand != null)
        {
            foreach (Transform t in GameManager.instance.playerHand)
            {
                CardDisplay cd = t.GetComponent<CardDisplay>();
                if (cd != null) list.Add(cd);
            }
        }
        return list;
    }
    
    List<CardDisplay> GetMyCardsOnBoard()
    {
        List<CardDisplay> list = new List<CardDisplay>();
        if (GameManager.instance.playerBoard != null)
        {
            foreach (Transform t in GameManager.instance.playerBoard)
            {
                CardDisplay cd = t.GetComponent<CardDisplay>();
                if (cd != null) list.Add(cd);
            }
        }
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
        if (nextTier < shop.tierCosts.Length)
        {
            return Mathf.Max(0, shop.tierCosts[nextTier] - shop.currentDiscount);
        }
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
        if (dsm.rollButton != null && dsm.rollButton.interactable)
        {
            dsm.rollButton.onClick.Invoke();
        }
    }
}