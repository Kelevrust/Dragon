using UnityEngine;

public enum AbilityTrigger 
{ 
    OnPlay,         // Battlecry
    OnDeath,        // Deathrattle
    PassiveAura,    // Continuous
    OnAllyDeath,    // Scavenge
    OnEnemyDeath,   // Triggers when an enemy dies (Observer)
    OnAnyDeath,     // Triggers when ANY unit dies
    OnEnemyKill,    // Triggers when THIS unit kills an enemy (Attacker)
    OnAllyPlay,     // When ally is Played from Hand
    OnAllySummon,   // When ally appears (Play OR Token)
    OnAnyPlay,      // When ANY unit is played (Global)
    OnAnySummon,    // When ANY unit is summoned (Global)
    OnDamageTaken,  // Enrage
    OnTurnStart,    // Loan Shark
    OnTurnEnd,      // The Don
    OnShieldBreak,  // Steam Knight
    OnAttack,       // When this unit attacks
    OnDealDamage,   // When this unit deals damage
    OnCombatStart,  // NEW: Rabid Bear / Sentry Turret
    OnSpawn         // NEW: Fires on self when created (for Immediate Attack)
}

public enum AbilityTarget 
{ 
    None, 
    Self, 
    AllFriendly, 
    AllEnemy, 
    RandomFriendly, 
    RandomEnemy, 
    AdjacentFriendly,
    AllFriendlyTribe,    
    RandomFriendlyTribe,
    SelectTarget,         // Required for targeted Hero Powers
    Opponent,             // Target the Enemy Board or Hero
    Killer,               // Target the specific unit that killed/damaged this unit
    AllFriendlyEverywhere,// Targets Board AND Hand
    AllInHand,            // Targets Hand Only
    AllInShop,            // Targets Shop Only
    GlobalTribe,          // Targets ALL units of targetTribe (Player + Enemy)
    GlobalCopies          // Targets ALL units matching targetUnitFilter (Player + Enemy)
}

public enum AbilityEffect 
{ 
    BuffStats, 
    SummonUnit, 
    DealDamage, 
    HealHero, 
    GainGold, 
    ReduceUpgradeCost,
    MakeGolden,      
    Magnetize,          // Merge stats AND abilities into target
    GiveKeyword,
    ModifyTriggerCount, // Rivendare/Brann
    ForceTrigger,       // Trigger another unit's ability
    GrantAbility,       // Add a specific ability to the target's list
    ImmediateAttack     // NEW: Forces a combat trade
}

// Defines persistence logic
public enum BuffDuration
{
    Permanent,       // Updates base stats. Persists after combat
    CombatTemporary, // Updates current stats. Resets after combat
    TurnTemporary    // Lasts until end of turn
}

// Defines how the ValueX/ValueY should scale
public enum ValueScaling
{
    None,                   // Fixed value
    PerGold,                // Scale based on current Gold
    PerTribeOnBoard,        // Scale based on Tribe Count
    PerAllyCount,           // Scale based on total unit count
    PerMissingHealth,       // Scale based on Hero missing HP
    PerTribePlayedThisGame, // Scale based on total Tribe units played this game
    PerTribePlayedThisTurn  // Scale based on total Tribe units played this turn
}

public enum KeywordType 
{ 
    None, 
    DivineShield, 
    Reborn, 
    Taunt, 
    Stealth, 
    Poison,   
    Venomous,
    Rush      // NEW: Can attack immediately
}

public enum VFXSpawnPoint { Target, Source, CenterOfBoard }

// Required for GameManager.SpawnToken
public enum AbilitySpawnLocation 
{ 
    BoardOnly, 
    HandOnly, 
    BoardThenHand, 
    ReplaceTarget 
}

[CreateAssetMenu(fileName = "New Ability", menuName = "DnD Battler/Ability Data")]
public class AbilityData : ScriptableObject
{
    public string id;
    public new string name; 
    [TextArea] public string description;

    [Header("Logic")]
    public AbilityTrigger triggerType;
    public AbilityTarget targetType;
    public AbilityEffect effectType;
    
    [Header("Meta Logic")]
    [Tooltip("Used for ModifyTriggerCount, ForceTrigger, or GrantAbility")]
    public AbilityTrigger metaTriggerType;
    
    [Tooltip("The Ability Asset to grant when using 'GrantAbility' effect")]
    public AbilityData abilityToGrant; 

    [Header("Chaining")]
    [Tooltip("Optional: Trigger another ability immediately after this one.")]
    public AbilityData chainedAbility;

    [Header("Persistence")]
    [Tooltip("Does this effect persist after combat ends?")]
    public BuffDuration duration = BuffDuration.Permanent;

    [Header("Scaling")]
    [Tooltip("If set, ValueX/Y will be multiplied by this factor.")]
    public ValueScaling scalingType; 
    
    [Header("Conditions")]
    [Tooltip("Used for Targeting OR for Scaling (e.g. Count all 'Syndicate' units)")]
    public Tribe targetTribe; 
    
    [Tooltip("Used for 'GlobalCopies' or specific unit filtering")]
    public UnitData targetUnitFilter; 

    [Header("Values")]
    public int valueX; // e.g. Attack Buff / Damage / Count
    public int valueY; // e.g. Health Buff
    public UnitData tokenUnit; // For summon effects
    public KeywordType keywordToGive; 

    [Header("Visuals")]
    public GameObject vfxPrefab;
    public AudioClip soundEffect;
    
    [Tooltip("Where to spawn the VFX")]
    public VFXSpawnPoint vfxSpawnPoint;
    public float vfxDuration = 1.0f;
}