using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

[RequireComponent(typeof(AudioSource))]
public class UIButtonSoundManager : MonoBehaviour
{
    [Header("Audio Settings")]
    [Tooltip("The sound effect to play when any button is clicked.")]
    public AudioClip clickSound;

    [Tooltip("Assign your SFX Audio Mixer Group here to route the sound properly.")]
    public AudioMixerGroup sfxMixerGroup;

    private AudioSource audioSource;

    void Awake()
    {
        // 1. Initialize the AudioSource automatically
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;

        // 2. Route the output to your specific SFX channel
        if (sfxMixerGroup != null)
        {
            audioSource.outputAudioMixerGroup = sfxMixerGroup;
        }
        else
        {
            Debug.LogWarning("UIButtonSoundManager: SFX Mixer Group is not assigned on " + gameObject.name);
        }

        // 3. Find EVERY button under this Canvas (including disabled ones)
        Button[] allButtons = GetComponentsInChildren<Button>(true);

        // 4. Attach the sound event to each button's onClick
        foreach (Button btn in allButtons)
        {
            btn.onClick.AddListener(PlayClickSound);
        }
    }

    private void PlayClickSound()
    {
        if (clickSound != null && audioSource != null)
        {
            // PlayOneShot allows multiple clicks to overlap naturally instead of cutting each other off
            audioSource.PlayOneShot(clickSound);
        }
    }

    /// <summary>
    /// Call this if you instantiate new buttons at runtime (like in a scrollable list or inventory)
    /// </summary>
    public void RegisterNewButton(Button newButton)
    {
        if (newButton != null)
        {
            newButton.onClick.AddListener(PlayClickSound);
        }
    }
}