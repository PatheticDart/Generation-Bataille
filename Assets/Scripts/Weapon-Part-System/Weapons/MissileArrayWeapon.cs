using UnityEngine;
using System.Collections.Generic;

public class MissileArrayWeapon : FunctionalWeapon
{
    public enum LaunchTrajectory { Direct, Vertical }

    [Header("Missile Array Settings")]
    public List<Transform> muzzlePoints = new List<Transform>();
    public PooledVFX muzzleFlash;
    public bool spawnFlashAsChild;

    public LaunchTrajectory trajectory = LaunchTrajectory.Direct;

    private FCSLockBox _fcs;
    private MissileLauncherPart _missileData;
    private float _nextFireTime = 0f;
    private float _nextStaggerTime = 0f;

    // --- NEW: Exposed so the FCS knows when to pause lock accumulation ---
    public bool IsFiringBurst { get; private set; } = false;
    
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
        if (isReloading || _missileData == null) return;

        if (!IsFiringBurst && Time.time >= _nextFireTime)
        {
            if (currentResource > 0)
            {
                int currentLocks = 1;
                _burstTarget = null;

                if (_fcs != null && _fcs.isHardLocked)
                {
                    bool isLeftWeapon = transform.parent.name.Contains("Left");
                    currentLocks = _fcs.GetMissileLocks(isLeftWeapon, _missileData.maxLocks);
                    
                    _burstTarget = _fcs.currentTarget;
                    _fcs.ConsumeMissileLocks(isLeftWeapon);
                }

                _missilesToFireThisBurst = Mathf.Min(currentLocks, muzzlePoints.Count, Mathf.FloorToInt(currentResource));

                if (_missilesToFireThisBurst > 0)
                {
                    IsFiringBurst = true;
                    _missilesFiredSoFar = 0;
                    _currentBarrelIndex = 0;
                    _nextStaggerTime = Time.time;
                }
            }
            else
            {
                _nextFireTime = Time.time + (_missileData.firingInterval / 1000f);
                if (currentReserveAmmo > 0) Reload();
            }
        }
    }

    private void Update()
    {
        if (isReloading && IsFiringBurst)
        {
            IsFiringBurst = false;
            _nextFireTime = Time.time + (_missileData.firingInterval / 1000f);
            return;
        }

        if (IsFiringBurst && Time.time >= _nextStaggerTime)
        {
            FireMissileFromCurrentTube();

            _missilesFiredSoFar++;
            _currentBarrelIndex++;
            _nextStaggerTime = Time.time + _missileData.staggerTime;

            if (_missilesFiredSoFar >= _missilesToFireThisBurst || currentResource <= 0)
            {
                IsFiringBurst = false;
                _nextFireTime = Time.time + (_missileData.firingInterval / 1000f);
            }
        }
    }

    private void FireMissileFromCurrentTube()
    {
        if (_missileData == null) return;

        Transform muzzle = muzzlePoints[_currentBarrelIndex];

        PlayMuzzleFlash(muzzleFlash, muzzle, spawnFlashAsChild);

        Quaternion spawnRotation = muzzle.rotation;
        if (trajectory == LaunchTrajectory.Vertical)
        {
            spawnRotation = Quaternion.LookRotation(muzzle.up, -muzzle.forward);
        }

        BaseProjectile proj = GlobalProjectilePool.Instance.GetProjectile(
            _missileData.bulletPrefab, muzzle.position, spawnRotation);

        proj.SetupStats(_missileData.attackPower, _missileData.bulletSpeed, shooterLayer);
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