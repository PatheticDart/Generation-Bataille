using UnityEngine;

public class MechFootIK : MonoBehaviour
{
    [System.Serializable]
    public class FootRig
    {
        [Tooltip("The foot target being moved by the Animator.")]
        public Transform animatedTarget;
        [Tooltip("The actual IK Target the Two-Bone IK Constraint is looking at.")]
        public Transform solvedTarget;
    }

    [Header("Feet Setup")]
    public FootRig leftFoot;
    public FootRig rightFoot;

    [Header("Raycast Settings")]
    public LayerMask groundLayer;
    [Tooltip("How high above the foot to start the raycast (prevents clipping inside stairs).")]
    public float rayOriginHeight = 2f;
    [Tooltip("How far down to shoot the raycast. Should be slightly longer than rayOriginHeight.")]
    public float raycastDistance = 4f;
    [Tooltip("The distance from the foot bone to the absolute bottom of the mech's sole.")]
    public float soleThickness = 0.2f;

    [Header("Smoothing")]
    public float positionLerpSpeed = 20f;
    public float rotationLerpSpeed = 15f;

    void LateUpdate()
    {
        UpdateFoot(leftFoot);
        UpdateFoot(rightFoot);
    }

    private void UpdateFoot(FootRig foot)
    {
        if (foot.animatedTarget == null || foot.solvedTarget == null) return;

        // 1. Start with the exact position and rotation the Animator wants
        Vector3 targetPosition = foot.animatedTarget.position;
        Quaternion targetRotation = foot.animatedTarget.rotation;

        // 2. Shoot a raycast straight down from above the animated foot
        Vector3 rayOrigin = foot.animatedTarget.position + (Vector3.up * rayOriginHeight);

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, raycastDistance, groundLayer))
        {
            float surfaceHeight = hit.point.y + soleThickness;

            // 3. We only snap the foot UP if the ground is higher than the animation.
            // If the animation is lifting the leg (like taking a step or jumping), we let it stay high!
            if (foot.animatedTarget.position.y < surfaceHeight)
            {
                targetPosition.y = surfaceHeight;

                // 4. Rotate the foot to match the slope of the hill
                Vector3 footForward = Vector3.ProjectOnPlane(foot.animatedTarget.forward, hit.normal);
                targetRotation = Quaternion.LookRotation(footForward, hit.normal);
            }
        }

        // 5. Smoothly move the actual IK target to the calculated position
        foot.solvedTarget.position = Vector3.Lerp(foot.solvedTarget.position, targetPosition, Time.deltaTime * positionLerpSpeed);
        foot.solvedTarget.rotation = Quaternion.Slerp(foot.solvedTarget.rotation, targetRotation, Time.deltaTime * rotationLerpSpeed);
    }
}