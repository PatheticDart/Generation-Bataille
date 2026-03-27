using UnityEngine;
using Unity.Cinemachine;
using NaughtyAttributes;

public class GarageCameraManager : MonoBehaviour
{
    public enum MechView
    {
        FullBody, Head, Torso, Arms, Legs,
        LeftArmWeapon, RightArmWeapon, LeftBackWeapon, RightBackWeapon,
        None
    }

    [System.Serializable]
    public class CameraSetup
    {
        public MechView viewName;
        public CinemachineCamera camera;

        [HideInInspector] public float defaultRotation;
        [HideInInspector] public Vector3 defaultOffset;
        [HideInInspector] public float defaultRadius;
    }

    [Header("References")]
    public CameraSetup[] cameraList;
    public GarageViewer viewerController;
    public PartSystem partSystem;

    void Awake()
    {
        foreach (CameraSetup setup in cameraList)
        {
            if (setup.camera != null)
            {
                CinemachineOrbitalFollow orbiter = setup.camera.GetComponent<CinemachineOrbitalFollow>();
                if (orbiter != null)
                {
                    setup.defaultRotation = orbiter.HorizontalAxis.Value;
                    setup.defaultOffset = orbiter.TargetOffset;
                    setup.defaultRadius = orbiter.Radius;
                }
            }
        }
    }

    void Start()
    {
        SwitchCamera(MechView.FullBody);
    }

    public void SwitchCameraForPartCategory(PartType category)
    {
        MechView targetView = MechView.FullBody;
        string nodeNameToTrack = "";

        switch (category)
        {
            case PartType.Head: targetView = MechView.Head; nodeNameToTrack = "HeadNode"; break;
            case PartType.Torso: targetView = MechView.Torso; nodeNameToTrack = "TorsoNode"; break;
            case PartType.Arms: targetView = MechView.Arms; nodeNameToTrack = "TorsoNode"; break;
            case PartType.Legs: targetView = MechView.Legs; nodeNameToTrack = "Pelvis"; break;
            case PartType.ArmL: targetView = MechView.LeftArmWeapon; nodeNameToTrack = "LeftArmWeaponNode"; break;
            case PartType.ArmR: targetView = MechView.RightArmWeapon; nodeNameToTrack = "RightArmWeaponNode"; break;
            case PartType.BackL: targetView = MechView.LeftBackWeapon; nodeNameToTrack = "LeftBackWeaponNode"; break;
            case PartType.BackR: targetView = MechView.RightBackWeapon; nodeNameToTrack = "RightBackWeaponNode"; break;
            default: targetView = MechView.FullBody; break;
        }

        SwitchCamera(targetView, nodeNameToTrack);
    }

    public void SwitchCamera(MechView targetView, string nodeToTrack = "")
    {
        foreach (CameraSetup setup in cameraList)
        {
            if (setup.camera == null) continue;

            if (setup.viewName == targetView)
            {
                CinemachineOrbitalFollow orbiter = setup.camera.GetComponent<CinemachineOrbitalFollow>();
                if (orbiter != null)
                {
                    // --- THE FIX: Check if we are ALREADY using this camera ---
                    bool isAlreadyActive = (setup.camera.Priority == 10);

                    // Only reset the player's custom rotation/zoom if we are switching to a completely new view
                    if (!isAlreadyActive)
                    {
                        orbiter.HorizontalAxis.Value = setup.defaultRotation;
                        orbiter.Radius = setup.defaultRadius;
                    }

                    // ALWAYS update the height offset to match the newly equipped part!
                    Vector3 finalOffset = setup.defaultOffset;
                    if (!string.IsNullOrEmpty(nodeToTrack) && partSystem != null)
                    {
                        Transform foundNode = PartSystem.FindDeepChild(partSystem.transform, nodeToTrack);
                        if (foundNode != null)
                        {
                            float heightDiff = foundNode.position.y - partSystem.transform.position.y;
                            finalOffset.y = heightDiff;
                        }
                    }
                    orbiter.TargetOffset = finalOffset;
                }

                setup.camera.Priority = 10;
                if (viewerController != null) viewerController.SetActiveCamera(setup.camera);
            }
            else
            {
                setup.camera.Priority = 0;
            }
        }
    }

    [Button("View Full Body")] public void ViewFullBody() => SwitchCamera(MechView.FullBody);
    [Button("View Head")] public void ViewHead() => SwitchCamera(MechView.Head, "HeadNode");
    [Button("View Torso")] public void ViewTorso() => SwitchCamera(MechView.Torso, "TorsoNode");
    [Button("View Arms")] public void ViewArms() => SwitchCamera(MechView.Arms, "TorsoNode");
    [Button("View Legs")] public void ViewLegs() => SwitchCamera(MechView.Legs, "Pelvis");
    [Button("View Left Arm Weapon")] public void ViewLeftArmWeapon() => SwitchCamera(MechView.LeftArmWeapon, "LeftArmWeaponNode");
    [Button("View Right Arm Weapon")] public void ViewRightArmWeapon() => SwitchCamera(MechView.RightArmWeapon, "RightArmWeaponNode");
    [Button("View Left Back Weapon")] public void ViewLeftBackWeapon() => SwitchCamera(MechView.LeftBackWeapon, "LeftBackWeaponNode");
    [Button("View Right Back Weapon")] public void ViewRightBackWeapon() => SwitchCamera(MechView.RightBackWeapon, "RightBackWeaponNode");
}