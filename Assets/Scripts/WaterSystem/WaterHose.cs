using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Manguera del bombero.
/// - Clic izquierdo: activa el Particle System del chorro
/// - E: recarga en fuente cercana
/// El daño al fuego lo gestiona WaterParticleCollision en el WaterJet.
/// </summary>
[RequireComponent(typeof(WaterTank))]
public class WaterHose : MonoBehaviour
{
    [Header("Referencias")]
    public ParticleSystem waterParticles;     // Arrastrar el WaterJet del player

    [Header("Detección de fuente de agua")]
    public float refillDetectionRadius = 3f;

    // ── Privados
    private WaterTank _tank;
    private PlayerInputActions _inputActions;
    private bool _isFiring = false;

    // ── INICIALIZACIÓN

    private void Awake()
    {
        _tank = GetComponent<WaterTank>();
        _inputActions = new PlayerInputActions();
    }

    private void OnEnable()
    {
        _inputActions.Player.Enable();
        _inputActions.Player.Fire.performed += OnFireStarted;
        _inputActions.Player.Fire.canceled += OnFireCanceled;
        _inputActions.Player.Refill.performed += OnRefillPressed;
    }

    private void OnDisable()
    {
        _inputActions.Player.Fire.performed -= OnFireStarted;
        _inputActions.Player.Fire.canceled -= OnFireCanceled;
        _inputActions.Player.Refill.performed -= OnRefillPressed;
        _inputActions.Player.Disable();
    }

    // ── CALLBACKS DE INPUT

    private void OnFireStarted(InputAction.CallbackContext ctx)
    {
        _isFiring = true;
        if (!_tank.IsEmpty)
            StartHose();
    }

    private void OnFireCanceled(InputAction.CallbackContext ctx)
    {
        _isFiring = false;
        StopHose();
    }

    private void OnRefillPressed(InputAction.CallbackContext ctx)
    {
        WaterSource source = FindNearbySource();
        if (source != null)
            source.TryToggleRefill();
        else
            Debug.Log("[WaterHose] No hay fuente de agua cerca.");
    }

    // ── BUCLE PRINCIPAL

    private void Update()
    {
        if (!_isFiring) return;

        // Parar si el tanque se vació
        if (_tank.IsEmpty)
        {
            StopHose();
            return;
        }

        // Consumir agua mientras se dispara
        _tank.Drain(_tank.drainRate * Time.deltaTime);
    }

    // ── CONTROL DEL CHORRO

    private void StartHose()
    {
        if (waterParticles != null && !waterParticles.isPlaying)
            waterParticles.Play();
    }

    private void StopHose()
    {
        if (waterParticles != null && waterParticles.isPlaying)
            waterParticles.Stop();
    }

    // ── BÚSQUEDA DE FUENTE CERCANA

    private WaterSource FindNearbySource()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, refillDetectionRadius);
        foreach (Collider col in hits)
        {
            WaterSource source = col.GetComponent<WaterSource>();
            if (source != null) return source;
        }
        return null;
    }

    // ── GIZMO

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.2f);
        Gizmos.DrawSphere(transform.position, refillDetectionRadius);
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, refillDetectionRadius);
    }
}