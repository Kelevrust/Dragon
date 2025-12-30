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
    
    private CardDisplay currentTarget;

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // 1. Auto-Generate Big Card Visual if missing in Inspector
        if (bigCardVisual == null)
        {
            // We grab the standard card prefab from the GameManager
            if (GameManager.instance != null && GameManager.instance.cardPrefab != null && tooltipRoot != null)
            {
                GameObject obj = Instantiate(GameManager.instance.cardPrefab, tooltipRoot.transform);
                obj.name = "BigCardVisual_Auto";
                
                // Ensure it sits at the start (Left side) of the layout
                obj.transform.SetAsFirstSibling();
                
                bigCardVisual = obj.GetComponent<CardDisplay>();
                
                // Remove interactive components since this is just a visual display
                Destroy(obj.GetComponent<Button>()); 
                
                // NEW: Fix the "Whole Screen" scaling bug
                // We force a Layout Element to dictate the size, ignoring parent stretch settings
                LayoutElement le = obj.GetComponent<LayoutElement>();
                if (le == null) le = obj.AddComponent<LayoutElement>();
                le.minWidth = 200;
                le.minHeight = 300;
                le.preferredWidth = 200;
                le.preferredHeight = 300;
                le.flexibleWidth = 0; // Do not stretch width
                le.flexibleHeight = 0; // Do not stretch height

                Debug.Log("TooltipManager: Auto-generated Big Card Visual with Fixed Size.");
            }
        }

        // 2. Setup Canvas Group to prevent flickering/blocking mouse
        if (tooltipRoot != null)
        {
            CanvasGroup cg = tooltipRoot.GetComponent<CanvasGroup>();
            if (cg == null) cg = tooltipRoot.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false; 
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
        Vector2 mousePos = Vector2.zero;
        if (Mouse.current != null)
        {
            mousePos = Mouse.current.position.ReadValue();
        }

        // Safety check if target disappeared
        if (currentTarget == null) 
        {
            Hide();
            return;
        }

        Vector3 targetPos = currentTarget.transform.position;

        float screenCenterX = Screen.width / 2f;
        bool cardIsOnLeft = targetPos.x < screenCenterX;
        
        float pivotX = cardIsOnLeft ? -0.1f : 1.1f; 
        if (rootRect != null)
        {
            rootRect.pivot = new Vector2(pivotX, 0.5f);
            rootRect.position = targetPos;
        }
    }

    public void Show(CardDisplay sourceCard)
    {
        if (sourceCard == null || sourceCard.unitData == null) return;

        // CRITICAL FIX: Guard against missing Big Card reference
        if (bigCardVisual == null)
        {
            // If auto-generation failed (e.g. GameManager was null in Start), try one last time or warn
            if (GameManager.instance != null && GameManager.instance.cardPrefab != null && tooltipRoot != null)
            {
                 GameObject obj = Instantiate(GameManager.instance.cardPrefab, tooltipRoot.transform);
                 obj.transform.SetAsFirstSibling();
                 bigCardVisual = obj.GetComponent<CardDisplay>();
                 Destroy(obj.GetComponent<Button>());
                 
                 // Apply size fix here too
                 LayoutElement le = obj.GetComponent<LayoutElement>();
                 if (le == null) le = obj.AddComponent<LayoutElement>();
                 le.minWidth = 200;
                 le.minHeight = 300;
                 le.preferredWidth = 200;
                 le.preferredHeight = 300;
            }

            if (bigCardVisual == null)
            {
                Debug.LogError("TooltipManager: 'Big Card Visual' could not be created! Assign it in Inspector.");
                return;
            }
        }

        currentTarget = sourceCard;

        // Copy Data
        bigCardVisual.LoadUnit(sourceCard.unitData);
        
        // Copy Stats
        bigCardVisual.currentAttack = sourceCard.currentAttack;
        bigCardVisual.currentHealth = sourceCard.currentHealth;
        bigCardVisual.permanentAttack = sourceCard.permanentAttack;
        bigCardVisual.permanentHealth = sourceCard.permanentHealth;
        bigCardVisual.isGolden = sourceCard.isGolden;
        
        // Update the visual representation
        bigCardVisual.UpdateVisuals();

        RefreshBuffList(sourceCard);

        if (tooltipRoot != null)
        {
            tooltipRoot.SetActive(true);
            
            // Force layout rebuild to prevent visual glitches
            LayoutRebuilder.ForceRebuildLayoutImmediate(rootRect);
            if (buffContainer != null) 
                LayoutRebuilder.ForceRebuildLayoutImmediate(buffContainer.GetComponent<RectTransform>());
        }

        UpdatePosition(); 
    }

    void RefreshBuffList(CardDisplay source)
    {
        if (buffContainer == null) return;

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
        
        t.textWrappingMode = TextWrappingModes.Normal; 
        t.alignment = TextAlignmentOptions.Left;
    }

    public void Hide()
    {
        currentTarget = null;
        if (tooltipRoot != null) tooltipRoot.SetActive(false);
    }
}