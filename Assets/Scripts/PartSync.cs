using UnityEngine;

public class PartSync : MonoBehaviour
{
    public Transform targetBone;
    
    [Tooltip("If true, this part will copy the skeleton's positional animations (the delta) without snapping to its exact location.")]
    public bool syncPosition = false;

    private Vector3 initialTargetLocalPos;
    private Vector3 initialMyLocalPos;
    private bool isInitialized = false;

    void LateUpdate()
    {
        if (targetBone == null) return;

        // On the very first frame, capture the resting positions of both the bone and the spawned mesh
        if (!isInitialized)
        {
            initialTargetLocalPos = targetBone.localPosition;
            initialMyLocalPos = transform.localPosition;
            isInitialized = true;
        }

        // 1. Always copy absolute world rotation
        transform.rotation = targetBone.rotation;

        // 2. Apply relative position delta (The Difference)
        if (syncPosition)
        {
            // Calculate how far the animation bone has moved from its starting position
            Vector3 localDelta = targetBone.localPosition - initialTargetLocalPos;
            
            // Add that exact difference to the 3D mesh's starting position
            transform.localPosition = initialMyLocalPos + localDelta;
        }
    }
}