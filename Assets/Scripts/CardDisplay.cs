using UnityEngine;
using UnityEngine.UI;
using TMPro; // Needed for TextMeshPro

public class CardDisplay : MonoBehaviour
{
    // These hold the references to your UI objects
    public UnitData unitData; // The data file we will drag in for testing

    [Header("UI References")]
    public Image artworkImage;
    public TMP_Text nameText;
    public TMP_Text descriptionText;
    public TMP_Text attackText;
    public TMP_Text healthText;
    public Image frameImage;

    // Called automatically when the game starts
    void Start()
    {
        if (unitData != null)
        {
            LoadUnit(unitData);
        }
    }

    // Call this function to update the card visuals
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
}