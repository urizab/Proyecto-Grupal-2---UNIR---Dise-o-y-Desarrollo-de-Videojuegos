using UnityEngine;
using UnityEngine.InputSystem;

public class ActivarColliderClick : MonoBehaviour
{
    private BoxCollider boxCollider;

    void Start()
    {
        boxCollider = GetComponent<BoxCollider>();

        if (boxCollider != null)
        {
            boxCollider.enabled = false;
        }
    }

    void Update()
    {

        if (Mouse.current != null)
        {

            if (Mouse.current.leftButton.isPressed)
            {
                boxCollider.enabled = true;
            }
            else
            {
                boxCollider.enabled = false;
            }
        }
    }
}