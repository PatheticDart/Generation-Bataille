using UnityEngine;

[CreateAssetMenu()]
public class EnemyDataSO : ScriptableObject
{
    public string enemyName;
    public int health;
    public int damage;
    public string description;
    public GameObject enemyPrefab;
}
