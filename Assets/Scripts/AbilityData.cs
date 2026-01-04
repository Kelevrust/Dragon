using UnityEngine;

public enum AbilityTrigger 
{ 
    OnPlay,         
    OnDeath,        
    PassiveAura,    
    OnAllyDeath,    
    OnEnemyDeath,   
    OnAnyDeath,     
    OnEnemyKill,    
    OnAllyPlay,     
    OnDamageTaken,  
    OnTurnStart,    
    OnTurnEnd,      
    OnShieldBreak,  
    OnAttack,       
    OnDealDamage,
    OnCombatStart
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
    SelectTarget,
    Opponent,       // NEW: Target the Enemy Board (for Summons) or Enemy Hero
    Killer          // NEW: Target the specific unit that killed/damaged this unit
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
    ModifyTriggerCount, 
    ForceTrigger,
    GrantAbility        // NEW: Add a specific ability to the target's list
}

public enum BuffDuration
{
    Permanent,       
    CombatTemporary, 
    TurnTemporary    
}

public enum ValueScaling
{
    None,                   
    PerGold,                
    PerTribeOnBoard,        
    PerAllyCount,           
    PerMissingHealth,       
    PerTribePlayedThisGame, 
    PerTribePlayedThisTurn  
}

public enum KeywordType 
{ 
    None, 
    DivineShield, 
    Reborn, 
    Taunt, 
    Stealth, 
    Poison,   
    Venomous  
}

public enum VFXSpawnPoint { Target, Source, CenterOfBoard }

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
    
    // NEW: The specific ability to grant to another unit
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

    [Header("Values")]
    public int valueX; 
    public int valueY; 
    public UnitData tokenUnit; 
    public KeywordType keywordToGive; 

    [Header("Visuals")]
    public GameObject vfxPrefab;
    public AudioClip soundEffect;
    
    [Tooltip("Where to spawn the VFX")]
    public VFXSpawnPoint vfxSpawnPoint;
    public float vfxDuration = 1.0f;
}