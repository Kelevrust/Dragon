using UnityEngine;
using System.Collections.Generic;

public class AbilityManager : MonoBehaviour
{
    public static AbilityManager instance;

    void Awake() { instance = this; }

    public void TriggerAbilities(AbilityTrigger trigger, CardDisplay sourceCard)
    {
        // Safety Checks
        if (sourceCard == null) return;
        if (sourceCard.unitData == null) return;
        if (sourceCard.unitData.abilities == null) return;

        foreach (AbilityData ability in sourceCard.unitData.abilities)
        {
            if (ability != null && ability.triggerType == trigger)
            {
                ExecuteAbility(ability, sourceCard);
            }
        }
    }

    public void RecalculateAuras()
    {
        // 1. Find all active cards
        // Note: This might pick up units about to be destroyed, so we need careful checks
        CardDisplay[] allCards = FindObjectsByType<CardDisplay>(FindObjectsSortMode.None);

        // 2. Reset everyone
        foreach (CardDisplay card in allCards)
        {
            if (card == null) continue;
            card.ResetToPermanent();
        }

        // 3. Apply Aura Effects
        foreach (CardDisplay source in allCards)
        {
            // Skip invalid sources
            if (source == null) continue;
            if (!source.gameObject.activeInHierarchy) continue; // Ignore disabled/ghost units
            if (!source.isPurchased) continue; 
            if (source.unitData == null || source.unitData.abilities == null) continue;

            foreach (AbilityData ability in source.unitData.abilities)
            {
                // CRITICAL FIX: Check if ability asset is null (empty slot in Inspector)
                if (ability == null) continue;

                if (ability.triggerType == AbilityTrigger.PassiveAura)
                {
                    ExecuteAbility(ability, source);
                }
            }
        }

        // 4. Update UI
        foreach (CardDisplay card in allCards)
        {
            if (card != null) card.UpdateVisuals();
        }
    }

    void ExecuteAbility(AbilityData ability, CardDisplay source)
    {
        if (ability == null || source == null) return;

        List<CardDisplay> targets = FindTargets(ability, source);

        if (targets.Count == 0 && (ability.targetType == AbilityTarget.Self || ability.effectType == AbilityEffect.SummonUnit))
        {
            ApplyEffect(ability, source, source); 
        }
        else
        {
            foreach (CardDisplay target in targets)
            {
                ApplyEffect(ability, target, source);
            }
        }
    }

    void ApplyEffect(AbilityData ability, CardDisplay target, CardDisplay source)
    {
        if (ability == null) return;

        switch (ability.effectType)
        {
            case AbilityEffect.BuffStats:
                if (target != null)
                {
                    int atk = ability.valueX;
                    int hp = ability.valueY;

                    if (source.isGolden && ability.triggerType == AbilityTrigger.PassiveAura)
                    {
                        atk *= 2;
                        hp *= 2;
                    }

                    if (ability.triggerType == AbilityTrigger.PassiveAura)
                    {
                        target.currentAttack += atk;
                        target.currentHealth += hp;
                    }
                    else
                    {
                        target.permanentAttack += atk;
                        target.permanentHealth += hp;
                        
                        target.currentAttack += atk;
                        target.currentHealth += hp;
                        target.UpdateVisuals();
                    }
                }
                break;

            case AbilityEffect.SummonUnit:
                if (ability.tokenUnit == null)
                {
                    Debug.LogWarning($"Ability '{ability.name}' on '{source.name}' has no Token Unit assigned.");
                    return;
                }
                if (source.transform.parent == null) return;
                
                if (GameManager.instance != null)
                {
                    GameManager.instance.SpawnToken(ability.tokenUnit, source.transform.parent);
                    RecalculateAuras();
                }
                break;

            case AbilityEffect.GainGold:
                if (GameManager.instance != null)
                {
                    GameManager.instance.gold += ability.valueX;
                    GameManager.instance.UpdateUI();
                }
                break;
                
            case AbilityEffect.HealHero:
                if (GameManager.instance != null)
                {
                    GameManager.instance.ModifyHealth(ability.valueX);
                }
                break;
        }
    }

    List<CardDisplay> FindTargets(AbilityData ability, CardDisplay source)
    {
        List<CardDisplay> targets = new List<CardDisplay>();
        if (source == null || source.transform.parent == null) return targets;
        
        Transform board = source.transform.parent;
        
        // 1. Gather ALL units on the board (Including Self)
        List<CardDisplay> allies = new List<CardDisplay>();
        foreach(Transform child in board)
        {
            CardDisplay cd = child.GetComponent<CardDisplay>();
            // Changed: Removed "&& cd != source" so "AllFriendly" includes the caster
            if(cd != null && cd.gameObject.activeInHierarchy) allies.Add(cd);
        }

        switch (ability.targetType)
        {
            case AbilityTarget.Self:
                targets.Add(source);
                break;

            case AbilityTarget.RandomFriendly:
                // Random usually implies "Other", so we remove self
                if (allies.Contains(source)) allies.Remove(source);
                
                if (allies.Count > 0)
                {
                    targets.Add(allies[Random.Range(0, allies.Count)]);
                }
                break;

            case AbilityTarget.AllFriendly:
                targets.AddRange(allies);
                // "All Friendly" now includes Self because we removed the filter above
                break;

            case AbilityTarget.AdjacentFriendly:
                // Re-find source index carefully
                CardDisplay[] boardCards = board.GetComponentsInChildren<CardDisplay>();
                List<CardDisplay> boardList = new List<CardDisplay>();
                foreach(var c in boardCards) if (c.gameObject.activeInHierarchy) boardList.Add(c);

                int index = boardList.IndexOf(source);
                if (index > 0) targets.Add(boardList[index - 1]); // Left
                if (index < boardList.Count - 1) targets.Add(boardList[index + 1]); // Right
                break;
        }

        return targets;
    }
}