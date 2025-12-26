using UnityEngine;

public enum HeroBonusType 
{ 
    None, ExtraHealth, ExtraGold, StartingUnit 
}

[CreateAssetMenu(fileName = "New Hero", menuName = "DnD Battler/Hero Data")]
public class HeroData : ScriptableObject
{
    [Header("Visuals")]
    public string heroName;
    [TextArea] public string description;
    public Sprite heroPortrait;

    [Header("Passive Bonus")]
    public HeroBonusType bonusType;
    [Tooltip("Amount of Gold or Health to add")]
    public int bonusValue; 
    [Tooltip("Only used if Bonus Type is StartingUnit")]
    public UnitData startingUnit; 

    [Header("Hero Power")]
    public string powerName = "Hero Power";
    public int powerCost = 2;
    public AbilityData powerAbility; // Drag an AbilityData file here
}