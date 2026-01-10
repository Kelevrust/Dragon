using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class AbilityManager : MonoBehaviour
{
    public static AbilityManager instance;

    void Awake() { instance = this; }

    // --- INTERACTION HANDLER (NEW) ---
    public bool TryHandleInteraction(CardDisplay source, CardDisplay target)
    {
        if (source == null || target == null || source == target) return false;
        
        // 1. Magnetize Logic
        List<AbilityData> abilities = source.runtimeAbilities ?? source.unitData.abilities;
        if (abilities != null)
        {
            foreach(var ab in abilities)
            {
                if (ab.effectType == AbilityEffect.Magnetize)
                {
                    // Check if target is Mech (Construct)
                    if (target.unitData.tribe == Tribe.Construct)
                    {
                        ApplyEffect(ab, target, source, null);
                        return true; 
                    }
                }
            }
        }

        // 2. Spell Logic (if dragging a spell onto a unit)
        if (source.unitData.cardType == CardType.Spell)
        {
            // Cast on target
            TriggerAbilities(AbilityTrigger.OnSpellCast, source, null, target);
            
            // Check if played successfully (handled by GameManager usually, but simplistic here)
            if (GameManager.instance.TryPlaySpell(source, target))
            {
                return true;
            }
        }

        return false;
    }

    // --- CORE TRIGGER SYSTEM ---

    public void TriggerAbilities(AbilityTrigger trigger, CardDisplay sourceCard, Transform overrideBoard = null, CardDisplay interactionTarget = null)
    {
        if (sourceCard == null) return;
        
        // Use Runtime Abilities (allows for Magnetized/Granted abilities)
        List<AbilityData> abilitiesToFire = sourceCard.runtimeAbilities;
        if (abilitiesToFire == null || abilitiesToFire.Count == 0) 
        {
            // Fallback to data if runtime list is empty
            if (sourceCard.unitData != null) abilitiesToFire = sourceCard.unitData.abilities;
        }

        if (abilitiesToFire == null) return;

        // 1. Check for Meta-Multipliers (e.g. Rivendare doubling Deathrattles)
        int executionCount = 1;
        
        Transform boardToScan = overrideBoard;
        if (boardToScan == null && sourceCard.transform.parent != null) boardToScan = sourceCard.transform.parent;
        
        if (boardToScan != null)
        {
            foreach(Transform child in boardToScan)
            {
                if (child == null) continue;
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

    public void TriggerTurnStartAbilities() { TriggerBoardPhase(AbilityTrigger.OnTurnStart); }
    public void TriggerTurnEndAbilities() { TriggerBoardPhase(AbilityTrigger.OnTurnEnd); }
    public void TriggerCombatStartAbilities() { TriggerBoardPhase(AbilityTrigger.OnCombatStart); }

    void TriggerBoardPhase(AbilityTrigger trigger)
    {
        if (GameManager.instance == null || GameManager.instance.playerBoard == null) return;

        List<CardDisplay> units = new List<CardDisplay>();
        foreach(Transform t in GameManager.instance.playerBoard) 
        {
            if (t.gameObject.activeInHierarchy) units.Add(t.GetComponent<CardDisplay>());
        }
        foreach (CardDisplay unit in units)
        {
            if (unit != null) TriggerAbilities(trigger, unit, GameManager.instance.playerBoard);
        }
    }

    public void HandleGlobalDeathTriggers(CardDisplay deadUnit, Transform alliesBoard, Transform enemiesBoard)
    {
        // 1. Process Allies (OnAllyDeath + OnAnyDeath)
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

        // 2. Process Enemies (OnEnemyDeath + OnAnyDeath)
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
        
        // Self Trigger
        TriggerAbilities(AbilityTrigger.OnShieldBreak, brokenUnit, board);

        // Global Listeners (e.g. Steam Knight)
        foreach(Transform child in board)
        {
            if (child == null) continue;
            CardDisplay ally = child.GetComponent<CardDisplay>();
            
            if (ally != null && ally != brokenUnit && ally.gameObject.activeInHierarchy)
            {
                 TriggerAbilities(AbilityTrigger.OnShieldBreak, ally, board, brokenUnit);
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
    
    public void TriggerSpellCastAbilities(CardDisplay spellCard, Transform board)
    {
        TriggerAbilities(AbilityTrigger.OnPlay, spellCard, board);

        if (board == null) return;
        foreach(Transform child in board)
        {
            if (child == null) continue;
            CardDisplay ally = child.GetComponent<CardDisplay>();
            if (ally != null && ally.gameObject.activeInHierarchy)
            {
                TriggerAbilities(AbilityTrigger.OnSpellCast, ally, board, spellCard);
                TriggerAbilities(AbilityTrigger.OnAnyPlay, ally, board, spellCard);
            }
        }
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
                    if (ability != null)
                    {
                        if (ability.triggerType == AbilityTrigger.OnAllyPlay)
                        {
                            bool tribeMatch = ability.targetTribe == Tribe.None || (playedCard.unitData != null && ability.targetTribe == playedCard.unitData.tribe);
                            if (tribeMatch)
                            {
                                ExecuteAbility(ability, ally, playedCard, board);
                            }
                        }
                        
                        if (ability.triggerType == AbilityTrigger.OnAnyPlay)
                        {
                            ExecuteAbility(ability, ally, playedCard, board);
                        }
                    }
                }
            }
        }
    }
    
    public void TriggerAllySummonAbilities(CardDisplay summonedUnit, Transform board)
    {
        if (board == null || summonedUnit == null) return;
        
        TriggerAbilities(AbilityTrigger.OnSpawn, summonedUnit, board);

        if (summonedUnit.hasRush || (summonedUnit.unitData != null && summonedUnit.unitData.hasRush)) 
        {
            if (GameManager.instance != null && GameManager.instance.currentPhase == GameManager.GamePhase.Combat)
            {
                PerformImmediateAttack(summonedUnit);
            }
        }

        foreach(Transform child in board)
        {
            if (child == null) continue;
            CardDisplay ally = child.GetComponent<CardDisplay>();
            if (ally == null || ally == summonedUnit || !ally.gameObject.activeInHierarchy) continue;

            List<AbilityData> abilities = ally.runtimeAbilities ?? ally.unitData.abilities;
            if (abilities != null)
            {
                foreach (AbilityData ability in abilities)
                {
                    if (ability != null) 
                    {
                        if (ability.triggerType == AbilityTrigger.OnAllySummon)
                        {
                            bool tribeMatch = ability.targetTribe == Tribe.None || 
                                             (summonedUnit.unitData != null && ability.targetTribe == summonedUnit.unitData.tribe);
                            
                            if (tribeMatch) ExecuteAbility(ability, ally, summonedUnit, board);
                        }
                        
                        if (ability.triggerType == AbilityTrigger.OnAnySummon)
                        {
                            ExecuteAbility(ability, ally, summonedUnit, board);
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
            ApplyEffect(ability, target, null, null);
        }
    }

    // --- EXECUTION CORE ---

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
            
            Transform p = source.transform.parent;
            bool isOnBoard = (GameManager.instance != null && p == GameManager.instance.playerBoard) || 
                             (CombatManager.instance != null && p == CombatManager.instance.enemyBoard);
                             
            if (!isOnBoard) continue;

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

    void ExecuteAbility(AbilityData ability, CardDisplay source, CardDisplay interactionTarget, Transform overrideBoard, int depth = 0)
    {
        if (ability == null || depth > 10) return; // Prevent StackOverflow

        List<CardDisplay> targets = FindTargets(ability, source, interactionTarget, overrideBoard);

        bool isGlobalEffect = ability.effectType == AbilityEffect.SummonUnit || 
                              ability.effectType == AbilityEffect.HealHero || 
                              ability.effectType == AbilityEffect.ReduceUpgradeCost || 
                              ability.effectType == AbilityEffect.GainGold ||
                              ability.effectType == AbilityEffect.ImmediateAttack; 

        if (targets.Count == 0 && isGlobalEffect)
        {
            ApplyEffect(ability, null, source, overrideBoard, depth + 1); 
        }
        else
        {
            foreach (CardDisplay target in targets)
            {
                ApplyEffect(ability, target, source, overrideBoard, depth + 1);
            }
        }
    }

    void ApplyEffect(AbilityData ability, CardDisplay target, CardDisplay source, Transform overrideBoard, int depth = 0)
    {
        if (ability == null) return;

        PlayVFX(ability, target, source);
        PlaySound(ability);

        bool isSourceGolden = source != null && source.isGolden;
        int mult = isSourceGolden ? 2 : 1;

        int finalX = ability.valueX * mult;
        int finalY = ability.valueY * mult;
        
        int scalingFactor = CalculateScaling(ability, source, overrideBoard);
        if (scalingFactor != 1) { finalX *= scalingFactor; finalY *= scalingFactor; }

        switch (ability.effectType)
        {
            case AbilityEffect.BuffStats:
                if (target != null)
                {
                    // FIX: Passive Auras must NEVER trigger Permanent stats, or they scale infinitely on Recalculate
                    bool isPermanent = ability.duration == BuffDuration.Permanent && ability.triggerType != AbilityTrigger.PassiveAura;

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

            case AbilityEffect.DealDamage:
                if (target != null)
                {
                    int damage = finalX; // valueX is the damage amount
                    target.TakeDamage(damage);

                    // Check if target died from damage
                    if (target.currentHealth <= 0 && CombatManager.instance != null)
                    {
                        CombatManager.instance.HandleDeath(target);
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
                    if (source.hasStealth) target.GainKeyword(KeywordType.Stealth, true);
                    if (source.hasRush) target.GainKeyword(KeywordType.Rush, true);

                    if (source.runtimeAbilities != null)
                    {
                        foreach(var ab in source.runtimeAbilities)
                        {
                            if (ab.effectType != AbilityEffect.Magnetize) target.AddAbility(ab);
                        }
                    }
                    
                    target.UpdateVisuals();
                    // IMPORTANT: Destroy source immediately to prevent loop
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
                
            case AbilityEffect.ImmediateAttack:
                if (source != null)
                {
                    PerformImmediateAttack(source);
                }
                break;
                
            case AbilityEffect.Consume:
                if (target != null && source != null && target != source)
                {
                    int eatenAtk = target.currentAttack;
                    int eatenHp = target.currentHealth;

                    if (isSourceGolden)
                    {
                        eatenAtk *= 2;
                        eatenHp *= 2;
                    }

                    source.permanentAttack += eatenAtk;
                    source.permanentHealth += eatenHp;
                    source.currentAttack += eatenAtk;
                    source.currentHealth += eatenHp;

                    bool stealAbilities = ability.consumeAbsorbsAbilities;
                    if (isSourceGolden && ability.consumeAbsorbsAbilitiesIfGolden) stealAbilities = true;

                    if (stealAbilities)
                    {
                        bool isPerm = ability.duration == BuffDuration.Permanent;
                        if (target.hasDivineShield) source.GainKeyword(KeywordType.DivineShield, isPerm);
                        if (target.hasReborn) source.GainKeyword(KeywordType.Reborn, isPerm);
                        if (target.hasPoison) source.GainKeyword(KeywordType.Poison, isPerm);
                        if (target.hasVenomous) source.GainKeyword(KeywordType.Venomous, isPerm);
                        if (target.hasTaunt) source.GainKeyword(KeywordType.Taunt, isPerm);
                        if (target.hasStealth) source.GainKeyword(KeywordType.Stealth, isPerm);
                        if (target.hasRush) source.GainKeyword(KeywordType.Rush, isPerm);

                        if (target.runtimeAbilities != null)
                        {
                            foreach(var ab in target.runtimeAbilities)
                            {
                                if (ab.effectType != AbilityEffect.Consume && ab.effectType != AbilityEffect.Magnetize) 
                                    source.AddAbility(ab);
                            }
                        }
                    }

                    source.UpdateVisuals();
                    Destroy(target.gameObject);
                    RecalculateAuras();
                }
                break;
                
            case AbilityEffect.Counter:
                Debug.Log($"Counter effect triggered by {source.name} on {target.name}");
                break;
        }

        // Fix: Ensure we don't loop if chainedAbility refers to self
        if (ability.chainedAbility != null && ability.chainedAbility != ability)
        {
            ExecuteAbility(ability.chainedAbility, source, target, overrideBoard, depth + 1);
        }
    }

    void PerformImmediateAttack(CardDisplay source)
    {
        if (source == null) return; // FIX: Null check

        Transform enemyBoard = null;
        if (GameManager.instance != null && CombatManager.instance != null)
        {
             if (source.transform.parent == GameManager.instance.playerBoard) 
                 enemyBoard = CombatManager.instance.enemyBoard;
             else if (source.transform.parent == CombatManager.instance.enemyBoard)
                 enemyBoard = GameManager.instance.playerBoard;
        }
                                
        if (enemyBoard == null) return;

        List<CardDisplay> enemies = new List<CardDisplay>();
        foreach(Transform t in enemyBoard) 
        { 
            if (t.gameObject.activeInHierarchy) enemies.Add(t.GetComponent<CardDisplay>()); 
        }
        
        if (enemies.Count > 0)
        {
            List<CardDisplay> taunts = enemies.Where(e => e.hasTaunt).ToList();
            CardDisplay targetUnit = (taunts.Count > 0) ? taunts[Random.Range(0, taunts.Count)] : enemies[Random.Range(0, enemies.Count)];
            
            Debug.Log($"{source.unitData.unitName} performs Immediate Attack/Rush on {targetUnit.unitData.unitName}!");

            int dmg = source.currentAttack;
            targetUnit.TakeDamage(dmg);
            source.TakeDamage(targetUnit.currentAttack);
            
            if (CombatManager.instance != null)
            {
                if (targetUnit.currentHealth <= 0) CombatManager.instance.HandleDeath(targetUnit);
                if (source.currentHealth <= 0) CombatManager.instance.HandleDeath(source);
            }
        }
    }

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
                        if (child == null) continue;
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
            if (child == null) continue;
            CardDisplay cd = child.GetComponent<CardDisplay>();
            if(cd != null && cd.gameObject.activeInHierarchy) allies.Add(cd);
        }

        if (source != null && !allies.Contains(source) && source.transform.parent == board)
        {
            allies.Add(source);
        }
        
        List<CardDisplay> allUnitsInGame = new List<CardDisplay>();
        if (ability.targetType == AbilityTarget.GlobalTribe || ability.targetType == AbilityTarget.GlobalCopies)
        {
             allUnitsInGame = FindObjectsByType<CardDisplay>(FindObjectsSortMode.None).Where(c => c != null && c.gameObject.activeInHierarchy).ToList();
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

            case AbilityTarget.RandomEnemy:
            case AbilityTarget.AllEnemy:
                {
                    // Find enemy board
                    Transform enemyBoard = null;
                    if (source != null && source.transform.parent != null)
                    {
                        Transform myBoard = source.transform.parent;
                        if (GameManager.instance != null && myBoard == GameManager.instance.playerBoard)
                            enemyBoard = CombatManager.instance != null ? CombatManager.instance.enemyBoard : null;
                        else if (CombatManager.instance != null && myBoard == CombatManager.instance.enemyBoard)
                            enemyBoard = GameManager.instance.playerBoard;
                    }

                    if (enemyBoard != null)
                    {
                        List<CardDisplay> enemies = new List<CardDisplay>();
                        foreach (Transform child in enemyBoard)
                        {
                            if (child == null) continue;
                            CardDisplay enemy = child.GetComponent<CardDisplay>();
                            if (enemy != null && enemy.gameObject.activeInHierarchy) enemies.Add(enemy);
                        }

                        if (ability.targetType == AbilityTarget.RandomEnemy && enemies.Count > 0)
                        {
                            targets.Add(enemies[Random.Range(0, enemies.Count)]);
                        }
                        else if (ability.targetType == AbilityTarget.AllEnemy)
                        {
                            targets.AddRange(enemies);
                        }
                    }
                }
                break;
            
            case AbilityTarget.AdjacentFriendly:
            {
                if (source == null) break;
                int index = allies.IndexOf(source);
                if (index > 0) targets.Add(allies[index - 1]); 
                if (index >= 0 && index < allies.Count - 1) targets.Add(allies[index + 1]); 
                break;
            }
            case AbilityTarget.AdjacentLeft:
            {
                if (source == null) break;
                int index = allies.IndexOf(source);
                if (index > 0) targets.Add(allies[index - 1]);
                break;
            }
            case AbilityTarget.AdjacentRight:
            {
                if (source == null) break;
                int index = allies.IndexOf(source);
                if (index >= 0 && index < allies.Count - 1) targets.Add(allies[index + 1]);
                break;
            }

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
            
            case AbilityTarget.OpposingUnit:
                if (source != null && source.transform.parent != null)
                {
                    Transform myBoard = source.transform.parent;
                    Transform otherBoard = null;
                    
                    if (GameManager.instance != null && myBoard == GameManager.instance.playerBoard)
                        otherBoard = CombatManager.instance != null ? CombatManager.instance.enemyBoard : null;
                    else if (CombatManager.instance != null && myBoard == CombatManager.instance.enemyBoard)
                        otherBoard = GameManager.instance.playerBoard;
                        
                    if (otherBoard != null)
                    {
                        int index = source.transform.GetSiblingIndex();
                        if (index < otherBoard.childCount)
                        {
                            Transform t = otherBoard.GetChild(index);
                            if (t != null) {
                                CardDisplay opp = t.GetComponent<CardDisplay>();
                                if (opp != null && opp.gameObject.activeInHierarchy) targets.Add(opp);
                            }
                        }
                    }
                }
                break;

            case AbilityTarget.AllInHand:
                if (GameManager.instance != null && GameManager.instance.playerHand != null)
                {
                    foreach(Transform child in GameManager.instance.playerHand)
                    {
                        if (child == null) continue;
                        CardDisplay cd = child.GetComponent<CardDisplay>();
                        if (cd != null && cd.gameObject.activeInHierarchy) targets.Add(cd);
                    }
                }
                break;

            case AbilityTarget.AllInShop:
                if (ShopManager.instance != null && ShopManager.instance.shopContainer != null)
                {
                    foreach(Transform child in ShopManager.instance.shopContainer)
                    {
                        if (child == null) continue;
                        CardDisplay cd = child.GetComponent<CardDisplay>();
                        if (cd != null && cd.gameObject.activeInHierarchy) targets.Add(cd);
                    }
                }
                break;
                
            case AbilityTarget.AllFriendlyEverywhere:
                targets.AddRange(allies);
                if (GameManager.instance != null && GameManager.instance.playerHand != null)
                {
                    foreach(Transform child in GameManager.instance.playerHand)
                    {
                        if (child == null) continue;
                        CardDisplay cd = child.GetComponent<CardDisplay>();
                        if (cd != null && cd.gameObject.activeInHierarchy) targets.Add(cd);
                    }
                }
                break;
                
            case AbilityTarget.GlobalTribe:
                foreach(var u in allUnitsInGame) if(u.unitData != null && u.unitData.tribe == ability.targetTribe) targets.Add(u);
                break;
                
            case AbilityTarget.GlobalCopies:
                foreach(var u in allUnitsInGame) 
                    if(u.unitData != null && ability.targetUnitFilter != null && u.unitData.id == ability.targetUnitFilter.id) 
                        targets.Add(u);
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