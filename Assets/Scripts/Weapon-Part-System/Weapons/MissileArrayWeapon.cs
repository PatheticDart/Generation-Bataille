using UnityEngine;
using System.Collections.Generic;

public class MissileArrayWeapon : FunctionalWeapon
{
    public enum LaunchTrajectory { Direct, Vertical }

    [Header("Missile Array Settings")]
    public List<Transform> muzzlePoints = new List<Transform>();
    public PooledVFX muzzleFlash; 
    public LaunchTrajectory trajectory = LaunchTrajectory.Direct;
    
    private FCSLockBox _fcs;
    private MissileLauncherPart _missileData; 
    private float _nextFireTime = 0f;
    private float _nextStaggerTime = 0f;
    
    private bool _isFiringBurst = false;
    private int _currentBarrelIndex = 0;
    private int _missilesToFireThisBurst = 0;
    private int _missilesFiredSoFar = 0;
    private Transform _burstTarget;

    public override void InitializeWeapon(Part data)
    {
        base.InitializeWeapon(data); 
        _missileData = data as MissileLauncherPart;
        _fcs = transform.root.GetComponentInChildren<FCSLockBox>();

        if (_missileData != null && _missileData.bulletPrefab != null && GlobalProjectilePool.Instance != null)
        {
            GlobalProjectilePool.Instance.PreWarm(_missileData.bulletPrefab, muzzlePoints.Count * 3);
        }
    }

    public override void OnFirePressed()
    {
        // Block firing if currently reloading
        if (isReloading || _missileData == null) return;

        // Check if the weapon has cooled down from the last shot/burst
        if (!_isFiringBurst && Time.time >= _nextFireTime)
        {
            if (currentResource > 0)
            {
                // --- NORMAL FIRING LOGIC ---
                int currentLocks = 1; 
                _burstTarget = null;

                if (_fcs != null && _fcs.isHardLocked)
                {
                    currentLocks = _fcs.currentLockCount;
                    _burstTarget = _fcs.currentTarget; 
                    _fcs.ConsumeLocks(); 
                }

                _missilesToFireThisBurst = Mathf.Min(currentLocks, muzzlePoints.Count, Mathf.FloorToInt(currentResource));

                if (_missilesToFireThisBurst > 0)
                {
                    _isFiringBurst = true;
                    _missilesFiredSoFar = 0;
                    _currentBarrelIndex = 0;
                    _nextStaggerTime = Time.time; 
                }
            }
            else
            {
                // --- DRY FIRE LOGIC ---
                // Apply the cooldown penalty even if nothing fired
                _nextFireTime = Time.time + (_missileData.firingInterval / 1000f);
                
                // Auto-reload if we have spare ammo
                if (currentReserveAmmo > 0)
                {
                    Reload();
                }
            }
        }
    }
    
    private void Update()
    {
        // Safety Catch: Cancel the burst if a reload is triggered mid-volley
        if (isReloading && _isFiringBurst)
        {
            _isFiringBurst = false;
            _nextFireTime = Time.time + (_missileData.firingInterval / 1000f);
            return;
        }

        if (_isFiringBurst && Time.time >= _nextStaggerTime)
        {
            FireMissileFromCurrentTube();

            _missilesFiredSoFar++;
            _currentBarrelIndex++; 
            _nextStaggerTime = Time.time + _missileData.staggerTime;

            // Check if the burst is finished, or if we ran out of ammo mid-burst
            if (_missilesFiredSoFar >= _missilesToFireThisBurst || currentResource <= 0)
            {
                _isFiringBurst = false;
                _nextFireTime = Time.time + (_missileData.firingInterval / 1000f);
            }
        }
    }

    private void FireMissileFromCurrentTube()
    {
        if (_missileData == null) return;

        Transform muzzle = muzzlePoints[_currentBarrelIndex];
        
        PlayMuzzleFlash(muzzleFlash, muzzle);
        
        Quaternion spawnRotation = muzzle.rotation;
        if (trajectory == LaunchTrajectory.Vertical)
        {
            spawnRotation = Quaternion.LookRotation(muzzle.up, -muzzle.forward);
        }

        BaseProjectile proj = GlobalProjectilePool.Instance.GetProjectile(
            _missileData.bulletPrefab, muzzle.position, spawnRotation);

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