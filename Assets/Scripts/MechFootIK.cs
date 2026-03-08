using UnityEngine;

public class MechFootIK : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The target that is being moved by your Animation Window.")]
    public Transform animatedTarget;
    
    [Header("Raycast Settings")]
    public LayerMask groundLayer;
    [Tooltip("How high above the foot to start the raycast (prevents clipping inside slopes).")]
    public float raycastOriginOffset = 2f;
    [Tooltip("How far down to shoot the raycast.")]
    public float raycastDistance = 4f;
    [Tooltip("The distance from the foot bone to the bottom of the mech's sole.")]
    public float soleThickness = 0.2f;

    [Header("Smoothing")]
    public float positionLerpSpeed = 20f;
    public float rotationLerpSpeed = 15f;

    void LateUpdate()
    {
        if (animatedTarget == null) return;

        // Start with the exact position and rotation the Animator wants
        Vector3 targetPosition = animatedTarget.position;
        Quaternion targetRotation = animatedTarget.rotation;

        // Shoot a raycast straight down from above the animated foot
        Vector3 rayOrigin = animatedTarget.position + (Vector3.up * raycastOriginOffset);
        
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, raycastDistance, groundLayer))
        {
            // We only want to snap the foot UP if the ground is higher than the animation.
            // If the animation is lifting the leg (like taking a step), we let it stay high.
            float surfaceHeight = hit.point.y + soleThickness;

            if (animatedTarget.position.y < surfaceHeight)
            {
                targetPosition.y = surfaceHeight;
                
                // Optional: Rotate the foot to match the slope of the hill
                Vector3 footForward = Vector3.ProjectOnPlane(animatedTarget.forward, hit.normal);
                targetRotation = Quaternion.LookRotation(footForward, hit.normal);
            }
        }

        // Smoothly move the actual IK target to the calculated position
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * positionLerpSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationLerpSpeed);
    }
}