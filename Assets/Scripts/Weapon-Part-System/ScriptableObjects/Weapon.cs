using System;
using UnityEngine;

[CreateAssetMenu(fileName = "RiflePart", menuName = "Parts/RiflePart")]
public class Rifle : ChargedPart
{
    [Header("Unique Stats")]
    public AmmoType ammoType;
    public int attackPower;
    public int ammo;
    public int effectiveRange;
    public int firingInterval;
    public int ammoPrice;
}

[Serializable]
public enum AmmoType
{
    Solid, Energy
}