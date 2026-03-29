using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OptionsManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject volumePanel;
    public GameObject controlTypePanel;

    [Header("Control Type Variables")]
    public PlayerBrain playerBrain;
    public TMPro.TMP_Dropdown controlTypeDropdown;

    public void Start()
    {
        if (volumePanel != null)
        {
            volumePanel.SetActive(false);
        }
        if (controlTypePanel != null)
        {
            controlTypePanel.SetActive(false);
        }
        if (controlTypeDropdown != null && playerBrain != null)
        {
            controlTypeDropdown.ClearOptions();
            List<string> options = new List<string>(Enum.GetNames(typeof(ControlSchemeType)));
            controlTypeDropdown.AddOptions(options);
            controlTypeDropdown.value = (int)playerBrain.activeControlScheme;
            controlTypeDropdown.RefreshShownValue();
            controlTypeDropdown.onValueChanged.AddListener(OnControlSchemeChanged);
        }
    }

    public void OnControlSchemeChanged(int index)
    {
        if (playerBrain == null) return;
        playerBrain.activeControlScheme = (ControlSchemeType)index;
    }

    private void OnDestroy()
    {
        if (controlTypeDropdown != null)
            controlTypeDropdown.onValueChanged.RemoveListener(OnControlSchemeChanged);
    }

    public void ShowVolumePanel()
    {
        if (volumePanel != null)
        {
            volumePanel.SetActive(true);
            controlTypePanel.SetActive(false);
        }
    }

    public void ShowControlTypePanel()
    {
        if (controlTypePanel != null)
        {
            controlTypePanel.SetActive(true);
            volumePanel.SetActive(false);
        }
    }
}
