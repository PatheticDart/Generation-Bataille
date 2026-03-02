using UnityEngine;

[RequireComponent(typeof(CharacterController), typeof(MechStats))]
public class MechController : MonoBehaviour
{
    private CharacterController controller;
    private MechStats stats;

    [Header("Visuals")]
    public Transform mechBody;

    [Header("Input (Driven by Player or AI)")]
    public Vector3 moveInput;
    public Vector3 lookTargetForward;
    public bool isBoosting;
    public bool isJumping;

    [Header("Camera & Effects")]
    [Tooltip("Drag the Virtual Camera here to trigger screen shake.")]
    public CameraEffects cameraEffects; // <-- Updated reference name!

    private float verticalVelocity;
    private Vector3 currentHorizontalVelocity;

    // --- Landing State Variables ---
    public bool isRecoveringFromLanding { get; private set; }
    private float recoveryTimer = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        stats = GetComponent<MechStats>();
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
        // --- 1. HANDLE HARD LANDING RECOVERY STATE ---
        Vector3 currentMoveInput = moveInput;
        bool currentIsJumping = isJumping;
        bool currentIsBoosting = isBoosting;

        if (isRecoveringFromLanding)
        {
            recoveryTimer -= Time.deltaTime;
            if (recoveryTimer <= 0)
            {
                isRecoveringFromLanding = false;
            }
            else
            {
                // Lock out all player input while recovering
                currentMoveInput = Vector3.zero;
                currentIsJumping = false;
                currentIsBoosting = false;
            }
        }

        // --- 2. HORIZONTAL MOMENTUM ---
        bool canHorizontalBoost = currentIsBoosting && !stats.energyIsDepleted && (currentMoveInput.magnitude > 0 || !controller.isGrounded);
        float targetSpeed = canHorizontalBoost ? stats.boostHorizontalSpeed : stats.walkSpeed;

        Vector3 forward = lookTargetForward;
        Vector3 right = Vector3.Cross(Vector3.up, forward);

        Vector3 targetDirection = (forward * currentMoveInput.z + right * currentMoveInput.x).normalized;
        Vector3 targetVelocity = targetDirection * targetSpeed;

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

        bool energyUsedThisFrame = false;

        // --- 3. VERTICAL LOGIC ---
        if (controller.isGrounded)
        {
            verticalVelocity = -2f;

            if (currentIsJumping)
            {
                verticalVelocity = stats.jumpForce;
            }
        }
        else
        {
            if (currentIsJumping && !stats.energyIsDepleted)
            {
                verticalVelocity += (stats.boostVerticalSpeed * 2f) * Time.deltaTime;
                if (verticalVelocity > stats.boostVerticalSpeed)
                {
                    verticalVelocity = stats.boostVerticalSpeed;
                }
                energyUsedThisFrame = true;
            }
            else
            {
                // Keeping your 10f multiplier here!
                float weightFactor = stats.totalWeight / stats.baselineWeight;
                verticalVelocity -= 9.81f * weightFactor * 12f * Time.deltaTime;
            }
        }

        if (canHorizontalBoost && currentMoveInput.magnitude > 0)
        {
            energyUsedThisFrame = true;
        }

        if (energyUsedThisFrame)
        {
            stats.ConsumeEnergy(stats.boostEnergyDrain * Time.deltaTime);
        }

        // --- 4. THE MOVEMENT & IMPACT DETECTION ---
        Vector3 finalMove = new Vector3(currentHorizontalVelocity.x, verticalVelocity, currentHorizontalVelocity.z);

        // Save the ground state BEFORE we move
        bool wasGroundedBeforeMove = controller.isGrounded;

        // Execute the physical movement
        controller.Move(finalMove * Time.deltaTime);

        // Check the ground state AFTER we move to see if we just impacted
        if (!wasGroundedBeforeMove && controller.isGrounded)
        {
            if (verticalVelocity <= stats.hardLandingThreshold)
            {
                isRecoveringFromLanding = true;

                float weightFactor = stats.totalWeight / stats.baselineWeight;
                float speedFactor = verticalVelocity / stats.hardLandingThreshold;
                float calculatedTime = stats.baseHardLandingTime * weightFactor * speedFactor;
                recoveryTimer = Mathf.Clamp(calculatedTime, stats.baseHardLandingTime, stats.maxHardLandingTime);

                // --- TRIGGER CAMERA SHAKE ---
                if (cameraEffects != null)
                {
                    cameraEffects.TriggerImpactShake(speedFactor);
                }

                Debug.Log($"HARD LANDING! Speed: {verticalVelocity:F1} | Weight: {stats.totalWeight} | Stagger Time: {recoveryTimer:F2}s");
            }
        }
    }
}