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
                ExecuteAbility(ability, sourceCard, null);
            }
        }
    }

    public void CastHeroPower(AbilityData ability)
    {
        if (ability == null) return;
        Debug.Log($"Casting Hero Power: {ability.name}");
        ExecuteAbility(ability, null, null);
    }

    public void CastTargetedAbility(AbilityData ability, CardDisplay target)
    {
        if (ability == null) return;
        
        if (GameManager.instance.TrySpendGold(GameManager.instance.activeHero.powerCost))
        {
            Debug.Log($"Casting Targeted Hero Power on {target.unitData.unitName}");
            ApplyEffect(ability, target, null);
        }
    }

    public void RecalculateAuras()
    {
        CardDisplay[] allCards = FindObjectsByType<CardDisplay>(FindObjectsSortMode.None);

        foreach (CardDisplay card in allCards)
        {
            if (card == null) continue;
            card.ResetToPermanent();
        }

        foreach (CardDisplay source in allCards)
        {
            if (source == null) continue;
            if (!source.gameObject.activeInHierarchy) continue; 
            if (!source.isPurchased) continue; 
            if (source.unitData == null || source.unitData.abilities == null) continue;

            foreach (AbilityData ability in source.unitData.abilities)
            {
                if (ability == null) continue;

                if (ability.triggerType == AbilityTrigger.PassiveAura)
                {
                    ExecuteAbility(ability, source, null);
                }
            }
        }

        foreach (CardDisplay card in allCards)
        {
            if (card != null) card.UpdateVisuals();
        }
    }

    void ExecuteAbility(AbilityData ability, CardDisplay source, CardDisplay specificTarget)
    {
        if (ability == null) return;

        List<CardDisplay> targets = FindTargets(ability, source);

        if (specificTarget != null)
        {
            targets.Clear();
            targets.Add(specificTarget);
        }

        // Execute if targets found OR if targeting is "None" / "Self" / "Summon"
        if (targets.Count == 0 && (ability.targetType == AbilityTarget.None || ability.targetType == AbilityTarget.Self || ability.effectType == AbilityEffect.SummonUnit))
        {
            ApplyEffect(ability, null, source); 
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

        bool isSourceGolden = source != null && source.isGolden;

        switch (ability.effectType)
        {
            case AbilityEffect.BuffStats:
                if (target != null)
                {
                    int atk = ability.valueX;
                    int hp = ability.valueY;

                    if (isSourceGolden && ability.triggerType == AbilityTrigger.PassiveAura)
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
                if (ability.tokenUnit == null) return;
                
                // Determine board source if possible
                Transform spawnParent = (source != null) ? source.transform.parent : null;
                
                // Use new Smart Summon logic
                if (GameManager.instance != null)
                {
                    // If source is a unit (Deathrattle), default to BoardOnly on that specific board
                    if (source != null && spawnParent != null)
                    {
                        GameManager.instance.SpawnToken(ability.tokenUnit, spawnParent);
                    }
                    else
                    {
                        // Hero Power Logic: Use Ability Preference
                        Transform replaceTarget = target != null ? target.transform : null;
                        GameManager.instance.TrySpawnUnit(ability.tokenUnit, ability.spawnLocation, replaceTarget);
                    }
                    
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
        
        Transform board = null;
        if (source != null && source.transform.parent != null) 
        {
            board = source.transform.parent;
        }
        else if (GameManager.instance != null) 
        {
            board = GameManager.instance.playerBoard;
        }

        if (board == null) return targets;
        
        List<CardDisplay> allies = new List<CardDisplay>();
        foreach(Transform child in board)
        {
            CardDisplay cd = child.GetComponent<CardDisplay>();
            if(cd != null && cd.gameObject.activeInHierarchy) allies.Add(cd);
        }

        switch (ability.targetType)
        {
            case AbilityTarget.None:
                // No specific target needed (used for Summons)
                break;

            case AbilityTarget.Self:
                if (source != null) targets.Add(source);
                break;

            case AbilityTarget.RandomFriendly:
                List<CardDisplay> validRandom = new List<CardDisplay>(allies);
                if (source != null && validRandom.Contains(source)) validRandom.Remove(source);
                
                if (validRandom.Count > 0)
                {
                    targets.Add(validRandom[Random.Range(0, validRandom.Count)]);
                }
                break;

            case AbilityTarget.AllFriendly:
                targets.AddRange(allies);
                break;

            case AbilityTarget.AdjacentFriendly:
                if (source == null) break;
                CardDisplay[] boardCards = board.GetComponentsInChildren<CardDisplay>();
                List<CardDisplay> boardList = new List<CardDisplay>();
                foreach(var c in boardCards) if (c.gameObject.activeInHierarchy) boardList.Add(c);

                int index = boardList.IndexOf(source);
                if (index > 0) targets.Add(boardList[index - 1]); 
                if (index < boardList.Count - 1) targets.Add(boardList[index + 1]); 
                break;

            case AbilityTarget.AllFriendlyTribe:
                foreach(var ally in allies)
                {
                    if (ally.unitData.tribe == ability.targetTribe)
                    {
                        targets.Add(ally);
                    }
                }
                break;

            case AbilityTarget.RandomFriendlyTribe:
                List<CardDisplay> tribeAllies = new List<CardDisplay>();
                foreach(var ally in allies)
                {
                    if (ally != source && ally.unitData.tribe == ability.targetTribe)
                    {
                        tribeAllies.Add(ally);
                    }
                }
                if (tribeAllies.Count > 0)
                {
                    targets.Add(tribeAllies[Random.Range(0, tribeAllies.Count)]);
                }
                break;
        }

        return targets;
    }
}