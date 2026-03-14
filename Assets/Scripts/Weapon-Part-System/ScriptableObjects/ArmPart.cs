using UnityEngine;

[CreateAssetMenu(fileName = "ArmPart", menuName = "Parts/ArmPart")]
public class ArmPart : BodyPart
{
    [Header("Unique Stats")]
    public int recoilControl;
}

