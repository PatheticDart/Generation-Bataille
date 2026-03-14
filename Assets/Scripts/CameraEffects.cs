using UnityEngine;
using Unity.Cinemachine;

[RequireComponent(typeof(CinemachineCamera))]
public class CameraEffects : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The root of your Mech/Player to track velocity.")]
    public Transform playerRoot;
    [Tooltip("The Main Camera in the scene.")]
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

    [Header("Impact Shake (Landings)")]
    [Tooltip("Base shake multiplier for hard landings.")]
    public float landingShakeBaseAmplitude = 4f;
    public float landingShakeFrequency = 15f;
    [Tooltip("How fast the landing shake fades away.")]
    public float shakeDecayRate = 5f;

    [Header("Footstep Shake (Walking)")]
    [Tooltip("Base shake amplitude for footsteps.")]
    public float footstepShakeBaseAmplitude = 1.0f;
    public float footstepShakeFrequency = 12f;
    [Tooltip("How fast the footstep shake fades away (usually faster than landings).")]
    public float footstepDecayRate = 12f;

    private CinemachineCamera vcam;
    private CinemachineBasicMultiChannelPerlin noise;
    private float currentTilt = 0f;
    private Vector3 lastPosition;

    // Tracks the current strength of a hard landing impact
    private float currentImpactAmplitude = 0f;
    // Tracks the current strength of footstep impacts
    private float currentFootstepAmplitude = 0f;

    void Start()
    {
        vcam = GetComponent<CinemachineCamera>();
        noise = GetComponent<CinemachineBasicMultiChannelPerlin>();

        if (noise == null)
        {
            Debug.LogWarning("No CinemachineBasicMultiChannelPerlin component found on this camera. Shake will not work.");
        }

        if (playerRoot != null)
        {
            lastPosition = playerRoot.position;
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }

    void LateUpdate()
    {
        if (mainCamera == null || playerRoot == null) return;

        // --- SAFETY CHECK FOR PAUSE MENU ---
        if (Time.deltaTime > 0f)
        {
            // --- 1. TILT LOGIC ---
            Vector3 velocity = (playerRoot.position - lastPosition) / Time.deltaTime;
            lastPosition = playerRoot.position;

            float sideSpeed = Vector3.Dot(velocity, mainCamera.transform.right);
            float speedFactor = Mathf.Clamp(sideSpeed / maxSpeedForTilt, -1f, 1f);
            float targetTilt = -speedFactor * maxTiltAngle;

            currentTilt = Mathf.Lerp(currentTilt, targetTilt, tiltSmoothing * Time.deltaTime);

            var lens = vcam.Lens;
            lens.Dutch = currentTilt;
            vcam.Lens = lens;
        }

        // --- 2. SHAKE LOGIC ---
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

                currentImpactAmplitude = Mathf.Lerp(currentImpactAmplitude, 0f, shakeDecayRate * Time.deltaTime);
            }

            // C. Footstep Shake 
            if (currentFootstepAmplitude > 0.1f)
            {
                finalAmplitude += currentFootstepAmplitude;
                finalFrequency = Mathf.Max(finalFrequency, footstepShakeFrequency);

                currentFootstepAmplitude = Mathf.Lerp(currentFootstepAmplitude, 0f, footstepDecayRate * Time.deltaTime);
            }

            // Apply to Cinemachine Noise Profile
            noise.AmplitudeGain = finalAmplitude;
            noise.FrequencyGain = finalFrequency;
        }
    }

    public void TriggerImpactShake(float severityMultiplier)
    {
        currentImpactAmplitude = landingShakeBaseAmplitude * severityMultiplier;
    }

    // --- NEW: Event function for the animation timeline ---
    public void TriggerFootstepShake(float intensity)
    {
        currentFootstepAmplitude = footstepShakeBaseAmplitude * intensity;
    }
}