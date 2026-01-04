using UnityEngine;
using System.Collections.Generic;

public class AbilityManager : MonoBehaviour
{
    public static AbilityManager instance;

    void Awake() { instance = this; }

    public void TriggerAbilities(AbilityTrigger trigger, CardDisplay sourceCard, Transform overrideBoard = null)
    {
        if (sourceCard == null) return;
        if (sourceCard.unitData == null) return;
        if (sourceCard.unitData.abilities == null) return;

        foreach (AbilityData ability in sourceCard.unitData.abilities)
        {
            if (ability != null && ability.triggerType == trigger)
            {
                ExecuteAbility(ability, sourceCard, null, overrideBoard);
            }
        }
    }

    public void TriggerTurnEndAbilities()
    {
        if (GameManager.instance == null || GameManager.instance.playerBoard == null) return;

        List<CardDisplay> boardUnits = new List<CardDisplay>();
        foreach(Transform t in GameManager.instance.playerBoard) 
        {
            CardDisplay cd = t.GetComponent<CardDisplay>();
            if (cd != null) boardUnits.Add(cd);
        }

        foreach (CardDisplay unit in boardUnits)
        {
            TriggerAbilities(AbilityTrigger.OnTurnEnd, unit, GameManager.instance.playerBoard);
        }
    }
    
    public void TriggerTurnStartAbilities()
    {
        if (GameManager.instance == null || GameManager.instance.playerBoard == null) return;

        List<CardDisplay> boardUnits = new List<CardDisplay>();
        foreach(Transform t in GameManager.instance.playerBoard) 
        {
            CardDisplay cd = t.GetComponent<CardDisplay>();
            if (cd != null) boardUnits.Add(cd);
        }

        foreach (CardDisplay unit in boardUnits)
        {
            TriggerAbilities(AbilityTrigger.OnTurnStart, unit, GameManager.instance.playerBoard);
        }
    }

    public void TriggerAllyPlayAbilities(CardDisplay playedCard, Transform board)
    {
        if (board == null || playedCard == null) return;
        if (playedCard.unitData == null) return;

        foreach(Transform child in board)
        {
            if (child == null) continue;
            
            CardDisplay ally = child.GetComponent<CardDisplay>();
            
            if (ally == null) continue;
            if (ally == playedCard) continue;
            if (!ally.gameObject.activeInHierarchy) continue;
            if (ally.unitData == null) continue;
            if (ally.unitData.abilities == null) continue; 

            foreach (AbilityData ability in ally.unitData.abilities)
            {
                if (ability == null) continue;

                if (ability.triggerType == AbilityTrigger.OnAllyPlay)
                {
                    bool tribeMatch = ability.targetTribe == Tribe.None || ability.targetTribe == playedCard.unitData.tribe;
                    
                    if (tribeMatch)
                    {
                        ExecuteAbility(ability, ally, null, board);
                    }
                }
            }
        }
    }

    public void TriggerAllyDeathAbilities(CardDisplay deadUnit, Transform board)
    {
        if (board == null) return;

        foreach(Transform child in board)
        {
            if (child == null) continue;

            CardDisplay ally = child.GetComponent<CardDisplay>();
            if (ally != null && ally != deadUnit && ally.gameObject.activeInHierarchy)
            {
                TriggerAbilities(AbilityTrigger.OnAllyDeath, ally, board);
            }
        }
    }

    public void TriggerShieldBreakAbilities(CardDisplay brokenUnit, Transform board)
    {
        if (board == null) return;
        
        TriggerAbilities(AbilityTrigger.OnShieldBreak, brokenUnit, board);

        foreach(Transform child in board)
        {
            if (child == null) continue;
            CardDisplay ally = child.GetComponent<CardDisplay>();
            
            if (ally != null && ally != brokenUnit && ally.gameObject.activeInHierarchy)
            {
                if (ally.unitData != null && ally.unitData.abilities != null)
                {
                     foreach (AbilityData ability in ally.unitData.abilities)
                     {
                         if (ability.triggerType == AbilityTrigger.OnShieldBreak)
                         {
                             ExecuteAbility(ability, ally, brokenUnit, board);
                         }
                     }
                }
            }
        }
    }

    public void CastHeroPower(AbilityData ability)
    {
        if (ability == null) return;
        Debug.Log($"Casting Hero Power: {ability.name}");
        ExecuteAbility(ability, null, null, null);
    }

    public void CastTargetedAbility(AbilityData ability, CardDisplay target)
    {
        if (ability == null) return;
        
        if (GameManager.instance.TrySpendGold(GameManager.instance.activeHero.powerCost))
        {
            Debug.Log($"Casting Targeted Hero Power on {target.unitData.unitName}");
            ApplyEffect(ability, target, null, null);
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
                    ExecuteAbility(ability, source, null, null);
                }
            }
        }

        foreach (CardDisplay card in allCards)
        {
            if (card != null) card.UpdateVisuals();
        }
    }

    void ExecuteAbility(AbilityData ability, CardDisplay source, CardDisplay specificTarget, Transform overrideBoard)
    {
        if (ability == null) return;

        List<CardDisplay> targets = FindTargets(ability, source, overrideBoard);

        if (specificTarget != null)
        {
            if (targets.Count == 0) targets.Add(specificTarget);
        }

        if (targets.Count == 0 && (ability.targetType == AbilityTarget.None || ability.targetType == AbilityTarget.Self || ability.effectType == AbilityEffect.SummonUnit || ability.effectType == AbilityEffect.HealHero || ability.effectType == AbilityEffect.ReduceUpgradeCost || ability.effectType == AbilityEffect.GainGold))
        {
            ApplyEffect(ability, null, source, overrideBoard); 
        }
        else
        {
            foreach (CardDisplay target in targets)
            {
                ApplyEffect(ability, target, source, overrideBoard);
            }
        }
    }

    void ApplyEffect(AbilityData ability, CardDisplay target, CardDisplay source, Transform overrideBoard)
    {
        if (ability == null) return;

        PlayVFX(ability, target, source);
        PlaySound(ability);

        bool isSourceGolden = source != null && source.isGolden;
        int mult = isSourceGolden ? 2 : 1;

        switch (ability.effectType)
        {
            case AbilityEffect.BuffStats:
                if (target != null)
                {
                    int atk = ability.valueX * mult;
                    int hp = ability.valueY * mult;

                    if (ability.triggerType == AbilityTrigger.PassiveAura)
                    {
                        target.currentAttack += atk;
                        target.currentHealth += hp;
                    }
                    else
                    {
                        target.permanentAttack += atk;
                        target.permanentHealth += hp;
                        target.ResetToPermanent(); 
                        target.UpdateVisuals();
                    }
                }
                break;

            case AbilityEffect.SummonUnit:
                if (ability.tokenUnit == null) return;
                
                Transform spawnParent = overrideBoard;
                if (spawnParent == null) spawnParent = (source != null) ? source.transform.parent : null;
                if (spawnParent == null && GameManager.instance != null) spawnParent = GameManager.instance.playerBoard;

                if (spawnParent != null && GameManager.instance != null)
                {
                    // 1. Determine Count from Data (Default to 1 if ValueX is 0)
                    int baseCount = Mathf.Max(1, ability.valueX);
                    
                    // 2. Apply Golden Multiplier (Doubles the spawn count)
                    int finalCount = isSourceGolden ? baseCount * 2 : baseCount;

                    for(int i = 0; i < finalCount; i++)
                    {
                        if (spawnParent.childCount < 7)
                        {
                            GameManager.instance.SpawnToken(ability.tokenUnit, spawnParent);
                        }
                    }
                    RecalculateAuras();
                }
                break;

            case AbilityEffect.GainGold:
                if (GameManager.instance != null)
                {
                    int amount = ability.valueX * mult;
                    GameManager.instance.gold += amount;
                    GameManager.instance.UpdateUI();
                }
                break;
                
            case AbilityEffect.HealHero:
                if (GameManager.instance != null)
                {
                    int amount = ability.valueX * mult;
                    GameManager.instance.ModifyHealth(amount);
                }
                break;

            case AbilityEffect.ReduceUpgradeCost:
                ShopManager shop = FindFirstObjectByType<ShopManager>();
                if (shop != null)
                {
                    int amount = ability.valueX * mult;
                    shop.currentDiscount += amount;
                }
                break;

            case AbilityEffect.MakeGolden:
                if (target != null && !target.isGolden)
                {
                    target.MakeGolden();
                }
                break;

            case AbilityEffect.GiveKeyword:
                if (target != null)
                {
                    if (ability.keywordToGive == KeywordType.DivineShield) target.hasDivineShield = true;
                    if (ability.keywordToGive == KeywordType.Reborn) target.hasReborn = true;
                    target.UpdateVisuals();
                }
                break;
        }
    }

    void PlayVFX(AbilityData ability, CardDisplay target, CardDisplay source)
    {
        if (ability.vfxPrefab == null) return;
        Vector3 spawnPos = Vector3.zero;
        switch (ability.vfxSpawnPoint)
        {
            case VFXSpawnPoint.Source: if (source != null) spawnPos = source.transform.position; break;
            case VFXSpawnPoint.Target: if (target != null) spawnPos = target.transform.position; else if (source != null) spawnPos = source.transform.position; break;
            case VFXSpawnPoint.CenterOfBoard: spawnPos = new Vector3(Screen.width/2f, Screen.height/2f, 0f); break;
        }
        spawnPos.z -= 10f; 
        GameObject vfx = Instantiate(ability.vfxPrefab, spawnPos, Quaternion.identity);
        Destroy(vfx, ability.vfxDuration);
    }

    void PlaySound(AbilityData ability)
    {
        if (ability.soundEffect != null) AudioSource.PlayClipAtPoint(ability.soundEffect, Vector3.zero);
    }

    List<CardDisplay> FindTargets(AbilityData ability, CardDisplay source, Transform overrideBoard)
    {
        List<CardDisplay> targets = new List<CardDisplay>();
        
        Transform board = overrideBoard;
        if (board == null)
        {
            if (source != null && source.transform.parent != null) board = source.transform.parent;
            else if (GameManager.instance != null) board = GameManager.instance.playerBoard;
        }

        if (board == null) return targets;
        
        List<CardDisplay> allies = new List<CardDisplay>();
        foreach(Transform child in board)
        {
            CardDisplay cd = child.GetComponent<CardDisplay>();
            if(cd != null && cd.gameObject.activeInHierarchy) allies.Add(cd);
        }

        if (source != null && !allies.Contains(source) && source.transform.parent == board)
        {
            allies.Add(source);
        }

        switch (ability.targetType)
        {
            case AbilityTarget.None: break;
            case AbilityTarget.Self: if (source != null) targets.Add(source); break;
            case AbilityTarget.RandomFriendly:
                List<CardDisplay> validRandom = new List<CardDisplay>(allies);
                if (source != null && validRandom.Contains(source)) validRandom.Remove(source);
                if (validRandom.Count > 0) targets.Add(validRandom[Random.Range(0, validRandom.Count)]);
                break;
            case AbilityTarget.AllFriendly: targets.AddRange(allies); break;
            case AbilityTarget.AdjacentFriendly:
                if (source == null) break;
                int index = allies.IndexOf(source);
                if (index > 0) targets.Add(allies[index - 1]); 
                if (index >= 0 && index < allies.Count - 1) targets.Add(allies[index + 1]); 
                break;
            case AbilityTarget.AllFriendlyTribe:
                foreach(var ally in allies) if (ally.unitData.tribe == ability.targetTribe) targets.Add(ally);
                break;
            case AbilityTarget.RandomFriendlyTribe:
                List<CardDisplay> tribeAllies = new List<CardDisplay>();
                foreach(var ally in allies) if (ally != source && ally.unitData.tribe == ability.targetTribe) tribeAllies.Add(ally);
                if (tribeAllies.Count > 0) targets.Add(tribeAllies[Random.Range(0, tribeAllies.Count)]);
                break;
        }
        return targets;
    }
}