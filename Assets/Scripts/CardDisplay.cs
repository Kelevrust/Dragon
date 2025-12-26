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

    public int currentAttack;
    public int currentHealth;

    // Drag State
    private Transform originalParent;
    private int originalIndex;
    private CanvasGroup canvasGroup;
    private Transform canvasTransform;

    void Start()
    {
        if (unitData != null && !isGolden) LoadUnit(unitData);

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null) canvasTransform = canvas.transform;
    }

    public void LoadUnit(UnitData data)
    {
        unitData = data;
        currentAttack = data.baseAttack;
        currentHealth = data.baseHealth;
        UpdateVisuals();
    }

    public void MakeGolden()
    {
        isGolden = true;
        currentAttack = unitData.baseAttack * 2;
        currentHealth = unitData.baseHealth * 2;
        UpdateVisuals();
    }

    public void UpdateVisuals()
    {
        if (nameText != null) nameText.text = isGolden ? "Golden " + unitData.unitName : unitData.unitName;

        // FIXED: Changed from abilityDescription to description to match new UnitData structure
        if (descriptionText != null) descriptionText.text = unitData.description;

        if (artworkImage != null) artworkImage.sprite = unitData.artwork;

        if (attackText != null)
        {
            attackText.text = currentAttack.ToString();
            attackText.color = isGolden ? Color.green : Color.black;
        }

        if (healthText != null)
        {
            healthText.text = currentHealth.ToString();
            healthText.color = isGolden ? Color.green : Color.black;
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

            // Case 1: Sell Button
            if (hitObject.name.Contains("SellButton"))
            {
                if (isPurchased)
                {
                    GameManager.instance.SellUnit(this);
                    actionHandled = true;
                    break;
                }
            }

            // Case 2: Player Board (Buying OR Reordering)
            bool hitBoard = hitObject.name == "PlayerBoard";
            if (!hitBoard && hitObject.transform.parent != null)
            {
                hitBoard = hitObject.transform.parent.name == "PlayerBoard";
            }

            if (hitBoard)
            {
                Transform boardTransform = GameManager.instance.playerBoard;

                // A. Buying from Shop
                if (!isPurchased) 
                {
                    if (GameManager.instance.gold >= unitData.cost)
                    {
                        if (GameManager.instance.TrySpendGold(unitData.cost))
                        {
                            transform.SetParent(boardTransform);
                            isPurchased = true;
                            
                            // NEW: Trigger Battlecry on Drag Buy
                            if (AbilityManager.instance != null)
                                AbilityManager.instance.TriggerAbilities(AbilityTrigger.OnPlay, this);

                            GameManager.instance.CheckForTriples(unitData);
                            actionHandled = true;
                        }
                    }
                }
                // B. Reordering Board
                else
                {
                    transform.SetParent(boardTransform);
                    int newIndex = 0;
                    foreach (Transform child in boardTransform)
                    {
                        if (child == transform) continue;
                        if (transform.position.x > child.position.x) newIndex++;
                    }
                    transform.SetSiblingIndex(newIndex);
                    actionHandled = true;
                }
                break;
            }
        }

        if (!actionHandled) ReturnToStart();
    }

    void ReturnToStart()
    {
        transform.SetParent(originalParent);
        transform.SetSiblingIndex(originalIndex);
    }

    // --- CLICK LOGIC ---
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.dragging) return;

        if (GameManager.instance.currentPhase != GameManager.GamePhase.Recruit) return;
        if (GameManager.instance.isUnconscious) return;

        if (!isPurchased)
        {
            // Only buy on Double Click
            if (eventData.clickCount >= 2)
            {
                int cost = unitData.cost;
                if (GameManager.instance.gold >= cost)
                {
                    if (GameManager.instance.TrySpendGold(cost))
                    {
                        GameObject playerBoard = GameObject.Find("PlayerBoard");
                        if (playerBoard != null)
                        {
                            transform.SetParent(playerBoard.transform);
                            isPurchased = true; 
                            
                            // NEW: Trigger Battlecry on Click Buy
                            if (AbilityManager.instance != null)
                                AbilityManager.instance.TriggerAbilities(AbilityTrigger.OnPlay, this);

                            GameManager.instance.CheckForTriples(unitData);
                        }
                    }
                }
            }
        }
        else
        {
            // Single click selects owned units
            GameManager.instance.SelectUnit(this);
        }
    }
}