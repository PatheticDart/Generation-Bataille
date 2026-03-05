using UnityEngine;

public class MechAnimator : MonoBehaviour
{
    [Header("Core References")]
    public MechController mechController;
    public CharacterController characterController;

    [Tooltip("Drag the Animation Skeleton object here!")]
    public Animator animator; // <--- CHANGED: Now public so you can assign the nested skeleton!

    // Animator Hash IDs for performance
    private readonly int speedHash = Animator.StringToHash("Speed");
    private readonly int isGroundedHash = Animator.StringToHash("IsGrounded");
    private readonly int isJumpingHash = Animator.StringToHash("IsJumping");
    private readonly int hardLandingHash = Animator.StringToHash("HardLanding");

    void Start()
    {
        // Auto-grab components on the Player root
        if (mechController == null) mechController = GetComponent<MechController>();
        if (characterController == null) characterController = GetComponent<CharacterController>();

        // Failsafe in case you forget to assign the animator in the inspector
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (animator == null || mechController == null || characterController == null) return;

        // 1. Pass the horizontal speed to the animator
        Vector3 horizontalVelocity = new Vector3(characterController.velocity.x, 0f, characterController.velocity.z);
        animator.SetFloat(speedHash, horizontalVelocity.magnitude);

        // 2. Pass grounded state
        animator.SetBool(isGroundedHash, characterController.isGrounded);

        // 3. Pass jumping state
        animator.SetBool(isJumpingHash, mechController.isJumping && !characterController.isGrounded);

        // 4. Trigger Hard Landing Stagger
        animator.SetBool(hardLandingHash, mechController.isRecoveringFromLanding);
    }
}