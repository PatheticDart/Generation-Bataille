using UnityEngine;

public abstract class PartFunctionality : MonoBehaviour
{
    public virtual void SpawnPart() {}
    public virtual void PartDestroyed() {}
    public virtual void PartUpdate() {}
}
