using UnityEngine;
using System.Collections.Generic;

public class MultiBarrelProjectileWeapon : FunctionalWeapon
{
    public enum FireMode { Sequential, Simultaneous }

    [Header("Multi-Barrel Setup")]
    public List<Transform> muzzlePoints = new List<Transform>();
    public PooledVFX muzzleFlash; 
    public FireMode fireMode = FireMode.Sequential;

    private ProjectileWeaponPart _weaponStats; // Updated from Rifle
    private float _nextFireTime = 0f;
    private int _currentBarrelIndex = 0;

    public override void InitializeWeapon(Part data)
    {
        base.InitializeWeapon(data); 
        _weaponStats = data as ProjectileWeaponPart;

        if (_weaponStats == null) return;

        if (_weaponStats.bulletPrefab != null && GlobalProjectilePool.Instance != null)
        {
            float shotsPerSecond = 1000f / _weaponStats.firingInterval;
            float maxAliveBullets = shotsPerSecond * _weaponStats.bulletPrefab.lifetime;
            
            int multiplier = (fireMode == FireMode.Simultaneous) ? muzzlePoints.Count : 1;
            int optimalPoolSize = (Mathf.CeilToInt(maxAliveBullets) * multiplier) + 5;

            GlobalProjectilePool.Instance.PreWarm(_weaponStats.bulletPrefab, optimalPoolSize);
        }
    }

    // --- NEW INPUT ROUTING ---
    public override void OnFirePressed()
    {
        if (_weaponStats != null && _weaponStats.triggerType == WeaponTriggerType.SemiAuto)
        {
            TryFire();
        }
    }

    public override void OnFireHeld()
    {
        if (_weaponStats != null && _weaponStats.triggerType == WeaponTriggerType.FullAuto)
        {
            TryFire();
        }
    }

    public override void OnFireReleased() { }

    // --- FIRING LOGIC ---
    private void TryFire()
    {
        if (isReloading || muzzlePoints.Count == 0) return;

        if (Time.time >= _nextFireTime)
        {
            if (currentResource > 0)
            {
                if (fireMode == FireMode.Sequential) FireSequential();
                else FireSimultaneous();

                _nextFireTime = Time.time + (_weaponStats.firingInterval / 1000f);
            }
            else if (currentReserveAmmo > 0)
            {
                Reload();
            }
        }
    }

    private void FireSequential()
    {
        Transform currentMuzzle = muzzlePoints[_currentBarrelIndex];
        
        PlayMuzzleFlash(muzzleFlash, currentMuzzle);
        SpawnBullet(currentMuzzle);

        currentResource--;
        NotifyResourceChange();
        _currentBarrelIndex = (_currentBarrelIndex + 1) % muzzlePoints.Count;
    }

    private void FireSimultaneous()
    {
        foreach (Transform muzzle in muzzlePoints)
        {
            if (currentResource <= 0) break;

            PlayMuzzleFlash(muzzleFlash, muzzle);
            SpawnBullet(muzzle);
            
            currentResource--;
        }

        NotifyResourceChange();
    }

    private void SpawnBullet(Transform muzzle)
    {
        if (_weaponStats.bulletPrefab == null || GlobalProjectilePool.Instance == null) return;

        BaseProjectile proj = GlobalProjectilePool.Instance.GetProjectile(
            _weaponStats.bulletPrefab, muzzle.position, muzzle.rotation);

        proj.SetupStats(_weaponStats.attackPower, _weaponStats.bulletSpeed);
        proj.SetPrefabReference(_weaponStats.bulletPrefab);
    }
}