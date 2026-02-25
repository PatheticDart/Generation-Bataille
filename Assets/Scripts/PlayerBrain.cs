using UnityEngine;

public class PlayerBrain : MonoBehaviour
{
    private MechController controller;
    
    [Header("Camera Configuration")]
    [Tooltip("The pivot object the camera follows. This MUST NOT be a child of the player capsule in the hierarchy!")]
    public Transform cameraPivot; 
    public float mouseSensitivity = 2.0f;
    [Tooltip("How high up the pivot should sit relative to the capsule's origin.")]
    public Vector3 pivotOffset = new Vector3(0, 1.5f, 0); 
    
    private Vector2 cameraRotation;
    private Vector2 lookDelta;

    void Start()
    {
        controller = GetComponent<MechController>();
        
        // Lock and hide the cursor for standard PC controls
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Initialize rotation based on the pivot's starting rotation
        if (cameraPivot != null)
        {
            cameraRotation.y = cameraPivot.eulerAngles.y;
            cameraRotation.x = cameraPivot.eulerAngles.x;
        }
    }

    void Update()
    {
        HandleCameraRotation();
        
        // 1. Capture Movement Input
        controller.moveInput = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized;

        // 2. The "VA" Bridge: Feed the Yaw (Y) of the camera directly as the target forward.
        // This ensures the mech tries to align its forward vector with the camera's view.
        if (cameraPivot != null)
        {
            Vector3 targetDir = Quaternion.Euler(0, cameraRotation.y, 0) * Vector3.forward;
            controller.lookTargetForward = targetDir.normalized;
        }

        // 3. Capture Action Inputs
        controller.isBoosting = Input.GetKey(KeyCode.LeftShift);
        controller.isJumping = Input.GetKey(KeyCode.Space);
    }

    void LateUpdate()
    {
        // Keep the pivot locked to the mech's position + offset.
        // Doing this in LateUpdate ensures it happens after the mech has moved this frame, preventing jitter.
        if (cameraPivot != null)
        {
            cameraPivot.position = transform.position + pivotOffset;
        }
    }

    private void HandleCameraRotation()
    {
        if (cameraPivot == null) return;

        // Capture raw mouse input
        lookDelta.x = Input.GetAxis("Mouse X") * mouseSensitivity;
        lookDelta.y = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Accumulate rotation
        cameraRotation.y += lookDelta.x;
        cameraRotation.x -= lookDelta.y;
        
        // Clamp the vertical viewing angle so the camera doesn't flip upside down
        cameraRotation.x = Mathf.Clamp(cameraRotation.x, -60f, 60f);

        // Apply rotation to the pivot directly in World Space
        cameraPivot.rotation = Quaternion.Euler(cameraRotation.x, cameraRotation.y, 0f);
    }
}