using UnityEngine;

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