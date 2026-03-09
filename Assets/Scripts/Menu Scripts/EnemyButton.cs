using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EnemyButton : MonoBehaviour
{
    public TextMeshProUGUI buttonLabel, enemyNameText, enemyHealthText, enemyDamageText, enemyDescriptionText;
    public EnemyDataSO enemyData;
    public GameObject rotateObject;
    public RotateObject rotateScript;

    public void Awake()
    {
        buttonLabel.text = GetComponentInChildren<TextMeshProUGUI>().text;
        buttonLabel.text = enemyData.enemyName;
    }

    public void SelectEnemy()
    {
        buttonLabel.text = enemyData.enemyName;
        enemyNameText.text = enemyData.enemyName;
        enemyHealthText.text = "Health: " + enemyData.health;
        enemyDamageText.text = "Damage: " + enemyData.damage;
        enemyDescriptionText.text = enemyData.description;
        rotateObject = enemyData.enemyPrefab;
        rotateScript.objectToRotate = rotateObject;
    }
}
