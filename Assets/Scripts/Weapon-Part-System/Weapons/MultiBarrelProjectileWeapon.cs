using UnityEngine;
using System.Collections.Generic;

public class MultiBarrelProjectileWeapon : FunctionalWeapon
{
    public enum FireMode { Sequential, Simultaneous }

    [Header("Multi-Barrel Setup")]
    [Tooltip("Add all muzzle points in the order they should fire.")]
    public List<Transform> muzzlePoints = new List<Transform>();
    
    [Tooltip("The muzzle flash to play for every barrel.")]
    public PooledVFX muzzleFlash; // Reverted to a single variable!
    
    public FireMode fireMode = FireMode.Sequential;

    private Rifle _rifleStats; 
    private float _nextFireTime = 0f;
    private int _currentBarrelIndex = 0;

    public override void InitializeWeapon(Part data)
    {
        base.InitializeWeapon(data); 
        _rifleStats = data as Rifle;

        if (_rifleStats == null) return;

        if (_rifleStats.bulletPrefab != null && GlobalProjectilePool.Instance != null)
        {
            float shotsPerSecond = 1000f / _rifleStats.firingInterval;
            float maxAliveBullets = shotsPerSecond * _rifleStats.bulletPrefab.lifetime;
            
            int multiplier = (fireMode == FireMode.Simultaneous) ? muzzlePoints.Count : 1;
            int optimalPoolSize = (Mathf.CeilToInt(maxAliveBullets) * multiplier) + 5;

            GlobalProjectilePool.Instance.PreWarm(_rifleStats.bulletPrefab, optimalPoolSize);
        }
    }

    public override void OnFireHeld()
    {
        if (_rifleStats == null || muzzlePoints.Count == 0) return;

        if (Time.time >= _nextFireTime && currentResource > 0)
        {
            if (fireMode == FireMode.Sequential) FireSequential();
            else FireSimultaneous();

            _nextFireTime = Time.time + (_rifleStats.firingInterval / 1000f);
        }
    }

    private void FireSequential()
    {
        Transform currentMuzzle = muzzlePoints[_currentBarrelIndex];
        
        // Play the single flash prefab at the current muzzle
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

            // Play the single flash prefab at every muzzle that fires
            PlayMuzzleFlash(muzzleFlash, muzzle);

            SpawnBullet(muzzle);
            
            currentResource--;
        }

        NotifyResourceChange();
    }

    private void SpawnBullet(Transform muzzle)
    {
        if (_rifleStats.bulletPrefab == null || GlobalProjectilePool.Instance == null) return;

        BaseProjectile proj = GlobalProjectilePool.Instance.GetProjectile(
            _rifleStats.bulletPrefab, muzzle.position, muzzle.rotation);

        proj.SetupStats(_rifleStats.attackPower, _rifleStats.bulletSpeed);
        proj.SetPrefabReference(_rifleStats.bulletPrefab);
    }

    public override void OnFirePressed() { }
    public override void OnFireReleased() { }
}