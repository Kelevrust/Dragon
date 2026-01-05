using UnityEngine;
using System.Collections.Generic;

// UPDATED: Preserved existing order to prevent data corruption.
public enum Tribe 
{ 
    None, 
    Syndicate, // Humans/Cops/Mob
    Specter,   // Undead/Ghosts
    Feral,     // Beasts/Werewolves
    Construct, // Golems/Drones
    Arcane,    // Magic/Fae/Witches
    Eldritch,  // Demons/Cosmic Horror
    Fae,       // NEW
    Giant,     // NEW
    Dragon     // NEW
}

// NEW: Support for Tavern Spells
public enum CardType 
{ 
    Unit, 
    Spell 
}

[CreateAssetMenu(fileName = "New Unit", menuName = "DnD Battler/Unit Data")]
public class UnitData : ScriptableObject
{
    [Header("Core Info")]
    public string id;
    public string unitName;
    [TextArea(3, 5)] public string description;
    public CardType cardType = CardType.Unit; // NEW: defaults to Unit

    [Header("Stats")]
    [Range(1, 6)] public int tier = 1;
    public int cost = 3;
    public int baseAttack;
    public int baseHealth;

    [Header("Flavor")]
    public Tribe tribe;

    [Header("Keywords")]
    public bool hasTaunt;
    public bool hasDivineShield;
    public bool hasReborn;
    
    // NEW: Expanded Keywords (Phase 4)
    public bool hasStealth;
    public bool hasPoison;    // Instantly kills
    public bool hasVenomous;  // One-time instant kill
    public bool hasRush;      // Can attack immediately

    [Header("Ability System")]
    public List<AbilityData> abilities = new List<AbilityData>(); 

    [Header("Visuals")]
    public Sprite artwork;
    public Color frameColor = Color.gray; // Kept your default gray
    
    // Kept your existing prefab reference
    public GameObject unitModelPrefab; 
    
    // NEW: Projectile to fire on attack
    public GameObject attackProjectilePrefab; 
    
    // NEW: Sound FX
    public AudioClip attackSound;
    public AudioClip deathSound;
    
    // NEW: Glamour / Transformation (Phase 4 Future Proofing)
    [Header("Transformation")]
    public UnitData transformationUnit;
}