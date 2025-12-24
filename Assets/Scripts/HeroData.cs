using UnityEngine;

// Defines what kind of bonus this hero provides
public enum HeroBonusType
{
    None,
    ExtraHealth, // Paladin (+5 Max HP)
    ExtraGold,   // Ranger (+1 Gold on Turn 1)
    StartingUnit // Necromancer (Starts with Skeleton)
}

[CreateAssetMenu(fileName = "New Hero", menuName = "DnD Battler/Hero Data")]
public class HeroData : ScriptableObject
{
    [Header("Visuals")]
    public string heroName;
    [TextArea] public string description;
    public Sprite heroPortrait;

    [Header("Mechanics")]
    public HeroBonusType bonusType;

    // We use generic fields that change meaning based on bonusType
    [Tooltip("Amount of Gold or Health to add")]
    public int bonusValue;

    [Tooltip("Only used if Bonus Type is StartingUnit")]
    public UnitData startingUnit;
}