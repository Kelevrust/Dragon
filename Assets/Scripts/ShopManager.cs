using UnityEngine;
using System.Collections.Generic;

public class ShopManager : MonoBehaviour
{
    [Header("Configuration")]
    public GameObject cardPrefab; 
    public Transform shopContainer; 
    public int shopSize = 3;
    public int rerollCost = 1; // Cost to reroll

    [Header("Database")]
    public UnitData[] availableUnits; 

    void Start()
    {
        RerollShop();
    }

    // Called automatically at start of turn (Free)
    public void RerollShop()
    {
        // Don't modify shop if Unconscious
        if (GameManager.instance.isUnconscious) return;

        ClearShop();
        GenerateCards();
    }

    // Called by the UI Button (Costs Gold)
    public void OnRerollClick()
    {
        // 1. Check State
        if (GameManager.instance.currentPhase != GameManager.GamePhase.Recruit) return;
        if (GameManager.instance.isUnconscious) return;

        // 2. Check Gold
        if (GameManager.instance.TrySpendGold(rerollCost))
        {
            Debug.Log("Rerolling Shop...");
            ClearShop();
            GenerateCards();
        }
        else
        {
            Debug.Log("Not enough gold to reroll!");
            // Optional: Play "Error" sound here
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
        for (int i = 0; i < shopSize; i++)
        {
            UnitData randomData = availableUnits[Random.Range(0, availableUnits.Length)];
            GameObject newCard = Instantiate(cardPrefab, shopContainer);
            
            CardDisplay display = newCard.GetComponent<CardDisplay>();
            if (display != null)
            {
                display.LoadUnit(randomData);
            }
        }
    }
}