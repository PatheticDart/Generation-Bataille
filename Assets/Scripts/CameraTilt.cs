using UnityEngine;
using Unity.Cinemachine;

[RequireComponent(typeof(CinemachineCamera))]
public class CameraTilt : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Drag the Player's main root object here.")]
    public Transform playerRoot;
    [Tooltip("Drag the Unity Main Camera here (the one with the CinemachineBrain).")]
    public Camera mainCamera; 

    [Header("Tilt Settings")]
    [Tooltip("Maximum degrees the camera will tilt.")]
    public float maxTiltAngle = 5f;
    [Tooltip("How fast the mech needs to move sideways to hit max tilt.")]
    public float maxSpeedForTilt = 20f; 
    [Tooltip("How smoothly it leans in and out.")]
    public float tiltSmoothing = 5f;

    private CinemachineCamera vcam;
    private float currentTilt = 0f;
    private Vector3 lastPosition;

    void Start()
    {
        vcam = GetComponent<CinemachineCamera>();
        
        if (playerRoot != null) 
        {
            lastPosition = playerRoot.position;
        }
        else
        {
            Debug.LogWarning("CameraTilt: Player Root is missing! The camera won't tilt.");
        }
    }

    void LateUpdate()
    {
        // Safety check to ensure we don't break the camera if a reference is missing
        if (playerRoot == null || mainCamera == null) return;

        // 1. Calculate true velocity based on position change (frame-rate independent)
        Vector3 velocity = (playerRoot.position - lastPosition) / Time.deltaTime;
        lastPosition = playerRoot.position;

        // 2. Use Dot Product to isolate ONLY the side-to-side movement relative to the camera lens
        // Positive value = moving right, Negative value = moving left
        float sideSpeed = Vector3.Dot(velocity, mainCamera.transform.right);

        // 3. Calculate how much we should tilt based on our max speed
        float speedFactor = Mathf.Clamp(sideSpeed / maxSpeedForTilt, -1f, 1f);
        
        // Multiply by negative maxTiltAngle so moving Right (positive speed) tilts the camera Left (negative angle)
        float targetTilt = -speedFactor * maxTiltAngle;

        // 4. Smooth the transition
        currentTilt = Mathf.Lerp(currentTilt, targetTilt, tiltSmoothing * Time.deltaTime);
        
        // 5. Apply the Dutch angle safely to the Cinemachine 3 Lens
        var lens = vcam.Lens;
        lens.Dutch = currentTilt;
        vcam.Lens = lens;
    }
}