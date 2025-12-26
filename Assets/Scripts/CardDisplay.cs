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
    
    public int damageTaken = 0;

    // Visual State
    private Color originalAttackColor = Color.black; // Default fallback
    private Color originalHealthColor = Color.black;
    private bool colorsInitialized = false;

    // Drag State
    private Transform originalParent;
    private int originalIndex;
    private CanvasGroup canvasGroup;
    private Transform canvasTransform;

    void Start()
    {
        InitializeColors(); // Ensure we capture colors before modifying them
        
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
        InitializeColors(); // Capture colors if LoadUnit is called before Start
        unitData = data;
        
        // Initialize permanent stats from base
        permanentAttack = data.baseAttack;
        permanentHealth = data.baseHealth;
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
        
        // Helper: Calculate what the "Base" is (taking Golden into account)
        int baseAtk = isGolden ? unitData.baseAttack * 2 : unitData.baseAttack;
        int baseHp = isGolden ? unitData.baseHealth * 2 : unitData.baseHealth;

        if (attackText != null) 
        {
            attackText.text = currentAttack.ToString();
            
            // Logic: Green if Buffed (Current > Base), Red if Debuffed (Current < Base), Original otherwise
            if (currentAttack > baseAtk) attackText.color = Color.green;
            else if (currentAttack < baseAtk) attackText.color = Color.red;
            else attackText.color = originalAttackColor; 
        }
        
        if (healthText != null) 
        {
            healthText.text = currentHealth.ToString();
            
            // Logic: 
            // 1. Red if Damaged (Current < Permanent Max) -> Damage takes priority visualization
            // 2. Green if Buffed (Current > Base) AND NOT Damaged
            // 3. Original otherwise
            
            if (currentHealth < permanentHealth) 
            {
                healthText.color = Color.red;
            }
            else if (currentHealth > baseHp) 
            {
                healthText.color = Color.green;
            }
            else 
            {
                healthText.color = originalHealthColor;
            }
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
        
        // Recalc when lifting (removing aura source)
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

            // 1. Sell Button
            if (hitObject.name.Contains("SellButton")) 
            {
                if (isPurchased)
                {
                    GameManager.instance.SellUnit(this);
                    actionHandled = true;
                    break;
                }
            }
            
            // 2. Player Hand (Buying)
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
                    actionHandled = true;
                }
                if (actionHandled) break;
            }

            // 3. Player Board (Playing or Rearranging)
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
                    actionHandled = true;
                }
                if (actionHandled) break;
            }
        }

        if (!actionHandled) ReturnToStart();
        
        // Recalc on drop (applying aura source)
        if (AbilityManager.instance != null) AbilityManager.instance.RecalculateAuras();
    }

    void ReturnToStart()
    {
        transform.SetParent(originalParent);
        transform.SetSiblingIndex(originalIndex);
        if (AbilityManager.instance != null) AbilityManager.instance.RecalculateAuras();
    }

    // --- CLICK LOGIC ---
    // ... inside CardDisplay.cs ...

    // --- CLICK LOGIC ---
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.dragging) return; 

        // NEW: Check Targeting Mode First
        if (GameManager.instance.isTargetingMode)
        {
            GameManager.instance.OnUnitClicked(this);
            return;
        }

        if (GameManager.instance.currentPhase != GameManager.GamePhase.Recruit) return;
        if (GameManager.instance.isUnconscious) return;

        if (!isPurchased)
        {
            // Only buy on Double Click
            if (eventData.clickCount >= 2)
            {
                GameManager.instance.TryBuyToHand(unitData, this);
            }
        }
        else
        {
            // If in Hand, Double Click -> Play to Board
            if (eventData.clickCount >= 2 && transform.parent == GameManager.instance.playerHand)
            {
                GameManager.instance.TryPlayCardToBoard(this);
            }
            // Else just select
            else
            {
                GameManager.instance.SelectUnit(this);
            }
        }
    }
}