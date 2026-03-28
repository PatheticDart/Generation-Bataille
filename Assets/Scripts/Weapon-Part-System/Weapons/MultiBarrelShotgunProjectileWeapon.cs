using UnityEngine;
using System.Collections.Generic;

public class MultiBarrelShotgunProjectileWeapon : FunctionalWeapon
{
    public enum FireMode { Sequential, Simultaneous }

    [Header("Multi-Barrel Setup")]
    public List<Transform> muzzlePoints = new List<Transform>();
    public PooledVFX muzzleFlash; 
    public FireMode fireMode = FireMode.Sequential;
    public bool spawnFlashAsChild;

    private ShotgunPart _shotgunStats; 
    private float _nextFireTime = 0f;
    private int _currentBarrelIndex = 0;

    public override void InitializeWeapon(Part data)
    {
        base.InitializeWeapon(data); 
        _shotgunStats = data as ShotgunPart;

        if (_shotgunStats == null) return;

        if (_shotgunStats.bulletPrefab != null && GlobalProjectilePool.Instance != null)
        {
            float shotsPerSecond = 1000f / _shotgunStats.firingInterval;
            float maxAliveShots = shotsPerSecond * _shotgunStats.bulletPrefab.lifetime;
            
            int multiplier = (fireMode == FireMode.Simultaneous) ? muzzlePoints.Count : 1;
            int optimalPoolSize = Mathf.CeilToInt(maxAliveShots * _shotgunStats.pelletCount * multiplier) + (_shotgunStats.pelletCount * 2);

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
        if (isReloading || muzzlePoints.Count == 0 || _shotgunStats == null) return;

        if (Time.time >= _nextFireTime)
        {
            if (currentResource > 0)
            {
                if (fireMode == FireMode.Sequential) FireSequential();
                else FireSimultaneous();

                _nextFireTime = Time.time + (_shotgunStats.firingInterval / 1000f);
            }
            else
            {
                _nextFireTime = Time.time + (_shotgunStats.firingInterval / 1000f);
                if (currentReserveAmmo > 0) Reload();
            }
        }
    }

    private void FireSequential()
    {
        Transform currentMuzzle = muzzlePoints[_currentBarrelIndex];
        
        PlayMuzzleFlash(muzzleFlash, currentMuzzle, spawnFlashAsChild);
        SpawnShotgunBlast(currentMuzzle);

        currentResource--;
        NotifyResourceChange();
        
        _currentBarrelIndex = (_currentBarrelIndex + 1) % muzzlePoints.Count;
    }

    private void FireSimultaneous()
    {
        foreach (Transform muzzle in muzzlePoints)
        {
            if (currentResource <= 0) break;

            PlayMuzzleFlash(muzzleFlash, muzzle, spawnFlashAsChild);
            SpawnShotgunBlast(muzzle);
            
            currentResource--;
        }

        NotifyResourceChange();
    }

    private void SpawnShotgunBlast(Transform muzzle)
    {
        float pelletDamage = (float)_shotgunStats.attackPower / _shotgunStats.pelletCount;

        for (int i = 0; i < _shotgunStats.pelletCount; i++)
        {
            float randomPitch = Random.Range(-_shotgunStats.spreadAngle, _shotgunStats.spreadAngle);
            float randomYaw = Random.Range(-_shotgunStats.spreadAngle, _shotgunStats.spreadAngle);
            
            Quaternion spreadRotation = muzzle.rotation * Quaternion.Euler(randomPitch, randomYaw, 0f);

            SpawnPellet(muzzle.position, spreadRotation, pelletDamage);
        }
    }

    private void SpawnPellet(Vector3 position, Quaternion rotation, float damage)
    {
        if (_shotgunStats.bulletPrefab == null || GlobalProjectilePool.Instance == null) return;

        BaseProjectile proj = GlobalProjectilePool.Instance.GetProjectile(
            _shotgunStats.bulletPrefab, position, rotation);

        // --- FIXED: Pass the shooterLayer ---
        proj.SetupStats(damage, _shotgunStats.bulletSpeed, shooterLayer);
        proj.SetPrefabReference(_shotgunStats.bulletPrefab);
    }
}