using UnityEngine;

/// <summary>
/// Punto de recarga de agua (hidrante, lago, depósito...).
/// Requiere un Collider marcado como Trigger en el mismo GameObject.
/// NO gestiona input directamente — WaterHose llama a TryToggleRefill().
/// </summary>
[RequireComponent(typeof(Collider))]
public class WaterSource : MonoBehaviour
{
    [Header("Suministro")]
    [Tooltip("Agua disponible en la fuente. -1 = ilimitada.")]
    public float supply = -1f;

    [Header("Feedback visual")]
    public GameObject interactPrompt;

    [Header("Brillo emisivo")]
    [Tooltip("MeshRenderer del objeto fuente. Arrastra aquí el renderer.")]
    public Renderer sourceRenderer;
    [Tooltip("Color del brillo cuando el jugador está cerca.")]
    public Color glowColor = new Color(0f, 0.5f, 1f);
    [Tooltip("Intensidad máxima del emisivo.")]
    [Range(0.5f, 4f)] public float glowIntensity = 1.4f;
    [Tooltip("Velocidad del pulso.")]
    [Range(0.5f, 6f)] public float pulseSpeed = 2f;

    // ── Estado interno
    private WaterTank _playerTank;
    private bool _isRefilling = false;
    private float _remainingSupply;
    private MaterialPropertyBlock _mpb;

    // ── Propiedad pública para que WaterHose sepa si hay jugador cerca
    public bool PlayerInRange => _playerTank != null;

    // ── INICIALIZACIÓN
    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
        _remainingSupply = supply;

        if (interactPrompt != null)
            interactPrompt.SetActive(false);

        _mpb = new MaterialPropertyBlock();
        ApplyGlow(Color.black); // emisivo apagado al inicio
    }

    // ── BUCLE PRINCIPAL
    private void Update()
    {
        // Pulso emisivo cuando el jugador está en rango
        if (_playerTank != null)
        {
            float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f; // oscila 0..1
            float intensity = Mathf.Lerp(glowIntensity * 0.25f, glowIntensity, t);
            ApplyGlow(glowColor * intensity);
        }

        if (_playerTank == null) return;

        // Parar si el tanque se llenó
        if (_isRefilling && _playerTank.IsFull)
        {
            StopRefill();
            Debug.Log("[WaterSource] Tanque lleno.");
        }

        // Consumir suministro limitado
        if (_isRefilling && supply > 0f)
        {
            _remainingSupply -= _playerTank.fillRate * Time.deltaTime;
            if (_remainingSupply <= 0f)
            {
                _remainingSupply = 0f;
                StopRefill();
                Debug.Log("[WaterSource] Fuente agotada.");
            }
        }
    }

    // ─── API PÚBLICA
    /// <summary>
    /// WaterHose llama esto cuando el jugador pulsa la tecla Refill
    /// y está dentro del trigger de esta fuente.
    /// </summary>
    public void TryToggleRefill()
    {
        if (_playerTank == null) return;
        if (_isRefilling) StopRefill();
        else StartRefill();
    }

    // ── TRIGGER DE PROXIMIDAD
    private void OnTriggerEnter(Collider other)
    {
        WaterTank tank = other.GetComponent<WaterTank>();
        if (tank == null) return;

        _playerTank = tank;

        if (interactPrompt != null)
            interactPrompt.SetActive(true);

        Debug.Log("[WaterSource] Jugador en rango. Pulsa [E] para recargar.");
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<WaterTank>() != _playerTank) return;

        StopRefill();
        _playerTank = null;
        ApplyGlow(Color.black); // apagar emisivo al salir

        if (interactPrompt != null)
            interactPrompt.SetActive(false);

        Debug.Log("[WaterSource] Jugador fuera del rango.");
    }

    // ── CONTROL DE RECARGA (privado)
    private void StartRefill()
    {
        if (_playerTank == null || _playerTank.IsFull) return;
        _isRefilling = true;
        _playerTank.StartRefilling();
        Debug.Log("[WaterSource] Recargando...");
    }

    private void StopRefill()
    {
        if (!_isRefilling) return;
        _isRefilling = false;
        _playerTank?.StopRefilling();
        Debug.Log("[WaterSource] Recarga detenida.");
    }

    // ── EMISIVO
    private void ApplyGlow(Color color)
    {
        if (sourceRenderer == null) return;
        sourceRenderer.GetPropertyBlock(_mpb);
        _mpb.SetColor("_EmissionColor", color);
        sourceRenderer.SetPropertyBlock(_mpb);
    }

    // ── GIZMOS
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 0.5f, 1f, 0.15f);
        Gizmos.DrawSphere(transform.position, 2f);
        Gizmos.color = new Color(0f, 0.5f, 1f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, 2f);
    }
}