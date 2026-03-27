using UnityEngine;
using System;
using System.Collections; // Needed for Coroutines

public abstract class FunctionalWeapon : PartTemplate
{
    protected Part weaponData; 

    [Header("Resource State")]
    public float currentResource;   // Ammo currently in the magazine
    public float maxResource;       // Size of the magazine
    public float currentReserveAmmo;// Ammo in your pockets
    
    public bool isOverheated { get; protected set; }
    public bool isReloading { get; protected set; } // NEW: Tracks reload state

    protected int weaponMagSize = -1;
    protected float weaponReloadTime = 0f;

    // Events for UI
    public event Action<float, float> OnResourceChanged;
    public event Action<float> OnReserveAmmoChanged; // NEW: So UI can show "30 / 120"

    public virtual void InitializeWeapon(Part data)
    {
        weaponData = data;
        
        if (data is ProjectileWeaponPart projPart)
        {
            weaponMagSize = projPart.magSize;
            weaponReloadTime = projPart.reloadTime;

            if (weaponMagSize != -1)
            {
                // Weapon uses magazines
                maxResource = weaponMagSize;
                currentResource = Mathf.Min(projPart.ammo, weaponMagSize);
                currentReserveAmmo = Mathf.Max(0, projPart.ammo - currentResource);
            }
            else
            {
                // Weapon has a bottomless clip
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

    // --- NEW: UNIVERSAL RELOAD LOGIC ---
    public virtual void Reload()
    {
        // Don't reload if already reloading, if it's a bottomless mag, if the mag is full, or if we have no extra ammo
        if (isReloading || weaponMagSize == -1 || currentResource >= maxResource || currentReserveAmmo <= 0) return;
        
        StartCoroutine(ReloadRoutine());
    }

    protected virtual IEnumerator ReloadRoutine()
    {
        isReloading = true;
        // NOTE: You can invoke an OnReloadStart event here later for Audio/UI animations!

        yield return new WaitForSeconds(weaponReloadTime);

        // Math to figure out how much we need vs how much we actually have
        float ammoNeeded = maxResource - currentResource;
        float ammoToLoad = Mathf.Min(ammoNeeded, currentReserveAmmo);

        currentResource += ammoToLoad;
        currentReserveAmmo -= ammoToLoad;

        isReloading = false;
        NotifyResourceChange();
    }

    protected void PlayMuzzleFlash(PooledVFX flashPrefab, Transform spawnLocation, bool spawnFlashAsChild)
    {
        if (flashPrefab != null && GlobalVFXPool.Instance != null && spawnLocation != null)
        {
            var p = GlobalVFXPool.Instance.Spawn(flashPrefab, spawnLocation.position, spawnLocation.rotation);

            if (spawnFlashAsChild) p.transform.parent = spawnLocation;
        }
    }

    public abstract void OnFireHeld();
    public abstract void OnFirePressed();
    public abstract void OnFireReleased();

    public Part GetWeaponData() => weaponData;
}