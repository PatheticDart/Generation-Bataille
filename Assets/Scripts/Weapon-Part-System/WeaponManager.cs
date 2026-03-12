using UnityEngine;

public class WeaponManager : MonoBehaviour
{
    public FunctionalWeapon[] lWeapons = new FunctionalWeapon[2];
    public FunctionalWeapon[] rWeapons = new FunctionalWeapon[2];

    #region State

    int _activeL = 0;
    int _activeR = 0;

    bool lPressed, lHeld, lReleased;
    bool rPressed, rHeld, rReleased;

    #endregion

    #region Events


    #endregion


    public void ToggleLeftWeapon()
    {
        _activeL = (_activeL + 1) % lWeapons.Length;
    }

    public void ToggleRightWeapon()
    {
        _activeR = (_activeR + 1) % lWeapons.Length;
    }

    void Update()
    {
        GetInputs();
    }

    void GetInputs()
    {
        lPressed = Input.GetKeyDown(KeyCode.Mouse0);
        rPressed = Input.GetKeyDown(KeyCode.Mouse1);

        lHeld = Input.GetKey(KeyCode.Mouse0);
        rHeld = Input.GetKey(KeyCode.Mouse1);

        lReleased = Input.GetKeyUp(KeyCode.Mouse0);
        rReleased = Input.GetKeyUp(KeyCode.Mouse1);
    }
}