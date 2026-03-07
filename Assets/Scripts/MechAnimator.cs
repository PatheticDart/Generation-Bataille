using UnityEngine;

public class MechAnimator : MonoBehaviour
{
    [Header("Core References")]
    public MechController mechController;
    public CharacterController characterController;

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

    [Header("Animation Blending")]
    [Tooltip("How long it takes (in seconds) for the walk animation to reach full speed.")]
    public float blendSmoothTime = 0.15f;

    // Internal smoothing variables
    private float currentAnimX;
    private float currentAnimZ;
    private float velocityX; // Required for SmoothDamp math
    private float velocityZ; // Required for SmoothDamp math

    void Start()
    {
        if (mechController == null) mechController = GetComponent<MechController>();
        if (characterController == null) characterController = GetComponent<CharacterController>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (animator == null || mechController == null || characterController == null) return;

        // 1. TRUE ACCELERATION FOR BLEND TREES
        float targetX = mechController.moveInput.x;
        float targetZ = mechController.moveInput.z;

        // SmoothDamp naturally accelerates the value towards the target over 'blendSmoothTime'
        currentAnimX = Mathf.SmoothDamp(currentAnimX, targetX, ref velocityX, blendSmoothTime);
        currentAnimZ = Mathf.SmoothDamp(currentAnimZ, targetZ, ref velocityZ, blendSmoothTime);

        animator.SetFloat(moveXHash, currentAnimX);
        animator.SetFloat(moveZHash, currentAnimZ);

        // 2. INSTANT STATE TRANSITIONS
        // We keep this completely separate from the smoothing so the Animator instantly enters/exits the Walk state
        bool isMoving = mechController.moveInput.magnitude > 0.1f;
        animator.SetBool(isMovingHash, isMoving);

        // 3. PASS THE REST OF THE STATES
        animator.SetBool(isGroundedHash, characterController.isGrounded);
        animator.SetBool(isJumpingHash, mechController.isJumping && !characterController.isGrounded);
        animator.SetBool(hardLandingHash, mechController.isRecoveringFromLanding);
        animator.SetBool(isBoostingHash, mechController.isBoosting);
    }
}