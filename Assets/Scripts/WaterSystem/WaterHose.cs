using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controla la manguera del bombero.
/// - Clic izquierdo: activa el chorro de agua (Particle System)
/// - E: recarga el tanque en una fuente de agua cercana
/// El daño al fuego lo gestiona WaterParticleCollision en el WaterJet.
/// Añadir al GameObject raíz del Player.
/// </summary>
[RequireComponent(typeof(WaterTank))]
public class WaterHose : MonoBehaviour
{
    // ── REFERENCIAS
    [Header("Referencias")]
    public ParticleSystem waterParticles;   // El Particle System del chorro (WaterJet)
    public WaterCrosshair crosshair;        // El crosshair del HUD (se busca automáticamente)

    // ── DETECCIÓN DE FUENTE
    [Header("Detección de fuente de agua")]
    public float refillDetectionRadius = 3f;   // Radio en metros para detectar fuentes cercanas

    // ── PRIVADOS
    private WaterTank _tank;           // Referencia al tanque de agua del jugador
    private PlayerInputActions _inputActions;   // Sistema de input del jugador
    private bool _isFiring = false;   // ¿Está el jugador disparando ahora mismo?

    // ── INICIALIZACIÓN
    private void Awake()
    {
        // Obtener componentes del mismo GameObject
        _tank = GetComponent<WaterTank>();
        _inputActions = new PlayerInputActions();

        // Buscar el crosshair automáticamente si no se asignó en el Inspector
        if (crosshair == null)
            crosshair = FindAnyObjectByType<WaterCrosshair>();
    }

    private void OnEnable()
    {
        // Activar el mapa de acciones y suscribirse a los eventos de input
        _inputActions.Player.Enable();
        _inputActions.Player.Fire.performed += OnFireStarted;    // Al pulsar el botón de disparo
        _inputActions.Player.Fire.canceled += OnFireCanceled;   // Al soltar el botón de disparo
        _inputActions.Player.Refill.performed += OnRefillPressed;  // Al pulsar E
    }

    private void OnDisable()
    {
        // Desuscribirse de los eventos al desactivar el script (buena práctica para evitar errores)
        _inputActions.Player.Fire.performed -= OnFireStarted;
        _inputActions.Player.Fire.canceled -= OnFireCanceled;
        _inputActions.Player.Refill.performed -= OnRefillPressed;
        _inputActions.Player.Disable();
    }

    // ── CALLBACKS DE INPUT

    // Se llama cuando el jugador empieza a pulsar el botón de disparo
    private void OnFireStarted(InputAction.CallbackContext ctx)
    {
        _isFiring = true;
        crosshair?.SetFiring(true);    // Notificar al crosshair para cambiar su estado visual
        if (!_tank.IsEmpty)
            StartHose();   // Activar el chorro solo si hay agua
    }

    // Se llama cuando el jugador suelta el botón de disparo
    private void OnFireCanceled(InputAction.CallbackContext ctx)
    {
        _isFiring = false;
        crosshair?.SetFiring(false);   // Notificar al crosshair para volver al estado normal
        StopHose();   // Desactivar el chorro siempre al soltar
    }

    // Se llama cuando el jugador pulsa E para recargar
    private void OnRefillPressed(InputAction.CallbackContext ctx)
    {
        // Buscar si hay una fuente de agua cerca
        WaterSource source = FindNearbySource();

        if (source != null)
            source.TryToggleRefill();   // Activar o desactivar la recarga en esa fuente
        else
            Debug.Log("[WaterHose] No hay fuente de agua cerca.");
    }

    // ── BUCLE PRINCIPAL
    private void Update()
    {
        if (!_isFiring) return;   // Si no está disparando, no hacer nada

        // Si el tanque se vació mientras disparaba, parar el chorro
        if (_tank.IsEmpty)
        {
            StopHose();
            return;
        }

        // Consumir agua del tanque cada frame mientras se dispara
        _tank.Drain(_tank.drainRate * Time.deltaTime);
    }

    // ── CONTROL DEL CHORRO

    // Activa el Particle System del chorro de agua
    private void StartHose()
    {
        if (waterParticles != null && !waterParticles.isPlaying)
            waterParticles.Play();
    }

    // Desactiva el Particle System del chorro de agua
    private void StopHose()
    {
        if (waterParticles != null && waterParticles.isPlaying)
            waterParticles.Stop();
    }

    // ── BÚSQUEDA DE FUENTE CERCANA

    // Busca si hay algún WaterSource dentro del radio de detección
    // Usa Physics.OverlapSphere para no depender de triggers en el Player
    private WaterSource FindNearbySource()
    {
        // Obtener todos los colliders dentro del radio
        Collider[] hits = Physics.OverlapSphere(transform.position, refillDetectionRadius);

        // Comprobar si alguno tiene el componente WaterSource
        foreach (Collider col in hits)
        {
            WaterSource source = col.GetComponent<WaterSource>();
            if (source != null) return source;   // Devolver la primera fuente encontrada
        }

        return null;   // No se encontró ninguna fuente
    }

    // ── GIZMO: muestra el radio de detección en la ventana Scene al seleccionar el Player
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.2f);
        Gizmos.DrawSphere(transform.position, refillDetectionRadius);
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, refillDetectionRadius);
    }
}