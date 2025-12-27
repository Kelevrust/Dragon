using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.InputSystem; 

public class TooltipManager : MonoBehaviour
{
    public static TooltipManager instance;

    [Header("Visual References")]
    public GameObject tooltipRoot;      
    public CardDisplay bigCardVisual;   
    
    [Header("Buff List (Side Panel)")]
    public GameObject buffContainer;    
    public TMP_Text buffTextPrefab;     
    public Image buffPanelBackground;   

    [Header("Layout")]
    public RectTransform rootRect;      
    
    // NEW: Track the card we are hovering over
    private CardDisplay currentTarget;

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // FIX: Ensure the tooltip UI ignores the mouse to prevent flickering
        if (tooltipRoot != null)
        {
            CanvasGroup cg = tooltipRoot.GetComponent<CanvasGroup>();
            if (cg == null) cg = tooltipRoot.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false; // Mouse passes through to card below
            cg.interactable = false;
        }

        Hide();
    }

    void Update()
    {
        if (tooltipRoot.activeSelf && currentTarget != null)
        {
            UpdatePosition();
        }
    }

    private void UpdatePosition()
    {
        // 1. Get Card Position (Screen Space)
        Vector3 targetPos = currentTarget.transform.position;
        
        // 2. Determine Side (Left vs Right of screen center)
        float screenCenterX = Screen.width / 2f;
        bool cardIsOnLeft = targetPos.x < screenCenterX;

        // 3. Set Pivot to flip direction
        // If card is on Left, Pivot = (0, 0.5) -> Tooltip grows to Right
        // If card is on Right, Pivot = (1, 0.5) -> Tooltip grows to Left
        // We add a slight offset to X so it doesn't overlap the card directly
        float pivotX = cardIsOnLeft ? -0.1f : 1.1f; 
        rootRect.pivot = new Vector2(pivotX, 0.5f);
        
        // 4. Apply Position to the VISUAL root (not just this manager object)
        rootRect.position = targetPos;
    }

    public void Show(CardDisplay sourceCard)
    {
        if (sourceCard == null || sourceCard.unitData == null) return;

        currentTarget = sourceCard;

        // Copy Data
        bigCardVisual.LoadUnit(sourceCard.unitData);
        
        // Copy Stats
        bigCardVisual.currentAttack = sourceCard.currentAttack;
        bigCardVisual.currentHealth = sourceCard.currentHealth;
        bigCardVisual.permanentAttack = sourceCard.permanentAttack;
        bigCardVisual.permanentHealth = sourceCard.permanentHealth;
        bigCardVisual.isGolden = sourceCard.isGolden;
        bigCardVisual.UpdateVisuals();

        // Buffs
        RefreshBuffList(sourceCard);

        tooltipRoot.SetActive(true);
        
        // FORCE LAYOUT REBUILD: Fixes the "Squashed Text" or "Vertical Line" bug
        // Unity UI ContentSizeFitter sometimes lags by one frame unless forced.
        LayoutRebuilder.ForceRebuildLayoutImmediate(rootRect);
        if (buffContainer != null) LayoutRebuilder.ForceRebuildLayoutImmediate(buffContainer.GetComponent<RectTransform>());

        UpdatePosition(); 
    }

    void RefreshBuffList(CardDisplay source)
    {
        foreach (Transform child in buffContainer.transform) Destroy(child.gameObject);

        bool hasBuffs = false;

        if (!string.IsNullOrEmpty(source.unitData.description))
        {
            CreateBuffText(source.unitData.description, Color.white);
            hasBuffs = true;
        }
        
        if (source.isGolden)
        {
            CreateBuffText("Golden: Double Stats", Color.yellow);
            hasBuffs = true;
        }

        // Show/Hide the buff panel based on content
        buffContainer.SetActive(hasBuffs);
        if (buffPanelBackground != null) 
        {
            buffPanelBackground.gameObject.SetActive(hasBuffs);
            buffPanelBackground.enabled = hasBuffs;
        }
    }

    void CreateBuffText(string text, Color color)
    {
        if (buffTextPrefab == null) return;
        TMP_Text t = Instantiate(buffTextPrefab, buffContainer.transform);
        t.text = text;
        t.color = color;
        
        // Ensure text settings are correct for layout
        t.enableWordWrapping = true; // Allow wrapping if too long
        t.alignment = TextAlignmentOptions.Left;
    }

    public void Hide()
    {
        currentTarget = null;
        tooltipRoot.SetActive(false);
    }
}