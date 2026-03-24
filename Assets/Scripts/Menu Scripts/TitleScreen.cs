using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleScreen : MonoBehaviour
{
    public void Update()
    {
        if(Input.anyKeyDown)
        {
            SceneManager.LoadScene("MenuScene");
        }
    }
}
