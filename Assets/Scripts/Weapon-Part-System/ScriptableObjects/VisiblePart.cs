using UnityEngine;

// Anything that physically spawns on the mech
public abstract class VisiblePart : Part
{
    [Header("Visuals")]
    public GameObject prefab;
}