using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MapSelect : MonoBehaviour
{
    public GarageGameTransition gameTransition;
    public Sprite colosseumSprite, buriedCitySprite, testSceneSprite;
    public Image mapPreviewRenderer;

    public void TESTSCENE()
    {
        if (gameTransition != null)
        {
            setIndex(3);
            if (mapPreviewRenderer != null) mapPreviewRenderer.sprite = testSceneSprite;
        }
    }

    public void COLOSSEUM()
    {
        if (gameTransition != null)
        {
            setIndex(1);
            if (mapPreviewRenderer != null) mapPreviewRenderer.sprite = colosseumSprite;
        }
    }

    public void BURRIEDCITY()
    {
        if (gameTransition != null)
        {
            setIndex(2);
            if (mapPreviewRenderer != null) mapPreviewRenderer.sprite = buriedCitySprite;
        }
    }

    public void setIndex(int index)
    {
        gameTransition.gameplaySceneIndex = index;
        Debug.Log(index);
    }
}
