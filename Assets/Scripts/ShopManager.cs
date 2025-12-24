using UnityEngine;
using System.Collections.Generic;

public class ShopManager : MonoBehaviour
{
    [Header("Configuration")]
    public GameObject cardPrefab; // The blue prefab we just made
    public Transform shopContainer; // The empty object with the Layout Group
    public int shopSize = 3;

    [Header("Database")]
    public UnitData[] availableUnits; // Drag your 3 unit files here

    void Start()
    {
        RerollShop();
    }

    public void RerollShop()
    {
        // 1. Clear existing cards (if any)
        foreach (Transform child in shopContainer)
        {
            Destroy(child.gameObject);
        }

        // 2. Spawn new cards
        for (int i = 0; i < shopSize; i++)
        {
            // Pick a random unit data
            UnitData randomData = availableUnits[Random.Range(0, availableUnits.Length)];

            // Create the card object inside the container
            GameObject newCard = Instantiate(cardPrefab, shopContainer);

            // Get the script and load the data
            CardDisplay display = newCard.GetComponent<CardDisplay>();
            if (display != null)
            {
                display.LoadUnit(randomData);
            }
        }
    }
}