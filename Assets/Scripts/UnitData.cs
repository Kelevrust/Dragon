using UnityEngine;
using System.Collections.Generic;

// UPDATED: Neon Noir Flavor
public enum Tribe 
{ 
    None, 
    Syndicate, // Humans/Cops/Mob
    Specter,   // Undead/Ghosts
    Feral,     // Beasts/Werewolves
    Construct, // Golems/Drones
    Arcane,    // Magic/Fae/Witches
    Eldritch   // Demons/Cosmic Horror
}

[CreateAssetMenu(fileName = "New Unit", menuName = "DnD Battler/Unit Data")]
public class UnitData : ScriptableObject
{
    [Header("Core Info")]
    public string id;
    public string unitName;
    [TextArea] public string description;

    [Header("Stats")]
    [Range(1, 6)] public int tier = 1;
    public int cost = 3;
    public int baseAttack;
    public int baseHealth;
    
    [Header("Keywords")]
    public bool hasTaunt;        // NEW
    public bool hasDivineShield; // NEW
    public bool hasReborn;       // NEW
    
    [Header("Flavor")]
    public Tribe tribe;
    
    [Header("New Ability System")]
    public List<AbilityData> abilities = new List<AbilityData>(); 

    [Header("Visuals")]
    public Sprite artwork; 
    public Color frameColor = Color.gray;
    public GameObject unitModelPrefab; 
}