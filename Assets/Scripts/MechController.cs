using UnityEngine;

[RequireComponent(typeof(CharacterController), typeof(MechStats))]
public class MechController : MonoBehaviour
{
    private CharacterController controller;
    private MechStats stats;

    [Header("Visuals")]
    [Tooltip("Drag the child Capsule/Mech Model here. This is what will visually rotate.")]
    public Transform mechBody;

    [Header("Input (Driven by Player or AI)")]
    public Vector3 moveInput;
    public Vector3 lookTargetForward;
    public bool isBoosting;
    public bool isJumping;

    private float verticalVelocity;
    // Tracks our actual momentum for the drifting effect
    private Vector3 currentHorizontalVelocity;

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
        // 1. Horizontal Momentum Setup
        bool isTryingToBoost = isBoosting && !stats.energyIsDepleted;
        float targetSpeed = isTryingToBoost ? stats.boostHorizontalSpeed : stats.walkSpeed;

        Vector3 forward = lookTargetForward;
        Vector3 right = Vector3.Cross(Vector3.up, forward);

        // This is the velocity the player WANTS to achieve
        Vector3 targetDirection = (forward * moveInput.z + right * moveInput.x).normalized;
        Vector3 targetVelocity = targetDirection * targetSpeed;

        // Determine how fast the heavy mech is allowed to change direction
        float accelRate;
        if (moveInput.magnitude > 0)
        {
            accelRate = isTryingToBoost ? stats.boostAcceleration : stats.walkAcceleration;
        }
        else
        {
            accelRate = isTryingToBoost ? stats.boostDeceleration : stats.walkDeceleration;
        }

        // THE DRIFT: Smoothly pull our current momentum toward the target velocity
        currentHorizontalVelocity = Vector3.MoveTowards(currentHorizontalVelocity, targetVelocity, accelRate * Time.deltaTime);

        // Unified flag: ensures 1x drain regardless of how many thrusters are firing
        bool energyUsedThisFrame = false;

        // 2. Vertical Logic & Weight
        if (controller.isGrounded)
        {
            if (verticalVelocity < 0) verticalVelocity = -2f; // Snap to ground

            // THE JUMP: Instant upward velocity
            if (isJumping)
            {
                verticalVelocity = stats.jumpForce;
            }
        }
        else
        {
            // THE VERTICAL BOOST
            if (isJumping && !stats.energyIsDepleted)
            {
                // WEIGH DOWN THE MECH: Heavy weight directly fights the thruster power
                float weightPenalty = stats.totalWeight * 0.005f;
                // Ensure a super heavy mech still has at least a tiny bit of lift
                float actualThrust = Mathf.Max((stats.boostVerticalSpeed * 2f) - weightPenalty, 5f);

                verticalVelocity += actualThrust * Time.deltaTime;
                if (verticalVelocity > stats.boostVerticalSpeed)
                {
                    verticalVelocity = stats.boostVerticalSpeed;
                }

                energyUsedThisFrame = true;
            }
            else
            {
                // Gravity applies heavier to heavier mechs
                float weightFactor = stats.totalWeight / 5000f;
                verticalVelocity -= 9.81f * weightFactor * 2f * Time.deltaTime;
            }
        }

        // 3. Horizontal Boost Energy Check
        if (isTryingToBoost && moveInput.magnitude > 0)
        {
            energyUsedThisFrame = true;
        }

        // 4. The 1x Energy Drain
        if (energyUsedThisFrame)
        {
            stats.ConsumeEnergy(stats.boostEnergyDrain * Time.deltaTime);
        }

        // 5. Final Execution: Combine our horizontal drift with our vertical lift
        Vector3 finalMove = currentHorizontalVelocity + new Vector3(0, verticalVelocity, 0);
        controller.Move(finalMove * Time.deltaTime);
    }
}