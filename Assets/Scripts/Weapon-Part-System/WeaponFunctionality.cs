using UnityEngine;

public class FunctionalWeapon : PartTemplate
{
    protected Part weaponData; 

    public virtual void InitializeWeapon(Part data)
    {
        weaponData = data;
    }

    public virtual void OnFireHeld() {}
    public virtual void OnFirePressed() {}
    public virtual void OnFireReleased() {}
}