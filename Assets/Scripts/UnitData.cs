using UnityEngine;
using System.Collections.Generic;

public enum Tribe 
{ 
    None, Human, Undead, Beast, Dragon, Construct, Elemental 
}

[CreateAssetMenu(fileName = "New Unit", menuName = "DnD Battler/Unit Data")]
public class UnitData : ScriptableObject
{
    [Header("Core Info")]
    public string id;
    public string unitName;
    
    // NEW: CardDisplay looks for this exact name
    [TextArea] public string description; 

    [Header("Stats")]
    [Range(1, 6)] public int tier = 1;
    public int cost = 3;
    public int baseAttack;
    public int baseHealth;
    
    [Header("Flavor")]
    public Tribe tribe;
    
    [Header("New Ability System")]
    public List<AbilityData> abilities = new List<AbilityData>(); 

    [Header("Visuals")]
    public Sprite artwork; 
    public Color frameColor = Color.gray;
    public GameObject unitModelPrefab; 
}