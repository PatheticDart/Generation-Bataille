using UnityEngine;

public class SelectOpponent : MonoBehaviour
{
    
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SelectOpponent1()
    {
        PlayerPrefs.SetInt("SelectedOpponent", 1);
        Debug.Log(PlayerPrefs.GetInt("SelectedOpponent"));
    }


}
