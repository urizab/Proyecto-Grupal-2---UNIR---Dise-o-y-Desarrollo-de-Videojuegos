// PushableObject.cs
using UnityEngine;

public class PushableObject : MonoBehaviour
{
    public float mass = 1f;
    private Rigidbody rb;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        rb.mass = mass;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }
}