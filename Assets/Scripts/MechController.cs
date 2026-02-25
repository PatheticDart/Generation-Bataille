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
        // Only rotate the visual body, leaving the root object's rotation at (0,0,0)
        if (lookTargetForward != Vector3.zero && mechBody != null)
        {
            Quaternion targetRot = Quaternion.LookRotation(lookTargetForward);
            mechBody.rotation = Quaternion.Slerp(mechBody.rotation, targetRot, stats.turnSpeed * Time.deltaTime);
        }
    }

    private void ApplyMovement()
    {
        // 1. Horizontal Speed Setup
        bool canHorizontalBoost = isBoosting && !stats.energyIsDepleted && (moveInput.magnitude > 0 || !controller.isGrounded);
        float currentSpeed = canHorizontalBoost ? stats.boostHorizontalSpeed : stats.walkSpeed;

        // Calculate horizontal movement relative to look direction
        Vector3 forward = lookTargetForward;
        Vector3 right = Vector3.Cross(Vector3.up, forward);
        Vector3 move = (forward * moveInput.z + right * moveInput.x).normalized * currentSpeed;

        // Unified flag to check if ANY boosting happened this frame
        bool energyUsedThisFrame = false;

        // 2. Vertical & Flight Logic
        if (controller.isGrounded)
        {
            verticalVelocity = -2f; // Keeps the mech grounded on slopes
            
            // Initial Jump Burst from the ground
            if (isJumping && !stats.energyIsDepleted)
            {
                verticalVelocity = stats.jumpForce;
            }
        }
        else
        {
            // Mid-air: Vertical Boosting (Holding Space)
            if (isJumping && !stats.energyIsDepleted)
            {
                // Apply upward acceleration instead of instantly snapping to max speed.
                // This preserves the initial jump arc and feels like firing heavy thrusters.
                verticalVelocity += (stats.boostVerticalSpeed * 2f) * Time.deltaTime;
                
                // Clamp it so we don't fly upwards infinitely fast
                if (verticalVelocity > stats.boostVerticalSpeed)
                {
                    verticalVelocity = stats.boostVerticalSpeed;
                }
                
                energyUsedThisFrame = true; // Mark vertical thrusters as active
            }
            else
            {
                // Gravity takes over if we aren't firing upward thrusters
                float weightFactor = stats.totalWeight / 5000f;
                verticalVelocity -= 9.81f * weightFactor * 2f * Time.deltaTime;
            }
        }

        // 3. Check for Horizontal Boosting
        if (canHorizontalBoost && moveInput.magnitude > 0)
        {
            energyUsedThisFrame = true; // Mark horizontal thrusters as active
        }

        // 4. The 1x Energy Drain
        // It only triggers once per frame, whether you are flying, dashing, or both.
        if (energyUsedThisFrame)
        {
            stats.ConsumeEnergy(stats.boostEnergyDrain * Time.deltaTime);
        }

        // 5. Final Execution
        Vector3 finalMove = new Vector3(move.x, verticalVelocity, move.z);
        controller.Move(finalMove * Time.deltaTime);
    }
}