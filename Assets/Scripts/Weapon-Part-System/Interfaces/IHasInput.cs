using UnityEngine;

public interface IHasInput
{
    virtual void OnFire() {}
    virtual void OnFirePress() {}
    virtual void OnFireRelease() {}
}
