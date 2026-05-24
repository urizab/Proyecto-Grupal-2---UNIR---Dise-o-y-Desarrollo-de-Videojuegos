using UnityEngine;

public class EndGameTrigger : MonoBehaviour
{
    public GameObject finalScreen;
    public MonoBehaviour playerMovement;

    bool finished = false;

    private void OnTriggerEnter(Collider other)
    {
        if (finished) return;

        if (other.CompareTag("Player"))
        {
            finished = true;

            finalScreen.SetActive(true);

            if (playerMovement != null)
                playerMovement.enabled = false;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}