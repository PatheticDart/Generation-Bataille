using UnityEngine;
using UnityEngine.InputSystem;

public enum ControlSchemeType { Modern_TypeA, Crow_TypeB }

[RequireComponent(typeof(MechController), typeof(MechWeaponManager), typeof(CharacterController))]
public class PlayerBrain : MonoBehaviour
{
    private MechController controller;
    private MechWeaponManager weaponManager;
    private CharacterController charController;
    private Transform mainCamera;

    [Header("Control Scheme")]
    public ControlSchemeType activeControlScheme = ControlSchemeType.Modern_TypeA;

    [Header("Quick Boost Timing Windows")]
    public float doubleTapWindow = 0.3f;
    public float perfectQBHoldTime = 0.5f;
    public float perfectQBReleaseWindow = 0.3f;

    [Header("Cinemachine Target Configuration")]
    [Tooltip("Assign the empty GameObject the Cinemachine Virtual Camera is set to Follow")]
    public Transform cinemachineFollowTarget;

    [Header("Look Sensitivities")]
    public float mouseSensitivity = 0.1f;
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

    // --- INPUT STATE BUFFERS ---
    private float lastBoostTapTime = -10f;
    private float lastBoostReleaseTime = -10f; // NEW: Tracks exactly when the boost key was let go
    private float boostHoldStartTime = 0f;
    private bool isChargingBoostQB = false;

    private float lastJumpTapTime = -10f;
    private float jumpHoldStartTime = 0f;
    private bool isChargingJumpQB = false;
    
    private bool doubleTapFlightActive = false; 

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
        charController = GetComponent<CharacterController>();

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

        if (activeControlScheme == ControlSchemeType.Modern_TypeA)
            ProcessModernControls();
        else
            ProcessCrowControls();

        // --- WEAPON TOGGLES ---
        if (toggleLeftAction.action.WasPressedThisFrame()) weaponManager.ToggleLeftWeapon();
        if (toggleRightAction.action.WasPressedThisFrame()) weaponManager.ToggleRightWeapon();

        // --- WEAPON FIRING ---
        weaponManager.ProcessLeftFire(fireLeftAction.action.WasPressedThisFrame(), fireLeftAction.action.IsPressed(), fireLeftAction.action.WasReleasedThisFrame());
        weaponManager.ProcessRightFire(fireRightAction.action.WasPressedThisFrame(), fireRightAction.action.IsPressed(), fireRightAction.action.WasReleasedThisFrame());
    }

    private void ProcessModernControls()
    {
        controller.isJumping = jumpAction.action.IsPressed();

        if (boostAction.action.WasPressedThisFrame())
        {
            if (Time.time - lastBoostTapTime <= doubleTapWindow)
            {
                isChargingBoostQB = true;
                boostHoldStartTime = Time.time;
            }
            lastBoostTapTime = Time.time;
        }

        if (boostAction.action.WasReleasedThisFrame() && isChargingBoostQB)
        {
            float holdDuration = Time.time - boostHoldStartTime;
            bool isPerfect = holdDuration >= perfectQBHoldTime && holdDuration <= (perfectQBHoldTime + perfectQBReleaseWindow);
            
            controller.TriggerQuickBoost(isPerfect);
            isChargingBoostQB = false;
        }

        controller.isBoosting = boostAction.action.IsPressed() && !isChargingBoostQB;
    }

    private void ProcessCrowControls()
    {
        if (jumpAction.action.WasPressedThisFrame())
        {
            isChargingJumpQB = true;
            jumpHoldStartTime = Time.time;
        }

        if (jumpAction.action.WasReleasedThisFrame() && isChargingJumpQB)
        {
            float holdDuration = Time.time - jumpHoldStartTime;
            bool isPerfect = holdDuration >= perfectQBHoldTime && holdDuration <= (perfectQBHoldTime + perfectQBReleaseWindow);
            
            controller.TriggerQuickBoost(isPerfect);
            isChargingJumpQB = false;
        }

        if (boostAction.action.WasPressedThisFrame())
        {
            // NEW: Checks if the time since the first tap OR the time since the release was fast enough
            if (Time.time - lastBoostTapTime <= doubleTapWindow || Time.time - lastBoostReleaseTime <= doubleTapWindow)
            {
                doubleTapFlightActive = true;
            }
            lastBoostTapTime = Time.time;
        }

        if (boostAction.action.WasReleasedThisFrame())
        {
            doubleTapFlightActive = false;
            lastBoostReleaseTime = Time.time; // Store the exact moment the boost was let go
        }

        bool isHoldingBoost = boostAction.action.IsPressed();
        bool inAir = charController != null && !charController.isGrounded;

        controller.isJumping = (doubleTapFlightActive && isHoldingBoost) || (inAir && isHoldingBoost);
        controller.isBoosting = isHoldingBoost;
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