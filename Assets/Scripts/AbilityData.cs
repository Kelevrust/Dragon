using UnityEngine;

public enum AbilityTrigger
{
    OnPlay,         // Battlecry (Self)
    OnDeath,        // Deathrattle
    OnTurnStart,    // Passive scaling
    OnDamageTaken,  // Enrage
    PassiveAura,    // Constant effect
    OnHeroPower,    // Active button
    OnAllyDeath,    // Scavenge
    OnAllyPlay      // NEW: Synergy (Spark Plug)
}

public enum AbilityTarget
{
    None,
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
    GainGold,
    ReduceUpgradeCost
}

public enum AbilitySpawnLocation
{
    BoardOnly,      
    HandOnly,       
    BoardThenHand,  
    ReplaceTarget   
}

public enum VFXSpawnPoint
{
    Source,         
    Target,         
    CenterOfBoard   
}

[CreateAssetMenu(fileName = "New Ability", menuName = "DnD Battler/Ability Data")]
public class AbilityData : ScriptableObject
{
    [Header("Trigger")]
    public AbilityTrigger triggerType;
    
    [Header("Targeting")]
    public AbilityTarget targetType;
    
    [Tooltip("Required if targeting by Tribe (e.g. Give all UNDEAD +1/+1)")]
    public Tribe targetTribe; 
    
    [Header("Effect")]
    public AbilityEffect effectType;
    public int valueX; 
    public int valueY; 
    
    [Header("Summoning (Optional)")]
    [Tooltip("Only used if Effect is SummonUnit")]
    public UnitData tokenUnit; 
    public AbilitySpawnLocation spawnLocation;
    
    [Header("Visuals")]
    public GameObject vfxPrefab; 
    public VFXSpawnPoint vfxSpawnPoint;
    public float vfxDuration = 2.0f;
    public AudioClip soundEffect;
}