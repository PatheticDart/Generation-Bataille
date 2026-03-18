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
        
        // If it's a Rifle, treat ammo as our resource
        if (data is Rifle rifleData)
        {
            maxResource = rifleData.ammo;
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
}