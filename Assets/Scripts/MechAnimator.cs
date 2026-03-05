using UnityEngine;

public class MechAnimator : MonoBehaviour
{
    [Header("Core References")]
    public MechController mechController;
    public CharacterController characterController;

    [Tooltip("Drag the Animation Skeleton object (which has the Animator component) here!")]
    public Animator animator;

    // Animator Hash IDs for performance
    private readonly int speedHash = Animator.StringToHash("Speed");
    private readonly int isGroundedHash = Animator.StringToHash("IsGrounded");
    private readonly int isJumpingHash = Animator.StringToHash("IsJumping");
    private readonly int hardLandingHash = Animator.StringToHash("HardLanding");

    void Start()
    {
        if (mechController == null) mechController = GetComponent<MechController>();
        if (characterController == null) characterController = GetComponent<CharacterController>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (animator == null || mechController == null || characterController == null) return;

        Vector3 horizontalVelocity = new Vector3(characterController.velocity.x, 0f, characterController.velocity.z);
        animator.SetFloat(speedHash, horizontalVelocity.magnitude);

        animator.SetBool(isGroundedHash, characterController.isGrounded);
        animator.SetBool(isJumpingHash, mechController.isJumping && !characterController.isGrounded);
        animator.SetBool(hardLandingHash, mechController.isRecoveringFromLanding);
    }
}