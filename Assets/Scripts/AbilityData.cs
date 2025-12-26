using UnityEngine;

public enum AbilityTrigger
{
    OnPlay,
    OnDeath,
    OnTurnStart,
    OnDamageTaken,
    PassiveAura,
    OnHeroPower
}

public enum AbilityTarget
{
    None,               // NEW: Instant cast (no target selection needed)
    Self,
    RandomFriendly,
    AllFriendly,
    AdjacentFriendly,
    RandomEnemy,
    LowestHealthFriendly,
    SelectTarget,       
    AllFriendlyTribe,   
    RandomFriendlyTribe 
}

public enum AbilityEffect
{
    BuffStats,
    SummonUnit,
    DealDamage,
    HealHero,
    GainGold
}

// NEW: Where should the summoned unit go?
public enum AbilitySpawnLocation
{
    BoardOnly,      // If board full, fail
    HandOnly,       // Add to hand
    BoardThenHand,  // Try board, fallback to hand (Standard Hero Power)
    ReplaceTarget   // Destroy target, put new unit there (Polymorph/Sacrifice)
}

[CreateAssetMenu(fileName = "New Ability", menuName = "DnD Battler/Ability Data")]
public class AbilityData : ScriptableObject
{
    [Header("Trigger")]
    public AbilityTrigger triggerType;
    
    [Header("Targeting")]
    public AbilityTarget targetType;
    public Tribe targetTribe; 
    
    [Header("Effect")]
    public AbilityEffect effectType;
    public int valueX; 
    public int valueY; 
    
    [Header("Summoning")]
    public UnitData tokenUnit; 
    public AbilitySpawnLocation spawnLocation; // NEW
    
    [Header("Visuals")]
    public GameObject vfxPrefab; 
}