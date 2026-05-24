using UnityEngine;

public class PlantaCreceConAgua : MonoBehaviour
{
    [Header("Crecimiento")]
    public float alturaSubida = 2.5f;
    public float duracion = 1.8f;
    public bool soloUnaVez = true;

    private bool crecio = false;
    private Rigidbody rb;

    private void Start()
    {
    
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;        
            rb.useGravity = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("agua"))
        {
            if (soloUnaVez && crecio) return;

            StartCoroutine(Crecimiento());
            crecio = true;
        }
    }

    private System.Collections.IEnumerator Crecimiento()
    {
        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + Vector3.up * alturaSubida;

        float time = 0;

        while (time < duracion)
        {
            time += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, time / duracion);  

            transform.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }

        transform.position = endPos;
    }
}