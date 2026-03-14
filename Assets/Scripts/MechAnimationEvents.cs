using UnityEngine;

public class MechAnimationEvents : MonoBehaviour
{
    [Tooltip("Drag your Main Camera (or the object holding CameraEffects) here.")]
    public CameraEffects cameraEffects;

    // Call this exact name from your Animation Event
    public void OnFootstep(float intensity)
    {
        if (cameraEffects != null)
        {
            cameraEffects.TriggerFootstepShake(intensity);
        }
    }
}