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
    private Fuego fuegoComponent;

    void Start()
    {
        if (!CompareTag("fuego"))
        {
            Debug.LogWarning("Este objeto no tiene el tag 'fuego'");
        }

        // Buscar al jugador automáticamente
        GameObject jugadorObj = GameObject.FindGameObjectWithTag("Player");
        if (jugadorObj != null)
        {
            jugador = jugadorObj.transform;
        }
        else
        {
            // Fallback: intentar buscar por tipo de controlador si no se encuentra el tag
            var controladorJugador = FindFirstObjectByType<FirstPersonController>();
            if (controladorJugador != null)
            {
                jugador = controladorJugador.transform;
            }
            else
            {
                Debug.LogWarning("[SonidoFuego] No se encontró ningún objeto 'Player' en la escena.");
            }
        }

        // Obtener el componente Fuego para sincronizar el sonido con su tamaño/salud
        fuegoComponent = GetComponent<Fuego>();

        audioSource = GetComponent<AudioSource>();
        audioSource.clip = fuegoSonido;
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f; // 3D Espacial
        audioSource.rolloffMode = AudioRolloffMode.Linear; // Rolloff lineal de Unity
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
        if (audioSource == null) return;

        // 1. Sincronizar el volumen con la intensidad actual del fuego
        float ratioFuego = 1f;
        if (fuegoComponent != null)
        {
            // Si el fuego ya está extinguido, silenciar y detener de inmediato
            if (fuegoComponent.intensidadActual <= 0f)
            {
                audioSource.volume = 0f;
                if (audioSource.isPlaying)
                {
                    audioSource.Stop();
                }
                return;
            }
            ratioFuego = Mathf.Clamp01(fuegoComponent.intensidadActual / fuegoComponent.intensidadMaxima);
        }

        // 2. Sincronizar el volumen con la distancia al jugador (para el decaimiento lineal exacto)
        float volumenPorDistancia = 1f;
        if (jugador != null)
        {
            float distancia = Vector3.Distance(transform.position, jugador.position);
            volumenPorDistancia = Mathf.Clamp01(1f - (distancia / distanciaMaxima));
        }

        // El volumen final es la combinación del volumen máximo, la distancia y el estado del fuego
        audioSource.volume = volumenMaximo * volumenPorDistancia * ratioFuego;
    }
}