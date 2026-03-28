using UnityEngine;

public class BasicProjectileWeapon : FunctionalWeapon
{
    [Header("Weapon Setup")]
    public Transform muzzlePoint;
    public PooledVFX muzzleFlash;
    public bool spawnFlashAsChild;

    private ProjectileWeaponPart _weaponStats;
    private float _nextFireTime = 0f;

    public override void InitializeWeapon(Part data)
    {
        base.InitializeWeapon(data);
        _weaponStats = data as ProjectileWeaponPart;

        if (_weaponStats == null) return;

        if (_weaponStats.bulletPrefab != null && GlobalProjectilePool.Instance != null)
        {
            float shotsPerSecond = 1000f / _weaponStats.firingInterval;
            float maxAliveBullets = shotsPerSecond * _weaponStats.bulletPrefab.lifetime;
            int optimalPoolSize = Mathf.CeilToInt(maxAliveBullets) + 5;

            GlobalProjectilePool.Instance.PreWarm(_weaponStats.bulletPrefab, optimalPoolSize);
        }
    }

    public override void OnFirePressed()
    {
        if (_weaponStats != null && _weaponStats.triggerType == WeaponTriggerType.SemiAuto) TryFire();
    }

    public override void OnFireHeld()
    {
        if (_weaponStats != null && _weaponStats.triggerType == WeaponTriggerType.FullAuto) TryFire();
    }

    public override void OnFireReleased() { }

    private void TryFire()
    {
        if (isReloading) return;

        if (Time.time >= _nextFireTime)
        {
            if (currentResource > 0)
            {
                Fire();
                _nextFireTime = Time.time + (_weaponStats.firingInterval / 1000f);
            }
            else if (currentReserveAmmo > 0)
            {
                Reload();
            }
        }
    }

    private void Fire()
    {
        if (muzzlePoint == null || _weaponStats.bulletPrefab == null) return;

        currentResource--;
        NotifyResourceChange();

        PlayMuzzleFlash(muzzleFlash, muzzlePoint, spawnFlashAsChild);

        BaseProjectile proj = GlobalProjectilePool.Instance.GetProjectile(
            _weaponStats.bulletPrefab, muzzlePoint.position, muzzlePoint.rotation);

        // --- FIXED: Pass the shooterLayer ---
        proj.SetupStats(_weaponStats.attackPower, _weaponStats.bulletSpeed, shooterLayer);
        proj.SetPrefabReference(_weaponStats.bulletPrefab);
    }
}