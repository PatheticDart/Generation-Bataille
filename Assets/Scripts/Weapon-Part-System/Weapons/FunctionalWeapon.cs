using UnityEngine;
using System;

public abstract class FunctionalWeapon : PartTemplate
{
    protected Part weaponData; 

    [Header("Resource State")]
    public float currentResource;
    public float maxResource;
    public bool isOverheated { get; protected set; }

    public event Action<float, float> OnResourceChanged;

    public virtual void InitializeWeapon(Part data)
    {
        weaponData = data;
        
        if (data is ProjectileWeaponPart projPart)
        {
            maxResource = projPart.ammo;
            currentResource = maxResource;
            NotifyResourceChange();
        }
    }

    protected void NotifyResourceChange()
    {
        OnResourceChanged?.Invoke(currentResource, maxResource);
    }

    // --- UPDATED: Now accepts the specific prefab as an argument ---
    protected void PlayMuzzleFlash(PooledVFX flashPrefab, Transform spawnLocation)
    {
        if (flashPrefab != null && GlobalVFXPool.Instance != null && spawnLocation != null)
        {
            GlobalVFXPool.Instance.Spawn(flashPrefab, spawnLocation.position, spawnLocation.rotation);
        }
    }

    public abstract void OnFireHeld();
    public abstract void OnFirePressed();
    public abstract void OnFireReleased();

    public Part GetWeaponData() => weaponData;
}