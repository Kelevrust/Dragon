using UnityEngine;
using System.Collections.Generic;

public class AbilityManager : MonoBehaviour
{
    public static AbilityManager instance;

    void Awake() { instance = this; }

    public void TriggerAbilities(AbilityTrigger trigger, CardDisplay sourceCard)
    {
        // 1. Safety Check: Source Validity
        if (sourceCard == null) 
        {
            Debug.LogWarning("AbilityManager: Source Card is null!");
            return;
        }
        
        if (sourceCard.unitData == null)
        {
            Debug.LogWarning($"AbilityManager: UnitData missing on {sourceCard.gameObject.name}");
            return;
        }

        // 2. Safety Check: Abilities List
        if (sourceCard.unitData.abilities == null) return;

        foreach (AbilityData ability in sourceCard.unitData.abilities)
        {
            if (ability != null && ability.triggerType == trigger)
            {
                ExecuteAbility(ability, sourceCard);
            }
        }
    }

    void ExecuteAbility(AbilityData ability, CardDisplay source)
    {
        Debug.Log($"Executing Ability: {ability.name} from {source.unitData.unitName}");

        List<CardDisplay> targets = FindTargets(ability, source);

        // Even if no targets found (e.g. Summon doesn't need targets), run effect if target is Self/None
        if (targets.Count == 0 && (ability.targetType == AbilityTarget.Self || ability.effectType == AbilityEffect.SummonUnit))
        {
            ApplyEffect(ability, source, source); // Use source as target for self-effects
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
                    target.currentAttack += ability.valueX;
                    target.currentHealth += ability.valueY;
                    target.UpdateVisuals();
                }
                break;

            case AbilityEffect.SummonUnit:
                // Safety Check: Token Unit
                if (ability.tokenUnit == null)
                {
                    Debug.LogError($"ABILITY ERROR: '{ability.name}' has no Token Unit assigned!");
                    return;
                }

                // Safety Check: Board
                if (source.transform.parent == null)
                {
                    Debug.LogError($"ABILITY ERROR: '{source.name}' has no parent board!");
                    return;
                }

                Transform parentBoard = source.transform.parent;
                
                // Safety Check: GameManager
                if (GameManager.instance != null)
                {
                    GameManager.instance.SpawnToken(ability.tokenUnit, parentBoard);
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
            // Strict active check to ignore ghosts
            if(cd != null && cd != source && cd.gameObject.activeInHierarchy) allies.Add(cd);
        }

        switch (ability.targetType)
        {
            case AbilityTarget.Self:
                targets.Add(source);
                break;

            case AbilityTarget.RandomFriendly:
                if (allies.Count > 0)
                {
                    targets.Add(allies[Random.Range(0, allies.Count)]);
                }
                break;

            case AbilityTarget.AllFriendly:
                targets.AddRange(allies);
                break;
        }

        return targets;
    }
}