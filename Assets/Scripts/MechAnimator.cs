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
    private float velocityX;
    private float velocityZ;

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

        // 1. INPUT HANDLING & SMOOTHING
        float targetX = mechController.moveInput.x;
        float targetZ = mechController.moveInput.z;

        // DYNAMIC SMOOTHING: If the player suddenly inputs the opposite direction, slow down the animation blend
        // to simulate the mech shifting its heavy weight before changing directions.
        float currentBlendTimeX = (Mathf.Sign(currentAnimX) != Mathf.Sign(targetX) && Mathf.Abs(targetX) > 0.1f && Mathf.Abs(currentAnimX) > 0.1f)
            ? blendSmoothTime * 2.5f
            : blendSmoothTime;

        float currentBlendTimeZ = (Mathf.Sign(currentAnimZ) != Mathf.Sign(targetZ) && Mathf.Abs(targetZ) > 0.1f && Mathf.Abs(currentAnimZ) > 0.1f)
            ? blendSmoothTime * 2.5f
            : blendSmoothTime;

        currentAnimX = Mathf.SmoothDamp(currentAnimX, targetX, ref velocityX, currentBlendTimeX);
        currentAnimZ = Mathf.SmoothDamp(currentAnimZ, targetZ, ref velocityZ, currentBlendTimeZ);

        animator.SetFloat(moveXHash, currentAnimX);
        animator.SetFloat(moveZHash, currentAnimZ);

        // 2. IS MOVING LOGIC 
        bool playerWantsToMove = mechController.HasRecentMovementInput;
        animator.SetBool(isMovingHash, playerWantsToMove && !mechController.isRecoveringFromLanding);

        // 3. BOOST LOGIC 
        bool hasEnergy = stats != null && !stats.energyIsDepleted;
        bool canBoostOnGround = mechController.isBoosting && playerWantsToMove && characterController.isGrounded && hasEnergy;
        bool finalIsBoostingState = characterController.isGrounded ? canBoostOnGround : (mechController.isBoosting && hasEnergy);

        animator.SetBool(isBoostingHash, finalIsBoostingState);

        // 4. JUMP & GROUND STATES
        animator.SetBool(isJumpingHash, mechController.isPreparingToJump);
        animator.SetBool(isGroundedHash, characterController.isGrounded);
        animator.SetBool(hardLandingHash, mechController.isRecoveringFromLanding);
    }
}