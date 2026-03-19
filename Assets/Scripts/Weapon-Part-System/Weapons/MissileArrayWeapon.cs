using UnityEngine;
using System.Collections.Generic;

public class MissileArrayWeapon : FunctionalWeapon
{
    public enum LaunchTrajectory { Direct, Vertical }

    [Header("Missile Array Settings")]
    public List<Transform> muzzlePoints = new List<Transform>();
    public LaunchTrajectory trajectory = LaunchTrajectory.Direct;
    
    // Internal State
    private FCSLockBox _fcs;
    private MissileLauncherPart _missileData; // Refactored: Now uses specialized MissileLauncher SO
    private float _nextFireTime = 0f;
    private float _nextStaggerTime = 0f;
    
    // Burst Tracking
    private bool _isFiringBurst = false;
    private int _currentBarrelIndex = 0;
    private int _missilesToFireThisBurst = 0;
    private int _missilesFiredSoFar = 0;
    private Transform _burstTarget;

    public override void InitializeWeapon(Part data)
    {
        base.InitializeWeapon(data); 
        
        // 1. Cast specifically to MissileLauncher
        _missileData = data as MissileLauncherPart;
        
        _fcs = transform.root.GetComponentInChildren<FCSLockBox>();

        if (_missileData != null && _missileData.bulletPrefab != null && GlobalProjectilePool.Instance != null)
        {
            // 2. Pre-warm the pool based on the SO's prefab
            GlobalProjectilePool.Instance.PreWarm(_missileData.bulletPrefab, muzzlePoints.Count * 3);
        }
    }

    public override void OnFirePressed()
    {
        // Null check against our new data container
        if (_missileData == null || muzzlePoints.Count == 0) return;

        if (!_isFiringBurst && Time.time >= _nextFireTime && currentResource > 0)
        {
            int currentLocks = 1; 
            _burstTarget = null;

            if (_fcs != null && _fcs.isHardLocked)
            {
                currentLocks = _fcs.currentLockCount;
                _burstTarget = _fcs.currentTarget; 
                _fcs.ConsumeLocks(); 
            }

            // Limit missiles by locks, available tubes, and current ammo
            _missilesToFireThisBurst = Mathf.Min(currentLocks, muzzlePoints.Count, Mathf.FloorToInt(currentResource));
            
            if (_missilesToFireThisBurst > 0)
            {
                _isFiringBurst = true;
                _missilesFiredSoFar = 0;
                _currentBarrelIndex = 0;
                _nextStaggerTime = Time.time; 
            }
        }
    }

    private void Update()
    {
        if (_isFiringBurst && Time.time >= _nextStaggerTime)
        {
            FireMissileFromCurrentTube();

            _missilesFiredSoFar++;
            _currentBarrelIndex++; 
            
            // 3. staggerTime is now pulled from the ScriptableObject!
            _nextStaggerTime = Time.time + _missileData.staggerTime;

            if (_missilesFiredSoFar >= _missilesToFireThisBurst || currentResource <= 0)
            {
                _isFiringBurst = false;
                
                // 4. Use firingInterval from the SO base class
                _nextFireTime = Time.time + (_missileData.firingInterval / 1000f);
            }
        }
    }

    private void FireMissileFromCurrentTube()
    {
        if (_missileData == null) return;

        Transform muzzle = muzzlePoints[_currentBarrelIndex];
        
        Quaternion spawnRotation = muzzle.rotation;
        if (trajectory == LaunchTrajectory.Vertical)
        {
            spawnRotation = Quaternion.LookRotation(muzzle.up, -muzzle.forward);
        }

        BaseProjectile proj = GlobalProjectilePool.Instance.GetProjectile(
            _missileData.bulletPrefab, muzzle.position, spawnRotation);

        // 5. Setup stats using MissileLauncher's inherited data
        proj.SetupStats(_missileData.attackPower, _missileData.bulletSpeed);
        proj.SetPrefabReference(_missileData.bulletPrefab);

        if (proj is HomingMissile homingMissile)
        {
            homingMissile.SetHomingData(_burstTarget);
        }

        currentResource--;
        NotifyResourceChange();
    }

    public override void OnFireHeld() { }
    public override void OnFireReleased() { } 
}