using UnityEngine;
using TMPro;

public class LoadEnemyData : MonoBehaviour
{
    public EnemyDataSO enemyData;

    public TextMeshProUGUI enemyNameText, enemyHealthText, enemyDamageText, enemyDescriptionText;
    public TextMeshProUGUI buttonLabel;

    public GameObject spawnPointObject;
    public Transform spawnPoint;
    
    void Start()
    {
        buttonLabel.text = enemyData.enemyName;
    }

    void Update()
    {
        
    }

    public void LoadData()
    {
        enemyNameText.text = enemyData.enemyName;
        enemyHealthText.text = enemyData.health.ToString();
        enemyDamageText.text = enemyData.damage.ToString();
        enemyDescriptionText.text = enemyData.description;

        if (spawnPointObject.transform.childCount > 0)
        {
            foreach (Transform child in spawnPointObject.transform)
            {
                Destroy(child.gameObject);
            }
        }
        Instantiate(enemyData.enemyPrefab, spawnPoint.position, Quaternion.identity, spawnPointObject.transform);
    }
}
