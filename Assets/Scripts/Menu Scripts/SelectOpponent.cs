using UnityEngine;

public class SelectOpponent : MonoBehaviour
{
    public GameObject opponentSelectPanel;
    public GameObject rotationPanel;

    public EnemyDataSO selectedEnemyData;

    public GameObject confirmationPanel;

    void Update()
    {
        if (opponentSelectPanel.activeSelf)
        {
            rotationPanel.SetActive(true);
        }
        else
        {
            rotationPanel.SetActive(false);
        }
    }


    public void CONFIRM()
    {
        Debug.Log("ENEMY CONFIRM");
    }

    public void ABORT()
    {
        confirmationPanel.SetActive(false);
    }
}
