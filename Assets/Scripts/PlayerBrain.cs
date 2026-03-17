using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(MechController), typeof(MechWeaponManager))]
public class PlayerBrain : MonoBehaviour
{
    private MechController controller;
    private MechWeaponManager weaponManager;
    private Transform mainCamera;

    [Header("Cinemachine Target Configuration")]
    [Tooltip("Assign the empty GameObject the Cinemachine Virtual Camera is set to Follow")]
    public Transform cinemachineFollowTarget;

    [Header("Look Sensitivities")]
    [Tooltip("Sensitivity for Mouse input (Raw pixel delta).")]
    public float mouseSensitivity = 0.1f;
    [Tooltip("Sensitivity for Gamepad input (Degrees per second). 150 is roughly equal to 0.1 mouse.")]
    public float controllerSensitivity = 150f;

    [Header("Movement Input Action References")]
    public InputActionReference moveAction;
    public InputActionReference lookAction;
    public InputActionReference jumpAction;
    public InputActionReference boostAction;

    [Header("Weapon Input Action References")]
    public InputActionReference toggleLeftAction;
    public InputActionReference toggleRightAction;
    public InputActionReference fireLeftAction;
    public InputActionReference fireRightAction;

    private Vector2 cameraRotation;
    private Vector2 lookDelta;

    void OnEnable()
    {
        if (moveAction != null) moveAction.action.Enable();
        if (lookAction != null) lookAction.action.Enable();
        if (jumpAction != null) jumpAction.action.Enable();
        if (boostAction != null) boostAction.action.Enable();
        
        if (toggleLeftAction != null) toggleLeftAction.action.Enable();
        if (toggleRightAction != null) toggleRightAction.action.Enable();
        if (fireLeftAction != null) fireLeftAction.action.Enable();
        if (fireRightAction != null) fireRightAction.action.Enable();
    }

    void OnDisable()
    {
        if (moveAction != null) moveAction.action.Disable();
        if (lookAction != null) lookAction.action.Disable();
        if (jumpAction != null) jumpAction.action.Disable();
        if (boostAction != null) boostAction.action.Disable();

        if (toggleLeftAction != null) toggleLeftAction.action.Disable();
        if (toggleRightAction != null) toggleRightAction.action.Disable();
        if (fireLeftAction != null) fireLeftAction.action.Disable();
        if (fireRightAction != null) fireRightAction.action.Disable();
    }

    void Start()
    {
        controller = GetComponent<MechController>();
        weaponManager = GetComponent<MechWeaponManager>();

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

        if (moveAction == null || fireLeftAction == null)
        {
            Debug.LogWarning("PlayerBrain: Input Action References are missing! Please assign them in the Inspector.");
            return;
        }

        HandleTargetRotation();

        // --- MOVEMENT & ACTIONS ---
        Vector2 moveValue = moveAction.action.ReadValue<Vector2>();
        controller.moveInput = new Vector3(moveValue.x, 0, moveValue.y).normalized;

        if (mainCamera != null)
        {
            Vector3 cameraForward = mainCamera.forward;
            cameraForward.y = 0;
            controller.lookTargetForward = cameraForward.normalized;
        }

        controller.isBoosting = boostAction.action.IsPressed();
        controller.isJumping = jumpAction.action.IsPressed();

        // --- WEAPON TOGGLES ---
        if (toggleLeftAction.action.WasPressedThisFrame()) weaponManager.ToggleLeftWeapon();
        if (toggleRightAction.action.WasPressedThisFrame()) weaponManager.ToggleRightWeapon();

        // --- WEAPON FIRING ---
        weaponManager.ProcessLeftFire(
            fireLeftAction.action.WasPressedThisFrame(),
            fireLeftAction.action.IsPressed(),
            fireLeftAction.action.WasReleasedThisFrame()
        );

        weaponManager.ProcessRightFire(
            fireRightAction.action.WasPressedThisFrame(),
            fireRightAction.action.IsPressed(),
            fireRightAction.action.WasReleasedThisFrame()
        );
    }

    private void HandleTargetRotation()
    {
        if (cinemachineFollowTarget == null) return;

        Vector2 lookValue = lookAction.action.ReadValue<Vector2>();

        bool isGamepad = lookAction.action.activeControl != null && lookAction.action.activeControl.device is Gamepad;

        if (isGamepad)
        {
            lookDelta.x = lookValue.x * controllerSensitivity * Time.deltaTime;
            lookDelta.y = lookValue.y * controllerSensitivity * Time.deltaTime;
        }
        else
        {
            lookDelta.x = lookValue.x * mouseSensitivity;
            lookDelta.y = lookValue.y * mouseSensitivity;
        }

        cameraRotation.y += lookDelta.x;
        cameraRotation.x -= lookDelta.y;

        cameraRotation.x = Mathf.Clamp(cameraRotation.x, -60f, 60f);

        cinemachineFollowTarget.rotation = Quaternion.Euler(cameraRotation.x, cameraRotation.y, 0f);
    }
}