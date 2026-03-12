[System.Flags]
public enum WeaponLocation
{
    None = 0,

    BackL = 1 << 1, 
    BackR = 1 << 2, 
    ArmL = 1 << 3, 
    ArmR = 1 << 4, 

    All = ~0,
}