using UnityEngine;

public class StandardMechPart : PartTemplate
{
    public override void SpawnPart()
    {
        base.SpawnPart();

        OnPartSpawned?.Invoke();
    }
}