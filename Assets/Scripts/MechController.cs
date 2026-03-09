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

    [Header("Buffers")]
    [Tooltip("How long movement/thrusters persist after letting go of WASD.")]
    public float thrusterBufferTime = 0.1f;
    private float lastMoveInputTime = -10f;

    [Tooltip("Amount of time the player has to execute a jump without animation or delay (Bunny Hop window).")]
    public float jumpBuffer = 0.5f;
    private float lastJumpOrLandTime = -10f; // Tracks when the window started

    // Public property for other scripts (like Animator) to read the buffered state
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

        // --- UPDATE MOVEMENT BUFFER ---
        if (currentMoveInput.magnitude > 0.1f)
        {
            lastMoveInputTime = Time.time;
        }

        // --- 1. OPPOSITE DIRECTION SWITCH (INSTANT MOMENTUM RESET) ---
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
                currentMoveInput = Vector3.zero;
                currentIsJumping = false;
                currentIsBoosting = false;

                // FIX: Stop any queued jump wind-up dead in its tracks if we are recovering!
                isPreparingToJump = false;
            }
        }

        // --- 3. HORIZONTAL MOMENTUM & TARGET SPEED ---
        bool canHorizontalBoost = currentIsBoosting && !stats.energyIsDepleted && (currentMoveInput.magnitude > 0 || !controller.isGrounded);

        float currentWalkSpeed = stats.walkSpeed;

        if (controller.isGrounded && currentMoveInput.z < -0.1f)
        {
            currentWalkSpeed *= (1f - stats.backwardSpeedPenalty);
        }

        float targetSpeed = canHorizontalBoost ? stats.boostHorizontalSpeed : currentWalkSpeed;

        Vector3 forward = lookTargetForward;
        Vector3 right = Vector3.Cross(Vector3.up, forward);

        Vector3 targetDirection = (forward * currentMoveInput.z + right * currentMoveInput.x).normalized;
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
                accelRate = (currentMoveInput.magnitude > 0) ? stats.airAcceleration : stats.airDeceleration;
            }
            else
            {
                if (currentMoveInput.magnitude > 0)
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

        // --- 5. VERTICAL LOGIC (WITH JUMP BUFFER & DELAY) ---
        if (controller.isGrounded)
        {
            verticalVelocity = -2f;

            if (currentIsJumping && !isPreparingToJump)
            {
                // Check if we are inside the Jump Buffer window
                if (Time.time <= lastJumpOrLandTime + jumpBuffer)
                {
                    // INSTANT JUMP (Bypass animation and delay)
                    verticalVelocity = stats.jumpForce;
                    lastJumpOrLandTime = Time.time; // Reset the window for continuous bunny-hopping
                    isPreparingToJump = false;
                }
                else
                {
                    // DELAYED JUMP (Trigger animation and start wind-up timer)
                    isPreparingToJump = true;
                    currentJumpDelayTimer = stats.jumpDelay;
                }
            }

            // Only process the delay if we are actively doing a full animated jump
            if (isPreparingToJump)
            {
                currentJumpDelayTimer -= Time.deltaTime;
                if (currentJumpDelayTimer <= 0f)
                {
                    verticalVelocity = stats.jumpForce;
                    lastJumpOrLandTime = Time.time; // Start the buffer window exactly when we leave the ground!
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

        if (canHorizontalBoost && currentMoveInput.magnitude > 0) energyUsedThisFrame = true;
        if (energyUsedThisFrame) stats.ConsumeEnergy(stats.boostEnergyDrain * Time.deltaTime);

        Vector3 finalMove = new Vector3(currentHorizontalVelocity.x, verticalVelocity, currentHorizontalVelocity.z);
        bool wasGroundedBeforeMove = controller.isGrounded;
        controller.Move(finalMove * Time.deltaTime);

        // --- 6. IMPACT DETECTION ---
        if (!wasGroundedBeforeMove && controller.isGrounded)
        {
            // The exact millisecond we hit the ground, open the jump buffer window!
            lastJumpOrLandTime = Time.time;

            if (verticalVelocity <= stats.minHardLandingThreshold)
            {
                isRecoveringFromLanding = true;

                // FIX: Instantly cancel any jump queued on the exact landing frame
                isPreparingToJump = false;

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

        // --- 7. VFX LOGIC (WITH THRUSTER BUFFER) ---
        if (mechLoader != null)
        {
            bool shouldFireThrusters = energyUsedThisFrame || (currentIsBoosting && HasRecentMovementInput && !stats.energyIsDepleted);
            mechLoader.ToggleThrusters(shouldFireThrusters);
        }
    }
}