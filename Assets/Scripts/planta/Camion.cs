using UnityEngine;

public class CamionMovimiento : MonoBehaviour
{
    [Header("Movimiento")]
    public float distanciaMovimiento = 5f;
    public float duracion = 2f;

    [Header("Tags")]
    public string playerTag = "Player";
    public string aguaTag = "agua";

    private Rigidbody rb;
    private Transform player;

    private bool moviendose = false;

    private void Start()
    {
  
        GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);

        if (playerObj != null)
        {
            player = playerObj.transform;
        }

  
        rb = GetComponent<Rigidbody>();

        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        rb.isKinematic = true;
        rb.useGravity = false;
    }

    private void OnTriggerStay(Collider other)
    {
    
        if (other.CompareTag(aguaTag))
        {
        
            if (!moviendose)
            {
                StartCoroutine(MoverCamion());
            }
        }
    }

    private System.Collections.IEnumerator MoverCamion()
    {
        moviendose = true;

        Vector3 startPos = transform.position;

        float direccion = 1f;

        if (player != null)
        {
   
            if (player.position.z < transform.position.z)
            {
                direccion = 1f;
            }
            else
            {
                direccion = -1f;
            }
        }

        Vector3 endPos = startPos + Vector3.forward * distanciaMovimiento * direccion;

        float time = 0f;

        while (time < duracion)
        {
            time += Time.deltaTime;

            float t = Mathf.SmoothStep(0, 1, time / duracion);

            transform.position = Vector3.Lerp(startPos, endPos, t);

            yield return null;
        }

        transform.position = endPos;

        moviendose = false;
    }
}