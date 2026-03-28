using UnityEngine;
using TMPro;

public class APCounter : MonoBehaviour
{
    [Header("UI Reference")]
    [Tooltip("Drag your TextMeshPro text object here.")]
    public TextMeshProUGUI apText;

    [Header("Targeting")]
    [Tooltip("Type the exact name of the layer (e.g., 'Team 1' or 'Team 2')")]
    public string targetLayerName = "Team 1";

    [Header("Formatting")]
    [Tooltip("Optional text to put in front of the number, like 'AP: '")]
    public string prefix = "AP: ";

    private MechStats trackedMech;
    private int targetLayerIndex;

    void Start()
    {
        // Convert the string name into Unity's internal layer integer
        targetLayerIndex = LayerMask.NameToLayer(targetLayerName);

        // Auto-grab the text component if you forgot to assign it
        if (apText == null) apText = GetComponent<TextMeshProUGUI>();
    }

    void Update()
    {
        // 1. If we haven't locked onto a mech yet, keep looking!
        if (trackedMech == null)
        {
            FindTargetMech();

            if (trackedMech == null)
            {
                if (apText != null) apText.text = prefix + "---";
                return;
            }
        }

        // 2. Update the text with the current AP in real time!
        if (apText != null)
        {
            // You can also add trackedMech.totalArmorPoints here if you want a "Current / Max" format!
            apText.text = prefix + trackedMech.currentArmorPoints.ToString();
        }
    }

    private void FindTargetMech()
    {
        // Find all mechs currently active in the arena
        MechStats[] allMechs = FindObjectsByType<MechStats>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        foreach (MechStats mech in allMechs)
        {
            // Lock on if this mech's layer matches our target layer
            if (mech.gameObject.layer == targetLayerIndex)
            {
                trackedMech = mech;
                break;
            }
        }
    }
}