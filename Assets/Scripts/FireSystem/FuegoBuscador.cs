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

    protected override void Start()
    {
        _tiempoSpawn = Time.time;
        
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

        if (objetivo == null || _yaImpacto || estaExtinguido) return;

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
        transform.position += direccion * velocidad * Time.deltaTime;

        if (direccion != Vector3.zero)
        {
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

    private void OnTriggerEnter(Collider other)
    {
        if (estaExtinguido || _yaImpacto) return;

        // Si el jugador choca físicamente antes de que alcance la distanciaParada
        WaterTank tanque = other.GetComponent<WaterTank>() ?? other.GetComponentInParent<WaterTank>();
        if (tanque != null)
        {
            ImpactarJugador(tanque);
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

        ParticleSystem[] particulas = GetComponentsInChildren<ParticleSystem>();
        foreach (var ps in particulas)
        {
            if (ps != null) ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        Destroy(gameObject, 1.5f);
    }
}