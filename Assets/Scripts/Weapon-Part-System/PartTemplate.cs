using UnityEngine;
using UnityEngine.Events;

public abstract class PartTemplate : MonoBehaviour
{
    public UnityEvent OnPartSpawned;
    public UnityEvent OnPartDestroyed;

    public virtual void SpawnPart() {}
    public void PartDestroyed() {}
    public void PartUpdate() {}
}
