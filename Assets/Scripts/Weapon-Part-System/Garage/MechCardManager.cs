using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using NaughtyAttributes;


#if UNITY_EDITOR
using UnityEditor;
#endif

public class MechCardManager : MonoBehaviour
{
    [Header("System References")]
    public GarageLoader garageLoader;

    [Header("Capture Setup")]
    public Camera snapshotCamera; 
    public Vector2Int resolution = new Vector2Int(1024, 1024);

    private string _defaultSaveDirectory;

    private void Awake()
    {
        if (snapshotCamera != null) snapshotCamera.gameObject.SetActive(false);
        
        // Ensure a dedicated folder exists for our cards if we aren't using the OS file picker
        _defaultSaveDirectory = Path.Combine(Application.persistentDataPath, "MechCards");
        if (!Directory.Exists(_defaultSaveDirectory))
        {
            Directory.CreateDirectory(_defaultSaveDirectory);
        }
    }

    // --- SERIALIZATION STRUCTURES ---
    [Serializable]
    public struct SavedPaint
    {
        public PartMaterialLocation map;
        public Color albedoColor;
        public Color emissionColor;
        public int albedoTexIdx;
        public int emissionTexIdx;
    }

    [Serializable]
    private struct MechSaveData
    {
        public int headIdx;
        public int torsoIdx;
        public int armsIdx;
        public int legsIdx;
        public int boosterIdx;
        public int genIdx;
        public int fcsIdx;
        
        public int armLIdx;
        public int armRIdx;
        public int backLIdx;
        public int backRIdx;

        public List<SavedPaint> paintJobs;
    }

    // --- 1. SAVING ---

    [ContextMenu("Take Snapshot & Save"), Button("Take Snapshot & Save")] 
    public void CaptureMechCard()
    {
        if (snapshotCamera == null || garageLoader == null) return;

        byte[] encodedSaveData = EncodeMechData();

        snapshotCamera.gameObject.SetActive(true);

        RenderTexture rt = new RenderTexture(resolution.x, resolution.y, 24, RenderTextureFormat.ARGB32);
        snapshotCamera.targetTexture = rt;
        snapshotCamera.Render();
        RenderTexture.active = rt;

        Texture2D screenShot = new Texture2D(resolution.x, resolution.y, TextureFormat.ARGB32, false);
        screenShot.ReadPixels(new Rect(0, 0, resolution.x, resolution.y), 0, 0);
        screenShot.Apply();

        byte[] pngImageData = screenShot.EncodeToPNG();

        // Cleanup Camera Resources immediately
        snapshotCamera.targetTexture = null;
        RenderTexture.active = null;
        DestroyImmediate(rt);
        DestroyImmediate(screenShot);
        snapshotCamera.gameObject.SetActive(false);

        // --- FILE MANAGER: SAVING ---
        string filePath = "";

        #if UNITY_EDITOR
                // Opens the OS Save Dialog in the Editor
                string defaultName = $"MechCard_{DateTime.Now.ToString("yyyyMMdd_HHmmss")}";
                filePath = EditorUtility.SaveFilePanel("Save Mech Card", "", defaultName, "png");
                if (string.IsNullOrEmpty(filePath)) return; // User canceled the save dialog
        #else
                // Silent fallback for built games
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                filePath = Path.Combine(_defaultSaveDirectory, $"MechCard_{timestamp}.png");
        #endif

        try
        {
            using (FileStream fileStream = File.Open(filePath, FileMode.Create))
            {
                fileStream.Write(pngImageData, 0, pngImageData.Length);       
                fileStream.Write(encodedSaveData, 0, encodedSaveData.Length); 
            }
            Debug.Log($"<color=green>Mech Card Saved:</color> {filePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save Mech Card: {e.Message}");
        }
    }

    private byte[] EncodeMechData()
    {
        var active = GarageLoader.ActiveLoadout; 

        MechSaveData data = new MechSaveData
        {
            headIdx    = active.ContainsKey(PartType.Head) ? garageLoader.availableHeads.IndexOf((HeadPart)active[PartType.Head]) : -1,
            torsoIdx   = active.ContainsKey(PartType.Torso) ? garageLoader.availableTorsos.IndexOf((TorsoPart)active[PartType.Torso]) : -1,
            armsIdx    = active.ContainsKey(PartType.Arms) ? garageLoader.availableArms.IndexOf((ArmPart)active[PartType.Arms]) : -1,
            legsIdx    = active.ContainsKey(PartType.Legs) ? garageLoader.availableLegs.IndexOf((LegPart)active[PartType.Legs]) : -1,
            boosterIdx = active.ContainsKey(PartType.Booster) ? garageLoader.availableBoosters.IndexOf((Booster)active[PartType.Booster]) : -1,
            genIdx     = active.ContainsKey(PartType.Generator) ? garageLoader.availableGenerators.IndexOf((Generator)active[PartType.Generator]) : -1,
            fcsIdx     = active.ContainsKey(PartType.FCS) ? garageLoader.availableFCS.IndexOf((FCSPart)active[PartType.FCS]) : -1,

            armLIdx  = active.ContainsKey(PartType.ArmL) ? garageLoader.availableLeftArmWeapons.IndexOf((WeaponPart)active[PartType.ArmL]) : -1,
            armRIdx  = active.ContainsKey(PartType.ArmR) ? garageLoader.availableRightArmWeapons.IndexOf((WeaponPart)active[PartType.ArmR]) : -1,
            backLIdx = active.ContainsKey(PartType.BackL) ? garageLoader.availableLeftBackWeapons.IndexOf((WeaponPart)active[PartType.BackL]) : -1,
            backRIdx = active.ContainsKey(PartType.BackR) ? garageLoader.availableRightBackWeapons.IndexOf((WeaponPart)active[PartType.BackR]) : -1,
            
            paintJobs = new List<SavedPaint>()
        };

        if (garageLoader.globalPaintJob != null)
        {
            foreach (var paint in garageLoader.globalPaintJob)
            {
                data.paintJobs.Add(new SavedPaint
                {
                    map = paint.map,
                    albedoColor = paint.albedoColor,
                    emissionColor = paint.emissionColor,
                    albedoTexIdx = paint.albedoTexture != null ? garageLoader.availableAlbedoTextures.IndexOf(paint.albedoTexture) : -1,
                    emissionTexIdx = paint.emissionTexture != null ? garageLoader.availableEmissionTextures.IndexOf(paint.emissionTexture) : -1
                });
            }
        }

        string json = JsonUtility.ToJson(data);
        return Encoding.UTF8.GetBytes(json);
    }


    // --- 2. LOADING ---

    [ContextMenu("Load From Mech Card"), Button("Load From Mech Card")]
    public void LoadFromMechCard()
    {
        string filePath = "";

#if UNITY_EDITOR
        // Opens the OS Open File Dialog in the Editor
        filePath = EditorUtility.OpenFilePanel("Load Mech Card", "", "png");
        if (string.IsNullOrEmpty(filePath)) return; // User canceled the load dialog
#else
        // In a real build, you'd likely want to load the newest file, or build a UI to pick one.
        // For now, it just looks for a generic test file in the persistent path.
        filePath = Path.Combine(_defaultSaveDirectory, "MechCard_Test.png");
#endif

        if (!File.Exists(filePath))
        {
            Debug.LogError($"Load Failed: File not found at {filePath}");
            return;
        }

        byte[] fileBytes = File.ReadAllBytes(filePath);

        // Find the PNG end chunk (IEND + CRC)
        byte[] iendSignature = new byte[] { 0x49, 0x45, 0x4e, 0x44, 0xae, 0x42, 0x60, 0x82 };
        int iendIndex = -1;

        for (int i = fileBytes.Length - iendSignature.Length; i >= 0; i--)
        {
            bool match = true;
            for (int j = 0; j < iendSignature.Length; j++)
            {
                if (fileBytes[i + j] != iendSignature[j]) { match = false; break; }
            }
            if (match) { iendIndex = i; break; }
        }

        if (iendIndex == -1 || iendIndex + iendSignature.Length >= fileBytes.Length)
        {
            Debug.LogError("Load Failed: This image does not contain embedded Mech data.");
            return;
        }

        int dataStartIndex = iendIndex + iendSignature.Length;
        int dataLength = fileBytes.Length - dataStartIndex;
        string json = Encoding.UTF8.GetString(fileBytes, dataStartIndex, dataLength);

        MechSaveData loadedData = JsonUtility.FromJson<MechSaveData>(json);
        ReconstructMech(loadedData);
    }

    private void ReconstructMech(MechSaveData data)
    {
        var loadout = GarageLoader.ActiveLoadout;
        loadout.Clear();

        void TryEquip<T>(PartType type, int index, List<T> inventory) where T : Part
        {
            if (index >= 0 && index < inventory.Count)
                loadout[type] = inventory[index];
        }

        TryEquip(PartType.Head, data.headIdx, garageLoader.availableHeads);
        TryEquip(PartType.Torso, data.torsoIdx, garageLoader.availableTorsos);
        TryEquip(PartType.Arms, data.armsIdx, garageLoader.availableArms);
        TryEquip(PartType.Legs, data.legsIdx, garageLoader.availableLegs);
        TryEquip(PartType.Booster, data.boosterIdx, garageLoader.availableBoosters);
        TryEquip(PartType.Generator, data.genIdx, garageLoader.availableGenerators);
        TryEquip(PartType.FCS, data.fcsIdx, garageLoader.availableFCS);

        TryEquip(PartType.ArmL, data.armLIdx, garageLoader.availableLeftArmWeapons);
        TryEquip(PartType.ArmR, data.armRIdx, garageLoader.availableRightArmWeapons);
        TryEquip(PartType.BackL, data.backLIdx, garageLoader.availableLeftBackWeapons);
        TryEquip(PartType.BackR, data.backRIdx, garageLoader.availableRightBackWeapons);

        if (data.paintJobs != null && data.paintJobs.Count > 0)
        {
            garageLoader.globalPaintJob = new PlayerPaint[data.paintJobs.Count];
            for (int i = 0; i < data.paintJobs.Count; i++)
            {
                Texture2D aTex = null;
                if (data.paintJobs[i].albedoTexIdx >= 0 && data.paintJobs[i].albedoTexIdx < garageLoader.availableAlbedoTextures.Count)
                    aTex = garageLoader.availableAlbedoTextures[data.paintJobs[i].albedoTexIdx];

                Texture2D eTex = null;
                if (data.paintJobs[i].emissionTexIdx >= 0 && data.paintJobs[i].emissionTexIdx < garageLoader.availableEmissionTextures.Count)
                    eTex = garageLoader.availableEmissionTextures[data.paintJobs[i].emissionTexIdx];

                garageLoader.globalPaintJob[i] = new PlayerPaint
                {
                    map = data.paintJobs[i].map,
                    albedoColor = data.paintJobs[i].albedoColor,
                    emissionColor = data.paintJobs[i].emissionColor,
                    albedoTexture = aTex,
                    emissionTexture = eTex
                };
            }
        }

        garageLoader.RefreshVisualMech();
        Debug.Log("<color=cyan>Mech successfully loaded from card!</color>");
    }
}