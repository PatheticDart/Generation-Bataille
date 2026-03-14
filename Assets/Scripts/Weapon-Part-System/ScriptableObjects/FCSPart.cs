using System;
using UnityEngine;

[CreateAssetMenu(fileName = "FCSPart", menuName = "Parts/FCSPart")]
public class FCSPart : Part
{
    [Header("Unique Stats")]
    public Vector2Int lockSize;
    public TargetingType targetingType;
    public int maximumLock;
    public int lockTime;
    public Vector2Int maxCapture;
    public Vector2Int averageCapture;
    public Vector2Int maxLockRange;
    public Vector2Int avgLockRange;
    public int precision;
}

[Serializable]
public enum TargetingType
{
    Single, Multiple
}