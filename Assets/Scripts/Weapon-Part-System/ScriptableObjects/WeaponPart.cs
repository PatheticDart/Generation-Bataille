using System;
using UnityEngine;

public abstract class WeaponPart : ChargedPart
{
    [Header("Mounting")]
    public WeaponLocation allowedLocations;
}