using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Real-time coaching hints that appear during gameplay
/// Shows when player makes poor decisions (based on CoachingSettings)
/// </summary>
public class CoachingHintUI : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject hintPanel;
    public TMP_Text hintTitleText;
    public TMP_Text hintDescriptionText;
    public TMP_Text alternativesText;
    public Button dismissButton;
    public Button continueButton;

    [Header("Display Settings")]
    public float autoHideDuration = 5f;
    public Color criticalColor = Color.red;
    public Color poorColor = new Color(1f, 0.5f, 0f); // Orange
    public Color questionableColor = Color.yellow;

    private Coroutine autoHideCoroutine;
    private bool isPaused = false;

    void Awake()
    {
        if (hintPanel != null) hintPanel.SetActive(false);

        if (dismissButton != null)
            dismissButton.onClick.AddListener(DismissHint);

        if (continueButton != null)
            continueButton.onClick.AddListener(ContinueFromPause);
    }

    public void ShowHint(DecisionEvaluation evaluation, bool pauseGame)
    {
        if (hintPanel == null) return;

        // Stop any existing auto-hide
        if (autoHideCoroutine != null)
        {
            StopCoroutine(autoHideCoroutine);
            autoHideCoroutine = null;
        }

        // Set title based on quality
        if (hintTitleText != null)
        {
            hintTitleText.text = GetHintTitle(evaluation.quality);
            hintTitleText.color = GetQualityColor(evaluation.quality);
        }

        // Set description
        if (hintDescriptionText != null)
        {
            hintDescriptionText.text = $"<b>{evaluation.actionTaken}</b>\n\n{evaluation.reasoning}";
        }

        // Set alternatives
        if (alternativesText != null && evaluation.alternatives != null && evaluation.alternatives.Count > 0)
        {
            string altText = "<b>Better alternatives:</b>\n";
            foreach (var alt in evaluation.alternatives)
            {
                altText += $"• {alt}\n";
            }
            alternativesText.text = altText;
        }
        else if (alternativesText != null)
        {
            alternativesText.text = "";
        }

        // Show appropriate buttons
        if (pauseGame)
        {
            // Pause the game
            Time.timeScale = 0f;
            isPaused = true;

            if (dismissButton != null) dismissButton.gameObject.SetActive(false);
            if (continueButton != null) continueButton.gameObject.SetActive(true);
        }
        else
        {
            // Just show hint, auto-hide after duration
            isPaused = false;

            if (dismissButton != null) dismissButton.gameObject.SetActive(true);
            if (continueButton != null) continueButton.gameObject.SetActive(false);

            autoHideCoroutine = StartCoroutine(AutoHideAfterDelay());
        }

        hintPanel.SetActive(true);
    }

    private IEnumerator AutoHideAfterDelay()
    {
        yield return new WaitForSeconds(autoHideDuration);
        DismissHint();
    }

    public void DismissHint()
    {
        if (autoHideCoroutine != null)
        {
            StopCoroutine(autoHideCoroutine);
            autoHideCoroutine = null;
        }

        if (hintPanel != null)
            hintPanel.SetActive(false);
    }

    private void ContinueFromPause()
    {
        if (isPaused)
        {
            Time.timeScale = 1f;
            isPaused = false;
        }

        DismissHint();
    }

    private string GetHintTitle(PlayQuality quality)
    {
        switch (quality)
        {
            case PlayQuality.Critical:
                return "CRITICAL MISTAKE!";
            case PlayQuality.Poor:
                return "Poor Decision";
            case PlayQuality.Questionable:
                return "Questionable Play";
            default:
                return "Coaching Tip";
        }
    }

    private Color GetQualityColor(PlayQuality quality)
    {
        switch (quality)
        {
            case PlayQuality.Critical:
                return criticalColor;
            case PlayQuality.Poor:
                return poorColor;
            case PlayQuality.Questionable:
                return questionableColor;
            default:
                return Color.white;
        }
    }

    void OnDestroy()
    {
        // Ensure game is unpaused if this UI is destroyed
        if (isPaused)
        {
            Time.timeScale = 1f;
        }
    }
}
