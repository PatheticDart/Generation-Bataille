using UnityEngine;

public class ShotgunProjectileWeapon : FunctionalWeapon
{
    [Header("Weapon Setup")]
    public Transform muzzlePoint;
    public PooledVFX muzzleFlash;
    public AudioClip shootSFX; // --- NEW: Sound Effect ---
    public bool spawnFlashAsChild;

    private ShotgunPart _shotgunStats;
    private float _nextFireTime = 0f;

    public override void InitializeWeapon(Part data)
    {
        base.InitializeWeapon(data);
        _shotgunStats = data as ShotgunPart;

        if (_shotgunStats == null) return;

        if (_shotgunStats.bulletPrefab != null && GlobalProjectilePool.Instance != null)
        {
            float shotsPerSecond = 1000f / _shotgunStats.firingInterval;
            float maxAliveShots = shotsPerSecond * _shotgunStats.bulletPrefab.lifetime;

            int optimalPoolSize = Mathf.CeilToInt(maxAliveShots * _shotgunStats.pelletCount) + (_shotgunStats.pelletCount * 2);

            GlobalProjectilePool.Instance.PreWarm(_shotgunStats.bulletPrefab, optimalPoolSize);
        }
    }

    public override void OnFirePressed()
    {
        if (_shotgunStats != null && _shotgunStats.triggerType == WeaponTriggerType.SemiAuto) TryFire();
    }

    public override void OnFireHeld()
    {
        if (_shotgunStats != null && _shotgunStats.triggerType == WeaponTriggerType.FullAuto) TryFire();
    }

    public override void OnFireReleased() { }

    private void TryFire()
    {
        if (isReloading || muzzlePoint == null || _shotgunStats == null) return;

        if (Time.time >= _nextFireTime)
        {
            if (currentResource > 0)
            {
                FireShotgunBlast();
                _nextFireTime = Time.time + (_shotgunStats.firingInterval / 1000f);
            }
            else
            {
                _nextFireTime = Time.time + (_shotgunStats.firingInterval / 1000f);
                if (currentReserveAmmo > 0) Reload();
            }
        }
    }

    private void FireShotgunBlast()
    {
        currentResource--;
        NotifyResourceChange();

        // --- FIXED: Updated to PlayMuzzleEffects ---
        PlayMuzzleEffects(muzzleFlash, shootSFX, muzzlePoint, spawnFlashAsChild);

        float pelletDamage = (float)_shotgunStats.attackPower / _shotgunStats.pelletCount;

        for (int i = 0; i < _shotgunStats.pelletCount; i++)
        {
            float randomPitch = Random.Range(-_shotgunStats.spreadAngle, _shotgunStats.spreadAngle);
            float randomYaw = Random.Range(-_shotgunStats.spreadAngle, _shotgunStats.spreadAngle);

            Quaternion spreadRotation = muzzlePoint.rotation * Quaternion.Euler(randomPitch, randomYaw, 0f);

            SpawnPellet(muzzlePoint.position, spreadRotation, pelletDamage);
        }
    }

    private void SpawnPellet(Vector3 position, Quaternion rotation, float damage)
    {
        if (_shotgunStats.bulletPrefab == null || GlobalProjectilePool.Instance == null) return;

        BaseProjectile proj = GlobalProjectilePool.Instance.GetProjectile(
            _shotgunStats.bulletPrefab, position, rotation);

        proj.SetupStats(damage, _shotgunStats.bulletSpeed, shooterLayer);
        proj.SetPrefabReference(_shotgunStats.bulletPrefab);
    }
}