using UnityEngine;
using System;
using System.Collections;

public abstract class FunctionalWeapon : PartTemplate
{
    protected Part weaponData;

    [Header("Resource State")]
    public float currentResource;
    public float maxResource;
    public float currentReserveAmmo;

    public bool isOverheated { get; protected set; }
    public bool isReloading { get; protected set; }

    protected int weaponMagSize = -1;
    protected float weaponReloadTime = 0f;

    protected int shooterLayer;

    // --- NEW: AudioSource reference for shooting SFX ---
    protected AudioSource weaponAudioSource;

    public event Action<float, float> OnResourceChanged;
    public event Action<float> OnReserveAmmoChanged;

    public virtual void InitializeWeapon(Part data)
    {
        weaponData = data;

        MechStats stats = GetComponentInParent<MechStats>();
        shooterLayer = stats != null ? stats.gameObject.layer : gameObject.layer;

        // --- NEW: Grab the AudioSource, or create one if it doesn't exist ---
        weaponAudioSource = GetComponent<AudioSource>();
        if (weaponAudioSource == null)
        {
            weaponAudioSource = gameObject.AddComponent<AudioSource>();
            weaponAudioSource.spatialBlend = 1.0f; // Force 3D sound
            weaponAudioSource.playOnAwake = false;
        }

        if (data is ProjectileWeaponPart projPart)
        {
            weaponMagSize = projPart.magSize;
            weaponReloadTime = projPart.reloadTime;

            if (weaponMagSize != -1)
            {
                maxResource = weaponMagSize;
                currentResource = Mathf.Min(projPart.ammo, weaponMagSize);
                currentReserveAmmo = Mathf.Max(0, projPart.ammo - currentResource);
            }
            else
            {
                maxResource = projPart.ammo;
                currentResource = maxResource;
                currentReserveAmmo = 0;
            }

            NotifyResourceChange();
        }
    }

    protected void NotifyResourceChange()
    {
        OnResourceChanged?.Invoke(currentResource, maxResource);
        OnReserveAmmoChanged?.Invoke(currentReserveAmmo);
    }

    public virtual void Reload()
    {
        if (isReloading || weaponMagSize == -1 || currentResource >= maxResource || currentReserveAmmo <= 0) return;
        StartCoroutine(ReloadRoutine());
    }

    protected virtual IEnumerator ReloadRoutine()
    {
        isReloading = true;
        yield return new WaitForSeconds(weaponReloadTime);

        float ammoNeeded = maxResource - currentResource;
        float ammoToLoad = Mathf.Min(ammoNeeded, currentReserveAmmo);

        currentResource += ammoToLoad;
        currentReserveAmmo -= ammoToLoad;

        isReloading = false;
        NotifyResourceChange();
    }

    // --- UPDATED: Renamed to PlayMuzzleEffects and added AudioClip parameter ---
    protected void PlayMuzzleEffects(PooledVFX flashPrefab, AudioClip shootSFX, Transform spawnLocation, bool spawnFlashAsChild)
    {
        // 1. Play the Visuals
        if (flashPrefab != null && GlobalVFXPool.Instance != null && spawnLocation != null)
        {
            var p = GlobalVFXPool.Instance.Spawn(flashPrefab, spawnLocation.position, spawnLocation.rotation);
            if (spawnFlashAsChild) p.transform.parent = spawnLocation;
        }

        // 2. Play the Sound Effect
        if (shootSFX != null && weaponAudioSource != null)
        {
            // PlayOneShot allows multiple rapid-fire sounds to overlap naturally
            weaponAudioSource.PlayOneShot(shootSFX);
        }
    }

    public abstract void OnFireHeld();
    public abstract void OnFirePressed();
    public abstract void OnFireReleased();

    public Part GetWeaponData() => weaponData;
}