using UnityEngine;

public class GarageAnimator : MonoBehaviour
{
    public enum GaragePose
    {
        Idle,
        WalkForward,
        WalkBackward,
        StrafeLeft,
        StrafeRight,
        BoostForward,
        MidAir,
        HardLanding
    }

    [Header("Setup")]
    [Tooltip("Drag the Animation Skeleton object here!")]
    public Animator animator;

    [Header("Preview Controls")]
    [Tooltip("Change this in the Inspector to preview different animations on loop.")]
    public GaragePose currentPose = GaragePose.Idle;
    
    [Tooltip("How fast the mech transitions between poses when you change the Enum.")]
    public float blendSpeed = 8f;

    // Animator Hash IDs (Identical to your combat animator)
    private readonly int moveXHash = Animator.StringToHash("LocalMoveX");
    private readonly int moveZHash = Animator.StringToHash("LocalMoveZ");
    private readonly int isGroundedHash = Animator.StringToHash("IsGrounded");
    private readonly int isJumpingHash = Animator.StringToHash("IsJumping");
    private readonly int hardLandingHash = Animator.StringToHash("HardLanding");
    private readonly int isBoostingHash = Animator.StringToHash("IsBoosting");
    private readonly int isMovingHash = Animator.StringToHash("IsMoving");

    // Internal smoothing variables
    private float currentAnimX = 0f;
    private float currentAnimZ = 0f;

    void Start()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (animator == null) return;

        // 1. Setup default "simulated" inputs
        float targetX = 0f;
        float targetZ = 0f;
        bool isGrounded = true;
        bool isJumping = false;
        bool isBoosting = false;
        bool isMoving = false;
        bool isHardLanding = false;

        // 2. Modify simulated inputs based on the selected enum
        switch (currentPose)
        {
            case GaragePose.Idle:
                // Defaults are fine for Idle
                break;
            case GaragePose.WalkForward:
                targetZ = 1f;
                isMoving = true;
                break;
            case GaragePose.WalkBackward:
                targetZ = -1f;
                isMoving = true;
                break;
            case GaragePose.StrafeLeft:
                targetX = -1f;
                isMoving = true;
                break;
            case GaragePose.StrafeRight:
                targetX = 1f;
                isMoving = true;
                break;
            case GaragePose.BoostForward:
                targetZ = 1f;
                isMoving = true;
                isBoosting = true;
                break;
            case GaragePose.MidAir:
                isGrounded = false;
                break;
            case GaragePose.HardLanding:
                isHardLanding = true;
                break;
        }

        // 3. Smoothly blend the floats so the mech physically shifts its weight
        currentAnimX = Mathf.Lerp(currentAnimX, targetX, Time.deltaTime * blendSpeed);
        currentAnimZ = Mathf.Lerp(currentAnimZ, targetZ, Time.deltaTime * blendSpeed);

        // 4. Feed the fake data into your existing Animator Controller
        animator.SetFloat(moveXHash, currentAnimX);
        animator.SetFloat(moveZHash, currentAnimZ);

        animator.SetBool(isMovingHash, isMoving);
        animator.SetBool(isBoostingHash, isBoosting);
        animator.SetBool(isGroundedHash, isGrounded);
        animator.SetBool(isJumpingHash, isJumping);
        animator.SetBool(hardLandingHash, isHardLanding);
    }
}