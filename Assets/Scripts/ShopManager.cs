using UnityEngine;
using System.Collections.Generic;

public class ShopManager : MonoBehaviour
{
    [Header("Configuration")]
    public GameObject cardPrefab; 
    public Transform shopContainer; 
    public int shopSize = 3;
    public int rerollCost = 1; 

    [Header("Tavern Tech")]
    public int tavernTier = 1;
    public int upgradeCost = 5; // Base cost for Tier 2

    [Header("Database")]
    public UnitData[] availableUnits; 

    void Start()
    {
        RerollShop();
    }

    public void RerollShop()
    {
        if (GameManager.instance.isUnconscious) return;

        ClearShop();
        GenerateCards();
    }

    public void OnRerollClick()
    {
        if (GameManager.instance.currentPhase != GameManager.GamePhase.Recruit) return;
        if (GameManager.instance.isUnconscious) return;

        if (GameManager.instance.TrySpendGold(rerollCost))
        {
            Debug.Log("Rerolling Shop...");
            ClearShop();
            GenerateCards();
        }
        else
        {
            Debug.Log("Not enough gold to reroll!");
        }
    }

    // NEW: Upgrade Logic
    public void OnUpgradeClick()
    {
        if (GameManager.instance.currentPhase != GameManager.GamePhase.Recruit) return;
        if (GameManager.instance.isUnconscious) return;

        if (GameManager.instance.TrySpendGold(upgradeCost))
        {
            tavernTier++;
            Debug.Log($"Tavern Upgraded to Tier {tavernTier}!");
            
            // Increase cost for next tier (Simple MVP scaling)
            upgradeCost += 5; 
            
            // Optional: Auto-refresh shop on upgrade? 
            // For now, let's leave it manual or force a refresh:
            // ClearShop();
            // GenerateCards();
        }
        else
        {
            Debug.Log("Not enough gold to upgrade!");
        }
    }

    void ClearShop()
    {
        foreach (Transform child in shopContainer)
        {
            Destroy(child.gameObject);
        }
    }

    void GenerateCards()
    {
        // 1. Filter units by Tier
        List<UnitData> validUnits = new List<UnitData>();
        foreach (UnitData u in availableUnits)
        {
            if (u != null && u.tier <= tavernTier)
            {
                validUnits.Add(u);
            }
        }

        if (validUnits.Count == 0) return;

        // 2. Spawn Cards
        for (int i = 0; i < shopSize; i++)
        {
            UnitData randomData = validUnits[Random.Range(0, validUnits.Count)];
            GameObject newCard = Instantiate(cardPrefab, shopContainer);
            
            CardDisplay display = newCard.GetComponent<CardDisplay>();
            if (display != null)
            {
                display.LoadUnit(randomData);
            }
        }
    }
}