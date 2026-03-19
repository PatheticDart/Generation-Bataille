using UnityEngine;

public abstract class ProjectileWeaponPart : WeaponPart
{
    [Header("Projectile Stats")]
    public int attackPower;
    public int ammo;
    public int firingInterval; // MS between shots
    public int effectiveRange;
    public int bulletSpeed = 500;
    public BaseProjectile bulletPrefab;
    public AmmoType ammoType;
}