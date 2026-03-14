using UnityEngine;

[CreateAssetMenu(fileName = "GeneratorPart", menuName = "Parts/Generator")]
public class Generator : Part
{
    [Header("Unique Stats")]
    public int energyCapacity;
    public int energyOutput;
}