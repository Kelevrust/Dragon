using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections; // Required for Coroutines

public class LeaderboardRow : MonoBehaviour
{
    [Header("UI References")]
    public Image portraitImage;
    public TMP_Text nameText;
    
    // CHANGED: Replaced Slider with Image for smoother "Filled" animation
    public Image healthBarFill; 
    
    public Image healthFillImage; // Optional: If you want to tint a separate image (or just use healthBarFill)
    public Image backgroundImage; 
    public GameObject deadIcon;   

    [Header("Settings")]
    public Color playerColor = Color.green;
    public Color enemyColor = Color.white;
    public Color opponentColor = new Color(1f, 0.5f, 0f); // Orange
    public Color deadColor = Color.gray;
    
    [Header("Animation")]
    public float fillSpeed = 5.0f; // How fast the bar drains

    private Coroutine fillCoroutine;

    public void Setup(string name, int health, int maxHealth, bool isDead, bool isPlayer, bool isOpponent, Sprite portrait)
    {
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
            
            // If this is the first initialization (e.g. object just created), set instantly
            // Otherwise, animate to the new value
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

        // 3. Health Color (Green -> Red gradient)
        // If you didn't assign a separate "Fill Image", we color the bar itself
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
            // Lerp gives it that nice "slow down as it gets closer" feel
            healthBarFill.fillAmount = Mathf.Lerp(current, targetAmount, t);
            yield return null;
        }
        
        healthBarFill.fillAmount = targetAmount;
    }
}