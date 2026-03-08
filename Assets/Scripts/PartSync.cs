using UnityEngine;

public class PartSync : MonoBehaviour
{
    public Transform targetBone;
    public bool syncPosition = false;

    // Lets us override the animation temporarily for manual aiming
    public bool overrideRotation = false;

    [Tooltip("How smoothly the part returns to the animation pose when deactivated.")]
    public float returnSmoothSpeed = 20f;

    private Vector3 positionOffset;
    private bool isInitialized = false;

    void LateUpdate()
    {
        if (targetBone == null) return;

        if (!isInitialized)
        {
            positionOffset = targetBone.InverseTransformPoint(transform.position);
            isInitialized = true;
        }

        if (!overrideRotation)
        {
            // Smoothly glide back to the animation's current frame
            transform.rotation = Quaternion.Slerp(transform.rotation, targetBone.rotation, Time.deltaTime * returnSmoothSpeed);
        }

        if (syncPosition)
        {
            transform.position = targetBone.TransformPoint(positionOffset);
        }
    }
}