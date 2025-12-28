using UnityEngine;

public enum AbilityTrigger
{
    OnPlay,         // Battlecry
    OnDeath,        // Deathrattle
    OnTurnStart,    // Passive scaling
    OnDamageTaken,  // Enrage
    PassiveAura,    // Constant effect
    OnHeroPower,    // Active button
    OnAllyDeath     // Scavenge
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
    BoardOnly,      // If board full, fail
    HandOnly,       // Add to hand
    BoardThenHand,  // Try board, fallback to hand
    ReplaceTarget   // For polymorph/sacrifice effects
}

// NEW: Where should the Visual Effect appear?
public enum VFXSpawnPoint
{
    Source,         // On the unit casting the spell (e.g. Roar)
    Target,         // On the unit receiving the effect (e.g. Explosion)
    CenterOfBoard   // In the middle of the screen (e.g. Weather effect)
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
    
    [Header("Visuals & Audio")]
    public GameObject vfxPrefab;
    public VFXSpawnPoint vfxSpawnPoint;
    public float vfxDuration = 2.0f; // How long before the effect destroys itself
    public AudioClip soundEffect;    // Sound to play when ability triggers
}