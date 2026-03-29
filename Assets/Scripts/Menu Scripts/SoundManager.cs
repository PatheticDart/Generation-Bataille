using UnityEngine;

public class SoundManager : MonoBehaviour
{
    [Header("Audio Sources")]
    public AudioSource masterAudioSource;
    public AudioSource sfxAudioSource;
    public AudioSource bgmAudioSource;

    // [Header("Audio Clips")]
    //insert audio Clips
    //EX:
    //public AudioClip buttonClickSFX;

    private void Start(){
        //sfxAudioSource = buttonClickSFX;
    }

    public void PlaySFX(AudioClip clip) //insert this function to any script to play a sound effect, make sure to assign the sfxAudioSource in the inspector and pass the desired AudioClip as an argument
    {
        sfxAudioSource.PlayOneShot(clip);
    }
}
