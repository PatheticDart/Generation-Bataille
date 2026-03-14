using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerBrain : MonoBehaviour
{
    private MechController controller;
    private Transform mainCamera;

    [Header("Cinemachine Target Configuration")]
    [Tooltip("Assign the empty GameObject the Cinemachine Virtual Camera is set to Follow")]
    public Transform cinemachineFollowTarget;

    [Header("Look Sensitivities")]
    [Tooltip("Sensitivity for Mouse input (Raw pixel delta).")]
    public float mouseSensitivity = 0.1f;
    [Tooltip("Sensitivity for Gamepad input (Degrees per second). 150 is roughly equal to 0.1 mouse.")]
    public float controllerSensitivity = 150f;

    [Header("Input Actions")]
    public InputAction moveAction = new InputAction("Move", InputActionType.Value);
    public InputAction lookAction = new InputAction("Look", InputActionType.Value);
    public InputAction jumpAction = new InputAction("Jump", InputActionType.Button);
    public InputAction boostAction = new InputAction("Boost", InputActionType.Button);

    private Vector2 cameraRotation;
    private Vector2 lookDelta;

    void Awake()
    {
        // Auto-setup default bindings so it works immediately for Keyboard/Mouse and Gamepad
        if (moveAction.bindings.Count == 0)
        {
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            moveAction.AddBinding("<Gamepad>/leftStick");
        }

        if (lookAction.bindings.Count == 0)
        {
            lookAction.AddBinding("<Pointer>/delta");
            lookAction.AddBinding("<Gamepad>/rightStick");
        }

        if (jumpAction.bindings.Count == 0)
        {
            jumpAction.AddBinding("<Keyboard>/space");
            jumpAction.AddBinding("<Gamepad>/buttonSouth"); // 'A' on Xbox, 'Cross' on PlayStation
        }

        if (boostAction.bindings.Count == 0)
        {
            boostAction.AddBinding("<Keyboard>/leftShift");
            boostAction.AddBinding("<Gamepad>/buttonEast"); // 'B' on Xbox, 'Circle' on PlayStation
        }
    }

    void OnEnable()
    {
        // Actions must be enabled to read input
        moveAction.Enable();
        lookAction.Enable();
        jumpAction.Enable();
        boostAction.Enable();
    }

    void OnDisable()
    {
        // Clean up when the script is disabled
        moveAction.Disable();
        lookAction.Disable();
        jumpAction.Disable();
        boostAction.Disable();
    }

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

        // 1. Movement Input (Reads Vector2 from WASD or Left Stick)
        Vector2 moveValue = moveAction.ReadValue<Vector2>();
        controller.moveInput = new Vector3(moveValue.x, 0, moveValue.y).normalized;

        // 2. Pass the Actual Camera's Yaw to the MechController
        if (mainCamera != null)
        {
            // Flatten the camera's forward vector on the XZ plane
            Vector3 cameraForward = mainCamera.forward;
            cameraForward.y = 0;
            controller.lookTargetForward = cameraForward.normalized;
        }

        // 3. Actions (Reads true if the button is currently held down)
        controller.isBoosting = boostAction.IsPressed();
        controller.isJumping = jumpAction.IsPressed();
    }

    private void HandleTargetRotation()
    {
        if (cinemachineFollowTarget == null) return;

        // Read Vector2 from Mouse Delta or Right Stick
        Vector2 lookValue = lookAction.ReadValue<Vector2>();

        // Dynamically check if the active control device is a Gamepad
        bool isGamepad = lookAction.activeControl != null && lookAction.activeControl.device is Gamepad;

        if (isGamepad)
        {
            // Gamepads need DeltaTime because the stick outputs a constant value (-1 to 1) while held
            lookDelta.x = lookValue.x * controllerSensitivity * Time.deltaTime;
            lookDelta.y = lookValue.y * controllerSensitivity * Time.deltaTime;
        }
        else
        {
            // Mice don't need DeltaTime because they output raw physical pixel movement delta per frame
            lookDelta.x = lookValue.x * mouseSensitivity;
            lookDelta.y = lookValue.y * mouseSensitivity;
        }

        cameraRotation.y += lookDelta.x;
        cameraRotation.x -= lookDelta.y;

        // Clamp pitch to prevent the camera from flipping over
        cameraRotation.x = Mathf.Clamp(cameraRotation.x, -60f, 60f);

        // We rotate the target, NOT the camera. Cinemachine will follow this target.
        cinemachineFollowTarget.rotation = Quaternion.Euler(cameraRotation.x, cameraRotation.y, 0f);
    }
}