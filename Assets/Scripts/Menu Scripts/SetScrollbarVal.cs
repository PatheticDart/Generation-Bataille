using UnityEngine;
using UnityEngine.UI;

public class SetScrollbarVal : MonoBehaviour
{
    //yeah idk why the container keeps centering, this script is just here to force it
    public Scrollbar scrollbar;

    void Start()
    {
        scrollbar.value = 1f;
    }
}
