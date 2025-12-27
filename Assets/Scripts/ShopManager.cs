using UnityEngine;
using System.Collections.Generic;
using TMPro; 

public class ShopManager : MonoBehaviour
{
    [Header("Configuration")]
    public GameObject cardPrefab; 
    public Transform shopContainer; 
    public int shopSize = 3;
    public int rerollCost = 1; 

    [Header("Tavern Tech")]
    public int tavernTier = 1;
    // Costs to reach next tier: Tier 1->2 (5g), 2->3 (7g), 3->4 (8g), 4->5 (9g), 5->6 (10g)
    // Index 0 and 1 are unused or 0 since we start at Tier 1.
    // We look up cost for NEXT tier. e.g. upgrading to Tier 2 uses index 2.
    public int[] tierCosts = new int[] { 0, 0, 5, 7, 8, 9, 10 }; 
    public int maxTier = 6;
    
    // Tracks how much the cost is reduced (increases by 1 every turn)
    public int currentDiscount = 0;

    [Header("UI References")]
    public TMP_Text upgradeButtonText; 

    [Header("Database")]
    public UnitData[] availableUnits; 

    void Start()
    {
        UpdateUpgradeUI();
        RerollShop();
    }

    public void RerollShop()
    {
        if (GameManager.instance.isUnconscious) return;
        ClearShop();
        GenerateCards();
    }

    // Called by CombatManager at the start of a new turn
    public void ReduceUpgradeCost()
    {
        if (tavernTier < maxTier)
        {
            currentDiscount++;
            UpdateUpgradeUI();
        }
    }

    public void OnRerollClick()
    {
        if (GameManager.instance.currentPhase != GameManager.GamePhase.Recruit) return;
        if (GameManager.instance.isUnconscious) return;

        if (GameManager.instance.TrySpendGold(rerollCost))
        {
            GameManager.instance.LogAction("Rerolled Shop");
            ClearShop();
            GenerateCards();
        }
    }

    public void OnUpgradeClick()
    {
        if (GameManager.instance.currentPhase != GameManager.GamePhase.Recruit) return;
        
        if (tavernTier >= maxTier) return;

        int cost = GetUpgradeCost();

        if (GameManager.instance.TrySpendGold(cost))
        {
            tavernTier++;
            
            // RESET Discount on upgrade
            currentDiscount = 0;
            
            GameManager.instance.LogAction($"Upgraded Tavern to Tier {tavernTier}");
            GameManager.instance.UpdateUI(); 
            UpdateUpgradeUI(); 
        }
    }

    int GetUpgradeCost()
    {
        int nextTier = tavernTier + 1;
        if (nextTier < tierCosts.Length)
        {
            // Cost is Base - Discount (Min 0)
            return Mathf.Max(0, tierCosts[nextTier] - currentDiscount);
        }
        return 0; 
    }

    void UpdateUpgradeUI()
    {
        if (upgradeButtonText != null)
        {
            if (tavernTier >= maxTier)
            {
                upgradeButtonText.text = "Max Tier";
            }
            else
            {
                upgradeButtonText.text = $"Upgrade ({GetUpgradeCost()}g)";
            }
        }
    }

    void ClearShop()
    {
        foreach (Transform child in shopContainer) Destroy(child.gameObject);
    }

    void GenerateCards()
    {
        List<UnitData> validUnits = new List<UnitData>();
        foreach (UnitData u in availableUnits)
        {
            if (u != null && u.tier <= tavernTier)
            {
                validUnits.Add(u);
            }
        }

        if (validUnits.Count == 0) return;

        string spawnLog = $"Shop Spawned (Tier {tavernTier}): ";

        for (int i = 0; i < shopSize; i++)
        {
            UnitData randomData = validUnits[Random.Range(0, validUnits.Count)];
            GameObject newCard = Instantiate(cardPrefab, shopContainer);
            
            spawnLog += $"[{randomData.unitName} T{randomData.tier}] ";

            CardDisplay display = newCard.GetComponent<CardDisplay>();
            if (display != null)
            {
                display.LoadUnit(randomData);
            }
        }
        Debug.Log(spawnLog);
    }
}