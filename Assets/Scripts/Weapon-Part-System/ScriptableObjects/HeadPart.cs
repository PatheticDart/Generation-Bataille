using UnityEngine;

[CreateAssetMenu(fileName = "HeadPart", menuName = "Parts/HeadPart")]
public class HeadPart : BodyPart
{
    [Header("Unique Stats")]
    public float lockOnTimeMultiplier = .9f;
    public float radarPollRate = .5f;
    public float radarScale = 1;
}