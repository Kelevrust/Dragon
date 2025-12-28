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

        // FIX: Define targetPos BEFORE checking if it's on the left
        Vector3 targetPos = currentTarget.transform.position;

        float screenCenterX = Screen.width / 2f;
        bool cardIsOnLeft = targetPos.x < screenCenterX;
        
        float pivotX = cardIsOnLeft ? -0.1f : 1.1f; 
        rootRect.pivot = new Vector2(pivotX, 0.5f);
        rootRect.position = targetPos;
    }

    public void Show(CardDisplay sourceCard)
    {
        if (sourceCard == null || sourceCard.unitData == null) return;

        currentTarget = sourceCard;

        bigCardVisual.LoadUnit(sourceCard.unitData);
        bigCardVisual.currentAttack = sourceCard.currentAttack;
        bigCardVisual.currentHealth = sourceCard.currentHealth;
        bigCardVisual.permanentAttack = sourceCard.permanentAttack;
        bigCardVisual.permanentHealth = sourceCard.permanentHealth;
        bigCardVisual.isGolden = sourceCard.isGolden;
        bigCardVisual.UpdateVisuals();

        RefreshBuffList(sourceCard);

        tooltipRoot.SetActive(true);
        
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
        tooltipRoot.SetActive(false);
    }
}