using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems; // Required for clicking

// We add IPointerClickHandler to detect clicks on UI elements
public class CardDisplay : MonoBehaviour, IPointerClickHandler
{
    public UnitData unitData;

    [Header("UI References")]
    public Image artworkImage;
    public TMP_Text nameText;
    public TMP_Text descriptionText;
    public TMP_Text attackText;
    public TMP_Text healthText;
    public Image frameImage;

    [Header("State")]
    public bool isPurchased = false; // Is this in the shop or on board?

    void Start()
    {
        if (unitData != null) LoadUnit(unitData);
    }

    public void LoadUnit(UnitData data)
    {
        unitData = data;
        if (nameText != null) nameText.text = data.unitName;
        if (descriptionText != null) descriptionText.text = data.abilityDescription;
        if (artworkImage != null) artworkImage.sprite = data.artwork;
        if (attackText != null) attackText.text = data.baseAttack.ToString();
        if (healthText != null) healthText.text = data.baseHealth.ToString();
        if (frameImage != null) frameImage.color = data.frameColor;
    }

    // This fires when you click the card
    public void OnPointerClick(PointerEventData eventData)
    {
        if (GameManager.instance.currentPhase != GameManager.GamePhase.Recruit) return;

        // If in shop, try to buy
        if (!isPurchased)
        {
            TryBuyCard();
        }
    }

    void TryBuyCard()
    {
        int cost = unitData.cost;

        // Ask GameManager if we have money
        if (GameManager.instance.TrySpendGold(cost))
        {
            Debug.Log($"Bought {unitData.unitName}!");

            // Move to Player Board
            // Find the PlayerBoard object by name (Quick and dirty for MVP)
            GameObject playerBoard = GameObject.Find("PlayerBoard");

            if (playerBoard != null)
            {
                transform.SetParent(playerBoard.transform);
                isPurchased = true; // Mark as owned
            }
        }
        else
        {
            Debug.Log("Not enough Gold!");
            // Optional: Shake the card or flash red here later
        }
    }
}