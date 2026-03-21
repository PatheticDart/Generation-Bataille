using UnityEngine;

[CreateAssetMenu()]
public class EnemyDataSO : ScriptableObject
{
    public enum EnemyRank { D, C, B, A, S, SS, SSS }

    public string enemyName;
    public int health;
    public int damage;
    public string description;
    public GameObject enemyPrefab;
    public EnemyRank rank;
}
