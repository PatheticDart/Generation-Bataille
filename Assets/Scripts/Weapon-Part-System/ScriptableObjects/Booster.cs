using UnityEngine;

[CreateAssetMenu(fileName = "BoosterPart", menuName = "Parts/BoosterPart")]
public class Booster : VisiblePart
{
    [Header("Unique Stats")]
    public int energyDrain;
    public int horizontalThrust;
    public int verticalThrust;
    public int boostEnergyDrain;
    public int qBThrust;
    public int qBEnergyDrain;
    public float qBDuration;
    public float qBCooldown;
}