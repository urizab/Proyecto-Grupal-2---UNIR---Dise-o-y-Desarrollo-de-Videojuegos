using UnityEngine;

/// <summary>
/// Clase base abstracta o virtual para todos los tipos de fuego en el juego.
/// Agrupa la lógica de intensidad, extinción reduciendo escala, regeneración y daño.
/// El escalado del GameObject escala automáticamente sus partículas en Unity.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class Fuego : MonoBehaviour
{
    [Header("── CONFIGURACIÓN DE FUEGO BASE")]
    [Tooltip("Intensidad o salud inicial de este fuego.")]
    public float intensidadMaxima = 100f;

    [Tooltip("Intensidad actual de este fuego.")]
    public float intensidadActual;

    [Tooltip("Umbral de intensidad por debajo del cual el fuego se considera vencido y se apaga automáticamente.")]
    public float umbralExtincion = 30f;

    [Tooltip("Cantidad de agua que pierde el jugador al colisionar con este fuego.")]
    public float penalizacionAgua = 20f;

    [Header("── CONFIGURACIÓN DE REGENERACIÓN")]
    [Tooltip("¿Este fuego es capaz de regenerar su intensidad con el tiempo si no se apaga del todo?")]
    public bool puedeRegenerarse = true;

    [Tooltip("Tiempo de espera en segundos sin recibir agua antes de que comience la regeneración.")]
    public float retrasoRegeneracion = 4f;

    [Tooltip("Intervalo de tiempo en segundos que tarda el fuego en regenerarse por completo desde su estado actual.")]
    public float duracionRegeneracion = 3f;

    [Header("── CONFIGURACIÓN DE AUDIO")]
    [Tooltip("Sonido de siseo/extinción que se reproduce mientras el fuego colisiona con el agua.")]
    public AudioClip apagarFuego;

    protected Vector3 escalaOriginal;
    protected bool estaExtinguido = false;

    // Control de tiempo de regeneración
    protected float ultimoTiempoAgua = -99f;

    // Control de sonido de agua
    protected AudioSource _audioSourceAgua;
    private float _ultimoTiempoContactoAgua = -99f;

    // Estructura para almacenar y escalar proporcionalmente la emisión de partículas
    protected struct ParticleSystemCache
    {
        public ParticleSystem ps;
        public Vector3 originalShapeScale;
        public float originalShapeRadius;
    }
    protected ParticleSystemCache[] _particulasCache;

    // Estructura para almacenar y escalar proporcionalmente la iluminación
    protected struct LightCache
    {
        public Light light;
        public float originalIntensity;
        public float originalRange;
    }
    protected LightCache[] _lucesCache;

    protected virtual void Start()
    {
        intensidadActual = intensidadMaxima;
        escalaOriginal = transform.localScale;

        // Configurar AudioSource para el sonido de siseo de extinción
        if (apagarFuego != null)
        {
            _audioSourceAgua = gameObject.AddComponent<AudioSource>();
            _audioSourceAgua.clip = apagarFuego;
            _audioSourceAgua.loop = true;
            _audioSourceAgua.playOnAwake = false;
            _audioSourceAgua.spatialBlend = 1f; // 3D espacial
            _audioSourceAgua.minDistance = 2f;
            _audioSourceAgua.maxDistance = 15f;
            _audioSourceAgua.volume = 0.6f; // Volumen inicial del siseo
        }

        // Cachear los sistemas de partículas y sus dimensiones de emisión originales
        ParticleSystem[] particulas = GetComponentsInChildren<ParticleSystem>();
        _particulasCache = new ParticleSystemCache[particulas.Length];
        for (int i = 0; i < particulas.Length; i++)
        {
            var ps = particulas[i];
            _particulasCache[i] = new ParticleSystemCache
            {
                ps = ps,
                originalShapeScale = ps.shape.scale,
                originalShapeRadius = ps.shape.radius
            };
        }

        // Cachear las luces de iluminación y sus propiedades originales
        Light[] luces = GetComponentsInChildren<Light>();
        _lucesCache = new LightCache[luces.Length];
        for (int i = 0; i < luces.Length; i++)
        {
            var l = luces[i];
            _lucesCache[i] = new LightCache
            {
                light = l,
                originalIntensity = l.intensity,
                originalRange = l.range
            };
        }
    }

    protected virtual void Update()
    {
        // Detener el sonido del agua si ya no colisiona (más de 0.25 segundos sin recibir agua) o si está extinguido
        if (_audioSourceAgua != null && _audioSourceAgua.isPlaying)
        {
            if (Time.time - _ultimoTiempoContactoAgua > 0.25f || estaExtinguido)
            {
                _audioSourceAgua.Stop();
            }
        }

        if (estaExtinguido) return;

        // Lógica de Regeneración Progresiva
        if (puedeRegenerarse && intensidadActual < intensidadMaxima)
        {
            // Esperar el retraso especificado (4 segundos) desde el último contacto con agua
            if (Time.time - ultimoTiempoAgua >= retrasoRegeneracion)
            {
                // Calcular velocidad para recuperar la intensidad máxima en 'duracionRegeneracion' (3 segundos)
                float velocidadRegeneracion = intensidadMaxima / duracionRegeneracion;
                intensidadActual += velocidadRegeneracion * Time.deltaTime;
                intensidadActual = Mathf.Min(intensidadActual, intensidadMaxima);

                ActualizarFuego();
            }
        }
    }

    /// <summary>
    /// Recibe agua restando intensidad. Muestra por consola el nuevo valor de intensidad.
    /// Apaga el fuego si cae por debajo del umbral de extinción.
    /// </summary>
    public virtual void RecibirAgua(float cantidad)
    {
        if (estaExtinguido) return;

        // Registrar marca de tiempo del último contacto con agua
        ultimoTiempoAgua = Time.time;
        _ultimoTiempoContactoAgua = Time.time;

        // Reproducir el sonido de siseo de agua si no está sonando ya
        if (_audioSourceAgua != null && !_audioSourceAgua.isPlaying)
        {
            _audioSourceAgua.Play();
        }

        intensidadActual -= cantidad;

        // Loggear en la consola el nuevo valor de intensidad
        Debug.Log($"[CONSOLA FUEGO: {gameObject.name}] Impactó agua. Nueva intensidad: {intensidadActual:F2} / {intensidadMaxima:F2}");

        // Si la intensidad cae por debajo o igual al umbral, el fuego se extingue del todo
        if (intensidadActual <= umbralExtincion)
        {
            intensidadActual = 0f;
            ApagarFuego();
        }
        else
        {
            ActualizarFuego();
        }
    }

    /// <summary>
    /// Contrae proporcionalmente la escala física del fuego.
    /// Unity se encarga de encoger las partículas del renderizador al cambiar localScale.
    /// </summary>
    protected virtual void ActualizarFuego()
    {
        float ratio = intensidadActual / intensidadMaxima;

        // Evitamos que la escala física baje del 10% para prevenir que los límites de colisión e iluminación colapsen a 0
        float ratioEscala = Mathf.Max(ratio, 0.1f);
        transform.localScale = escalaOriginal * ratioEscala;

        // Reducir proporcionalmente el área de emisión (Shape) de cada ParticleSystem
        if (_particulasCache != null)
        {
            foreach (var cache in _particulasCache)
            {
                if (cache.ps != null)
                {
                    var shape = cache.ps.shape;
                    shape.scale = cache.originalShapeScale * ratioEscala;
                    shape.radius = cache.originalShapeRadius * ratioEscala;
                }
            }
        }

        // Reducir proporcionalmente el radio (rango) y la intensidad de cada luz (Light)
        if (_lucesCache != null)
        {
            foreach (var cache in _lucesCache)
            {
                if (cache.light != null)
                {
                    cache.light.intensity = cache.originalIntensity * ratioEscala;
                    cache.light.range = cache.originalRange * ratioEscala;
                }
            }
        }
    }

    /// <summary>
    /// Extingue el fuego, detiene la emisión de partículas y destruye el GameObject.
    /// </summary>
    public virtual void ApagarFuego()
    {
        if (estaExtinguido) return;
        estaExtinguido = true;
        transform.localScale = Vector3.zero;

        // Detener sonido de agua de inmediato
        if (_audioSourceAgua != null)
        {
            _audioSourceAgua.Stop();
        }

        // Apagar todas las luces de iluminación de inmediato
        if (_lucesCache != null)
        {
            foreach (var cache in _lucesCache)
            {
                if (cache.light != null)
                {
                    cache.light.enabled = false;
                }
            }
        }

        // Detener partículas suavemente al apagarse por completo
        ParticleSystem[] particulas = GetComponentsInChildren<ParticleSystem>();
        foreach (var ps in particulas)
        {
            if (ps != null)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        // Auto-destrucción tras un breve retraso para permitir desvanecimiento del humo residual
        Destroy(gameObject, 2f);
    }

    /// <summary>
    /// Daño continuo cuando el jugador se queda parado dentro del fuego.
    /// </summary>
    protected virtual void OnTriggerStay(Collider other)
    {
        if (estaExtinguido) return;

        WaterTank tanque = other.GetComponent<WaterTank>() ?? other.GetComponentInParent<WaterTank>();
        if (tanque != null)
        {
            tanque.Drain(penalizacionAgua * Time.deltaTime);
        }
    }
}
