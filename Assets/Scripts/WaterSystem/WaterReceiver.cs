using UnityEngine;
using UnityEngine.Events;

public class WaterReceiver : MonoBehaviour
{
    // ── CONFIGURACIÓN
    [Header("Configuración")]
    [Tooltip("Cantidad de agua necesaria para activar la acción.")]
    public float waterThreshold = 100f;

    [Tooltip("¿Se puede activar más de una vez?")]
    public bool repeatable = false;

    [Tooltip("Si repeatable está activo, cuánta agua se necesita entre activaciones.")]
    public float repeatCooldownWater = 100f;

    [Tooltip("¿El agua acumulada se reduce con el tiempo?")]
    public bool drainOverTime = false;

    [Tooltip("Unidades de agua que se pierden por segundo si drainOverTime está activo.")]
    public float drainRate = 5f;

    // ── ESTADO
    [Header("Estado (solo lectura en Play)")]
    [SerializeField] private float _currentWater = 0f;
    [SerializeField] private int _activationCount = 0;

    // ── EVENTOS
    [Header("Eventos")]
    public UnityEvent onThresholdReached;
    public UnityEvent<float> onWaterReceived;
    public UnityEvent onWaterDepleted;

    // ── BRILLO EMISIVO
    [Header("Brillo emisivo")]
    [Tooltip("MeshRenderer del objeto. Arrastra aquí el renderer.")]
    public Renderer sourceRenderer;
    [Tooltip("Color cuando está vacío (sin agua).")]
    public Color emptyColor = new Color(1f, 0f, 0f);       // rojo
    [Tooltip("Color cuando está lleno (umbral alcanzado).")]
    public Color fullColor = new Color(0f, 0.5f, 1f);      // azul
    [Tooltip("Intensidad máxima del emisivo.")]
    [Range(0.5f, 4f)] public float glowIntensity = 1.4f;
    [Tooltip("Velocidad del pulso.")]
    [Range(0.5f, 6f)] public float pulseSpeed = 2f;

    // ── PROPIEDADES PÚBLICAS
    public float CurrentWater => _currentWater;
    public float FillPercent => _currentWater / waterThreshold;
    public int ActivationCount => _activationCount;

    // ── ESTADO INTERNO
    private bool _activated = false;
    private bool _wasActivated = false;
    private MaterialPropertyBlock _mpb;

    private void Awake()
    {
        _mpb = new MaterialPropertyBlock();
        ApplyGlow(emptyColor * glowIntensity * 0.25f); // rojo tenue al inicio
    }

    private void Update()
    {
        // Pulso cuyo color interpola según el llenado
        float fill = Mathf.Clamp01(FillPercent);
        Color currentColor = Color.Lerp(emptyColor, fullColor, fill);
        float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f; // 0..1
        float intensity = Mathf.Lerp(glowIntensity * 0.25f, glowIntensity, t);
        ApplyGlow(currentColor * intensity);

        // Drain
        if (drainOverTime && _currentWater > 0f)
        {
            _currentWater -= drainRate * Time.deltaTime;
            _currentWater = Mathf.Max(_currentWater, 0f);

            if (_currentWater <= 0f)
            {
                onWaterDepleted?.Invoke();
                _wasActivated = false;
            }
        }
    }

    // ── API PÚBLICA
    public void ReceiveWater(float amount)
    {
        if (_activated && !repeatable) return;

        _currentWater += amount;
        onWaterReceived?.Invoke(amount);

        if (_currentWater >= waterThreshold && !_wasActivated)
        {
            _wasActivated = true;
            _activated = true;
            _activationCount++;

            if (repeatable)
            {
                _currentWater -= repeatCooldownWater;
                _currentWater = Mathf.Max(_currentWater, 0f);
                _wasActivated = false;
            }

            onThresholdReached?.Invoke();
            Debug.Log($"[WaterReceiver] {gameObject.name} activado (x{_activationCount}). Agua: {_currentWater:F1}");
        }
    }

    public void Reset()
    {
        _currentWater = 0f;
        _activated = false;
        _wasActivated = false;
        _activationCount = 0;
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
        Gizmos.color = new Color(1f, 0f, 0f, 0.15f);
        Gizmos.DrawSphere(transform.position, 1f);
        Gizmos.color = new Color(1f, 0f, 0f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, 1f);
    }
}