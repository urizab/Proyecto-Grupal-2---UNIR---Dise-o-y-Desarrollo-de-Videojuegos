using UnityEngine;

public class GnomoMessage : MonoBehaviour
{
    public GameObject messageUI;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            messageUI.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            messageUI.SetActive(false);
        }
    }
}