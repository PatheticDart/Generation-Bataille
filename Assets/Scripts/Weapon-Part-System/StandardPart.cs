using UnityEngine;

public class StandardMechPart : PartTemplate
{
    public override void SpawnPart()
    {
        base.SpawnPart();

        string partName = gameObject.name;
        if (partName.Contains("Head") ||
            partName.Contains("Left Arm") ||
            partName.Contains("Right Arm") ||
            partName.Contains("Torso") ||
            partName.Contains("Legs"))
        {
            GenerateHitboxes();
        }

        OnPartSpawned?.Invoke();
    }

    private void GenerateHitboxes()
    {
        MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>();

        // --- NEW: Find the master layer of the mech ---
        int parentLayer = gameObject.layer;
        MechStats stats = GetComponentInParent<MechStats>();
        if (stats != null) parentLayer = stats.gameObject.layer;

        foreach (MeshFilter mf in meshFilters)
        {
            if (mf.gameObject.GetComponent<MeshCollider>() == null)
            {
                MeshCollider mc = mf.gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;
                mc.convex = false;
            }

            // --- NEW: Assign the child mesh to the exact same team layer ---
            mf.gameObject.layer = parentLayer;
        }
    }
}