using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using Unity.Cinemachine; // NEW Cinemachine 3 Namespace!

public class GarageViewer : MonoBehaviour
{
    [Header("Cinemachine 3 Setup")]
    [Tooltip("Drag your CinemachineCamera here (not the old VirtualCamera!)")]
    public CinemachineCamera vcam;

    [Header("Orbit Settings")]
    public float rotationSpeed = 0.2f;
    
    [Tooltip("How fast the camera moves up and down the mech's body.")]
    public float heightSpeed = 0.02f;
    public float minHeight = 0f;
    public float maxHeight = 10f;

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

    void Start()
    {
        if (vcam != null)
        {
            // In CM3, pipeline components are just standard components on the camera!
            _orbiter = vcam.GetComponent<CinemachineOrbitalFollow>();
        }
    }

    void OnEnable()
    {
        orbitPressAction.Enable();
        orbitDeltaAction.Enable();
        zoomScrollAction.Enable();

        // Check if the initial click was on a UI menu
        orbitPressAction.started += ctx =>
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                _startedClickOnUI = true;
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

        if (!_startedClickOnUI)
        {
            HandleOrbit();
        }
    }

    private void HandleOrbit()
    {
        if (orbitPressAction.IsPressed())
        {
            Vector2 delta = orbitDeltaAction.ReadValue<Vector2>();

            // X-Axis: Orbit around the mech
            _orbiter.HorizontalAxis.Value += delta.x * rotationSpeed;

            // Y-Axis: Move the TargetOffset up and down to look at feet vs head
            Vector3 offset = _orbiter.TargetOffset;
            offset.y -= delta.y * heightSpeed; 
            offset.y = Mathf.Clamp(offset.y, minHeight, maxHeight);
            
            _orbiter.TargetOffset = offset;
        }
    }

    private void HandleZoom()
    {
        Vector2 scroll = zoomScrollAction.ReadValue<Vector2>();
        
        if (Mathf.Abs(scroll.y) > 0.01f)
        {
            // CM3 has a dedicated Radius property, making zoom logic beautifully simple
            float newRadius = _orbiter.Radius - (scroll.y * zoomSpeed);
            _orbiter.Radius = Mathf.Clamp(newRadius, minRadius, maxRadius);
        }
    }
}