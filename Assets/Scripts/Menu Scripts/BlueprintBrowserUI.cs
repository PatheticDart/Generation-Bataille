using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Collections.Generic;

public class BlueprintBrowserUI : MonoBehaviour
{
    public MechCardManager cardManager;

    [Header("Save UI")]
    public TMP_InputField saveNameInput; // Where the player types "CoolMech"
    public Button saveButton;            // The button to save it

    [Header("Load UI")]
    public TMP_Dropdown fileDropdown;    // Dropdown listing all saved PNGs
    public Button loadButton;            // The button to load the selected dropdown item
    public Button refreshButton;         // Optional button to refresh the list

    private List<string> availableFilePaths = new List<string>();

    void Start()
    {
        if (saveButton) saveButton.onClick.AddListener(SaveBlueprint);
        if (loadButton) loadButton.onClick.AddListener(LoadBlueprint);
        if (refreshButton) refreshButton.onClick.AddListener(RefreshFileList);

        RefreshFileList();
    }

    void OnEnable()
    {
        RefreshFileList();
    }

    public void SaveBlueprint()
    {
        string fileName = saveNameInput != null ? saveNameInput.text : "MyMech";
        if (string.IsNullOrEmpty(fileName)) fileName = "MyMech_" + System.DateTime.Now.ToString("MMdd_HHmm");

        cardManager.CaptureMechCard(fileName);

        // Clear the input and refresh the dropdown so the new file appears
        if (saveNameInput != null) saveNameInput.text = "";
        RefreshFileList();
    }

    public void RefreshFileList()
    {
        if (fileDropdown == null || cardManager == null) return;

        string dir = cardManager.GetSaveDirectory();

        availableFilePaths.Clear();
        fileDropdown.ClearOptions();

        string[] files = Directory.GetFiles(dir, "*.png");
        List<string> fileNames = new List<string>();

        foreach (string file in files)
        {
            availableFilePaths.Add(file);
            fileNames.Add(Path.GetFileNameWithoutExtension(file)); // Display "CoolMech" instead of "C:/.../CoolMech.png"
        }

        fileDropdown.AddOptions(fileNames);
    }

    public void LoadBlueprint()
    {
        if (availableFilePaths.Count == 0 || fileDropdown == null) return;

        int index = fileDropdown.value;
        if (index >= 0 && index < availableFilePaths.Count)
        {
            cardManager.LoadFromMechCard(availableFilePaths[index]);
        }
    }
}