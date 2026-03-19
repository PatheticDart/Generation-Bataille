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
    public Animator animator;
    public PartSystem partSystem;

    [Header("Preview Controls")]
    public GaragePose currentPose = GaragePose.Idle;
    public float blendSpeed = 8f;

    private readonly int moveXHash = Animator.StringToHash("LocalMoveX");
    private readonly int moveZHash = Animator.StringToHash("LocalMoveZ");
    private readonly int isGroundedHash = Animator.StringToHash("IsGrounded");
    private readonly int isJumpingHash = Animator.StringToHash("IsJumping");
    private readonly int hardLandingHash = Animator.StringToHash("HardLanding");
    private readonly int isBoostingHash = Animator.StringToHash("IsBoosting");
    private readonly int isMovingHash = Animator.StringToHash("IsMoving");

    private float currentAnimX = 0f;
    private float currentAnimZ = 0f;
    private bool _wasBoosting = false;

    void Start()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (partSystem == null) partSystem = GetComponentInParent<PartSystem>();
    }

    void Update()
    {
        if (animator == null) return;

        float targetX = 0f;
        float targetZ = 0f;
        bool isGrounded = true;
        bool isJumping = false;
        bool isBoosting = false;
        bool isMoving = false;
        bool isHardLanding = false;

        switch (currentPose)
        {
            case GaragePose.Idle:
                // THE FIX: Provide a tiny forward bias so Atan2(0,0) doesn't flip the mech 180 degrees
                targetZ = 0.01f; 
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

        if (isBoosting != _wasBoosting)
        {
            if (partSystem != null) partSystem.ToggleThrusters(isBoosting);
            _wasBoosting = isBoosting;
        }

        currentAnimX = Mathf.Lerp(currentAnimX, targetX, Time.deltaTime * blendSpeed);
        currentAnimZ = Mathf.Lerp(currentAnimZ, targetZ, Time.deltaTime * blendSpeed);

        // Snap to absolute 0 if the values get small enough to prevent float precision jitters
        if (Mathf.Abs(currentAnimX) < 0.005f) currentAnimX = 0f;
        if (Mathf.Abs(currentAnimZ) < 0.005f && currentPose != GaragePose.Idle) currentAnimZ = 0f;

        animator.SetFloat(moveXHash, currentAnimX);
        animator.SetFloat(moveZHash, currentAnimZ);

        animator.SetBool(isMovingHash, isMoving);
        animator.SetBool(isBoostingHash, isBoosting);
        animator.SetBool(isGroundedHash, isGrounded);
        animator.SetBool(isJumpingHash, isJumping);
        animator.SetBool(hardLandingHash, isHardLanding);
    }
}