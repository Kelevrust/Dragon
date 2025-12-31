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
    public float maxTextWidth = 250f; // NEW: Constraint for text width
    
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
            if (GameManager.instance != null && GameManager.instance.cardPrefab != null && tooltipRoot != null)
            {
                GameObject obj = Instantiate(GameManager.instance.cardPrefab, tooltipRoot.transform);
                obj.name = "BigCardVisual_Auto";
                obj.transform.SetAsFirstSibling();
                
                bigCardVisual = obj.GetComponent<CardDisplay>();
                Destroy(obj.GetComponent<Button>()); 
                
                LayoutElement le = obj.GetComponent<LayoutElement>();
                if (le == null) le = obj.AddComponent<LayoutElement>();
                le.minWidth = 200;
                le.minHeight = 300;
                le.preferredWidth = 200;
                le.preferredHeight = 300;
                le.flexibleWidth = 0; 
                le.flexibleHeight = 0;
            }
        }

        // 2. Setup Canvas Group
        if (tooltipRoot != null)
        {
            CanvasGroup cg = tooltipRoot.GetComponent<CanvasGroup>();
            if (cg == null) cg = tooltipRoot.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false; 
            cg.interactable = false;
            
            ContentSizeFitter csf = tooltipRoot.GetComponent<ContentSizeFitter>();
            if (csf == null) csf = tooltipRoot.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
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
        // Safety check if target disappeared
        if (currentTarget == null || !currentTarget.gameObject.activeInHierarchy) 
        {
            Hide();
            return;
        }

        // 1. Get Target Position
        Vector3 targetPos = currentTarget.transform.position;

        // 2. FIX: Override Z to match the Canvas Plane to prevent zoom/scale artifacts
        if (tooltipRoot.transform.parent != null)
        {
            targetPos.z = tooltipRoot.transform.parent.position.z;
        }

        // 3. Determine Side
        // Convert to Screen point to check left/right accurately
        Vector3 screenPos = Camera.main != null ? Camera.main.WorldToScreenPoint(targetPos) : targetPos;
        float screenCenterX = Screen.width / 2f;
        bool cardIsOnLeft = screenPos.x < screenCenterX;
        
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

        if (bigCardVisual == null) return;

        currentTarget = sourceCard;
        
        // Restore Card Mode (Show Image, Stats)
        SetVisualMode(true);

        bigCardVisual.LoadUnit(sourceCard.unitData);
        bigCardVisual.currentAttack = sourceCard.currentAttack;
        bigCardVisual.currentHealth = sourceCard.currentHealth;
        bigCardVisual.permanentAttack = sourceCard.permanentAttack;
        bigCardVisual.permanentHealth = sourceCard.permanentHealth;
        bigCardVisual.isGolden = sourceCard.isGolden;
        bigCardVisual.UpdateVisuals();

        RefreshBuffList(sourceCard);

        if (tooltipRoot != null)
        {
            tooltipRoot.SetActive(true);
            tooltipRoot.transform.localScale = Vector3.one;

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

        // 1. Manual Description
        if (!string.IsNullOrEmpty(source.unitData.description))
        {
            CreateBuffText(source.unitData.description, Color.white);
            hasBuffs = true;
        }
        
        // 2. NEW: Auto-Generated Mechanics Text (The self-documenting code!)
        // CardDisplay generates this in UpdateVisuals(), usually into its own text box.
        // We grab it here to show in the tooltip side panel.
        if (source.mechanicsText != null && !string.IsNullOrEmpty(source.mechanicsText.text))
        {
            // Avoid duplicate text if description matches mechanics
            if (source.mechanicsText.text != source.unitData.description)
            {
                CreateBuffText(source.mechanicsText.text, new Color(0.8f, 0.8f, 1f)); // Light Blue
                hasBuffs = true;
            }
        }

        // 3. Golden Status
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

        // FIX: Add Layout Element to enforce width wrapping
        LayoutElement le = t.GetComponent<LayoutElement>();
        if (le == null) le = t.gameObject.AddComponent<LayoutElement>();
        le.preferredWidth = maxTextWidth; // Constraint width
        le.flexibleWidth = 0;
    }

    public void Hide()
    {
        currentTarget = null;
        if (tooltipRoot != null) tooltipRoot.SetActive(false);
    }
    
    // --- LEADERBOARD TOOLTIP SUPPORT ---

    public void ShowSimpleTooltip(string content, Vector2 position)
    {
        if (tooltipRoot == null || bigCardVisual == null) return;
        
        // Stop tracking units
        currentTarget = null;
        
        tooltipRoot.SetActive(true);
        tooltipRoot.transform.localScale = Vector3.one;

        // Hide visuals to look like a text box
        SetVisualMode(false);

        // Set Text
        if (bigCardVisual.nameText != null) bigCardVisual.nameText.text = "Player Info";
        if (bigCardVisual.descriptionText != null) bigCardVisual.descriptionText.text = content;

        // Position manually since we aren't tracking a Transform
        if (rootRect != null)
        {
            rootRect.position = position;
            
            // Pivot logic based on screen side
            float screenCenterX = Screen.width / 2f;
            bool onLeft = position.x < screenCenterX;
            rootRect.pivot = new Vector2(onLeft ? -0.1f : 1.1f, 0.5f);
            
            LayoutRebuilder.ForceRebuildLayoutImmediate(rootRect);
        }

        // Hide Side Panel
        if (buffContainer != null) buffContainer.SetActive(false);
        if (buffPanelBackground != null) buffPanelBackground.gameObject.SetActive(false);
    }

    public void HideTooltip()
    {
        Hide();
    }

    void SetVisualMode(bool isCard)
    {
        if (bigCardVisual == null) return;
        
        // Toggle Image
        if (bigCardVisual.artworkImage != null) bigCardVisual.artworkImage.gameObject.SetActive(isCard);
        
        // Toggle Stats (Parent usually holds the icon background)
        if (bigCardVisual.attackText != null && bigCardVisual.attackText.transform.parent != null) 
            bigCardVisual.attackText.transform.parent.gameObject.SetActive(isCard);
            
        if (bigCardVisual.healthText != null && bigCardVisual.healthText.transform.parent != null) 
            bigCardVisual.healthText.transform.parent.gameObject.SetActive(isCard);
            
        if (bigCardVisual.tribeBanner != null) 
            bigCardVisual.tribeBanner.gameObject.SetActive(isCard);
    }
}