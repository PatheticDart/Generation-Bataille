using UnityEngine;

public class PlayerBrain : MonoBehaviour
{
    private MechController controller;
    
    [Header("Camera Settings")]
    public Transform cameraPivot;
    public float mouseSensitivity = 2.0f;
    public Vector3 pivotOffset = new Vector3(0, 2f, 0); // Height of the "Head"
    
    private Vector2 cameraRotation;

    void Start()
    {
        controller = GetComponent<MechController>();
        Cursor.lockState = CursorLockMode.Locked;

        // Sync initial rotation
        Vector3 rot = cameraPivot.eulerAngles;
        cameraRotation.x = rot.x;
        cameraRotation.y = rot.y;
    }

    void LateUpdate() // Use LateUpdate for camera following to prevent jitter
    {
        FollowMech();
        RotateCamera();
        
        // Feed the decoupled pivot direction to the mech controller
        Vector3 lookDir = cameraPivot.forward;
        lookDir.y = 0; 
        controller.lookTargetForward = lookDir.normalized;
    }

    void Update()
    {
        // Inputs
        controller.moveInput = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized;
        controller.isBoosting = Input.GetKey(KeyCode.LeftShift);
        controller.isJumping = Input.GetKey(KeyCode.Space);
    }

    void FollowMech()
    {
        // Move the pivot to the mech's position plus the height offset
        // Because the pivot is not a child, it won't rotate when the mech rotates
        cameraPivot.position = transform.position + pivotOffset;
    }

    void RotateCamera()
    {
        cameraRotation.y += Input.GetAxis("Mouse X") * mouseSensitivity;
        cameraRotation.x -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        cameraRotation.x = Mathf.Clamp(cameraRotation.x, -60f, 60f);

        cameraPivot.rotation = Quaternion.Euler(cameraRotation.x, cameraRotation.y, 0f);
    }
}