using UnityEngine;

public class ChangeScreen : MonoBehaviour
{
    public GameObject currentScreen;
    public GameObject nextScreen;

    public void ChangeToNext()
    {
        currentScreen.SetActive(false);
        nextScreen.SetActive(true);
    }
}