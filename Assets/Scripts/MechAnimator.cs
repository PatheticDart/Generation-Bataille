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

        // 1. INPUT HANDLING & SMOOTHING (WASD)
        float targetX;
        float targetZ;

        // Stagger logic handles the HardLanding blend treeConvergence back to neutral (0,0)
        if (mechController.isRecoveringFromLanding)
        {
            Vector3 flatVelocity = new Vector3(characterController.velocity.x, 0f, characterController.velocity.z);
            Vector3 localVelocity = mechController.mechBody.InverseTransformDirection(flatVelocity);
            float refSpeed = (stats != null) ? stats.walkSpeed : 15f;
            targetX = Mathf.Clamp(localVelocity.x / refSpeed, -1f, 1f);
            targetZ = Mathf.Clamp(localVelocity.z / refSpeed, -1f, 1f);
        }
        else
        {
            targetX = mechController.moveInput.x;
            targetZ = mechController.moveInput.z;
        }

        currentAnimX = Mathf.SmoothDamp(currentAnimX, targetX, ref velocityX, blendSmoothTime);
        currentAnimZ = Mathf.SmoothDamp(currentAnimZ, targetZ, ref velocityZ, blendSmoothTime);

        animator.SetFloat(moveXHash, currentAnimX);
        animator.SetFloat(moveZHash, currentAnimZ);

        // 2. IS MOVING LOGIC 
        // We now pull from the buffered input so the run/boost animations don't instantly snap off if the player stutters
        bool playerWantsToMove = mechController.HasRecentMovementInput;

        // IsMoving parameter should be instant so transitions are snappy
        animator.SetBool(isMovingHash, playerWantsToMove && !mechController.isRecoveringFromLanding);

        // 3. BOOST LOGIC 
        bool hasEnergy = stats != null && !stats.energyIsDepleted;

        // You only animate boosting if holding shift AND pressing WASD (or within the buffer) AND grounded AND have energy.
        bool canBoostOnGround = mechController.isBoosting && playerWantsToMove && characterController.isGrounded && hasEnergy;

        bool finalIsBoostingState = characterController.isGrounded ? canBoostOnGround : (mechController.isBoosting && hasEnergy);

        animator.SetBool(isBoostingHash, finalIsBoostingState);

        // 4. JUMP & GROUND STATES
        animator.SetBool(isJumpingHash, mechController.isPreparingToJump);
        animator.SetBool(isGroundedHash, characterController.isGrounded);
        animator.SetBool(hardLandingHash, mechController.isRecoveringFromLanding);
    }
}