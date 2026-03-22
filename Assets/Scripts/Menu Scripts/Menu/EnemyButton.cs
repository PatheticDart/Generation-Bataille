using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EnemyButton : MonoBehaviour
{
    public TextMeshProUGUI buttonLabel, enemyNameText, enemyHealthText, enemyDamageText, enemyDescriptionText;
    public EnemyDataSO enemyData;
    public GameObject rotateObject;
    public GameObject buttonManager;
    public RotateObject rotateScript;
    public bool isSelected = false;

    [Header("Confirm Map Stuff")]
    public GameObject confirmationPanel;

    public void Awake()
    {
        buttonLabel.text = GetComponentInChildren<TextMeshProUGUI>().text;
        buttonLabel.text = enemyData.enemyName;
    }

    public void SelectEnemy()
    {
        buttonLabel.text = enemyData.enemyName;
        enemyNameText.text = enemyData.enemyName;
        enemyHealthText.text = enemyData.health.ToString();
        enemyDamageText.text = enemyData.damage.ToString();
        enemyDescriptionText.text = enemyData.description;
        rotateObject = enemyData.enemyPrefab;
        rotateScript.objectToRotate = rotateObject;
        
        if (buttonManager.GetComponent<SelectOpponent>().selectedEnemyData == enemyData)
        {
            confirmationPanel.SetActive(true);
        }
        else
        {
            buttonManager.GetComponent<SelectOpponent>().selectedEnemyData = enemyData;
        }
    }
}
