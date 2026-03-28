using UnityEngine;

public class GaragePaintMessenger : MonoBehaviour
{
    [Tooltip("Drag your GarageLoader here so we can grab the paint job before leaving.")]
    public GarageLoader garageLoader;

    // --- NEW: A tiny wrapper to safely save colors to the hard drive without breaking textures ---
    [System.Serializable]
    private class PaintAutoSave
    {
        public Color[] albedo = new Color[5];
        public Color[] emission = new Color[5];
    }

    private void Start()
    {
        // 1. When the Garage opens, check if we have an auto-saved paint job from last time
        if (garageLoader != null && PlayerPrefs.HasKey("AutoSavePaint"))
        {
            string json = PlayerPrefs.GetString("AutoSavePaint");
            PaintAutoSave savedData = JsonUtility.FromJson<PaintAutoSave>(json);

            if (savedData != null && garageLoader.globalPaintJob != null)
            {
                // Unpack the saved colors into the GarageLoader
                for (int i = 0; i < Mathf.Min(5, garageLoader.globalPaintJob.Length); i++)
                {
                    garageLoader.globalPaintJob[i].albedoColor = savedData.albedo[i];
                    garageLoader.globalPaintJob[i].emissionColor = savedData.emission[i];
                }
                
                // Push the colors to the 3D model!
                garageLoader.FastApplyPaintToMech();
            }
        }
    }

    private void OnDestroy()
    {
        if (garageLoader != null && garageLoader.globalPaintJob != null)
        {
            // 1. Pack the paint job into the Arena's static backpack for the scene transition
            ArenaMechLoader.TransitPaintJob = garageLoader.globalPaintJob;

            // 2. Auto-save the colors so they survive hitting "Stop" in the editor
            SaveToPlayerPrefs();
        }
    }

    // This ensures it also saves if you just close the game window or hit Stop
    private void OnApplicationQuit()
    {
        SaveToPlayerPrefs();
    }

    private void SaveToPlayerPrefs()
    {
        if (garageLoader == null || garageLoader.globalPaintJob == null) return;

        PaintAutoSave saveData = new PaintAutoSave();
        
        for (int i = 0; i < Mathf.Min(5, garageLoader.globalPaintJob.Length); i++)
        {
            saveData.albedo[i] = garageLoader.globalPaintJob[i].albedoColor;
            saveData.emission[i] = garageLoader.globalPaintJob[i].emissionColor;
        }

        // Convert to text and save silently in the background
        string json = JsonUtility.ToJson(saveData);
        PlayerPrefs.SetString("AutoSavePaint", json);
        PlayerPrefs.Save();
    }
}