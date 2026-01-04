using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class AbilityManager : MonoBehaviour
{
    public static AbilityManager instance;

    void Awake() { instance = this; }

    // --- CORE TRIGGER SYSTEM ---

    public void TriggerAbilities(AbilityTrigger trigger, CardDisplay sourceCard, Transform overrideBoard = null, CardDisplay interactionTarget = null)
    {
        if (sourceCard == null) return;
        
        // Use Runtime Abilities
        List<AbilityData> abilitiesToFire = sourceCard.runtimeAbilities;
        if (abilitiesToFire == null || abilitiesToFire.Count == 0) 
        {
            if (sourceCard.unitData != null) abilitiesToFire = sourceCard.unitData.abilities;
        }

        if (abilitiesToFire == null) return;

        // 1. Check for Meta-Multipliers (e.g. Rivendare)
        int executionCount = 1;
        
        Transform boardToScan = overrideBoard;
        if (boardToScan == null && sourceCard.transform.parent != null) boardToScan = sourceCard.transform.parent;
        
        if (boardToScan != null)
        {
            foreach(Transform child in boardToScan)
            {
                CardDisplay unit = child.GetComponent<CardDisplay>();
                if (unit != null && unit.gameObject.activeInHierarchy && unit.runtimeAbilities != null)
                {
                    foreach(var ab in unit.runtimeAbilities)
                    {
                        if (ab.triggerType == AbilityTrigger.PassiveAura && 
                            ab.effectType == AbilityEffect.ModifyTriggerCount &&
                            ab.metaTriggerType == trigger)
                        {
                            int mult = unit.isGolden ? ab.valueX + 1 : ab.valueX; 
                            executionCount = Mathf.Max(executionCount, mult);
                        }
                    }
                }
            }
        }

        // 2. Execute
        foreach (AbilityData ability in abilitiesToFire)
        {
            if (ability != null && ability.triggerType == trigger)
            {
                for(int i = 0; i < executionCount; i++)
                {
                    ExecuteAbility(ability, sourceCard, interactionTarget, overrideBoard);
                }
            }
        }
    }

    // --- SPECIFIC EVENT HANDLERS ---

    public void TriggerTurnStartAbilities()
    {
        TriggerBoardPhase(AbilityTrigger.OnTurnStart);
    }

    public void TriggerTurnEndAbilities()
    {
        TriggerBoardPhase(AbilityTrigger.OnTurnEnd);
    }
    
    public void TriggerCombatStartAbilities()
    {
        TriggerBoardPhase(AbilityTrigger.OnCombatStart);
    }

    void TriggerBoardPhase(AbilityTrigger trigger)
    {
        if (GameManager.instance == null || GameManager.instance.playerBoard == null) return;

        List<CardDisplay> units = new List<CardDisplay>();
        foreach(Transform t in GameManager.instance.playerBoard) 
        {
            if (t.gameObject.activeInHierarchy)
                units.Add(t.GetComponent<CardDisplay>());
        }

        foreach (CardDisplay unit in units)
        {
            if (unit != null) TriggerAbilities(trigger, unit, GameManager.instance.playerBoard);
        }
    }

    public void HandleGlobalDeathTriggers(CardDisplay deadUnit, Transform alliesBoard, Transform enemiesBoard)
    {
        // 1. Process Allies
        if (alliesBoard != null)
        {
            foreach(Transform child in alliesBoard)
            {
                if (child == null) continue;
                CardDisplay ally = child.GetComponent<CardDisplay>();
                
                if (ally != null && ally != deadUnit && ally.gameObject.activeInHierarchy)
                {
                    TriggerAbilities(AbilityTrigger.OnAllyDeath, ally, alliesBoard, deadUnit);
                    TriggerAbilities(AbilityTrigger.OnAnyDeath, ally, alliesBoard, deadUnit);
                }
            }
        }

        // 2. Process Enemies
        if (enemiesBoard != null)
        {
            foreach(Transform child in enemiesBoard)
            {
                if (child == null) continue;
                CardDisplay enemy = child.GetComponent<CardDisplay>();
                
                if (enemy != null && enemy.gameObject.activeInHierarchy)
                {
                    TriggerAbilities(AbilityTrigger.OnEnemyDeath, enemy, enemiesBoard, deadUnit);
                    TriggerAbilities(AbilityTrigger.OnAnyDeath, enemy, enemiesBoard, deadUnit);
                }
            }
        }
    }
    
    // Legacy method for CombatManager compatibility
    public void TriggerAllyDeathAbilities(CardDisplay deadUnit, Transform board)
    {
        if (board == null) return;
        foreach(Transform child in board)
        {
            if (child == null) continue;
            CardDisplay ally = child.GetComponent<CardDisplay>();
            if (ally != null && ally != deadUnit && ally.gameObject.activeInHierarchy)
            {
                TriggerAbilities(AbilityTrigger.OnAllyDeath, ally, board, deadUnit);
            }
        }
    }
    
    public void TriggerOnKill(CardDisplay killer, CardDisplay victim, Transform board)
    {
        TriggerAbilities(AbilityTrigger.OnEnemyKill, killer, board, victim);
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
                if (ally.unitData != null)
                {
                     // Check runtime abilities for shield break listeners
                     List<AbilityData> abilities = ally.runtimeAbilities ?? ally.unitData.abilities;
                     if (abilities != null)
                     {
                         foreach (AbilityData ability in abilities)
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
    }
    
    public void TriggerAttackAbilities(CardDisplay attacker, CardDisplay defender, Transform board)
    {
        TriggerAbilities(AbilityTrigger.OnAttack, attacker, board, defender);
    }
    
    public void TriggerDamageDealtAbilities(CardDisplay dealer, CardDisplay victim, Transform board)
    {
        TriggerAbilities(AbilityTrigger.OnDealDamage, dealer, board, victim);
    }
    
    public void TriggerAllyPlayAbilities(CardDisplay playedCard, Transform board)
    {
        if (board == null || playedCard == null) return;

        foreach(Transform child in board)
        {
            CardDisplay ally = child.GetComponent<CardDisplay>();
            if (ally == null || ally == playedCard || !ally.gameObject.activeInHierarchy) continue;

            List<AbilityData> abilities = ally.runtimeAbilities ?? ally.unitData.abilities;
            if (abilities != null)
            {
                foreach (AbilityData ability in abilities)
                {
                    if (ability != null && ability.triggerType == AbilityTrigger.OnAllyPlay)
                    {
                        bool tribeMatch = ability.targetTribe == Tribe.None || (playedCard.unitData != null && ability.targetTribe == playedCard.unitData.tribe);
                        if (tribeMatch)
                        {
                            ExecuteAbility(ability, ally, playedCard, board);
                        }
                    }
                }
            }
        }
    }

    // --- HERO POWERS ---

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
            ApplyEffect(ability, target, null, null);
        }
    }

    // --- EXECUTION CORE ---

    // Restored Method
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
            
            List<AbilityData> abilities = source.runtimeAbilities ?? source.unitData.abilities;
            if (abilities == null) continue;

            foreach (AbilityData ability in abilities)
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

    void ExecuteAbility(AbilityData ability, CardDisplay source, CardDisplay interactionTarget, Transform overrideBoard)
    {
        if (ability == null) return;

        List<CardDisplay> targets = FindTargets(ability, source, interactionTarget, overrideBoard);

        bool isGlobalEffect = ability.effectType == AbilityEffect.SummonUnit || 
                              ability.effectType == AbilityEffect.HealHero || 
                              ability.effectType == AbilityEffect.ReduceUpgradeCost || 
                              ability.effectType == AbilityEffect.GainGold;

        if (targets.Count == 0 && isGlobalEffect)
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

        int finalX = ability.valueX * mult;
        int finalY = ability.valueY * mult;
        
        int scalingFactor = CalculateScaling(ability, source, overrideBoard);
        if (scalingFactor != 1)
        {
            finalX *= scalingFactor;
            finalY *= scalingFactor;
        }

        switch (ability.effectType)
        {
            case AbilityEffect.BuffStats:
                if (target != null)
                {
                    bool isPermanent = ability.duration == BuffDuration.Permanent;
                    
                    if (isPermanent)
                    {
                        target.permanentAttack += finalX;
                        target.permanentHealth += finalY;
                    }
                    
                    target.currentAttack += finalX;
                    target.currentHealth += finalY;
                    target.UpdateVisuals();
                }
                break;

            case AbilityEffect.SummonUnit:
                if (ability.tokenUnit == null) return;
                
                Transform spawnParent = overrideBoard;
                if (spawnParent == null) spawnParent = (source != null) ? source.transform.parent : null;
                if (spawnParent == null && GameManager.instance != null) spawnParent = GameManager.instance.playerBoard;

                if (spawnParent != null && GameManager.instance != null)
                {
                    int baseCount = Mathf.Max(1, ability.valueX);
                    int finalCount = isSourceGolden ? baseCount * 2 : baseCount;

                    for(int i=0; i<finalCount; i++)
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
                    bool isPerm = ability.duration == BuffDuration.Permanent;
                    if (ability.keywordToGive != KeywordType.None)
                        target.GainKeyword(ability.keywordToGive, isPerm);
                }
                break;

            case AbilityEffect.Magnetize:
                if (target != null && source != null && target != source)
                {
                    target.permanentAttack += source.permanentAttack;
                    target.permanentHealth += source.permanentHealth;
                    target.currentAttack += source.currentAttack;
                    target.currentHealth += source.currentHealth;
                    
                    if (source.hasDivineShield) target.GainKeyword(KeywordType.DivineShield, true);
                    if (source.hasReborn) target.GainKeyword(KeywordType.Reborn, true);
                    if (source.hasPoison) target.GainKeyword(KeywordType.Poison, true);
                    if (source.hasVenomous) target.GainKeyword(KeywordType.Venomous, true);
                    if (source.hasTaunt) target.GainKeyword(KeywordType.Taunt, true);

                    if (source.runtimeAbilities != null)
                    {
                        foreach(var ab in source.runtimeAbilities)
                        {
                            if (ab.effectType != AbilityEffect.Magnetize) target.AddAbility(ab);
                        }
                    }
                    
                    target.UpdateVisuals();
                    Destroy(source.gameObject);
                    RecalculateAuras();
                }
                break;

            case AbilityEffect.GrantAbility:
                if (target != null && ability.abilityToGrant != null)
                {
                    target.AddAbility(ability.abilityToGrant);
                }
                break;
                
            case AbilityEffect.ForceTrigger:
                if (target != null)
                {
                    TriggerAbilities(ability.metaTriggerType, target, overrideBoard);
                }
                break;
        }

        if (ability.chainedAbility != null)
        {
            ExecuteAbility(ability.chainedAbility, source, target, overrideBoard);
        }
    }

    // --- HELPERS ---

    int CalculateScaling(AbilityData ability, CardDisplay source, Transform board)
    {
        if (ability.scalingType == ValueScaling.None) return 1;

        int factor = 1;
        
        switch(ability.scalingType)
        {
            case ValueScaling.PerGold:
                if (GameManager.instance != null) factor = GameManager.instance.gold;
                break;
            case ValueScaling.PerTribeOnBoard:
                if (board != null)
                {
                    int count = 0;
                    foreach(Transform child in board)
                    {
                        CardDisplay cd = child.GetComponent<CardDisplay>();
                        if (cd != null && cd.unitData != null && cd.unitData.tribe == ability.targetTribe) count++;
                    }
                    factor = count;
                }
                break;
            case ValueScaling.PerAllyCount:
                if (board != null) factor = board.childCount;
                break;
            case ValueScaling.PerMissingHealth:
                if (GameManager.instance != null) 
                    factor = GameManager.instance.maxPlayerHealth - GameManager.instance.playerHealth;
                break;
        }
        
        return factor;
    }

    List<CardDisplay> FindTargets(AbilityData ability, CardDisplay source, CardDisplay interactionTarget, Transform overrideBoard)
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
                
            case AbilityTarget.Killer:
            case AbilityTarget.Opponent:
                if (interactionTarget != null) targets.Add(interactionTarget);
                break;
        }
        return targets;
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
}