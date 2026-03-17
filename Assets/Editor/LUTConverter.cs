using UnityEngine;
using UnityEditor;
using System.IO;

public class LUTConverter : EditorWindow
{
    [MenuItem("Assets/Convert to 3D LUT", false, 20)]
    public static void ConvertTo3DLUT()
    {
        Texture2D selectedTexture = Selection.activeObject as Texture2D;

        if (selectedTexture == null)
        {
            EditorUtility.DisplayDialog("Error", "Please select a 2D LUT texture first.", "OK");
            return;
        }

        // Calculate the dimension size. For a 256x16 texture, N = 16.
        int N = selectedTexture.height;
        if (selectedTexture.width != N * N)
        {
            EditorUtility.DisplayDialog("Error", 
                $"Texture dimensions must be (N*N) x N. Expected {N * N}x{N} for height {N}, but got {selectedTexture.width}x{selectedTexture.height}.", 
                "OK");
            return;
        }

        string assetPath = AssetDatabase.GetAssetPath(selectedTexture);
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        
        // Ensure the texture is readable and set to linear/no compression to prevent artifacting
        if (importer != null)
        {
            bool needsReimport = false;
            if (!importer.isReadable)
            {
                importer.isReadable = true;
                needsReimport = true;
            }
            if (importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                needsReimport = true;
            }
            if (importer.sRGBTexture)
            {
                importer.sRGBTexture = false;
                needsReimport = true;
            }

            if (needsReimport)
            {
                importer.SaveAndReimport();
            }
        }

        // Initialize the 3D Texture
        Texture3D tex3D = new Texture3D(N, N, N, TextureFormat.RGBA32, false);
        tex3D.wrapMode = TextureWrapMode.Clamp;
        tex3D.filterMode = FilterMode.Bilinear;

        Color[] pixels2D = selectedTexture.GetPixels();
        Color[] pixels3D = new Color[N * N * N];

        // Map the 2D strip pixels into 3D space
        for (int z = 0; z < N; z++)
        {
            for (int y = 0; y < N; y++)
            {
                for (int x = 0; x < N; x++)
                {
                    int x2D = (z * N) + x;
                    int y2D = y;
                    
                    int index2D = y2D * selectedTexture.width + x2D;
                    int index3D = x + (y * N) + (z * N * N);
                    
                    pixels3D[index3D] = pixels2D[index2D];
                }
            }
        }

        tex3D.SetPixels(pixels3D);
        tex3D.Apply();

        // Save it as a Unity Asset
        string directory = Path.GetDirectoryName(assetPath);
        string fileName = Path.GetFileNameWithoutExtension(assetPath);
        string newPath = Path.Combine(directory, fileName + "_3D.asset").Replace("\\", "/");

        AssetDatabase.CreateAsset(tex3D, newPath);
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog("Success", $"Successfully created 3D LUT at:\n{newPath}", "OK");
    }

    // Validate that the menu item only turns on when a Texture2D is selected
    [MenuItem("Assets/Convert to 3D LUT", true)]
    public static bool ValidateConvertTo3DLUT()
    {
        return Selection.activeObject is Texture2D;
    }
}