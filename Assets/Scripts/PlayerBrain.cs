using UnityEngine;

public class PlayerBrain : MonoBehaviour
{
    private MechController controller;
    
    [Header("Camera Configuration")]
    public Transform cameraPivot; 
    public float mouseSensitivity = 2.0f;
    
    private Vector2 cameraRotation;
    private Vector2 lookDelta;

    void Start()
    {
        controller = GetComponent<MechController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (cameraPivot != null)
        {
            cameraRotation.y = cameraPivot.eulerAngles.y;
            cameraRotation.x = cameraPivot.eulerAngles.x;
        }
    }

    void Update()
    {
        HandleCameraRotation();
        
        // 1. Movement Input
        controller.moveInput = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized;

        // 2. Pass the Pivot's Yaw to the MechController
        if (cameraPivot != null)
        {
            Vector3 targetDir = Quaternion.Euler(0, cameraRotation.y, 0) * Vector3.forward;
            controller.lookTargetForward = targetDir.normalized;
        }

        // 3. Actions
        controller.isBoosting = Input.GetKey(KeyCode.LeftShift);
        controller.isJumping = Input.GetKey(KeyCode.Space);
    } 

    private void HandleCameraRotation()
    {
        if (cameraPivot == null) return;

        lookDelta.x = Input.GetAxis("Mouse X") * mouseSensitivity;
        lookDelta.y = Input.GetAxis("Mouse Y") * mouseSensitivity;

        cameraRotation.y += lookDelta.x;
        cameraRotation.x -= lookDelta.y;
        cameraRotation.x = Mathf.Clamp(cameraRotation.x, -60f, 60f);

        // Apply rotation to the pivot. 
        // Local rotation is fine now since the parent root never spins!
        cameraPivot.localRotation = Quaternion.Euler(cameraRotation.x, cameraRotation.y, 0f);
    }
}