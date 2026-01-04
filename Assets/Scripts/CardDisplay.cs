using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems; 
using System.Collections.Generic;
using System.Text; 
using DG.Tweening; 

public class CardDisplay : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    public UnitData unitData;
    
    // Runtime modification support
    public List<AbilityData> runtimeAbilities = new List<AbilityData>();

    [Header("UI References")]
    public Image artworkImage;
    public TMP_Text nameText;
    public TMP_Text descriptionText; 
    
    public Image tribeBanner;        
    public TMP_Text tribeText;       
    
    public TMP_Text mechanicsText; 

    public TMP_Text attackText;
    public TMP_Text healthText;
    public Image frameImage;
    public Image goldenBorderImage; 

    [Header("State")]
    public bool isPurchased = false; 
    public bool isGolden = false;

    public int permanentAttack;
    public int permanentHealth;
    public int currentAttack;
    public int currentHealth;
    
    public int damageTaken = 0;
    
    [Header("Current Keywords")]
    public bool hasDivineShield;
    public bool hasReborn;
    public bool hasTaunt;
    public bool hasStealth;
    public bool hasPoison;
    public bool hasVenomous;

    [Header("Permanent Keywords (Base State)")]
    public bool permDivineShield;
    public bool permReborn;
    public bool permTaunt;
    public bool permStealth;
    public bool permPoison;
    public bool permVenomous;

    private Color originalAttackColor = Color.black; 
    private Color originalHealthColor = Color.black;
    private bool colorsInitialized = false;

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
    
    void OnDestroy()
    {
        transform.DOKill();
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
        
        // Clone abilities to local list for runtime modification
        runtimeAbilities.Clear();
        if (data.abilities != null)
        {
            runtimeAbilities.AddRange(data.abilities);
        }
        
        // Initialize Base Stats
        if (!isGolden)
        {
            permanentAttack = data.baseAttack;
            permanentHealth = data.baseHealth;
        }
        
        // Initialize Permanent Keywords from Data
        permDivineShield = data.hasDivineShield;
        permReborn = data.hasReborn;
        permTaunt = data.hasTaunt;
        // Assume default false for others until added to UnitData, or modify UnitData similarly
        permStealth = false; 
        permPoison = false;
        permVenomous = false;

        damageTaken = 0;
        ResetToPermanent();
        UpdateVisuals();
    }

    public void ResetToPermanent()
    {
        currentAttack = permanentAttack;
        currentHealth = permanentHealth - damageTaken;

        // Reset Keywords to their permanent state (removes temporary combat buffs)
        hasDivineShield = permDivineShield;
        hasReborn = permReborn;
        hasTaunt = permTaunt;
        hasStealth = permStealth;
        hasPoison = permPoison;
        hasVenomous = permVenomous;
    }

    public void MakeGolden()
    {
        isGolden = true;
        permanentAttack = unitData.baseAttack * 2;
        permanentHealth = unitData.baseHealth * 2;
        
        transform.DOPunchScale(Vector3.one * 0.2f, 0.5f, 10, 1);
        
        ResetToPermanent();
        UpdateVisuals();
    }
    
    public void AddAbility(AbilityData newAbility)
    {
        if (newAbility != null && !runtimeAbilities.Contains(newAbility))
        {
            runtimeAbilities.Add(newAbility);
            UpdateVisuals(); 
        }
    }

    // NEW: API to grant keywords with persistence logic
    public void GainKeyword(KeywordType keyword, bool isPermanent)
    {
        switch (keyword)
        {
            case KeywordType.DivineShield:
                hasDivineShield = true;
                if (isPermanent) permDivineShield = true;
                break;
            case KeywordType.Reborn:
                hasReborn = true;
                if (isPermanent) permReborn = true;
                break;
            case KeywordType.Taunt:
                hasTaunt = true;
                if (isPermanent) permTaunt = true;
                break;
            case KeywordType.Stealth:
                hasStealth = true;
                if (isPermanent) permStealth = true;
                break;
            case KeywordType.Poison:
                hasPoison = true;
                if (isPermanent) permPoison = true;
                break;
            case KeywordType.Venomous:
                hasVenomous = true;
                if (isPermanent) permVenomous = true;
                break;
        }
        UpdateVisuals();
    }

    public void TakeDamage(int amount)
    {
        if (hasDivineShield && amount > 0)
        {
            hasDivineShield = false;
            transform.DOShakePosition(0.3f, 5f, 20, 90, false, true);
            UpdateVisuals();
            return; 
        }

        damageTaken += amount;
        transform.DOShakePosition(0.2f, 3f, 20, 90, false, true);
        
        // Update stats but keep current keywords (don't full reset)
        currentHealth = permanentHealth - damageTaken;
        UpdateVisuals();
    }
    
    public void BreakShield()
    {
        hasDivineShield = false;
        UpdateVisuals();
    }

    public void UpdateVisuals()
    {
        if (unitData == null) return;

        if (nameText != null) 
        {
            nameText.text = unitData.unitName;
            nameText.color = isGolden ? new Color(1f, 0.8f, 0.2f) : unitData.frameColor;
        }

        if (descriptionText != null) descriptionText.text = unitData.description;
        if (mechanicsText != null) mechanicsText.text = GenerateMechanicsText();
        if (artworkImage != null) artworkImage.sprite = unitData.artwork;
        if (tribeText != null) tribeText.text = unitData.tribe.ToString(); 

        bool hasTribe = unitData.tribe != Tribe.None;
        if (tribeBanner != null) tribeBanner.gameObject.SetActive(hasTribe);
        else if (tribeText != null) tribeText.gameObject.SetActive(hasTribe);

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
            
            if (hasDivineShield) healthText.color = Color.yellow;
            else if (currentHealth < permanentHealth) healthText.color = Color.red;
            else if (currentHealth > baseHp) healthText.color = Color.green;
            else healthText.color = originalHealthColor;
        }
        
        if (frameImage != null) 
        {
            frameImage.color = isGolden ? new Color(1f, 0.8f, 0.2f) : unitData.frameColor;
        }
        
        if (goldenBorderImage != null)
        {
            goldenBorderImage.gameObject.SetActive(isGolden);
        }
    }

    string GenerateMechanicsText()
    {
        StringBuilder sb = new StringBuilder();

        // Use Runtime flags instead of static Data
        if (hasTaunt) sb.Append("<b>Taunt</b>. ");
        if (hasDivineShield) sb.Append("<b>Divine Shield</b>. "); 
        if (hasReborn) sb.Append("<b>Reborn</b>. "); 
        if (hasStealth) sb.Append("<b>Stealth</b>. ");
        if (hasPoison) sb.Append("<b>Poison</b>. ");
        if (hasVenomous) sb.Append("<b>Venomous</b>. ");

        if (sb.Length > 0) sb.Append("\n");

        if (runtimeAbilities != null)
        {
            foreach (AbilityData ability in runtimeAbilities)
            {
                if (ability == null) continue;

                switch (ability.triggerType)
                {
                    case AbilityTrigger.OnPlay: sb.Append("<b>Battlecry:</b> "); break;
                    case AbilityTrigger.OnDeath: sb.Append("<b>Deathrattle:</b> "); break;
                    case AbilityTrigger.PassiveAura: sb.Append("<b>Passive:</b> "); break;
                    case AbilityTrigger.OnAllyDeath: sb.Append("<b>Scavenge:</b> "); break;
                    case AbilityTrigger.OnTurnStart: sb.Append("<b>Start of Turn:</b> "); break;
                    case AbilityTrigger.OnDamageTaken: sb.Append("<b>Enrage:</b> "); break;
                    case AbilityTrigger.OnAllyPlay: 
                        string tribe = ability.targetTribe != Tribe.None ? ability.targetTribe.ToString() : "Ally";
                        sb.Append($"<b>Synergy ({tribe}):</b> "); 
                        break;
                    case AbilityTrigger.OnAttack: sb.Append("<b>On Attack:</b> "); break;
                    case AbilityTrigger.OnDealDamage: sb.Append("<b>On Hit:</b> "); break;
                    case AbilityTrigger.OnCombatStart: sb.Append("<b>Start of Combat:</b> "); break;
                }

                switch (ability.effectType)
                {
                    case AbilityEffect.BuffStats:
                        int x = isGolden ? ability.valueX * 2 : ability.valueX;
                        int y = isGolden ? ability.valueY * 2 : ability.valueY;
                        string targetStr = GetTargetString(ability.targetType);
                        sb.Append($"Give {targetStr} +{x}/+{y}.");
                        break;
                    
                    case AbilityEffect.SummonUnit:
                        string tokenName = ability.tokenUnit != null ? ability.tokenUnit.unitName : "Unit";
                        sb.Append($"Summon a {tokenName}.");
                        break;
                    
                    case AbilityEffect.GainGold:
                        int gold = isGolden ? ability.valueX * 2 : ability.valueX;
                        sb.Append($"Gain {gold} Gold.");
                        break;
                    
                    case AbilityEffect.HealHero:
                        int heal = isGolden ? ability.valueX * 2 : ability.valueX;
                        sb.Append($"Restore {heal} Health to Hero.");
                        break;

                    case AbilityEffect.GrantAbility:
                        string abilityName = ability.abilityToGrant != null ? ability.abilityToGrant.name : "Ability";
                        sb.Append($"Give {GetTargetString(ability.targetType)} '{abilityName}'.");
                        break;
                        
                    case AbilityEffect.Magnetize:
                        sb.Append("<b>Magnetize</b> (Merge with Mech).");
                        break;

                    case AbilityEffect.GiveKeyword:
                        sb.Append($"Give {GetTargetString(ability.targetType)} <b>{ability.keywordToGive}</b>.");
                        break;
                }
                sb.Append("\n");
            }
        }

        return sb.ToString();
    }

    string GetTargetString(AbilityTarget target)
    {
        switch (target)
        {
            case AbilityTarget.Self: return "self";
            case AbilityTarget.AllFriendly: return "all allies";
            case AbilityTarget.RandomFriendly: return "a random ally";
            case AbilityTarget.AdjacentFriendly: return "adjacent allies";
            case AbilityTarget.AllFriendlyTribe: return "all friendly Tribe"; 
            case AbilityTarget.RandomFriendlyTribe: return "a random friendly Tribe";
            default: return "target";
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (TooltipManager.instance != null)
        {
            TooltipManager.instance.Show(this);
        }

        if (!eventData.dragging)
        {
            transform.DOScale(1.1f, 0.2f).SetEase(Ease.OutBack);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (TooltipManager.instance != null)
        {
            TooltipManager.instance.Hide();
        }

        transform.DOScale(1f, 0.2f);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (GameManager.instance.currentPhase != GameManager.GamePhase.Recruit || GameManager.instance.isUnconscious) 
            return;

        transform.DOScale(0.9f, 0.1f);

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
            
        if (canvasTransform != null)
        {
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)canvasTransform, 
                eventData.position, 
                GetComponentInParent<Canvas>().worldCamera, 
                out localPoint
            );
            transform.position = canvasTransform.TransformPoint(localPoint);
        }
        else
        {
            transform.position = eventData.position;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;
        transform.DOScale(1f, 0.2f);

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

            if (hitObject.name.Contains("SellButton") || hitObject.name.Contains("SellZone")) 
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
            if (hitBoard)
            {
                int newIndex = 0;
                foreach(Transform child in GameManager.instance.playerBoard)
                {
                    if (child == transform) continue; 
                    if (transform.position.x > child.position.x) newIndex++;
                }

                if (!isPurchased)
                {
                    if (GameManager.instance.TryBuyToHand(unitData, this)) 
                    {
                        actionHandled = true;
                    }
                }
                else if (originalParent == GameManager.instance.playerHand)
                {
                    if (GameManager.instance.TryPlayCardToBoard(this, newIndex)) 
                    {
                        actionHandled = true;
                    }
                }
                else if (originalParent == GameManager.instance.playerBoard)
                {
                    transform.SetParent(GameManager.instance.playerBoard);
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
        transform.DOMove(originalParent.GetChild(originalIndex).position, 0.2f).OnComplete(() => {
             transform.SetParent(originalParent);
             transform.SetSiblingIndex(originalIndex);
             if (AbilityManager.instance != null) AbilityManager.instance.RecalculateAuras();
        });
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.dragging) return; 

        transform.DOPunchScale(new Vector3(0.1f, 0.1f, 0), 0.1f);

        if (GameManager.instance.isTargetingMode)
        {
            GameManager.instance.OnUnitClicked(this);
            return;
        }

        if (GameManager.instance.currentPhase != GameManager.GamePhase.Recruit || GameManager.instance.isUnconscious) return;

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