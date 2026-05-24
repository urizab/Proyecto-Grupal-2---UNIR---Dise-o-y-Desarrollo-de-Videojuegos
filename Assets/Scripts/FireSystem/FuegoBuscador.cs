using UnityEngine;

/// <summary>
/// Fuego de tipo Buscador. Hereda de Fuego.
/// Persigue de forma activa al jugador respetando restricciones de ejes,
/// explotando y drenando agua al entrar en contacto o cercanía.
/// No se regenera.
/// </summary>
public class FuegoBuscador : Fuego
{
    public enum RestriccionMovimiento
    {
        TresDimensiones,  // Movimiento libre en las 3 dimensiones (Full 3D)
        SoloPlanoXZ,      // Solo movimiento horizontal (mantiene la altura Y del spawn)
        SoloEjeY          // Solo movimiento vertical (se mueve de arriba a abajo)
    }

    [Header("── CONFIGURACIÓN DEL BUSCADOR")]
    [Tooltip("Objetivo a perseguir (el jugador).")]
    public Transform objetivo;

    [Tooltip("Velocidad de movimiento hacia el jugador.")]
    public float velocidad = 3f;

    [Tooltip("Restricción del plano o eje de movimiento.")]
    public RestriccionMovimiento restriccionMovimiento = RestriccionMovimiento.SoloPlanoXZ;

    [Tooltip("Distancia mínima al jugador para explotar/impactar.")]
    public float distanciaParada = 0.8f;

    [Tooltip("Tiempo de vida máximo en segundos antes de disiparse solo.")]
    public float tiempoVida = 6f;

    private float _tiempoSpawn;
    private bool _yaImpacto = false;
    private bool _estaAtrapado = false;
    private float _tasaDecaimiento = 0f;

    protected override void Start()
    {
        _tiempoSpawn = Time.time;
        
        // Configurar intensidad máxima específica para el fuego buscador (50 puntos de salud)
        intensidadMaxima = 50f;

        // Deshabilitar explícitamente la regeneración de intensidad para flamas buscadoras
        puedeRegenerarse = false;

        // Ejecutar inicialización de la clase base Fuego (detección de escala original, etc.)
        base.Start();

        // Auto-destrucción por tiempo de vida por si no alcanza al jugador
        Destroy(gameObject, tiempoVida);

        // Buscar al jugador automáticamente si no se asignó
        if (objetivo == null)
        {
            var controladorJugador = FindFirstObjectByType<FirstPersonController>();
            if (controladorJugador != null)
            {
                objetivo = controladorJugador.transform;
            }
        }
    }

    protected override void Update()
    {
        // Ejecutar la lógica de la clase base (Update de regeneración, aunque esté en false)
        base.Update();

        if (estaExtinguido || _yaImpacto) return;

        if (_estaAtrapado)
        {
            // Reducir la intensidad gradualmente para que se encoja durante 10 segundos hasta extinguirse
            intensidadActual -= _tasaDecaimiento * Time.deltaTime;

            if (intensidadActual <= umbralExtincion)
            {
                intensidadActual = 0f;
                ApagarFuego();
            }
            else
            {
                ActualizarFuego();
            }
            return;
        }

        if (objetivo == null) return;

        // Calcular dirección hacia el jugador
        Vector3 posicionObjetivo = objetivo.position;
        Vector3 posicionActual = transform.position;

        // Aplicar las restricciones de movimiento
        switch (restriccionMovimiento)
        {
            case RestriccionMovimiento.SoloPlanoXZ:
                posicionObjetivo.y = posicionActual.y;
                break;

            case RestriccionMovimiento.SoloEjeY:
                posicionObjetivo.x = posicionActual.x;
                posicionObjetivo.z = posicionActual.z;
                break;

            case RestriccionMovimiento.TresDimensiones:
                break;
        }

        // Moverse hacia la posición calculada
        Vector3 direccion = (posicionObjetivo - posicionActual).normalized;
        float distanciaMovimiento = velocidad * Time.deltaTime;

        // Evitar atravesar paredes y obstáculos sólidos usando un SphereCast
        float radioDeteccion = 0.3f; // Radio aproximado del buscador
        radioDeteccion *= Mathf.Max(transform.localScale.x, 0.1f); // Ajustar según escala local

        if (Physics.SphereCast(posicionActual, radioDeteccion, direccion, out RaycastHit hit, distanciaMovimiento + 0.05f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            // Si el objeto chocado es el jugador (el objetivo), le permitimos avanzar para que ocurra la colisión
            bool esJugador = hit.transform == objetivo || hit.collider.GetComponentInParent<WaterTank>() != null;

            if (!esJugador)
            {
                // Es un obstáculo sólido (pared, mueble, etc.). Quedamos atrapados.
                QuedarAtrapado();
                return;
            }
        }

        if (distanciaMovimiento > 0f && direccion != Vector3.zero)
        {
            transform.position += direccion * distanciaMovimiento;
            transform.rotation = Quaternion.LookRotation(direccion);
        }

        // Si llega muy cerca del jugador, simula impacto y se destruye
        float distancia = Vector3.Distance(posicionActual, posicionObjetivo);
        if (distancia <= distanciaParada)
        {
            WaterTank tanque = objetivo.GetComponent<WaterTank>() ?? objetivo.GetComponentInParent<WaterTank>();
            ImpactarJugador(tanque);
        }
    }

    /// <summary>
    /// Activa el estado atrapado, calculando la tasa de decaimiento necesaria 
    /// para extinguirse reduciendo su tamaño a lo largo de exactamente 10 segundos.
    /// </summary>
    private void QuedarAtrapado()
    {
        if (_estaAtrapado || estaExtinguido || _yaImpacto) return;
        _estaAtrapado = true;

        // Calcular la tasa para ir de la intensidad actual a la de extinción en exactamente 10 segundos
        float rangoIntensidad = Mathf.Max(0f, intensidadActual - umbralExtincion);
        _tasaDecaimiento = rangoIntensidad > 0f ? (rangoIntensidad / 10f) : 1f;

        Debug.Log($"[CONSOLA FUEGO BUSCADOR: {gameObject.name}] Se ha chocado con un obstáculo sólido. Queda atrapado y se extinguirá en 10 segundos.");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (estaExtinguido || _yaImpacto) return;

        // Si el jugador choca físicamente antes de que alcance la distanciaParada
        WaterTank tanque = other.GetComponent<WaterTank>() ?? other.GetComponentInParent<WaterTank>();
        if (tanque != null)
        {
            ImpactarJugador(tanque);
            return;
        }

        // Si colisiona con cualquier objeto sólido (que no sea un trigger y no sea el jugador)
        if (!_estaAtrapado && !other.isTrigger && other.transform != objetivo)
        {
            QuedarAtrapado();
        }
    }

    protected override void OnTriggerStay(Collider other)
    {
        // El buscador usa impacto instantáneo (OnTriggerEnter y distanciaParada),
        // así que desactivamos el daño continuo del stay heredado de Fuego para evitar doble daño.
    }

    /// <summary>
    /// Simula el impacto del fuego contra el jugador y le drena agua de su WaterTank.
    /// </summary>
    private void ImpactarJugador(WaterTank tanque)
    {
        if (_yaImpacto) return;
        _yaImpacto = true;

        if (tanque == null)
        {
            tanque = FindFirstObjectByType<WaterTank>();
        }

        if (tanque != null)
        {
            tanque.Drain(penalizacionAgua);
            Debug.Log($"[FuegoBuscador] Impacto en jugador. Se drenaron {penalizacionAgua} unidades de agua del WaterTank.");
        }

        // Apagar e iniciar desvanecimiento inmediato en impacto
        ApagarFuegoInmediato();
    }

    /// <summary>
    /// Apagado rápido del fuego y detención de partículas al explotar en el jugador.
    /// </summary>
    private void ApagarFuegoInmediato()
    {
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

        ParticleSystem[] particulas = GetComponentsInChildren<ParticleSystem>();
        foreach (var ps in particulas)
        {
            if (ps != null) ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        Destroy(gameObject, 0.4f);
    }

    /// <summary>
    /// Extinción estándar al ser apagado por agua.
    /// Sobrescribe el método de la clase base Fuego para tener una destrucción ligeramente más rápida (1.5s).
    /// </summary>
    public override void ApagarFuego()
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

        ParticleSystem[] particulas = GetComponentsInChildren<ParticleSystem>();
        foreach (var ps in particulas)
        {
            if (ps != null) ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        Destroy(gameObject, 1.5f);
    }
}