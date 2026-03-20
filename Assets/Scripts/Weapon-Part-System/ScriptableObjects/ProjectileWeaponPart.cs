using UnityEngine;

public enum WeaponTriggerType { FullAuto, SemiAuto }

public abstract class ProjectileWeaponPart : WeaponPart
{
    [Header("Projectile Stats")]
    public int attackPower;
    public int ammo; // TOTAL Reserve Ammo
    
    [Header("Magazine")]
    [Tooltip("-1: Bottomless clip")]
    public int magSize = -1;
    [Tooltip("Time in seconds to reload.")]
    public float reloadTime = 2f; 
    
    [Header("Firing Stats")]
    public WeaponTriggerType triggerType = WeaponTriggerType.FullAuto; // NEW!
    public int firingInterval; // MS between shots
    public int effectiveRange;
    public int bulletSpeed = 500;
    public BaseProjectile bulletPrefab;
    public AmmoType ammoType;
}