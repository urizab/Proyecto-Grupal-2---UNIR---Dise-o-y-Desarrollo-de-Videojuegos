using UnityEngine;

/// <summary>
/// Abre una puerta rotándola suavemente cuando se llama a AbrirPuerta().
/// No necesita Animator ni clips de animación.
/// Conectar al evento OnThresholdReached del WaterReceiver desde el Inspector.
/// </summary>
public class DoorOpener : MonoBehaviour
{
    [Header("Configuración")]
    [Tooltip("Grados que rota la puerta al abrirse. Prueba 90 o -90 según la orientación.")]
    public float anguloApertura = 90f;

    [Tooltip("Velocidad a la que se abre la puerta.")]
    public float velocidad = 2f;

    [Tooltip("Eje sobre el que rota la puerta.")]
    public Vector3 ejeRotacion = Vector3.up;   // Y por defecto (giro horizontal)

    // ── Estado interno
    private Quaternion _rotacionCerrada;   // Rotación original de la puerta
    private Quaternion _rotacionAbierta;   // Rotación destino al abrir
    private bool _abriendo = false;  // ¿Está en proceso de abrirse?
    private bool _abierta = false;  // ¿Ya está completamente abierta?

    private void Awake()
    {
        // Guardar la rotación inicial como estado "cerrada"
        _rotacionCerrada = transform.rotation;

        // Calcular la rotación destino sumando el ángulo de apertura
        _rotacionAbierta = _rotacionCerrada * Quaternion.AngleAxis(anguloApertura, ejeRotacion);
    }

    private void Update()
    {
        // Si se está abriendo, rotar suavemente hacia la rotación destino
        if (_abriendo && !_abierta)
        {
            transform.rotation = Quaternion.Lerp(
                transform.rotation,
                _rotacionAbierta,
                velocidad * Time.deltaTime
            );

            // Considerar la puerta abierta cuando está muy cerca del destino
            if (Quaternion.Angle(transform.rotation, _rotacionAbierta) < 0.5f)
            {
                transform.rotation = _rotacionAbierta;
                _abierta = true;
                _abriendo = false;
                Debug.Log("[DoorOpener] Puerta abierta.");
            }
        }
    }

    // ── API PÚBLICA

    /// <summary>
    /// Inicia la apertura de la puerta.
    /// Conectar este método al evento OnThresholdReached del WaterReceiver.
    /// </summary>
    public void AbrirPuerta()
    {
        if (_abierta) return;   // Si ya está abierta, no hacer nada
        _abriendo = true;
        Debug.Log("[DoorOpener] Abriendo puerta...");
    }

    /// <summary>
    /// Cierra la puerta volviendo a su posición original.
    /// Opcional — útil si quieres que la puerta se pueda cerrar de nuevo.
    /// </summary>
    public void CerrarPuerta()
    {
        _abierta = false;
        _abriendo = false;
        transform.rotation = _rotacionCerrada;
        Debug.Log("[DoorOpener] Puerta cerrada.");
    }
}