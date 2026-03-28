using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ClearText : MonoBehaviour
{
    public TextMeshProUGUI enemyNameText, enemyHealthText, enemyDamageText, enemyDescriptionText;
    public string defaultDescription = "Select your Opponent";

    public void ClearEnemyInfo()
    {
        if (enemyNameText != null) enemyNameText.text = "";
        if (enemyHealthText != null) enemyHealthText.text = "";
        if (enemyDamageText != null) enemyDamageText.text = "";
        if (enemyDescriptionText != null) enemyDescriptionText.text = defaultDescription;
    }
}
