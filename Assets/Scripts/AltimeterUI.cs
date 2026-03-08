using UnityEngine;
using UnityEngine.UI; // Required for RawImage
using System.Collections.Generic; // Required for Lists

public class AltimeterUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The Transform of your player/mech to track world height.")]
    public Transform mechTransform;

    [Tooltip("The Raw Images displaying your repeating bars.")]
    public List<RawImage> altimeterBars = new List<RawImage>();

    [Header("Settings")]
    [Tooltip("How fast the bars scroll relative to your jump height. Adjust until it looks natural.")]
    public float scrollMultiplier = 0.1f;

    void Update()
    {
        if (mechTransform == null || altimeterBars.Count == 0) return;

        // 1. Get the mech's actual world height. We only need to calculate this once per frame.
        float altitude = mechTransform.position.y;

        // 2. Loop through every Raw Image assigned in the inspector
        foreach (RawImage bar in altimeterBars)
        {
            // Skip any empty slots someone might have accidentally left in the list
            if (bar == null) continue;

            // 3. Grab the current UV mapping of this specific Raw Image
            Rect currentUV = bar.uvRect;

            // 4. Offset the Y value based on the mech's altitude
            currentUV.y = altitude * scrollMultiplier;

            // 5. Apply the new offset
            bar.uvRect = currentUV;
        }
    }
}