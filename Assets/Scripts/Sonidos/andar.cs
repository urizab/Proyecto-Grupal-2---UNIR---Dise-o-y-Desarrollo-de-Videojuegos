using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PasoSonido : MonoBehaviour
{
    [Header("Configuración")]
    public string playerTag = "Player";
    public AudioClip sueloSonido;
    public float velocidadMinima = 0.1f;

    private AudioSource audioSource;
    private Rigidbody rb;
    private CharacterController cc;

    void Start()
    {
        // Buscar el objeto con el tag Player
        GameObject player = GameObject.FindGameObjectWithTag(playerTag);

        if (player == null)
        {
            Debug.LogError("No se encontró un objeto con el tag: " + playerTag);
            enabled = false;
            return;
        }

        // Intentar obtener Rigidbody o CharacterController
        rb = player.GetComponent<Rigidbody>();
        cc = player.GetComponent<CharacterController>();

        audioSource = GetComponent<AudioSource>();
        audioSource.clip = sueloSonido;
        audioSource.loop = true;
    }

    void Update()
    {
        float velocidad = 0f;

        // Detectar movimiento dependiendo del componente
        if (rb != null)
        {
            velocidad = rb.linearVelocity.magnitude;
        }
        else if (cc != null)
        {
            velocidad = cc.velocity.magnitude;
        }

        // Reproducir sonido si se mueve
        if (velocidad > velocidadMinima)
        {
            if (!audioSource.isPlaying)
            {
                audioSource.Play();
            }
        }
        else
        {
            if (audioSource.isPlaying)
            {
                audioSource.Stop();
            }
        }
    }
}