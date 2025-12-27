using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems; 
using System.Collections.Generic;

public class CardDisplay : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
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
    public bool isPurchased = false; 
    public bool isGolden = false;

    // Stats
    public int permanentAttack;
    public int permanentHealth;
    public int currentAttack;
    public int currentHealth;
    
    // NEW: Runtime Keywords
    public bool hasDivineShield;
    public bool hasReborn;
    // Taunt usually doesn't "break", so we can read it from unitData, 
    // but if you want to be able to give/remove taunt later, add a bool here.

    public int damageTaken = 0;

    private Color originalAttackColor = Color.black; 
    private Color originalHealthColor = Color.black;
    private bool colorsInitialized = false;

    // Drag State
    private Transform originalParent;
    private int originalIndex;
    private CanvasGroup canvasGroup;
    private Transform canvasTransform;

    void Start()
    {
        InitializeColors();
        if (unitData != null && !isGolden) LoadUnit(unitData);
        
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null) canvasTransform = canvas.transform;
    }

    void InitializeColors()
    {
        if (colorsInitialized) return;
        if (attackText != null) originalAttackColor = attackText.color;
        if (healthText != null) originalHealthColor = healthText.color;
        colorsInitialized = true;
    }

    public void LoadUnit(UnitData data)
    {
        InitializeColors();
        unitData = data;
        
        if (!isGolden)
        {
            permanentAttack = data.baseAttack;
            permanentHealth = data.baseHealth;
        }
        
        // Initialize Keywords
        hasDivineShield = data.hasDivineShield;
        hasReborn = data.hasReborn;
        
        damageTaken = 0;
        ResetToPermanent();
        UpdateVisuals();
    }

    public void ResetToPermanent()
    {
        currentAttack = permanentAttack;
        currentHealth = permanentHealth - damageTaken;
    }

    public void MakeGolden()
    {
        isGolden = true;
        permanentAttack = unitData.baseAttack * 2;
        permanentHealth = unitData.baseHealth * 2;
        
        ResetToPermanent();
        UpdateVisuals();
    }

    public void TakeDamage(int amount)
    {
        if (amount > 0 && hasDivineShield)
        {
            hasDivineShield = false;
            // TODO: Play Shield Break Sound/VFX
            UpdateVisuals();
            return; // No damage taken
        }

        damageTaken += amount;
        ResetToPermanent();
        UpdateVisuals();
    }

    public void UpdateVisuals()
    {
        if (unitData == null) return;

        if (nameText != null) nameText.text = isGolden ? "Golden " + unitData.unitName : unitData.unitName;
        if (descriptionText != null) descriptionText.text = unitData.description;
        if (artworkImage != null) artworkImage.sprite = unitData.artwork;
        
        int baseAtk = isGolden ? unitData.baseAttack * 2 : unitData.baseAttack;
        int baseHp = isGolden ? unitData.baseHealth * 2 : unitData.baseHealth;

        if (attackText != null) 
        {
            attackText.text = currentAttack.ToString();
            if (currentAttack > baseAtk) attackText.color = Color.green;
            else if (currentAttack < baseAtk) attackText.color = Color.red;
            else attackText.color = originalAttackColor; 
        }
        
        if (healthText != null) 
        {
            healthText.text = currentHealth.ToString();
            
            // Visual logic for Divine Shield (Yellow Text? Or just an Icon later?)
            if (hasDivineShield) healthText.color = Color.yellow; 
            else if (currentHealth < permanentHealth) healthText.color = Color.red;
            else if (currentHealth > baseHp) healthText.color = Color.green;
            else healthText.color = originalHealthColor;
        }
        
        if (frameImage != null) 
        {
            frameImage.color = isGolden ? new Color(1f, 0.8f, 0.2f) : unitData.frameColor;
        }
    }

    // --- DRAG IMPLEMENTATION ---

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (GameManager.instance.currentPhase != GameManager.GamePhase.Recruit || GameManager.instance.isUnconscious) 
            return;

        originalParent = transform.parent;
        originalIndex = transform.GetSiblingIndex();
        
        if (canvasTransform != null) transform.SetParent(canvasTransform);
        canvasGroup.blocksRaycasts = false;
        
        if (AbilityManager.instance != null) AbilityManager.instance.RecalculateAuras();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (GameManager.instance.currentPhase != GameManager.GamePhase.Recruit || GameManager.instance.isUnconscious) 
            return;
            
        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;

        if (GameManager.instance.currentPhase != GameManager.GamePhase.Recruit || GameManager.instance.isUnconscious) 
        {
            ReturnToStart();
            return;
        }

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        bool actionHandled = false;

        foreach (var result in results)
        {
            GameObject hitObject = result.gameObject;

            if (hitObject.name.Contains("SellButton")) 
            {
                if (isPurchased)
                {
                    GameManager.instance.SellUnit(this);
                    actionHandled = true;
                    break;
                }
            }
            
            bool hitHand = hitObject.name == "PlayerHand" || (hitObject.transform.parent != null && hitObject.transform.parent.name == "PlayerHand");
            if (hitHand)
            {
                if (!isPurchased) 
                {
                    if (GameManager.instance.TryBuyToHand(unitData, this)) actionHandled = true;
                }
                else 
                {
                    transform.SetParent(GameManager.instance.playerHand);
                    GameManager.instance.LogAction($"Reordered Hand: {unitData.unitName}");
                    actionHandled = true;
                }
                if (actionHandled) break;
            }

            bool hitBoard = hitObject.name == "PlayerBoard" || (hitObject.transform.parent != null && hitObject.transform.parent.name == "PlayerBoard");
            if (hitBoard && isPurchased)
            {
                Transform boardTransform = GameManager.instance.playerBoard;

                if (originalParent == GameManager.instance.playerHand)
                {
                    if (GameManager.instance.TryPlayCardToBoard(this)) actionHandled = true;
                }
                else if (originalParent == boardTransform)
                {
                    transform.SetParent(boardTransform);
                    int newIndex = 0;
                    foreach(Transform child in boardTransform)
                    {
                        if (child == transform) continue; 
                        if (transform.position.x > child.position.x) newIndex++;
                    }
                    transform.SetSiblingIndex(newIndex);
                    GameManager.instance.LogAction($"Reordered Board: {unitData.unitName} to pos {newIndex}");
                    actionHandled = true;
                }
                if (actionHandled) break;
            }
        }

        if (!actionHandled) ReturnToStart();
        
        if (AbilityManager.instance != null) AbilityManager.instance.RecalculateAuras();
    }

    void ReturnToStart()
    {
        transform.SetParent(originalParent);
        transform.SetSiblingIndex(originalIndex);
        if (AbilityManager.instance != null) AbilityManager.instance.RecalculateAuras();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.dragging) return; 

        if (GameManager.instance.isTargetingMode)
        {
            GameManager.instance.OnUnitClicked(this);
            return;
        }

        if (GameManager.instance.currentPhase != GameManager.GamePhase.Recruit) return;
        if (GameManager.instance.isUnconscious) return;

        if (!isPurchased)
        {
            if (eventData.clickCount >= 2)
            {
                GameManager.instance.TryBuyToHand(unitData, this);
            }
        }
        else
        {
            if (eventData.clickCount >= 2 && transform.parent == GameManager.instance.playerHand)
            {
                GameManager.instance.TryPlayCardToBoard(this);
            }
            else
            {
                GameManager.instance.SelectUnit(this);
            }
        }
    }
}