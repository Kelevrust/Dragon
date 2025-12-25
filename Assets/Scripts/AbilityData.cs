using UnityEngine;

public enum AbilityTrigger
{
    OnPlay,         // Battlecry
    OnDeath,        // Deathrattle
    OnTurnStart,    // Passive scaling
    OnDamageTaken,  // Enrage
    PassiveAura     // Constant effect (Dire Wolf)
}

public enum AbilityTarget
{
    Self,
    RandomFriendly,
    AllFriendly,
    AdjacentFriendly,
    RandomEnemy,
    LowestHealthFriendly
}

public enum AbilityEffect
{
    BuffStats,      // Give +X/+X
    SummonUnit,     // Spawn a specific token
    DealDamage,     // Fireball/Snipe
    HealHero,       // Restore Player HP
    GainGold        // Economy boost
}

[CreateAssetMenu(fileName = "New Ability", menuName = "DnD Battler/Ability Data")]
public class AbilityData : ScriptableObject
{
    [Header("Trigger")]
    public AbilityTrigger triggerType;
    
    [Header("Targeting")]
    public AbilityTarget targetType;
    
    [Header("Effect")]
    public AbilityEffect effectType;
    public int valueX; // Attack buff, Damage amount, or Gold amount
    public int valueY; // Health buff
    
    [Header("Summoning (Optional)")]
    [Tooltip("Only used if Effect is SummonUnit")]
    public UnitData tokenUnit; 
    
    [Header("Visuals")]
    public GameObject vfxPrefab; // Particle effect to play when triggered
}