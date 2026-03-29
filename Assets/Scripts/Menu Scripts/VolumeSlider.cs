using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using TMPro;

public class VolumeSlider : MonoBehaviour
{
    public TextMeshProUGUI masterVolumeValueText;
    public TextMeshProUGUI sfxVolumeValueText;
    public TextMeshProUGUI bgmVolumeValueText;
    public AudioMixer audioMixer;
    public Slider masterSlider;
    public Slider sfxSlider;
    public Slider bgmSlider;

    public void Start()
    {
        if (PlayerPrefs.HasKey("BGM") && PlayerPrefs.HasKey("SFX") && PlayerPrefs.HasKey("Master"))
        {
            LoadVolume();
        }
        else
        {
            SetBGMVolume();
            SetSFXVolume();
            SetMasterVolume();
        }
    }

    public void SetBGMVolume()
    {
        float volume = bgmSlider.value;
        audioMixer.SetFloat("BGM", Mathf.Log10(volume) * 20);
        PlayerPrefs.SetFloat("BGM", volume);
        bgmVolumeValueText.text = Mathf.RoundToInt(volume * 100).ToString();
    }

    public void SetSFXVolume()
    {
        float volume = sfxSlider.value;
        audioMixer.SetFloat("SFX", Mathf.Log10(volume) * 20);
        PlayerPrefs.SetFloat("SFX", volume);
        sfxVolumeValueText.text = Mathf.RoundToInt(volume * 100).ToString();
    }

    public void SetMasterVolume()
    {
        float volume = masterSlider.value;
        audioMixer.SetFloat("Master", Mathf.Log10(volume) * 20);
        PlayerPrefs.SetFloat("Master", volume);
        masterVolumeValueText.text = Mathf.RoundToInt(volume * 100).ToString();
    }

    private void LoadVolume()
    {
        bgmSlider.value = PlayerPrefs.GetFloat("BGM");
        sfxSlider.value = PlayerPrefs.GetFloat("SFX");
        masterSlider.value = PlayerPrefs.GetFloat("Master");
        SetBGMVolume();
        SetSFXVolume();
        SetMasterVolume();
    }
}
