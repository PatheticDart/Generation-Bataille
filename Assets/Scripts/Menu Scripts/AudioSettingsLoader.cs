using UnityEngine;
using UnityEngine.Audio;

public class AudioSettingsLoader : MonoBehaviour
{
    [Tooltip("Drag your Main Audio Mixer here.")]
    public AudioMixer audioMixer;

    void Start()
    {
        if (audioMixer == null)
        {
            Debug.LogWarning("AudioSettingsLoader: No AudioMixer assigned!");
            return;
        }

        // Apply saved settings immediately. If no save exists, it defaults to 1 (max volume).
        ApplySavedVolume("Master");
        ApplySavedVolume("SFX");
        ApplySavedVolume("BGM");
    }

    private void ApplySavedVolume(string parameterName)
    {
        float savedVolume = PlayerPrefs.GetFloat(parameterName, 1f);

        // Safety catch: Mathf.Log10(0) equals negative infinity, which breaks the mixer.
        // If the volume is effectively zero, manually set it to -80dB (standard Unity mute).
        if (savedVolume <= 0.0001f)
        {
            audioMixer.SetFloat(parameterName, -80f);
        }
        else
        {
            audioMixer.SetFloat(parameterName, Mathf.Log10(savedVolume) * 20f);
        }
    }
}