using UnityEngine;

public abstract class BodyPart : ChargedPart
{
    [Header("Armor")]
    public int armorPoints;
    public int shellDef;
    public int energyDef;
}