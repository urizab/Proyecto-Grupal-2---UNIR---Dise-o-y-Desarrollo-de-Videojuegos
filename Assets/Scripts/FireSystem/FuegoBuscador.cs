using UnityEngine;

/// <summary>
/// Controla el movimiento de un fuego secundario que persigue al jugador
/// bajo ciertas restricciones de ejes. Resta agua al jugador al impactar.
/// Integrado con el sistema oficial WaterTank del proyecto.
/// Asegura configuración física autónoma en escena.
/// </summary>
[RequireComponent(typeof(Collider))]
public class FuegoBuscador : MonoBehaviour
{
    public enum RestriccionMovimiento
    {
        TresDimensiones,  // Movimiento libre en las 3 dimensiones (Full 3D)
        SoloPlanoXZ,      // Solo movimiento horizontal (mantiene la altura Y del spawn)
        SoloEjeY          // Solo movimiento vertical (se mueve de arriba a abajo)
    }

    [Header("── MOVIMIENTO")]
    [Tooltip("Objetivo a perseguir (el jugador).")]
    public Transform objetivo;

    [Tooltip("Velocidad de movimiento hacia el jugador.")]
    public float velocidad = 3f;

    [Tooltip("Restricción del plano o eje de movimiento.")]
    public RestriccionMovimiento restriccionMovimiento = RestriccionMovimiento.SoloPlanoXZ;

    [Tooltip("Distancia mínima al jugador para auto-destruirse.")]
    public float distanciaParada = 0.8f;

    [Tooltip("Tiempo de vida máximo en segundos antes de disiparse solo.")]
    public float tiempoVida = 6f;

    [Header("── DAÑO / PENALIZACIÓN")]
    [Tooltip("Cantidad de agua que pierde el jugador al ser golpeado por este fuego.")]
    public float penalizacionAgua = 15f;

    private float _tiempoSpawn;
    private bool _yaImpacto = false;

    private void Start()
    {
        _tiempoSpawn = Time.time;

        // Auto-destrucción por tiempo de vida por si no alcanza al jugador
        Destroy(gameObject, tiempoVida);

        // Si no se asignó un objetivo, buscar al jugador automáticamente
        if (objetivo == null)
        {
            var controladorJugador = FindFirstObjectByType<FirstPersonController>();
            if (controladorJugador != null)
            {
                objetivo = controladorJugador.transform;
            }
        }

        // ── CONFIGURACIÓN FÍSICA AUTOMÁTICA
        // Asegurar que el collider esté en modo Trigger
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        // Configurar un Rigidbody Cinemático automático para garantizar disparadores físicos
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    private void Update()
    {
        if (objetivo == null || _yaImpacto) return;

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
            ImpactarJugador();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Si el jugador choca físicamente antes de que alcance la distanciaParada
        if (other.GetComponent<WaterTank>() != null || other.GetComponentInParent<WaterTank>() != null)
        {
            ImpactarJugador();
        }
    }

    /// <summary>
    /// Simula el impacto del fuego contra el jugador y le drena agua.
    /// </summary>
    private void ImpactarJugador()
    {
        if (_yaImpacto) return;
        _yaImpacto = true;

        // Restar agua al jugador si se encuentra disponible
        WaterTank tanque = null;
        if (objetivo != null)
        {
            tanque = objetivo.GetComponent<WaterTank>() ?? objetivo.GetComponentInParent<WaterTank>();
        }

        if (tanque == null)
        {
            tanque = FindFirstObjectByType<WaterTank>();
        }

        if (tanque != null)
        {
            tanque.Drain(penalizacionAgua);
            Debug.Log($"[FuegoBuscador] Impacto en jugador. Se drenaron {penalizacionAgua} unidades de agua del WaterTank.");
        }

        // Detener partículas suavemente
        var particulas = GetComponentsInChildren<ParticleSystem>();
        foreach (var ps in particulas)
        {
            if (ps != null) ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        Destroy(gameObject, 0.5f);
    }
}