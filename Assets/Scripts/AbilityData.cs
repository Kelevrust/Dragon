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
    OnCombatStart,  // Rabid Bear
    OnSpawn,        // Fires on self when created (for Immediate Attack)
    OnSpellCast     // NEW: Specifically when a Tavern Spell is played
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
    AdjacentLeft,         // Left Neighbor only
    AdjacentRight,        // Right Neighbor only
    AllFriendlyTribe,     // e.g. "Give all Mechs +2/+2"
    RandomFriendlyTribe,  // e.g. "Give a random Beast +2/+2"
    Opponent,             // The enemy Hero (for damage)
    OpposingUnit,         // The unit directly across (Chess style)
    Killer,               // The unit that killed this one (Avenger)
    AllInHand,            // Hand buff
    AllInShop,            // Shop buff
    AllFriendlyEverywhere,// Hand + Board
    GlobalTribe,          // All Murlocs (Enemy & Friendly) - rarely used but fun
    GlobalCopies,         // All copies of "Corpse Rat" everywhere

    // --- NEW TARGETS FOR SPELLS ---
    SelectTarget,         // Manual selection (generic)
    AnyUnit,              // Select any unit on board
    FriendlyUnit,         // Select any friendly unit
    EnemyUnit             // Select any enemy unit
}

public enum AbilityEffect 
{ 
    BuffStats,        // Give +X/+Y
    DealDamage,       // Deal X damage
    SummonUnit,       // Summon a specific token
    GainGold,         // Hero gains X Gold
    HealHero,         // Restore X Health
    GrantAbility,     // Give target a new AbilityData
    MakeGolden,       // Turn target Golden
    GiveKeyword,      // Give Divine Shield, Taunt, etc.
    TransformUnit,    // Polymorph target into Token
    ModifyTriggerCount, // Rivendare: Your Deathrattles trigger +X times
    ReduceUpgradeCost,  // Reduce Tavern Tier cost by X
    ForceTrigger,     // Trigger a unit's Deathrattle/Battlecry immediately
    ImmediateAttack,  // Unit attacks immediately (Rush/Charge logic)
    Magnetize,        // Combine stats/keywords with target Mech
    Consume,          // Eat target unit to gain its stats
    Counter,          // Counter the next spell/ability
    Freeze            // Freeze target (Shop or Unit)
}

public enum BuffDuration
{
    Permanent,
    UntilEndOfTurn,
    UntilEndOfCombat
}

public enum ValueScaling
{
    None,
    PerGold,          // +1/+1 for each Gold you have
    PerTribeOnBoard,  // +1/+1 for each Mech you have
    PerAllyCount,     // +1/+1 for each Ally
    PerMissingHealth, // +1/+1 for each missing Hero HP
    PerTier           // +1/+1 per Tavern Tier
}

public enum KeywordType
{
    None,
    Taunt,
    DivineShield,
    Reborn,
    Poison,
    Venomous,
    Stealth,
    Rush,
    Windfury,
    Cleave
}

public enum VFXSpawnPoint
{
    Source,
    Target,
    CenterOfBoard
}

// Added missing enum for spawning logic
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
    [Header("Trigger Conditions")]
    public AbilityTrigger triggerType;
    public AbilityTrigger metaTriggerType; // For "Your Deathrattles trigger twice" or "Counter next Spell"
    
    [Header("Targeting")]
    public AbilityTarget targetType;
    
    [Header("Effect")]
    public AbilityEffect effectType;
    
    [Header("Chaining")]
    [Tooltip("Optional: Trigger another ability immediately after this one.")]
    public AbilityData chainedAbility;
    [Tooltip("Grant this ability to the target.")]
    public AbilityData abilityToGrant; 

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

    [Header("Consume Settings")]
    [Tooltip("If true, Consume also steals Keywords and Abilities (like Magnetize).")]
    public bool consumeAbsorbsAbilities;
    
    [Tooltip("If true, Consume steals Keywords/Abilities ONLY when the source unit is Golden.")]
    public bool consumeAbsorbsAbilitiesIfGolden;

    [Header("Visuals")]
    public GameObject vfxPrefab;
    public AudioClip soundEffect;
    [Range(0f, 2f)] public float vfxDuration = 1f;
    public VFXSpawnPoint vfxSpawnPoint;
}