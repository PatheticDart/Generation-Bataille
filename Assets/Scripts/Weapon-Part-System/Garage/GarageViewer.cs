using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using Unity.Cinemachine; 

public class GarageViewer : MonoBehaviour
{
    [Header("Cinemachine 3 Setup")]
    [Tooltip("Drag your active CinemachineCamera here.")]
    public CinemachineCamera vcam;

    [Header("Orbit Settings")]
    public float rotationSpeed = 0.2f;
    public float heightSpeed = 0.02f;
    public float minHeight = 0f;
    public float maxHeight = 10f;
    
    [Tooltip("How quickly the camera slows down after letting go. Lower = spins longer.")]
    public float orbitFriction = 5.0f;

    [Header("Zoom Settings")]
    public float zoomSpeed = 0.01f;
    public float minRadius = 5f;
    public float maxRadius = 25f;

    [Header("Input Actions (Auto-Bound)")]
    public InputAction orbitPressAction = new InputAction("OrbitPress", binding: "<Pointer>/press");
    public InputAction orbitDeltaAction = new InputAction("OrbitDelta", binding: "<Pointer>/delta");
    public InputAction zoomScrollAction = new InputAction("ZoomScroll", binding: "<Pointer>/scroll");

    private CinemachineOrbitalFollow _orbiter;
    private bool _startedClickOnUI = false;
    
    // Tracks the current spin speed
    private float _orbitVelocityX = 0f;

    void Start()
    {
        if (vcam != null)
        {
            _orbiter = vcam.GetComponent<CinemachineOrbitalFollow>();
        }
    }

    void OnEnable()
    {
        orbitPressAction.Enable();
        orbitDeltaAction.Enable();
        zoomScrollAction.Enable();

        orbitPressAction.started += ctx =>
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                _startedClickOnUI = true;
                // Instantly kill momentum if the user clicks a UI button
                _orbitVelocityX = 0f; 
            }
        };

        orbitPressAction.canceled += ctx =>
        {
            _startedClickOnUI = false;
        };
    }

    void OnDisable()
    {
        orbitPressAction.Disable();
        orbitDeltaAction.Disable();
        zoomScrollAction.Disable();
    }

    void Update()
    {
        if (_orbiter == null) return;

        HandleZoom();
        HandleOrbit();
    }

    private void HandleOrbit()
    {
        // 1. Capture Input and set velocity
        if (!_startedClickOnUI && orbitPressAction.IsPressed())
        {
            Vector2 delta = orbitDeltaAction.ReadValue<Vector2>();

            // Horizontal: Convert mouse delta to velocity
            _orbitVelocityX = delta.x * rotationSpeed;

            // Vertical: We keep this direct for precise framing
            Vector3 offset = _orbiter.TargetOffset;
            offset.y -= delta.y * heightSpeed; 
            offset.y = Mathf.Clamp(offset.y, minHeight, maxHeight);
            _orbiter.TargetOffset = offset;
        }
        else
        {
            // 2. Apply friction to decay the velocity over time
            _orbitVelocityX = Mathf.Lerp(_orbitVelocityX, 0f, Time.deltaTime * orbitFriction);
        }

        // 3. Always apply the velocity to the camera axis
        if (Mathf.Abs(_orbitVelocityX) > 0.001f)
        {
            _orbiter.HorizontalAxis.Value += _orbitVelocityX;
        }
    }

    private void HandleZoom()
    {
        Vector2 scroll = zoomScrollAction.ReadValue<Vector2>();
        
        if (Mathf.Abs(scroll.y) > 0.01f)
        {
            float newRadius = _orbiter.Radius - (scroll.y * zoomSpeed);
            _orbiter.Radius = Mathf.Clamp(newRadius, minRadius, maxRadius);
        }
    }

    public void SetActiveCamera(CinemachineCamera newCam)
    {
        vcam = newCam;
        if (vcam != null)
        {
            _orbiter = vcam.GetComponent<CinemachineOrbitalFollow>();
            // Reset velocity when switching cameras so it doesn't spin the new view wildly
            _orbitVelocityX = 0f; 
        }
    }
}