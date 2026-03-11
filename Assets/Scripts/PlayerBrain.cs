using UnityEngine;

public class PlayerBrain : MonoBehaviour
{
    private MechController controller;
    private Transform mainCamera;

    [Header("Cinemachine Target Configuration")]
    [Tooltip("Assign the empty GameObject the Cinemachine Virtual Camera is set to Follow")]
    public Transform cinemachineFollowTarget;
    public float mouseSensitivity = 2.0f;

    private Vector2 cameraRotation;
    private Vector2 lookDelta;

    void Start()
    {
        controller = GetComponent<MechController>();
        
        // Cache the main camera to read its current facing direction
        if (Camera.main != null)
        {
            mainCamera = Camera.main.transform;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (cinemachineFollowTarget != null)
        {
            cameraRotation.y = cinemachineFollowTarget.eulerAngles.y;
            cameraRotation.x = cinemachineFollowTarget.eulerAngles.x;
        }
    }

    void Update()
    {
        if (Time.timeScale == 0f) return;

        HandleTargetRotation();

        // 1. Movement Input
        controller.moveInput = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized;

        // 2. Pass the Actual Camera's Yaw to the MechController
        if (mainCamera != null)
        {
            // Flatten the camera's forward vector on the XZ plane
            Vector3 cameraForward = mainCamera.forward;
            cameraForward.y = 0;
            controller.lookTargetForward = cameraForward.normalized;
        }

        // 3. Actions
        controller.isBoosting = Input.GetKey(KeyCode.LeftShift);
        controller.isJumping = Input.GetKey(KeyCode.Space);
    }

    private void HandleTargetRotation()
    {
        if (cinemachineFollowTarget == null) return;

        lookDelta.x = Input.GetAxis("Mouse X") * mouseSensitivity;
        lookDelta.y = Input.GetAxis("Mouse Y") * mouseSensitivity;

        cameraRotation.y += lookDelta.x;
        cameraRotation.x -= lookDelta.y;
        
        // Clamp pitch to prevent the camera from flipping over
        cameraRotation.x = Mathf.Clamp(cameraRotation.x, -60f, 60f);

        // We rotate the target, NOT the camera. Cinemachine will follow this target.
        cinemachineFollowTarget.rotation = Quaternion.Euler(cameraRotation.x, cameraRotation.y, 0f);
    }
}