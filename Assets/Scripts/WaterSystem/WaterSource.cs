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

    // ── Estado interno
    private WaterTank _playerTank;
    private bool _isRefilling = false;
    private float _remainingSupply;

    // ── Propiedad pública para que WaterHose sepa si hay jugador cerca
    public bool PlayerInRange => _playerTank != null;

    // ── INICIALIZACIÓN

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
        _remainingSupply = supply;

        if (interactPrompt != null)
            interactPrompt.SetActive(false);
    }

    // ── BUCLE PRINCIPAL

    private void Update()
    {
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

    // ── GIZMOS

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 0.5f, 1f, 0.15f);
        Gizmos.DrawSphere(transform.position, 2f);
        Gizmos.color = new Color(0f, 0.5f, 1f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, 2f);
    }
}