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

    // Tracks if we need to smooth the return
    private bool isRecovering = false;

    void LateUpdate()
    {
        if (targetBone == null) return;

        if (!isInitialized)
        {
            positionOffset = targetBone.InverseTransformPoint(transform.position);
            isInitialized = true;
        }

        if (overrideRotation)
        {
            isRecovering = true;
        }
        else
        {
            if (isRecovering)
            {
                // Smoothly glide back to the animation's current frame when you stop shooting
                transform.rotation = Quaternion.Slerp(transform.rotation, targetBone.rotation, Time.deltaTime * returnSmoothSpeed);

                // Once we are practically identical to the bone, stop recovering and lock on
                if (Quaternion.Angle(transform.rotation, targetBone.rotation) < 1f)
                {
                    isRecovering = false;
                }
            }
            else
            {
                // INSTANT SNAP: Prevents double-smoothing lag which causes high-speed jitter!
                transform.rotation = targetBone.rotation;
            }
        }

        if (syncPosition)
        {
            transform.position = targetBone.TransformPoint(positionOffset);
        }
    }
}