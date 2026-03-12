using UnityEngine;

public class FunctionalWeapon : PartTemplate
{
    public virtual void OnFireHeld() {}
    public virtual void OnFirePressed() {}
    public virtual void OnFireReleased() {}
}
