using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MechAnimationEvents : MonoBehaviour
{
    [Header("Camera Effects")]
    [Tooltip("Drag your Main Camera (or the object holding CameraEffects) here.")]
    public CameraEffects cameraEffects;

    [Header("Audio Settings")]
    public AudioClip footstepSound;

    private AudioSource baseAudioSource;
    private AudioSource footstepAudioEmitter;

    private Transform leftFootBone;
    private Transform rightFootBone;

    void Awake()
    {
        baseAudioSource = GetComponent<AudioSource>();
        baseAudioSource.playOnAwake = false;

        GameObject emitterObj = new GameObject("FootstepAudioEmitter");
        emitterObj.transform.SetParent(transform);
        footstepAudioEmitter = emitterObj.AddComponent<AudioSource>();

        footstepAudioEmitter.outputAudioMixerGroup = baseAudioSource.outputAudioMixerGroup;
        footstepAudioEmitter.spatialBlend = baseAudioSource.spatialBlend;
        footstepAudioEmitter.rolloffMode = baseAudioSource.rolloffMode;
        footstepAudioEmitter.minDistance = baseAudioSource.minDistance;
        footstepAudioEmitter.maxDistance = baseAudioSource.maxDistance;
        footstepAudioEmitter.playOnAwake = false;

        // We removed FindFootBones() from here so it waits until the mech is fully built!
    }

    private void FindFootBones()
    {
        // Search for LegsNode anywhere under the mech's absolute root
        Transform legsNode = FindDeepChild(transform.root, "LegsNode");

        if (legsNode != null)
        {
            leftFootBone = FindDeepChild(legsNode, "Left Foot Bone");
            rightFootBone = FindDeepChild(legsNode, "Right Foot Bone");

            if (leftFootBone == null) Debug.LogWarning("Left Foot Bone not found under LegsNode!");
            if (rightFootBone == null) Debug.LogWarning("Right Foot Bone not found under LegsNode!");
        }
    }

    private Transform FindDeepChild(Transform parent, string childName)
    {
        if (parent.name == childName) return parent;

        foreach (Transform child in parent)
        {
            Transform result = FindDeepChild(child, childName);
            if (result != null) return result;
        }
        return null;
    }

    public void OnFootstep(AnimationEvent animEvent)
    {
        float intensity = animEvent.floatParameter;
        int footIndex = animEvent.intParameter; // 0 for Left, 1 for Right

        if (cameraEffects != null)
        {
            cameraEffects.TriggerFootstepShake(intensity);
        }

        // --- THE FIX: Try to find the bones right as the step happens if we haven't yet! ---
        if (leftFootBone == null || rightFootBone == null)
        {
            FindFootBones();
        }

        Transform targetFoot = (footIndex == 0) ? leftFootBone : rightFootBone;

        // --- FAIL-SAFE: If the bone still isn't found, just play it at the mech's center ---
        Vector3 playPosition = targetFoot != null ? targetFoot.position : transform.position;

        if (footstepSound != null)
        {
            footstepAudioEmitter.volume = baseAudioSource.volume;
            footstepAudioEmitter.pitch = baseAudioSource.pitch;
            footstepAudioEmitter.transform.position = playPosition;

            footstepAudioEmitter.PlayOneShot(footstepSound);
        }
    }
}