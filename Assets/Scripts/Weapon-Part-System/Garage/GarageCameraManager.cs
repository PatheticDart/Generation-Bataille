using UnityEngine;
using Unity.Cinemachine;
using NaughtyAttributes;

public class GarageCameraManager : MonoBehaviour
{
    public enum MechView
    {
        FullBody,
        Head,
        Torso,
        Arms,
        Legs,
        LeftArmWeapon,
        RightArmWeapon,
        LeftBackWeapon,
        RightBackWeapon
    }

    // CHANGED TO CLASS: This allows us to modify and save the cached values easily
    [System.Serializable]
    public class CameraSetup
    {
        public MechView viewName;
        public CinemachineCamera camera;

        // Hidden cache variables to store the snapshot
        [HideInInspector] public float defaultRotation;
        [HideInInspector] public Vector3 defaultOffset;
        [HideInInspector] public float defaultRadius;
    }

    [Header("Camera Roster")]
    [Tooltip("Assign your different Cinemachine 3 cameras here.")]
    public CameraSetup[] cameraList;

    [Header("Controller Handoff")]
    [Tooltip("Drag the object holding your GarageViewer script here.")]
    public GarageViewer viewerController;

    void Awake()
    {
        // 1. TAKE A SNAPSHOT: Loop through all cameras on startup and save their exact Inspector settings
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
        // Default to the full body view on startup
        SwitchCamera(MechView.FullBody);
    }

    public void SwitchCamera(MechView targetView)
    {
        foreach (CameraSetup setup in cameraList)
        {
            if (setup.camera == null) continue;

            if (setup.viewName == targetView)
            {
                // 2. RESET THE CAMERA: Apply the snapshot values back to the orbiter
                CinemachineOrbitalFollow orbiter = setup.camera.GetComponent<CinemachineOrbitalFollow>();
                if (orbiter != null)
                {
                    orbiter.HorizontalAxis.Value = setup.defaultRotation;
                    orbiter.TargetOffset = setup.defaultOffset;
                    orbiter.Radius = setup.defaultRadius;
                }

                // 3. Boost priority so the Cinemachine Brain smoothly flies to this reset position
                setup.camera.Priority = 10;
                
                if (viewerController != null)
                {
                    viewerController.SetActiveCamera(setup.camera);
                }
            }
            else
            {
                setup.camera.Priority = 0;
            }
        }
    }

    // --- NAUGHTY ATTRIBUTE BUTTONS ---

    [Button("View Full Body")]
    public void ViewFullBody() => SwitchCamera(MechView.FullBody);

    [Button("View Head")]
    public void ViewHead() => SwitchCamera(MechView.Head);

    [Button("View Torso")]
    public void ViewTorso() => SwitchCamera(MechView.Torso);

    [Button("View Arms")]
    public void ViewArms() => SwitchCamera(MechView.Arms);

    [Button("View Legs")]
    public void ViewLegs() => SwitchCamera(MechView.Legs);

    [Button("View Left Arm Weapon")]
    public void ViewLeftArmWeapon() => SwitchCamera(MechView.LeftArmWeapon);

    [Button("View Right Arm Weapon")]
    public void ViewRightArmWeapon() => SwitchCamera(MechView.RightArmWeapon);

    [Button("View Left Back Weapon")]
    public void ViewLeftBackWeapon() => SwitchCamera(MechView.LeftBackWeapon);

    [Button("View Right Back Weapon")]
    public void ViewRightBackWeapon() => SwitchCamera(MechView.RightBackWeapon);
}