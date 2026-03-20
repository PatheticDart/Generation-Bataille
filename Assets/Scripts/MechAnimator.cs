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

    // --- ADDED: Quick Boost Hashes ---
    private readonly int isQuickBoostingHash = Animator.StringToHash("IsQuickBoosting");
    private readonly int isPerfectQuickBoostingHash = Animator.StringToHash("IsPerfectQuickBoosting");
    private readonly int qbxHash = Animator.StringToHash("QBX"); // NEW
    private readonly int qbyHash = Animator.StringToHash("QBY"); // NEW

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

        // 1. INPUT HANDLING (READING FROM CUSTOM ANIMATOR INPUT IN CONTROLLER)
        float targetX = mechController.animMoveInput.x;
        float targetZ = mechController.animMoveInput.z;

        // DYNAMIC SMOOTHING: Slow down blend when shifting heavy weight
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
        bool isAnimationMoving = mechController.animMoveInput.magnitude > 0.1f;
        animator.SetBool(isMovingHash, isAnimationMoving && !mechController.isRecoveringFromLanding);

        // 3. BOOST LOGIC (Reads exact physics intent from the controller)
        animator.SetBool(isBoostingHash, mechController.isAnimationBoosting);

        // 4. JUMP & GROUND STATES
        animator.SetBool(isJumpingHash, mechController.isPreparingToJump);
        animator.SetBool(isGroundedHash, characterController.isGrounded);
        animator.SetBool(hardLandingHash, mechController.isRecoveringFromLanding);

        // --- 5. ADDED: QUICK BOOST ANIMATION OVERRIDES ---
        animator.SetBool(isQuickBoostingHash, mechController.isQuickBoosting);
        animator.SetBool(isPerfectQuickBoostingHash, mechController.isPerfectQuickBoosting);

        // Feed the dash direction specifically into the QB Blend tree parameters!
        // This isolates the Quick Boost from the standard movement so transitions work properly.
        if (mechController.isQuickBoosting)
        {
            animator.SetFloat(qbxHash, mechController.LastQBDirection.x);
            animator.SetFloat(qbyHash, mechController.LastQBDirection.z);
        }
    }
}