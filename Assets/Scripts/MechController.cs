using UnityEngine;

[RequireComponent(typeof(CharacterController), typeof(MechStats))]
public class MechController : MonoBehaviour
{
    private CharacterController controller;
    private MechStats stats;

    [Header("Input (Driven by Player or AI)")]
    public Vector3 moveInput; 
    public Vector3 lookTargetForward; // The direction the mech wants to face
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
        if (lookTargetForward != Vector3.zero)
        {
            // Flatten the look direction so the mech doesn't tilt up/down
            Vector3 flattenedForward = lookTargetForward;
            flattenedForward.y = 0;

            Quaternion targetRotation = Quaternion.LookRotation(flattenedForward);
            
            // This Slerp provides the "Heavy Mech" turn speed from AC3/Vertical Armor
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, stats.turnSpeed * Time.deltaTime);
        }
    }

    private void ApplyMovement()
    {
        bool canBoost = isBoosting && !stats.energyIsDepleted && (moveInput.magnitude > 0 || !controller.isGrounded);
        float currentSpeed = canBoost ? stats.boostHorizontalSpeed : stats.walkSpeed;

        Vector3 move = transform.TransformDirection(moveInput) * currentSpeed;

        // Vertical / Gravity
        if (controller.isGrounded)
        {
            verticalVelocity = -2f;
            if (isJumping && !stats.energyIsDepleted) verticalVelocity = stats.jumpForce;
        }
        else
        {
            if (isJumping && !stats.energyIsDepleted)
            {
                verticalVelocity = stats.boostVerticalSpeed;
                stats.ConsumeEnergy(stats.boostEnergyDrain * Time.deltaTime);
            }
            else
            {
                float weightFactor = stats.totalWeight / 5000f;
                verticalVelocity -= 9.81f * weightFactor * 2f * Time.deltaTime;
            }
        }

        if (canBoost && moveInput.magnitude > 0)
        {
            stats.ConsumeEnergy(stats.boostEnergyDrain * Time.deltaTime);
        }

        Vector3 finalMove = new Vector3(move.x, verticalVelocity, move.z);
        controller.Move(finalMove * Time.deltaTime);
    }
}