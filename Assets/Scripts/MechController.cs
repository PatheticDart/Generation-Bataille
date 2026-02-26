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
        // Ensure we have a direction to look and the visual model is assigned
        if (lookTargetForward != Vector3.zero && mechBody != null)
        {
            // Calculate the target rotation based on the camera's heading
            Quaternion targetRot = Quaternion.LookRotation(lookTargetForward);
            
            // Slerp the capsule's rotation for that heavy, mechanical turn
            mechBody.rotation = Quaternion.Slerp(mechBody.rotation, targetRot, stats.turnSpeed * Time.deltaTime);
        }
    }

    private void ApplyMovement()
    {
        // 1. Horizontal Speed Setup
        bool canHorizontalBoost = isBoosting && !stats.energyIsDepleted && (moveInput.magnitude > 0 || !controller.isGrounded);
        float currentSpeed = canHorizontalBoost ? stats.boostHorizontalSpeed : stats.walkSpeed;

        Vector3 forward = lookTargetForward;
        Vector3 right = Vector3.Cross(Vector3.up, forward);
        Vector3 move = (forward * moveInput.z + right * moveInput.x).normalized * currentSpeed;

        // Unified flag: ensures 1x drain regardless of how many thrusters are firing
        bool energyUsedThisFrame = false;

        // 2. Vertical Logic (Jump vs Vertical Boost)
        if (controller.isGrounded)
        {
            verticalVelocity = -2f; // Snap to ground
            
            // THE JUMP: Instant upward velocity. No energy cost.
            if (isJumping)
            {
                verticalVelocity = stats.jumpForce;
            }
        }
        else
        {
            // THE VERTICAL BOOST: Mid-air thrusters. Costs energy.
            if (isJumping && !stats.energyIsDepleted)
            {
                // Upward acceleration up to the max vertical speed
                verticalVelocity += (stats.boostVerticalSpeed * 2f) * Time.deltaTime;
                if (verticalVelocity > stats.boostVerticalSpeed)
                {
                    verticalVelocity = stats.boostVerticalSpeed;
                }
                
                energyUsedThisFrame = true; // Mark vertical thrusters as firing
            }
            else
            {
                // Gravity (applies when falling or out of energy)
                float weightFactor = stats.totalWeight / 5000f;
                verticalVelocity -= 9.81f * weightFactor * 2f * Time.deltaTime;
            }
        }

        // 3. Horizontal Boost Check
        if (canHorizontalBoost && moveInput.magnitude > 0)
        {
            energyUsedThisFrame = true; // Mark horizontal thrusters as firing
        }

        // 4. The 1x Energy Drain
        if (energyUsedThisFrame)
        {
            stats.ConsumeEnergy(stats.boostEnergyDrain * Time.deltaTime);
        }

        // 5. Final Execution
        Vector3 finalMove = new Vector3(move.x, verticalVelocity, move.z);
        controller.Move(finalMove * Time.deltaTime);
    }
}