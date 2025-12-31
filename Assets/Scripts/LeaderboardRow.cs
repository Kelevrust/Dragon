using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.EventSystems; // Required for Tooltip events

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
    public Color opponentColor = new Color(1f, 0.5f, 0f); 
    public Color deadColor = Color.gray;
    
    [Header("Animation")]
    public float fillSpeed = 5.0f; 

    private Coroutine fillCoroutine;
    private string tooltipContent; // Stores the Tribe data

    public void Setup(string name, int health, int maxHealth, bool isDead, bool isPlayer, bool isOpponent, Sprite portrait, string tooltipData)
    {
        tooltipContent = tooltipData; // Store for mouseover

        // 1. Text
        if (nameText != null)
        {
            nameText.text = name;
            nameText.color = isDead ? deadColor : (isPlayer ? playerColor : Color.white);
        }

        // 2. Health Bar (Animated)
        if (healthBarFill != null)
        {
            float targetAmount = (float)health / maxHealth;
            
            if (healthBarFill.fillAmount == 0 && health > 0) 
            {
                 healthBarFill.fillAmount = targetAmount;
            }
            else
            {
                if (fillCoroutine != null) StopCoroutine(fillCoroutine);
                fillCoroutine = StartCoroutine(AnimateFill(targetAmount));
            }
        }

        // 3. Health Color
        Image targetColorImg = healthFillImage != null ? healthFillImage : healthBarFill;
        if (targetColorImg != null && !isDead)
        {
            float hpPercent = (float)health / maxHealth;
            targetColorImg.color = Color.Lerp(Color.red, Color.green, hpPercent);
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
            if (isOpponent) backgroundImage.color = new Color(1f, 0.5f, 0f, 0.3f); 
            else if (isPlayer) backgroundImage.color = new Color(0f, 1f, 0f, 0.1f); 
            else backgroundImage.color = new Color(0f, 0f, 0f, 0.5f); 
        }
    }

    IEnumerator AnimateFill(float targetAmount)
    {
        float current = healthBarFill.fillAmount;
        float t = 0f;
        while (Mathf.Abs(current - targetAmount) > 0.01f)
        {
            t += Time.deltaTime * fillSpeed;
            healthBarFill.fillAmount = Mathf.Lerp(current, targetAmount, t);
            yield return null;
        }
        healthBarFill.fillAmount = targetAmount;
    }

    // --- TOOLTIP EVENTS ---
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (TooltipManager.instance != null && !string.IsNullOrEmpty(tooltipContent))
        {
            // Use existing Unit Tooltip logic but repurpose it? 
            // Or create a simple text tooltip method in TooltipManager.
            // For now, let's assume TooltipManager has a ShowSimpleText method.
            // If not, we should probably add one or just use the console for now.
            // *Assuming you have a simple tooltip method, if not I'll add one below*
            TooltipManager.instance.ShowSimpleTooltip(tooltipContent, transform.position);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (TooltipManager.instance != null)
        {
            TooltipManager.instance.HideTooltip();
        }
    }
}