using UnityEngine;

[RequireComponent(typeof(CharacterController), typeof(MechStats))]
public class MechController : MonoBehaviour
{
    private CharacterController controller;
    private MechStats stats;
    private MechLoader mechLoader;

    [Header("Visuals")]
    public Transform mechBody;

    [Header("Input (Driven by Player or AI)")]
    public Vector3 moveInput;
    public Vector3 lookTargetForward;
    public bool isBoosting;
    public bool isJumping;

    [Header("Camera & Effects")]
    public CameraEffects cameraEffects;

    [Header("Jump & Landing States")]
    public bool isPreparingToJump { get; private set; }
    private float currentJumpDelayTimer = 0f;

    private float verticalVelocity;
    private Vector3 currentHorizontalVelocity;

    // --- State Variables ---
    public bool isRecoveringFromLanding { get; private set; }
    private float recoveryTimer = 0f;

    // --- Direction Tracking Variables ---
    private Vector3 lastActiveMoveInput;

    [Header("Buffers & Timings")]
    public float thrusterBufferTime = 0.1f;
    private float lastMoveInputTime = -10f;

    [Tooltip("Time after landing where a jump is instant (bypasses animation and delay).")]
    public float bunnyHopWindow = 0.2f;
    private float lastJumpOrLandTime = -10f;

    [Tooltip("Delay before jumping after starting a boost, allowing animations to blend smoothly.")]
    public float boostToJumpDelay = 0.25f;
    private float boostStartTime = -10f;
    private float boostEndTime = -10f; // NEW: Tracks when boosting stops for deceleration blending
    private bool wasActuallyBoostingLastFrame = false;

    [Tooltip("Delay before jumping after starting to walk, allowing animations to blend smoothly.")]
    public float walkToJumpDelay = 0.15f;
    private float walkStartTime = -10f;
    private bool wasActuallyWalkingLastFrame = false;

    [Tooltip("How long to remember a jump press if locked out by another animation.")]
    public float jumpInputBufferTime = 0.2f;
    private float lastJumpInputTime = -10f;

    public bool HasRecentMovementInput => Time.time <= lastMoveInputTime + thrusterBufferTime;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        stats = GetComponent<MechStats>();
        mechLoader = GetComponent<MechLoader>();
    }

    void Update()
    {
        HandleBodyRotation();
        ApplyMovement();
    }

    private void HandleBodyRotation()
    {
        if (lookTargetForward != Vector3.zero && mechBody != null)
        {
            Quaternion targetRot = Quaternion.LookRotation(lookTargetForward);
            mechBody.rotation = Quaternion.Slerp(mechBody.rotation, targetRot, stats.turnSpeed * Time.deltaTime);
        }
    }

    private void ApplyMovement()
    {
        Vector3 currentMoveInput = moveInput;
        bool currentIsJumping = isJumping;
        bool currentIsBoosting = isBoosting;

        if (currentMoveInput.magnitude > 0.1f)
        {
            lastMoveInputTime = Time.time;
        }

        // --- 1. OPPOSITE DIRECTION SWITCH ---
        if (!isRecoveringFromLanding && currentMoveInput.magnitude > 0.1f)
        {
            if (controller.isGrounded && !currentIsBoosting)
            {
                if (lastActiveMoveInput.magnitude > 0.1f)
                {
                    if (Vector3.Dot(currentMoveInput.normalized, lastActiveMoveInput.normalized) < -0.5f)
                    {
                        currentHorizontalVelocity = Vector3.zero;
                    }
                }
            }
            lastActiveMoveInput = currentMoveInput;
        }

        // --- EFFECTIVE MOVEMENT BUFFER ---
        Vector3 effectiveMoveInput = (currentMoveInput.magnitude > 0.1f) ? currentMoveInput : (HasRecentMovementInput ? lastActiveMoveInput : Vector3.zero);

        // --- 2. HARD LANDING RECOVERY STATE ---
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

        // --- 3. HORIZONTAL MOMENTUM & TARGET SPEED ---
        bool canHorizontalBoost = currentIsBoosting && !stats.energyIsDepleted && (effectiveMoveInput.magnitude > 0 || !controller.isGrounded);

        // Track Boost Start and End
        bool isActuallyBoostingOnGround = canHorizontalBoost && controller.isGrounded;
        if (isActuallyBoostingOnGround && !wasActuallyBoostingLastFrame)
        {
            boostStartTime = Time.time;
        }
        else if (!isActuallyBoostingOnGround && wasActuallyBoostingLastFrame)
        {
            // NEW: The exact frame the mech stops boosting, start the deceleration timer!
            boostEndTime = Time.time;
        }
        wasActuallyBoostingLastFrame = isActuallyBoostingOnGround;

        // Track Walk Start
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

        Vector3 forward = lookTargetForward;
        Vector3 right = Vector3.Cross(Vector3.up, forward);

        Vector3 targetDirection = (forward * effectiveMoveInput.z + right * effectiveMoveInput.x).normalized;
        Vector3 targetVelocity = targetDirection * targetSpeed;

        // --- 4. ACCELERATION & SLIDE LOGIC ---
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

        // --- 5. VERTICAL LOGIC (WITH BUNNY HOP & INPUT BUFFER) ---
        if (controller.isGrounded)
        {
            verticalVelocity = -2f;

            if (currentIsJumping) lastJumpInputTime = Time.time;
            bool hasBufferedJump = (Time.time <= lastJumpInputTime + jumpInputBufferTime);

            bool canBunnyHop = (Time.time <= lastJumpOrLandTime + bunnyHopWindow);

            // Check all movement lockouts
            bool isBoostJumpLocked = isActuallyBoostingOnGround && (Time.time < boostStartTime + boostToJumpDelay);
            bool isWalkJumpLocked = isActuallyWalkingOnGround && (Time.time < walkStartTime + walkToJumpDelay);
            // NEW: Applies the walkToJumpDelay window immediately after a boost ends (whether walking or dropping to idle)
            bool isDeceleratingFromBoostLocked = !isActuallyBoostingOnGround && (Time.time < boostEndTime + walkToJumpDelay);

            bool isMovementJumpLocked = isBoostJumpLocked || isWalkJumpLocked || isDeceleratingFromBoostLocked;

            if (hasBufferedJump && !isPreparingToJump)
            {
                if (canBunnyHop)
                {
                    // INSTANT JUMP (Bypasses movement lockouts and animation delay)
                    lastJumpInputTime = -10f;
                    verticalVelocity = stats.jumpForce;
                    lastJumpOrLandTime = Time.time;
                    isPreparingToJump = false;
                }
                else if (!isMovementJumpLocked)
                {
                    // DELAYED JUMP (Normal jump, plays animation)
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

        // --- 6. IMPACT DETECTION ---
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

        // --- 7. VFX LOGIC ---
        if (mechLoader != null)
        {
            mechLoader.ToggleThrusters(energyUsedThisFrame);
        }
    }
}