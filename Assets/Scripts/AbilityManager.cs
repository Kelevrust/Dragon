using UnityEngine;
using System.Collections.Generic;

public class AbilityManager : MonoBehaviour
{
    public static AbilityManager instance;

    void Awake() { instance = this; }

    public void TriggerAbilities(AbilityTrigger trigger, CardDisplay sourceCard)
    {
        if (sourceCard == null || sourceCard.unitData == null) return;

        foreach (AbilityData ability in sourceCard.unitData.abilities)
        {
            if (ability.triggerType == trigger)
            {
                ExecuteAbility(ability, sourceCard);
            }
        }
    }

    void ExecuteAbility(AbilityData ability, CardDisplay source)
    {
        Debug.Log($"Executing Ability: {ability.name} from {source.unitData.unitName}");

        List<CardDisplay> targets = FindTargets(ability, source);

        foreach (CardDisplay target in targets)
        {
            ApplyEffect(ability, target, source);
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
                // FIX: Spawn on the specific board the source unit is on
                Transform parentBoard = source.transform.parent;

                if (ability.tokenUnit == null)
                {
                    Debug.LogError("Ability Error: Token Unit is missing in the Ability Data!");
                    return;
                }

                if (parentBoard != null)
                {
                    GameManager.instance.SpawnToken(ability.tokenUnit, parentBoard);
                }
                break;

            case AbilityEffect.GainGold:
                GameManager.instance.gold += ability.valueX;
                GameManager.instance.UpdateUI();
                break;

            case AbilityEffect.HealHero:
                GameManager.instance.ModifyHealth(ability.valueX);
                break;
        }
    }

    List<CardDisplay> FindTargets(AbilityData ability, CardDisplay source)
    {
        List<CardDisplay> targets = new List<CardDisplay>();
        Transform board = source.transform.parent;
        if (board == null) return targets;

        List<CardDisplay> allies = new List<CardDisplay>();
        foreach (Transform child in board)
        {
            CardDisplay cd = child.GetComponent<CardDisplay>();
            if (cd != null && cd != source && cd.gameObject.activeSelf) allies.Add(cd);
        }

        switch (ability.targetType)
        {
            case AbilityTarget.Self:
                targets.Add(source);
                break;

            case AbilityTarget.RandomFriendly:
                if (allies.Count > 0) targets.Add(allies[Random.Range(0, allies.Count)]);
                break;

            case AbilityTarget.AllFriendly:
                targets.AddRange(allies);
                break;
        }

        return targets;
    }
}