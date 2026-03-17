using UnityEngine;

// Anything physical that drains the generator's energy
public abstract class ChargedPart : VisiblePart
{
    [Header("Power")]
    public int energyDrain;
}