using UnityEngine;
using Unity.Cinemachine;

[RequireComponent(typeof(CinemachineCamera))]
public class CameraEffects : MonoBehaviour
{
    [Header("References")]
    public Transform playerRoot;
    public Camera mainCamera;
    [Tooltip("Drag the MechController here so the camera knows when you are boosting.")]
    public MechController mechController;

    [Header("Tilt Settings")]
    public float maxTiltAngle = 5f;
    public float maxSpeedForTilt = 20f;
    public float tiltSmoothing = 5f;

    [Header("Camera Shake Settings")]
    public float boostShakeAmplitude = 0.8f;
    public float boostShakeFrequency = 10f;
    [Tooltip("Base shake multiplier for hard landings.")]
    public float landingShakeBaseAmplitude = 4f;
    public float landingShakeFrequency = 15f;
    [Tooltip("How fast the landing shake fades away.")]
    public float shakeDecayRate = 5f;

    private CinemachineCamera vcam;
    private CinemachineBasicMultiChannelPerlin noise;
    private float currentTilt = 0f;
    private Vector3 lastPosition;

    // Tracks the current strength of a hard landing impact
    private float currentImpactAmplitude = 0f;

    void Start()
    {
        vcam = GetComponent<CinemachineCamera>();
        noise = GetComponent<CinemachineBasicMultiChannelPerlin>();

        if (noise == null)
        {
            Debug.LogWarning("No Cinemachine Noise component found. Camera shake will not work.");
        }

        if (playerRoot != null)
        {
            lastPosition = playerRoot.position;
        }
    }

    void LateUpdate()
    {
        if (mainCamera == null || playerRoot == null) return;

        // --- NEW: SAFETY CHECK FOR PAUSE MENU ---
        // If the game is paused, or Time.deltaTime is effectively zero, skip calculating the tilt
        // to prevent DivideByZeroException / NaN corruption.
        if (Time.deltaTime > 0f)
        {
            // --- 1. TILT LOGIC ---
            Vector3 velocity = (playerRoot.position - lastPosition) / Time.deltaTime;
            lastPosition = playerRoot.position;

            float sideSpeed = Vector3.Dot(velocity, mainCamera.transform.right);
            float speedFactor = Mathf.Clamp(sideSpeed / maxSpeedForTilt, -1f, 1f);
            float targetTilt = -speedFactor * maxTiltAngle;

            currentTilt = Mathf.Lerp(currentTilt, targetTilt, tiltSmoothing * Time.deltaTime);

            // Apply to Cinemachine Lens
            var lens = vcam.Lens;
            lens.Dutch = currentTilt;
            vcam.Lens = lens;
        }

        // --- 2. SHAKE LOGIC ---
        // (This remains untouched since you mentioned it still works perfectly!)
        if (noise != null && mechController != null)
        {
            float finalAmplitude = 0f;
            float finalFrequency = 0f;

            // A. Continuous Boost Shake
            if (mechController.isBoosting && mechController.moveInput.magnitude > 0.1f && !mechController.isRecoveringFromLanding)
            {
                finalAmplitude = boostShakeAmplitude;
                finalFrequency = boostShakeFrequency;
            }

            // B. Hard Landing Impact Shake 
            if (currentImpactAmplitude > 0.1f)
            {
                finalAmplitude += currentImpactAmplitude;
                finalFrequency = Mathf.Max(finalFrequency, landingShakeFrequency);

                // Rapidly fade the impact out over time
                currentImpactAmplitude = Mathf.Lerp(currentImpactAmplitude, 0f, shakeDecayRate * Time.deltaTime);
            }

            noise.AmplitudeGain = finalAmplitude;
            noise.FrequencyGain = finalFrequency;
        }
    }

    public void TriggerImpactShake(float severityMultiplier)
    {
        currentImpactAmplitude = landingShakeBaseAmplitude * severityMultiplier;
    }
}