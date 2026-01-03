using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using DG.Tweening; // NEW: The Magic Ingredient

public class LeaderboardRow : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI References")]
    public Image portraitImage;
    public TMP_Text nameText;
    public Image healthBarFill; 
    public Image healthFillImage; 
    public Image backgroundImage; 
    public GameObject deadIcon;   

    [Header("Settings")]
    public Color playerColor = Color.green;
    public Color enemyColor = Color.white;
    public Color deadColor = Color.gray;
    
    [Header("Animation")]
    public float fillSpeed = 0.5f; 

    private string tooltipContent; 
    private RectTransform rectTransform;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    public void Setup(string name, int health, int maxHealth, bool isDead, bool isPlayer, bool isOpponent, Sprite portrait, string tooltipData)
    {
        tooltipContent = tooltipData; 

        // 1. Text
        if (nameText != null)
        {
            nameText.text = name;
            nameText.color = isDead ? deadColor : (isPlayer ? playerColor : Color.white);
        }

        // 2. Health Bar (DOTween Version)
        if (healthBarFill != null)
        {
            float targetAmount = (float)health / maxHealth;
            
            // Kill any running tweens on this object to prevent conflicts
            healthBarFill.DOKill();
            
            if (healthBarFill.fillAmount == 0 && health > 0) 
            {
                 // Instant set on init
                 healthBarFill.fillAmount = targetAmount;
            }
            else
            {
                // Smooth slide
                healthBarFill.DOFillAmount(targetAmount, fillSpeed).SetEase(Ease.OutQuart);
                
                // Juice: Shake the row if taking damage
                if (targetAmount < healthBarFill.fillAmount)
                {
                    // Shake position: Duration, Strength, Vibrato
                    rectTransform.DOShakeAnchorPos(0.3f, new Vector2(5f, 0f), 10, 90, false, true);
                }
            }
        }

        // 3. Health Color (Green -> Red gradient)
        Image targetColorImg = healthFillImage != null ? healthFillImage : healthBarFill;
        if (targetColorImg != null && !isDead)
        {
            float hpPercent = (float)health / maxHealth;
            targetColorImg.DOColor(Color.Lerp(Color.red, Color.green, hpPercent), fillSpeed);
        }

        // 4. Portrait
        if (portraitImage != null)
        {
            portraitImage.sprite = portrait;
            portraitImage.color = isDead ? new Color(0.3f, 0.3f, 0.3f) : Color.white; 
        }

        // 5. Dead State
        if (deadIcon != null) deadIcon.SetActive(isDead);

        // 6. Highlight Current Opponent
        if (backgroundImage != null)
        {
            Color targetColor = new Color(0f, 0f, 0f, 0.5f); // Default
            if (isOpponent) targetColor = new Color(1f, 0.5f, 0f, 0.3f); 
            else if (isPlayer) targetColor = new Color(0f, 1f, 0f, 0.1f); 
            
            backgroundImage.color = targetColor;
        }
    }

    // --- TOOLTIP EVENTS ---
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (TooltipManager.instance != null && !string.IsNullOrEmpty(tooltipContent))
        {
            TooltipManager.instance.ShowSimpleTooltip(tooltipContent, transform.position);
            
            // Juice: Slight scale up on hover
            transform.DOScale(1.05f, 0.1f);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (TooltipManager.instance != null)
        {
            TooltipManager.instance.HideTooltip();
        }
        
        // Restore scale
        transform.DOScale(1f, 0.1f);
    }
    
    void OnDestroy()
    {
        // Safety: Kill tweens if object is destroyed
        transform.DOKill();
        if(healthBarFill != null) healthBarFill.DOKill();
        if(healthFillImage != null) healthFillImage.DOKill();
    }
}