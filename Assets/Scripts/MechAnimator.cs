using UnityEngine;
using UnityEngine.Animations.Rigging; // Added in case you need to shut off RigBuilder

public class MechAnimator : MonoBehaviour
{
    [Header("Core References")]
    public MechController mechController;
    public CharacterController characterController;
    private MechStats stats;

    [Tooltip("Drag the Animation Skeleton object (which has the Animator component) here!")]
    public Animator animator;

    // Animator Hash IDs for performance
    private readonly int moveXHash = Animator.StringToHash("LocalMoveX");
    private readonly int moveZHash = Animator.StringToHash("LocalMoveZ");
    private readonly int isGroundedHash = Animator.StringToHash("IsGrounded");
    private readonly int isJumpingHash = Animator.StringToHash("IsJumping");
    private readonly int hardLandingHash = Animator.StringToHash("HardLanding");
    private readonly int isBoostingHash = Animator.StringToHash("IsBoosting");
    private readonly int isMovingHash = Animator.StringToHash("IsMoving");

    // Quick Boost Hashes
    private readonly int isQuickBoostingHash = Animator.StringToHash("IsQuickBoosting");
    private readonly int isPerfectQuickBoostingHash = Animator.StringToHash("IsPerfectQuickBoosting");
    private readonly int qbxHash = Animator.StringToHash("QBX");
    private readonly int qbyHash = Animator.StringToHash("QBY");

    // --- NEW: Death Hash ---
    private readonly int isDeadHash = Animator.StringToHash("IsDead");

    [Header("Animation Blending")]
    public float blendSmoothTime = 0.15f;
    public float qbBlendTimeX = 0.05f;
    public float qbBlendTimeY = 0.05f;

    private float currentAnimX;
    private float currentAnimZ;
    private float velocityX;
    private float velocityZ;

    private bool hasDied = false;

    // References to shut down IK
    private MechAimController aimController;
    private MechFootIK footIK;

    void Start()
    {
        if (mechController != null)
        {
            stats = mechController.GetComponent<MechStats>();
            aimController = mechController.GetComponent<MechAimController>();
            footIK = mechController.GetComponent<MechFootIK>();
        }

        if (animator == null)
            animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (animator == null || mechController == null || characterController == null) return;

        // --- 1. DEATH LOGIC ---
        if (stats != null && stats.currentArmorPoints <= 0)
        {
            if (!hasDied)
            {
                animator.SetBool(isDeadHash, true);

                // Zero out movement floats
                animator.SetFloat(moveXHash, 0f);
                animator.SetFloat(moveZHash, 0f);
                animator.SetBool(isMovingHash, false);
                animator.SetBool(isBoostingHash, false);

                // SHUT DOWN IK SO THE MECH CAN FALL OVER
                if (aimController != null)
                {
                    // Zero out the aiming weight so the rig lets go of the spine
                    if (aimController.torsoAimConstraint != null) aimController.torsoAimConstraint.weight = 0f;
                    aimController.enabled = false;
                }
                if (footIK != null) footIK.enabled = false;

                // Optional: If you use a RigBuilder component, turn it off entirely
                if (TryGetComponent(out RigBuilder rigBuilder)) rigBuilder.enabled = false;

                hasDied = true;
            }
            return; // Exit early
        }

        // --- 2. SMOOTH BLENDING FOR MOVEMENT ---
        float targetX = mechController.animMoveInput.x;
        float targetZ = mechController.animMoveInput.z;

        float currentBlendTimeX = mechController.isQuickBoosting ? qbBlendTimeX : blendSmoothTime;
        float currentBlendTimeZ = mechController.isQuickBoosting ? qbBlendTimeY : blendSmoothTime;

        currentAnimX = Mathf.SmoothDamp(currentAnimX, targetX, ref velocityX, currentBlendTimeX);
        currentAnimZ = Mathf.SmoothDamp(currentAnimZ, targetZ, ref velocityZ, currentBlendTimeZ);

        animator.SetFloat(moveXHash, currentAnimX);
        animator.SetFloat(moveZHash, currentAnimZ);

        // --- 3. IS MOVING LOGIC ---
        bool isAnimationMoving = mechController.animMoveInput.magnitude > 0.1f;
        animator.SetBool(isMovingHash, isAnimationMoving && !mechController.isRecoveringFromLanding);

        // --- 4. BOOST LOGIC ---
        animator.SetBool(isBoostingHash, mechController.isAnimationBoosting);

        // --- 5. JUMP & GROUND STATES ---
        animator.SetBool(isJumpingHash, mechController.isPreparingToJump);
        animator.SetBool(isGroundedHash, characterController.isGrounded);
        animator.SetBool(hardLandingHash, mechController.isRecoveringFromLanding);

        // --- 6. QUICK BOOST ANIMATION OVERRIDES ---
        animator.SetBool(isQuickBoostingHash, mechController.isQuickBoosting);
        animator.SetBool(isPerfectQuickBoostingHash, mechController.isPerfectQuickBoosting);

        if (mechController.isQuickBoosting)
        {
            animator.SetFloat(qbxHash, mechController.LastQBDirection.x);
            animator.SetFloat(qbyHash, mechController.LastQBDirection.z);
        }
    }
}