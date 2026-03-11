using UnityEngine;

[System.Flags]
public enum EquipLocation
{
    None = 0,

    BackL = 1 << 1, 
    BackR = 1 << 2, 
    ArmL = 1 << 3, 
    ArmR = 1 << 4, 

    All = ~0,
}

public abstract class ChargedPart : Part
{
    public int energyDrain;
}

public abstract class BodyPart : ChargedPart
{
    [Header("Armor")]
    public int armorPoints;
    public int shellDef;
    public int energyDef;
}

[CreateAssetMenu(fileName = "HeadPart", menuName = "Parts/HeadPart")]
public class HeadPart : BodyPart
{
    [Header("Unique Stats")]
    public float lockOnTimeMultiplier = .9f;

    public float radarPollRate = .5f;
    public float radarScale = 1;
}

[CreateAssetMenu(fileName = "TorsoPart", menuName = "Parts/TorsoPart")]
public class TorsoPart : BodyPart
{
    [Header("Unique Stats")]
    public int maxArmWeight;
}

[CreateAssetMenu(fileName = "ArmPart", menuName = "Parts/ArmPart")]
public class ArmPart : BodyPart
{
    [Header("Unique Stats")]
    public int recoilControl;
}

[CreateAssetMenu(fileName = "LegPart", menuName = "Parts/LegPart")]
public class LegPart : BodyPart
{
    [Header("Unique Stats")]
    public int movingEnergyDrain;
    public int maxLegWeight;
    public int movingAbility;
    public int turningAbility;
    public int landingStability;
    public int defenseStability;
    public int jumpPerformance;
}