using JetBrains.Annotations;
using UnityEngine;

public abstract class Part : ScriptableObject
{
    public PartFunctionality prefab;

    [Header("Garage Metadata")]
    public string partName;
    [TextArea]
    public string partDescription;
    public int price;

    
    [Header("Stats")]
    public int weight;
}
