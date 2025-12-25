using UnityEngine;
using System.Collections.Generic;

public class ShopManager : MonoBehaviour
{
    [Header("Configuration")]
    public GameObject cardPrefab;
    public Transform shopContainer;
    public int shopSize = 3;

    [Header("Database")]
    public UnitData[] availableUnits;

    void Start()
    {
        RerollShop();
    }

    public void RerollShop()
    {
        // Don't modify shop if Unconscious
        if (GameManager.instance.isUnconscious) return;

        foreach (Transform child in shopContainer)
        {
            Destroy(child.gameObject);
        }

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