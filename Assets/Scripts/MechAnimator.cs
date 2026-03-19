using UnityEngine;

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
    // FIXED: Ensured this matches your screenshot's "LocalMoveY" parameter
    private readonly int moveYHash = Animator.StringToHash("LocalMoveY");
    private readonly int isGroundedHash = Animator.StringToHash("IsGrounded");
    private readonly int isJumpingHash = Animator.StringToHash("IsJumping");
    private readonly int hardLandingHash = Animator.StringToHash("HardLanding");
    private readonly int isBoostingHash = Animator.StringToHash("IsBoosting");
    private readonly int isMovingHash = Animator.StringToHash("IsMoving");

    // NEW: Quick Boost Hashes
    private readonly int isQuickBoostingHash = Animator.StringToHash("IsQuickBoosting");
    private readonly int isPerfectQuickBoostingHash = Animator.StringToHash("IsPerfectQuickBoosting");
    private readonly int qbxHash = Animator.StringToHash("QBX");
    private readonly int qbyHash = Animator.StringToHash("QBY");

    [Header("Animation Blending")]
    [Tooltip("How long it takes (in seconds) for the walk animation to reach full speed.")]
    public float blendSmoothTime = 0.15f;

    // Internal smoothing variables
    private float currentAnimX;
    private float currentAnimY;
    private float velocityX;
    private float velocityY;

    void Start()
    {
        if (mechController == null) mechController = GetComponent<MechController>();
        if (characterController == null) characterController = GetComponent<CharacterController>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        stats = GetComponent<MechStats>();
    }

    void Update()
    {
        if (animator == null || mechController == null || characterController == null) return;

        // 1. INPUT HANDLING FOR STANDARD MOVEMENT
        float targetX = mechController.animMoveInput.x;
        float targetY = mechController.animMoveInput.z; // Unity Z maps to 2D Y in Blend Trees

        // INSTANT SNAP for dashes, so the mech leans instantly
        if (mechController.isQuickBoosting)
        {
            currentAnimX = targetX;
            currentAnimY = targetY;
            velocityX = 0f;
            velocityY = 0f;
        }
        else
        {
            // DYNAMIC SMOOTHING: Slow down blend when shifting heavy weight normally
            float currentBlendTimeX = (Mathf.Sign(currentAnimX) != Mathf.Sign(targetX) && Mathf.Abs(targetX) > 0.1f && Mathf.Abs(currentAnimX) > 0.1f)
                ? blendSmoothTime * 2.5f
                : blendSmoothTime;

            float currentBlendTimeY = (Mathf.Sign(currentAnimY) != Mathf.Sign(targetY) && Mathf.Abs(targetY) > 0.1f && Mathf.Abs(currentAnimY) > 0.1f)
                ? blendSmoothTime * 2.5f
                : blendSmoothTime;

            currentAnimX = Mathf.SmoothDamp(currentAnimX, targetX, ref velocityX, currentBlendTimeX);
            currentAnimY = Mathf.SmoothDamp(currentAnimY, targetY, ref velocityY, currentBlendTimeY);
        }

        animator.SetFloat(moveXHash, currentAnimX);
        animator.SetFloat(moveYHash, currentAnimY);

        // 2. IS MOVING LOGIC
        bool isAnimationMoving = mechController.animMoveInput.magnitude > 0.1f;
        animator.SetBool(isMovingHash, isAnimationMoving && !mechController.isRecoveringFromLanding);

        // 3. BOOST & QUICK BOOST LOGIC
        animator.SetBool(isBoostingHash, mechController.isAnimationBoosting);
        animator.SetBool(isQuickBoostingHash, mechController.isQuickBoosting);
        animator.SetBool(isPerfectQuickBoostingHash, mechController.isPerfectQuickBoosting);

        // FEED THE QUICK BOOST BLEND TREE!
        if (mechController.isQuickBoosting)
        {
            animator.SetFloat(qbxHash, mechController.qbDirection.x);
            animator.SetFloat(qbyHash, mechController.qbDirection.z);
        }

        // 4. JUMP & GROUND STATES
        animator.SetBool(isJumpingHash, mechController.isPreparingToJump);
        animator.SetBool(isGroundedHash, characterController.isGrounded);
        animator.SetBool(hardLandingHash, mechController.isRecoveringFromLanding);
    }
}