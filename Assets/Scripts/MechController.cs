using UnityEngine;

[RequireComponent(typeof(CharacterController), typeof(MechStats))]
public class MechController : MonoBehaviour
{
    private CharacterController controller;
    private MechStats stats;
    private PartSystem partSystem;

    [Header("Visuals")]
    public Transform mechBody;

    [Header("Targeting & Rotation")]
    [Tooltip("Drag your FCSLockbox here so the mech chassis chases its rotation.")]
    public Transform fcsLockBox;

    [Header("Input (Driven by Player or AI)")]
    public Vector3 moveInput;
    public Vector3 lookTargetForward; // Kept as a fallback
    public bool isBoosting;
    public bool isJumping;

    [Header("Movement Settings")]
    [Tooltip("Forces analog controller movement into 8 rigid directions to simulate a mechanical chassis.")]
    public bool restrictTo8Directions = true;

    [Header("Camera & Effects")]
    public CameraEffects cameraEffects;

    [Header("Jump & Landing States")]
    public bool isPreparingToJump { get; private set; }
    private float currentJumpDelayTimer = 0f;

    private float verticalVelocity;
    private Vector3 currentHorizontalVelocity;

    public bool isRecoveringFromLanding { get; private set; }
    private float recoveryTimer = 0f;

    private Vector3 lastActiveMoveInput;

    [Header("Buffers & Timings")]
    public float thrusterBufferTime = 0.1f;
    private float lastMoveInputTime = -10f;

    public float bunnyHopWindow = 0.2f;
    private float lastJumpOrLandTime = -10f;

    public float boostToJumpDelay = 0.25f;
    private float boostStartTime = -10f;
    private float boostEndTime = -10f;
    private bool wasActuallyBoostingLastFrame = false;

    public float walkToJumpDelay = 0.15f;
    private float walkStartTime = -10f;
    private bool wasActuallyWalkingLastFrame = false;

    public float jumpInputBufferTime = 0.2f;
    private float lastJumpInputTime = -10f;

    public bool HasRecentMovementInput => Time.time <= lastMoveInputTime + thrusterBufferTime;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        stats = GetComponent<MechStats>();
        partSystem = GetComponent<PartSystem>();
    }

    void Update()
    {
        HandleBodyRotation();
        ApplyMovement();
    }

    private void HandleBodyRotation()
    {
        // 1. Grab the FCS direction if available, otherwise use the old fallback
        Vector3 targetForward = fcsLockBox != null ? fcsLockBox.forward : lookTargetForward;

        // 2. Flatten the Y-axis so the mech doesn't physically lean backwards or forwards
        targetForward.y = 0f;

        if (targetForward.sqrMagnitude > 0.01f && mechBody != null)
        {
            Quaternion targetRot = Quaternion.LookRotation(targetForward.normalized);
            mechBody.rotation = Quaternion.Slerp(mechBody.rotation, targetRot, stats.turnSpeed * Time.deltaTime);
        }
    }

    private void ApplyMovement()
    {
        Vector3 currentMoveInput = moveInput;
        bool currentIsJumping = isJumping;
        bool currentIsBoosting = isBoosting;

        // --- 8-WAY MOVEMENT SNAP (ONLY WHEN WALKING) ---
        bool isWalkingState = controller.isGrounded && !currentIsBoosting;

        if (restrictTo8Directions && isWalkingState && currentMoveInput.magnitude > 0.1f)
        {
            float angle = Mathf.Atan2(currentMoveInput.x, currentMoveInput.z) * Mathf.Rad2Deg;
            angle = Mathf.Round(angle / 45f) * 45f;
            float mag = currentMoveInput.magnitude;
            currentMoveInput = new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad) * mag, 0f, Mathf.Cos(angle * Mathf.Deg2Rad) * mag);
            moveInput = currentMoveInput;
        }

        if (currentMoveInput.magnitude > 0.1f)
        {
            lastMoveInputTime = Time.time;
            lastActiveMoveInput = currentMoveInput;
        }

        Vector3 effectiveMoveInput = (currentMoveInput.magnitude > 0.1f) ? currentMoveInput : (HasRecentMovementInput ? lastActiveMoveInput : Vector3.zero);

        if (isRecoveringFromLanding)
        {
            recoveryTimer -= Time.deltaTime;
            if (recoveryTimer <= 0)
            {
                isRecoveringFromLanding = false;
            }
            else
            {
                effectiveMoveInput = Vector3.zero;
                currentIsJumping = false;
                currentIsBoosting = false;
                isPreparingToJump = false;
            }
        }

        bool canHorizontalBoost = currentIsBoosting && !stats.energyIsDepleted && (effectiveMoveInput.magnitude > 0 || !controller.isGrounded);

        bool isActuallyBoostingOnGround = canHorizontalBoost && controller.isGrounded;
        if (isActuallyBoostingOnGround && !wasActuallyBoostingLastFrame)
        {
            boostStartTime = Time.time;
        }
        else if (!isActuallyBoostingOnGround && wasActuallyBoostingLastFrame)
        {
            boostEndTime = Time.time;
        }
        wasActuallyBoostingLastFrame = isActuallyBoostingOnGround;

        bool isActuallyWalkingOnGround = !canHorizontalBoost && controller.isGrounded && (effectiveMoveInput.magnitude > 0);
        if (isActuallyWalkingOnGround && !wasActuallyWalkingLastFrame)
        {
            walkStartTime = Time.time;
        }
        wasActuallyWalkingLastFrame = isActuallyWalkingOnGround;

        float currentWalkSpeed = stats.walkSpeed;

        if (controller.isGrounded && effectiveMoveInput.z < -0.1f)
        {
            currentWalkSpeed *= (1f - stats.backwardSpeedPenalty);
        }

        float targetSpeed = canHorizontalBoost ? stats.boostHorizontalSpeed : currentWalkSpeed;

        // --- DYNAMIC REFERENCE FRAME FOR MOVEMENT ---
        Vector3 referenceForward;
        Vector3 referenceRight;

        if (isWalkingState && mechBody != null)
        {
            // Walking: Movement is bound to the physical chassis of the mech
            referenceForward = mechBody.forward;
            referenceRight = mechBody.right;

            referenceForward.y = 0f;
            referenceRight.y = 0f;
            referenceForward.Normalize();
            referenceRight.Normalize();
        }
        else
        {
            // Boosting/Flying: Movement chases the FCS Lockbox (or camera fallback)
            referenceForward = fcsLockBox != null ? fcsLockBox.forward : lookTargetForward;
            if (referenceForward == Vector3.zero) referenceForward = transform.forward;

            referenceForward.y = 0f;
            referenceForward.Normalize();
            referenceRight = Vector3.Cross(Vector3.up, referenceForward).normalized;
        }

        Vector3 targetDirection = (referenceForward * effectiveMoveInput.z + referenceRight * effectiveMoveInput.x).normalized;
        Vector3 targetVelocity = targetDirection * targetSpeed;

        // --- ACCELERATION & VELOCITY ---
        if (isRecoveringFromLanding && controller.isGrounded)
        {
            currentHorizontalVelocity = Vector3.Lerp(currentHorizontalVelocity, Vector3.zero, stats.hardLandingSlideDeceleration * Time.deltaTime);
        }
        else
        {
            float accelRate;
            if (!controller.isGrounded)
            {
                accelRate = (effectiveMoveInput.magnitude > 0) ? stats.airAcceleration : stats.airDeceleration;
            }
            else
            {
                if (effectiveMoveInput.magnitude > 0)
                {
                    accelRate = canHorizontalBoost ? stats.boostAcceleration : stats.walkAcceleration;
                }
                else
                {
                    accelRate = canHorizontalBoost ? stats.boostDeceleration : stats.walkDeceleration;
                }
            }
            currentHorizontalVelocity = Vector3.MoveTowards(currentHorizontalVelocity, targetVelocity, accelRate * Time.deltaTime);
        }

        bool energyUsedThisFrame = false;

        // --- JUMP LOGIC ---
        if (controller.isGrounded)
        {
            verticalVelocity = -2f;

            if (currentIsJumping) lastJumpInputTime = Time.time;
            bool hasBufferedJump = (Time.time <= lastJumpInputTime + jumpInputBufferTime);

            bool canBunnyHop = (Time.time <= lastJumpOrLandTime + bunnyHopWindow);

            bool isBoostJumpLocked = isActuallyBoostingOnGround && (Time.time < boostStartTime + boostToJumpDelay);
            bool isWalkJumpLocked = isActuallyWalkingOnGround && (Time.time < walkStartTime + walkToJumpDelay);
            bool isDeceleratingFromBoostLocked = !isActuallyBoostingOnGround && (Time.time < boostEndTime + walkToJumpDelay);

            bool isMovementJumpLocked = isBoostJumpLocked || isWalkJumpLocked || isDeceleratingFromBoostLocked;

            if (hasBufferedJump && !isPreparingToJump)
            {
                if (canBunnyHop)
                {
                    lastJumpInputTime = -10f;
                    verticalVelocity = stats.jumpForce;
                    lastJumpOrLandTime = Time.time;
                    isPreparingToJump = false;
                }
                else if (!isMovementJumpLocked)
                {
                    lastJumpInputTime = -10f;
                    isPreparingToJump = true;
                    currentJumpDelayTimer = stats.jumpDelay;
                }
            }

            if (isPreparingToJump)
            {
                currentJumpDelayTimer -= Time.deltaTime;
                if (currentJumpDelayTimer <= 0f)
                {
                    verticalVelocity = stats.jumpForce;
                    lastJumpOrLandTime = Time.time;
                    isPreparingToJump = false;
                }
            }
        }
        else
        {
            isPreparingToJump = false;

            if (currentIsJumping && !stats.energyIsDepleted)
            {
                verticalVelocity += (stats.boostVerticalSpeed * 2f) * Time.deltaTime;
                if (verticalVelocity > stats.boostVerticalSpeed) verticalVelocity = stats.boostVerticalSpeed;
                energyUsedThisFrame = true;
            }
            else
            {
                float weightFactor = stats.totalWeight / stats.baselineWeight;
                verticalVelocity -= 9.81f * weightFactor * 15f * Time.deltaTime;
            }
        }

        if (canHorizontalBoost && effectiveMoveInput.magnitude > 0) energyUsedThisFrame = true;
        if (energyUsedThisFrame) stats.ConsumeEnergy(stats.boostEnergyDrain * Time.deltaTime);

        Vector3 finalMove = new Vector3(currentHorizontalVelocity.x, verticalVelocity, currentHorizontalVelocity.z);
        bool wasGroundedBeforeMove = controller.isGrounded;
        controller.Move(finalMove * Time.deltaTime);

        // --- HARD LANDING IMPACT ---
        if (!wasGroundedBeforeMove && controller.isGrounded)
        {
            lastJumpOrLandTime = Time.time;

            if (verticalVelocity <= stats.minHardLandingThreshold)
            {
                isRecoveringFromLanding = true;
                isPreparingToJump = false;

                lastJumpOrLandTime = -100f;

                float weightFactor = stats.totalWeight / stats.baselineWeight;
                float speedFactor = Mathf.InverseLerp(stats.minHardLandingThreshold, stats.maxHardLandingThreshold, verticalVelocity);

                float baseCalculatedTime = Mathf.Lerp(stats.baseHardLandingTime, stats.maxHardLandingTime, speedFactor);
                float calculatedTime = baseCalculatedTime * weightFactor;
                recoveryTimer = Mathf.Clamp(calculatedTime, stats.baseHardLandingTime, stats.maxHardLandingTime);

                if (cameraEffects != null)
                {
                    float shakeSeverity = Mathf.Lerp(1.0f, 3.0f, speedFactor) * weightFactor;
                    cameraEffects.TriggerImpactShake(shakeSeverity);
                }
            }
        }

        if (partSystem != null)
        {
            partSystem.ToggleThrusters(energyUsedThisFrame);
        }
    }
}