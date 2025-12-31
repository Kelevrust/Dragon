using UnityEngine;
using System.Collections.Generic;

public enum HeroBonusType { None, ExtraHealth, ExtraGold, StartingUnit }

[CreateAssetMenu(fileName = "New Hero", menuName = "DnD Battler/Hero Data")]
public class HeroData : ScriptableObject
{
    public string id;
    public string heroName;
    public Sprite heroPortrait;
    
    [Header("Hero Power")]
    public string powerName;
    public int powerCost = 2;
    public AbilityData powerAbility;
    
    [Header("Passive Bonuses")]
    public HeroBonusType bonusType;
    public int bonusValue;
    public UnitData startingUnit;

    [Header("Visuals")]
    // NEW: Thematic Freeze mechanics
    [Tooltip("VFX to spawn on EACH shop card when frozen (e.g. Chains, Candy, Fear Icon)")]
    public GameObject freezeVFXPrefab; 
    public AudioClip freezeSound;      
}