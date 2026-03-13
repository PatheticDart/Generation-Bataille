using UnityEngine;
using UnityEngine.Events;

public abstract class PartTemplate : MonoBehaviour
{
    public UnityEvent OnPartSpawned;
    public UnityEvent OnPartDestroyed;

    public virtual void SpawnPart() {}
    public virtual void PartDestroyed() {}
    public virtual void PartUpdate() {}
}
