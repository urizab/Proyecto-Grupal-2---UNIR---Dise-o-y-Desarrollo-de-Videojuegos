using UnityEngine;

public class GnomeJump : MonoBehaviour
{
    public float jumpForce = 1.5f;
    public float jumpCooldown = 0.8f;

    private Rigidbody rb;
    private float lastJumpTime = -999f;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void OnParticleCollision(GameObject other)
    {
        if (Time.time < lastJumpTime + jumpCooldown) return;

        lastJumpTime = Time.time;

        rb.linearVelocity = Vector3.zero;

       // OPCION 2
        Vector3 jumpDirection =
            Vector3.up +
            new Vector3(
                Random.Range(-0.5f, 0.5f),
                0,
                Random.Range(-0.5f, 0.5f)
            );

        rb.AddForce(jumpDirection * jumpForce, ForceMode.Impulse);
    }
}