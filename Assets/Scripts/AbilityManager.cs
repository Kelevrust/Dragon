using UnityEngine;
using System.Collections.Generic;

public class AbilityManager : MonoBehaviour
{
    public static AbilityManager instance;

    void Awake() { instance = this; }

    public void TriggerAbilities(AbilityTrigger trigger, CardDisplay sourceCard)
    {
        if (sourceCard == null || sourceCard.unitData == null) return;
        if (sourceCard.unitData.abilities == null) return;

        foreach (AbilityData ability in sourceCard.unitData.abilities)
        {
            if (ability != null && ability.triggerType == trigger)
            {
                ExecuteAbility(ability, sourceCard);
            }
        }
    }

    // NEW: Called whenever the board state changes (Buy, Sell, Move, Die)
    public void RecalculateAuras()
    {
        // 1. Find all active cards using the modern API
        CardDisplay[] allCards = FindObjectsByType<CardDisplay>(FindObjectsSortMode.None);

        // 2. Reset everyone to their "Real" stats (Base + Permanent Buffs)
        foreach (CardDisplay card in allCards)
        {
            card.ResetToPermanent();
        }

        // 3. Apply Aura Effects
        foreach (CardDisplay source in allCards)
        {
            if (!source.isPurchased) continue; // Shop items don't emit auras
            if (source.unitData == null || source.unitData.abilities == null) continue;

            foreach (AbilityData ability in source.unitData.abilities)
            {
                if (ability.triggerType == AbilityTrigger.PassiveAura)
                {
                    ExecuteAbility(ability, source);
                }
            }
        }

        // 4. Update UI
        foreach (CardDisplay card in allCards)
        {
            card.UpdateVisuals();
        }
    }

    void ExecuteAbility(AbilityData ability, CardDisplay source)
    {
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

                    // Auras modify CURRENT stats (temporary)
                    if (ability.triggerType == AbilityTrigger.PassiveAura)
                    {
                        target.currentAttack += atk;
                        target.currentHealth += hp;
                    }
                    // Battlecries/Deathrattles modify PERMANENT stats
                    else
                    {
                        target.permanentAttack += atk;
                        target.permanentHealth += hp;
                        
                        // Apply to current immediately too
                        target.currentAttack += atk;
                        target.currentHealth += hp;
                        target.UpdateVisuals();
                    }
                }
                break;

            case AbilityEffect.SummonUnit:
                if (ability.tokenUnit == null || source.transform.parent == null) return;
                
                if (GameManager.instance != null)
                {
                    GameManager.instance.SpawnToken(ability.tokenUnit, source.transform.parent);
                    // Recalculate auras after spawn
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
        if (source.transform.parent == null) return targets;
        
        Transform board = source.transform.parent;
        
        List<CardDisplay> allies = new List<CardDisplay>();
        foreach(Transform child in board)
        {
            CardDisplay cd = child.GetComponent<CardDisplay>();
            if(cd != null && cd.gameObject.activeInHierarchy) allies.Add(cd);
        }

        switch (ability.targetType)
        {
            case AbilityTarget.Self:
                targets.Add(source);
                break;

            case AbilityTarget.RandomFriendly:
                List<CardDisplay> validRandom = new List<CardDisplay>(allies);
                validRandom.Remove(source);
                if (validRandom.Count > 0) targets.Add(validRandom[Random.Range(0, validRandom.Count)]);
                break;

            case AbilityTarget.AllFriendly:
                targets.AddRange(allies);
                targets.Remove(source);
                break;

            case AbilityTarget.AdjacentFriendly:
                int index = allies.IndexOf(source);
                if (index > 0) targets.Add(allies[index - 1]); // Left
                if (index < allies.Count - 1) targets.Add(allies[index + 1]); // Right
                break;
        }

        return targets;
    }
}