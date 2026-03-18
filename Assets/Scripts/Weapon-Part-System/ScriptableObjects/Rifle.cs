using System;
using UnityEngine;

[CreateAssetMenu(fileName = "RiflePart", menuName = "Parts/RiflePart")]
public class Rifle : WeaponPart
{
    [Header("Unique Stats")]
    public AmmoType ammoType;
    public int attackPower;
    public int ammo;
    public int effectiveRange;
    public int firingInterval;
    public int ammoPrice;

    [Header("Hidden Stats")]
    public int bulletSpeed = 500;
    public BaseProjectile bulletPrefab;
    
}

[Serializable]
public enum AmmoType
{
    Solid, Energy
}