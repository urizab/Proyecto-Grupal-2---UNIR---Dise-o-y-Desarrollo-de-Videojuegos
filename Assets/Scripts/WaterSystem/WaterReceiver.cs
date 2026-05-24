using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Componente genérico para cualquier objeto que pueda recibir agua.
/// Cuando el agua acumulada alcanza el umbral definido, dispara una acción.
/// Ejemplos de uso: palanca que se activa, planta que crece, puerta que se abre...
/// Añadir a cualquier GameObject que deba reaccionar al agua de la manguera.
/// IMPORTANTE: el objeto necesita un Collider para que las partículas lo detecten.
/// </summary>
public class WaterReceiver : MonoBehaviour
{
    // ── CONFIGURACIÓN
    [Header("Configuración")]
    [Tooltip("Cantidad de agua necesaria para activar la acción.")]
    public float waterThreshold = 100f;

    [Tooltip("¿Se puede activar más de una vez? Si está desactivado, solo se dispara una vez.")]
    public bool repeatable = false;

    [Tooltip("Si repeatable está activo, cuánta agua se necesita entre activaciones.")]
    public float repeatCooldownWater = 100f;

    [Tooltip("¿El agua acumulada se reduce con el tiempo?")]
    public bool drainOverTime = false;

    [Tooltip("Unidades de agua que se pierden por segundo si drainOverTime está activo.")]
    public float drainRate = 5f;

    // ── ESTADO (visible en Inspector durante Play para depuración)
    [Header("Estado (solo lectura en Play)")]
    [SerializeField] private float _currentWater = 0f;   // Agua acumulada actualmente
    [SerializeField] private int _activationCount = 0; // Veces que se ha activado

    // ── EVENTOS
    [Header("Eventos")]
    [Tooltip("Se dispara cuando el agua acumulada alcanza el umbral.")]
    public UnityEvent onThresholdReached;           // Acción principal al alcanzar el umbral

    [Tooltip("Se dispara cada vez que recibe agua (útil para efectos visuales o sonidos).")]
    public UnityEvent<float> onWaterReceived;       // (cantidad recibida en este frame)

    [Tooltip("Se dispara cuando el agua baja de 0 (si drainOverTime está activo).")]
    public UnityEvent onWaterDepleted;

    // ── PROPIEDADES PÚBLICAS
    public float CurrentWater => _currentWater;
    public float FillPercent => _currentWater / waterThreshold;   // 0.0 a 1.0
    public int ActivationCount => _activationCount;

    // ── ESTADO INTERNO
    private bool _activated = false;   // ¿Ya se activó al menos una vez?
    private bool _wasActivated = false;   // Control para repeatable


    private void Update()
    {
        // Reducir el agua acumulada con el tiempo si está configurado
        if (drainOverTime && _currentWater > 0f)
        {
            _currentWater -= drainRate * Time.deltaTime;
            _currentWater = Mathf.Max(_currentWater, 0f);

            if (_currentWater <= 0f)
            {
                onWaterDepleted?.Invoke();
                _wasActivated = false;   // Permite reactivarse si es repeatable
            }
        }
    }

    // ── API PÚBLICA

    /// <summary>
    /// Añade agua acumulada a este objeto.
    /// Llamar desde WaterParticleCollision cuando las partículas impacten aquí.
    /// </summary>
    public void ReceiveWater(float amount)
    {
        // Si ya se activó y no es repetible, ignorar el agua
        if (_activated && !repeatable) return;

        _currentWater += amount;

        // Notificar que se recibió agua (útil para efectos)
        onWaterReceived?.Invoke(amount);

        // Comprobar si se alcanzó el umbral
        if (_currentWater >= waterThreshold && !_wasActivated)
        {
            _wasActivated = true;
            _activated = true;
            _activationCount++;

            // Si es repetible, reiniciar el agua para el siguiente ciclo
            if (repeatable)
            {
                _currentWater -= repeatCooldownWater;
                _currentWater = Mathf.Max(_currentWater, 0f);
                _wasActivated = false;
            }

            // ── DISPARAR LA ACCIÓN PRINCIPAL
            onThresholdReached?.Invoke();

            Debug.Log($"[WaterReceiver] {gameObject.name} activado (x{_activationCount}). Agua: {_currentWater:F1}");
        }
    }

    /// <summary>
    /// Resetea el agua acumulada y el estado de activación.
    /// Útil para puzzles que se pueden reiniciar.
    /// </summary>
    public void Reset()
    {
        _currentWater = 0f;
        _activated = false;
        _wasActivated = false;
        _activationCount = 0;
    }
}