using UnityEngine;

public class ShotgunProjectileWeapon : FunctionalWeapon
{
    [Header("Weapon Setup")]
    public Transform muzzlePoint;
    public PooledVFX muzzleFlash;

    private ShotgunPart _shotgunStats; 
    private float _nextFireTime = 0f;

    public override void InitializeWeapon(Part data)
    {
        base.InitializeWeapon(data); 
        
        // Cast directly to the new ShotgunPart
        _shotgunStats = data as ShotgunPart;

        if (_shotgunStats == null) return;

        if (_shotgunStats.bulletPrefab != null && GlobalProjectilePool.Instance != null)
        {
            float shotsPerSecond = 1000f / _shotgunStats.firingInterval;
            float maxAliveShots = shotsPerSecond * _shotgunStats.bulletPrefab.lifetime;
            
            // Multiply the pool requirement by pellet count since each shot spawns many objects
            int optimalPoolSize = Mathf.CeilToInt(maxAliveShots * _shotgunStats.pelletCount) + (_shotgunStats.pelletCount * 2);

            GlobalProjectilePool.Instance.PreWarm(_shotgunStats.bulletPrefab, optimalPoolSize);
        }
    }

    // --- INPUT ROUTING ---
    public override void OnFirePressed()
    {
        if (_shotgunStats != null && _shotgunStats.triggerType == WeaponTriggerType.SemiAuto)
        {
            TryFire();
        }
    }

    public override void OnFireHeld()
    {
        if (_shotgunStats != null && _shotgunStats.triggerType == WeaponTriggerType.FullAuto)
        {
            TryFire();
        }
    }

    public override void OnFireReleased() { }

    // --- FIRING LOGIC ---
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
                // Apply a dry-fire cooldown penalty even if nothing fired
                _nextFireTime = Time.time + (_shotgunStats.firingInterval / 1000f);
                
                if (currentReserveAmmo > 0)
                {
                    Reload();
                }
            }
        }
    }

    private void FireShotgunBlast()
    {
        // Subtract exactly 1 ammo for the entire blast
        currentResource--;
        NotifyResourceChange();

        PlayMuzzleFlash(muzzleFlash, muzzlePoint);

        // Divide the base attack power by the number of pellets. 
        // A full meatshot connecting all pellets does exactly the SO's damage value.
        float pelletDamage = (float)_shotgunStats.attackPower / _shotgunStats.pelletCount;

        for (int i = 0; i < _shotgunStats.pelletCount; i++)
        {
            // Calculate random spread angles for Pitch (Up/Down) and Yaw (Left/Right)
            float randomPitch = Random.Range(-_shotgunStats.spreadAngle, _shotgunStats.spreadAngle);
            float randomYaw = Random.Range(-_shotgunStats.spreadAngle, _shotgunStats.spreadAngle);
            
            // Multiply the muzzle's forward rotation by our random Euler angles
            Quaternion spreadRotation = muzzlePoint.rotation * Quaternion.Euler(randomPitch, randomYaw, 0f);

            SpawnPellet(muzzlePoint.position, spreadRotation, pelletDamage);
        }
    }

    private void SpawnPellet(Vector3 position, Quaternion rotation, float damage)
    {
        if (_shotgunStats.bulletPrefab == null || GlobalProjectilePool.Instance == null) return;

        BaseProjectile proj = GlobalProjectilePool.Instance.GetProjectile(
            _shotgunStats.bulletPrefab, position, rotation);

        proj.SetupStats(damage, _shotgunStats.bulletSpeed);
        proj.SetPrefabReference(_shotgunStats.bulletPrefab);
    }
}