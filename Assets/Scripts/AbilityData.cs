using UnityEngine;

public enum AbilityTrigger 
{ 
    OnPlay,         // Battlecry
    OnDeath,        // Deathrattle
    PassiveAura,    // Continuous
    OnAllyDeath,    // Scavenge
    OnEnemyDeath,   // Triggers when an enemy dies (Observer)
    OnEnemyKill,    // Triggers when THIS unit kills an enemy (Attacker)
    OnAllyPlay,     // Synergy
    OnDamageTaken,  // Enrage
    OnTurnStart,    // Loan Shark
    OnTurnEnd,      // The Don
    OnShieldBreak,  // Steam Knight
    OnAttack,       // When this unit attacks (e.g. "Whenever this attacks, gain +1/+1")
    OnDealDamage,   // When this unit deals damage (e.g. "Whenever this deals damage, heal hero")
    OnCombatStart   // Triggers at start of combat (e.g. Rabid Bear, Sentry Turret)
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
    SelectTarget,        // Required for targeted Hero Powers
    
    // NEW: Stat-based targeting
    LowestHealthEnemy,
    HighestHealthEnemy,
    LowestAttackEnemy,
    HighestAttackEnemy,
    LowestHealthAlly,
    HighestHealthAlly,
    LowestAttackAlly,
    HighestAttackAlly
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
    Magnetize,       
    GiveKeyword      
}

// Defines persistence logic
public enum BuffDuration
{
    Permanent,       // Updates base stats. Persists after combat (e.g. Scaling units)
    CombatTemporary, // Updates current stats. Resets after combat (e.g. Rage buffs)
    TurnTemporary    // Lasts until end of turn (e.g. "Give +2 Attack this turn")
}

// Defines how the ValueX/ValueY should scale
public enum ValueScaling
{
    None,                   // Fixed value (Standard)
    PerGold,                // Scale based on current Gold (Smuggler)
    PerTribeOnBoard,        // Scale based on Tribe Count (The Don, Scrap Golem)
    PerAllyCount,           // Scale based on total unit count
    PerMissingHealth,       // Scale based on Hero missing HP
    PerTribePlayedThisGame, // Scale based on total Tribe units played this game (e.g. Elementals)
    PerTribePlayedThisTurn  // Scale based on total Tribe units played this turn (e.g. Storm/Combo)
}

public enum KeywordType 
{ 
    None, 
    DivineShield, 
    Reborn, 
    Taunt, 
    Stealth, 
    Poison,   // Instantly kills minion damaged by this
    Venomous  // Instantly kills minion, then keyword is removed (One-shot poison)
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
    
    [Header("Chaining")]
    [Tooltip("Optional: Trigger another ability immediately after this one (e.g. Buff Self THEN Summon).")]
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

    [Header("Values")]
    public int valueX; // e.g. Attack Buff / Damage
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