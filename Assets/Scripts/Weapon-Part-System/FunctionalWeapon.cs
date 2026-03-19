using UnityEngine;
using System;

public abstract class FunctionalWeapon : PartTemplate
{
    protected Part weaponData; 

    [Header("Resource State")]
    public float currentResource;
    public float maxResource;
    public bool isOverheated { get; protected set; }

    // UI scripts subscribe to this to update bars/numbers
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

    public abstract void OnFireHeld();
    public abstract void OnFirePressed();
    public abstract void OnFireReleased();

    public Part GetWeaponData() => weaponData;
}