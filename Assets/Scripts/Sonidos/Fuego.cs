using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SonidoFuego : MonoBehaviour
{
    [Header("Configuración de Audio")]
    public AudioClip fuegoSonido;

    [Range(0f, 1f)]
    public float volumenMaximo = 1f;

    [Header("Distancia")]
    public float distanciaMinima = 2f;
    public float distanciaMaxima = 15f;

    private AudioSource audioSource;
    private Transform jugador;

    void Start()
    {
   
        if (!CompareTag("fuego"))
        {
            Debug.LogWarning("Este objeto no tiene el tag 'fuego'");
        }


        GameObject jugadorObj = GameObject.FindGameObjectWithTag("Player");

        if (jugadorObj != null)
        {
            jugador = jugadorObj.transform;
        }
        else
        {
            Debug.LogError("No se encontró ningún objeto con el tag 'Player'");
        }


        audioSource = GetComponent<AudioSource>();
        audioSource.clip = fuegoSonido;
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f; // 3D
        audioSource.minDistance = distanciaMinima;
        audioSource.maxDistance = distanciaMaxima;
        audioSource.volume = volumenMaximo;


        if (fuegoSonido != null)
        {
            audioSource.Play();
        }
    }

    void Update()
    {
        if (jugador == null) return;

  
        float distancia = Vector3.Distance(transform.position, jugador.position);


        float volumen = Mathf.Lerp(
            volumenMaximo,
            0f,
            distancia / distanciaMaxima
        );

        volumen = Mathf.Clamp(volumen, 0f, volumenMaximo);

        audioSource.volume = volumen;
    }
}