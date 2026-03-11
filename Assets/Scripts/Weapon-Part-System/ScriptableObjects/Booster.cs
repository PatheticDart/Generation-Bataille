using UnityEngine;

[CreateAssetMenu(fileName = "BoosterPart", menuName = "Parts/BoosterPart")]
public class Booster : Part
{
    [Header("Unique Stats")]
    public int boostPower;
    public int chargeDrain;
}