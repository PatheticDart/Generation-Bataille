using UnityEngine;

public class PlayerBrain : MonoBehaviour
{
    private MechController controller;
    public Transform camTransform; // Assign Main Camera or a Cinemachine Target

    void Start()
    {
        controller = GetComponent<MechController>();
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        // 1. Send movement relative to input
        controller.moveInput = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized;
        
        // 2. Feed the Camera's forward to the Mech's rotation logic
        // This recreates the Vertical Armor feel where the body chases the camera
        controller.lookTargetForward = camTransform.forward;

        // 3. Buttons
        controller.isBoosting = Input.GetKey(KeyCode.LeftShift);
        controller.isJumping = Input.GetKey(KeyCode.Space);
    }
}