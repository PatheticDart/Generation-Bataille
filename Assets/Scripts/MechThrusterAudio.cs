using UnityEngine;

[RequireComponent(typeof(MechController))]
[RequireComponent(typeof(AudioSource))]
public class MechThrusterAudio : MonoBehaviour
{
    [Header("Thruster Audio Settings")]
    [Tooltip("The looping sound effect for the thrusters.")]
    public AudioClip thrusterLoopSFX;

    [Tooltip("Maximum volume when fully boosting.")]
    [Range(0f, 1f)] public float maxVolume = 1f;

    [Tooltip("How fast the sound fades IN when boosting starts. Higher is faster.")]
    public float fadeInSpeed = 15f;

    [Tooltip("How fast the sound fades OUT when boosting stops. Lower is smoother/slower.")]
    public float fadeOutSpeed = 7f;

    private MechController mechController;
    private AudioSource audioSource;

    void Start()
    {
        mechController = GetComponent<MechController>();
        audioSource = GetComponent<AudioSource>();

        // Automatically configure the AudioSource for seamless looping
        audioSource.clip = thrusterLoopSFX;
        audioSource.loop = true;
        audioSource.volume = 0f;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f; // Force 3D sound so you can hear the AI flying around!
    }

    void Update()
    {
        if (mechController == null) return;

        if (mechController.isBoosting)
        {
            // If it isn't playing yet, start it
            if (!audioSource.isPlaying && thrusterLoopSFX != null)
            {
                audioSource.Play();
            }

            // Smoothly fade the volume UP to max
            audioSource.volume = Mathf.Lerp(audioSource.volume, maxVolume, Time.deltaTime * fadeInSpeed);
        }
        else
        {
            // If we are no longer boosting, smoothly fade the volume DOWN
            if (audioSource.isPlaying && audioSource.volume > 0f)
            {
                audioSource.volume = Mathf.Lerp(audioSource.volume, 0f, Time.deltaTime * fadeOutSpeed);

                // Once it gets quiet enough, completely stop the source to save CPU processing
                if (audioSource.volume <= 0.01f)
                {
                    audioSource.volume = 0f;
                    audioSource.Stop();
                }
            }
        }
    }
}