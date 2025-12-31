using UnityEngine;
using System.Collections.Generic;

public class AbilityManager : MonoBehaviour
{
    public static AbilityManager instance;

    void Awake() { instance = this; }

    public void TriggerAbilities(AbilityTrigger trigger, CardDisplay sourceCard, Transform overrideBoard = null)
    {
        if (sourceCard == null || sourceCard.unitData == null || sourceCard.unitData.abilities == null) return;

        foreach (AbilityData ability in sourceCard.unitData.abilities)
        {
            if (ability != null && ability.triggerType == trigger)
            {
                ExecuteAbility(ability, sourceCard, null, overrideBoard);
            }
        }
    }

    // NEW: Trigger for Spark Plug logic
    public void TriggerAllyPlayAbilities(CardDisplay playedCard, Transform board)
    {
        if (board == null || playedCard == null) return;

        foreach(Transform child in board)
        {
            CardDisplay ally = child.GetComponent<CardDisplay>();
            // 1. Must be active
            // 2. Must NOT be the card we just played
            // 3. Must have abilities
            if (ally != null && ally != playedCard && ally.gameObject.activeInHierarchy)
            {
                // Check if this ally has an OnAllyPlay trigger
                if (ally.unitData.abilities != null)
                {
                    foreach (AbilityData ability in ally.unitData.abilities)
                    {
                        if (ability.triggerType == AbilityTrigger.OnAllyPlay)
                        {
                            // Optional: Check Tribe requirement?
                            // For "Whenever you play a Construct", we check if the *playedCard* matches the requirement.
                            // We can use 'targetTribe' in the ability data to act as a filter for the trigger source.
                            
                            bool tribeMatch = ability.targetTribe == Tribe.None || ability.targetTribe == playedCard.unitData.tribe;
                            
                            if (tribeMatch)
                            {
                                ExecuteAbility(ability, ally, null, board);
                            }
                        }
                    }
                }
            }
        }
    }

    // ... (TriggerAllyDeathAbilities, CastHeroPower, CastTargetedAbility remain the same) ...
    public void TriggerAllyDeathAbilities(CardDisplay deadUnit, Transform board)
    {
        if (board == null) return;
        foreach(Transform child in board)
        {
            CardDisplay ally = child.GetComponent<CardDisplay>();
            if (ally != null && ally != deadUnit && ally.gameObject.activeInHierarchy)
            {
                TriggerAbilities(AbilityTrigger.OnAllyDeath, ally, board);
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

    // ... (RecalculateAuras, ExecuteAbility, ApplyEffect, FindTargets, PlayVFX, PlaySound remain same - omitted for brevity unless requested) ...
    
    // NOTE: Ensure you keep the rest of the file!
    // I will include RecalculateAuras and below to ensure the file is complete.

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
            if (source == null || !source.gameObject.activeInHierarchy || !source.isPurchased) continue; 
            if (source.unitData == null || source.unitData.abilities == null) continue;

            foreach (AbilityData ability in source.unitData.abilities)
            {
                if (ability != null && ability.triggerType == AbilityTrigger.PassiveAura)
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
            targets.Clear();
            targets.Add(specificTarget);
        }

        if (targets.Count == 0 && (ability.targetType == AbilityTarget.None || ability.targetType == AbilityTarget.Self || ability.effectType == AbilityEffect.SummonUnit || ability.effectType == AbilityEffect.HealHero || ability.effectType == AbilityEffect.ReduceUpgradeCost))
        {
            ApplyEffect(ability, null, source, overrideBoard); 
        }
        else
        {
            foreach (CardDisplay target in targets) ApplyEffect(ability, target, source, overrideBoard);
        }
    }

    void ApplyEffect(AbilityData ability, CardDisplay target, CardDisplay source, Transform overrideBoard)
    {
        if (ability == null) return;
        PlayVFX(ability, target, source);
        PlaySound(ability);

        bool isSourceGolden = source != null && source.isGolden;

        switch (ability.effectType)
        {
            case AbilityEffect.BuffStats:
                if (target != null)
                {
                    int atk = ability.valueX;
                    int hp = ability.valueY;
                    if (isSourceGolden) { atk *= 2; hp *= 2; }

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
                    GameManager.instance.SpawnToken(ability.tokenUnit, spawnParent);
                    RecalculateAuras();
                }
                break;
            case AbilityEffect.GainGold:
                if (GameManager.instance != null)
                {
                    int amount = ability.valueX;
                    if (isSourceGolden) amount *= 2;
                    GameManager.instance.gold += amount;
                    GameManager.instance.UpdateUI();
                }
                break;
            case AbilityEffect.HealHero:
                if (GameManager.instance != null)
                {
                    int amount = ability.valueX;
                    if (isSourceGolden) amount *= 2;
                    GameManager.instance.ModifyHealth(amount);
                }
                break;
            case AbilityEffect.ReduceUpgradeCost:
                ShopManager shop = FindFirstObjectByType<ShopManager>();
                if (shop != null)
                {
                    int amount = ability.valueX;
                    if (isSourceGolden) amount *= 2;
                    shop.currentDiscount += amount;
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

        if (source != null && !allies.Contains(source) && source.transform.parent == board) allies.Add(source);

        switch (ability.targetType)
        {
            case AbilityTarget.Self: if (source != null) targets.Add(source); break;
            case AbilityTarget.RandomFriendly:
                List<CardDisplay> validRandom = new List<CardDisplay>(allies);
                if (source != null && validRandom.Contains(source)) validRandom.Remove(source);
                if (validRandom.Count > 0) targets.Add(validRandom[Random.Range(0, validRandom.Count)]);
                break;
            case AbilityTarget.AllFriendly: targets.AddRange(allies); break;
            case AbilityTarget.AdjacentFriendly:
                if (source == null) break;
                List<CardDisplay> boardList = new List<CardDisplay>();
                foreach(Transform t in board)
                {
                     CardDisplay c = t.GetComponent<CardDisplay>();
                     if (c != null && c.gameObject.activeInHierarchy) boardList.Add(c);
                }
                int index = boardList.IndexOf(source);
                if (index > 0) targets.Add(boardList[index - 1]); 
                if (index >= 0 && index < boardList.Count - 1) targets.Add(boardList[index + 1]); 
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