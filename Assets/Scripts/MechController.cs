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
        bool canBoost = isBoosting && !stats.energyIsDepleted && (moveInput.magnitude > 0 || !controller.isGrounded);
        float currentSpeed = canBoost ? stats.boostHorizontalSpeed : stats.walkSpeed;

        // Calculate movement relative to where we are aiming (the camera's forward)
        // Since the root doesn't rotate, we must build the movement vector manually
        Vector3 forward = lookTargetForward;
        Vector3 right = Vector3.Cross(Vector3.up, forward);
        
        Vector3 move = (forward * moveInput.z + right * moveInput.x).normalized * currentSpeed;

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