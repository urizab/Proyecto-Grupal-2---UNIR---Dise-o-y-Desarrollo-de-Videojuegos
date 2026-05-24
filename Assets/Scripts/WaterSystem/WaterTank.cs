using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Gestiona el tanque de agua del bombero.
/// Añadir al GameObject raíz del Player.
/// </summary>
public class WaterTank : MonoBehaviour
{
    [Header("Configuración del Tanque")]
    public float maxCapacity = 500f;
    public float drainRate = 30f;    // Unidades/segundo que gasta la manguera
    public float fillRate = 80f;    // Unidades/segundo que recarga en una fuente

    [Header("Estado (solo lectura en Play)")]
    [SerializeField] private float _currentWater = 0f;

    // ── Eventos para UI y otros sistemas
    public UnityEvent<float, float> onWaterChanged;  // (actual, máximo)
    public UnityEvent onTankEmpty;
    public UnityEvent onTankFull;

    // ── Propiedades públicas
    public float CurrentWater => _currentWater;
    public float MaxCapacity => maxCapacity;
    public float FillPercent => _currentWater / maxCapacity;
    public bool IsEmpty => _currentWater <= 0f;
    public bool IsFull => _currentWater >= maxCapacity;

    // ── Estado interno
    private bool _isRefilling = false;

    private void Update()
    {
        if (_isRefilling && !IsFull)
            Fill(fillRate * Time.deltaTime);
    }

    // ── API PÚBLICA

    /// <summary>Añade agua al tanque.</summary>
    public void Fill(float amount)
    {
        if (IsFull) return;
        _currentWater = Mathf.Min(_currentWater + amount, maxCapacity);
        onWaterChanged?.Invoke(_currentWater, maxCapacity);
        if (IsFull) onTankFull?.Invoke();
    }

    /// <summary>
    /// Extrae agua del tanque. Devuelve cuánta salió realmente
    /// (puede ser menos si el tanque estaba casi vacío).
    /// </summary>
    public float Drain(float amount)
    {
        if (IsEmpty) return 0f;
        float drained = Mathf.Min(amount, _currentWater);
        _currentWater -= drained;
        onWaterChanged?.Invoke(_currentWater, maxCapacity);
        if (IsEmpty) onTankEmpty?.Invoke();
        return drained;
    }

    /// <summary>Inicia recarga continua (llamado por WaterSource).</summary>
    public void StartRefilling() => _isRefilling = true;

    /// <summary>Para la recarga continua.</summary>
    public void StopRefilling() => _isRefilling = false;
}