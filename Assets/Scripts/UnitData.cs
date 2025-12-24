using UnityEngine;

// This Enum matches the 'CardTribe' type we used in React
public enum Tribe
{
    None,
    Human,
    Undead,
    Beast,
    Dragon,
    Construct,
    Elemental
}

// The 'AbilityType' from React
public enum AbilityType
{
    None,
    Battlecry,
    Deathrattle,
    Passive
}

// 'CreateAssetMenu' adds this to the Right-Click -> Create menu in Unity
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

    [Header("Flavor & Mechanics")]
    public Tribe tribe;
    public AbilityType abilityType;
    [Tooltip("Text description of what the ability does")]
    [TextArea] public string abilityDescription;

    [Header("Visuals")]
    // This connects your logic to your artwork
    public Sprite artwork;
    public Color frameColor = Color.gray;
    public GameObject unitModelPrefab; // For 3D board pieces later
}