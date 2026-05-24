using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Anima el crosshair del HUD según el estado de la manguera.
/// Los elementos visuales se crean MANUALMENTE en el Canvas (ver instrucciones).
/// Añadir al GameObject raíz "Crosshair" dentro del Canvas.
/// </summary>
public class WaterCrosshair : MonoBehaviour
{
    [Header("Elementos del crosshair (arrastrar desde la Hierarchy)")]
    public Image[] lines;       // Los 4 Image de las líneas (Top, Bottom, Left, Right)

    [Header("Colores")]
    public Color colorNormal = new Color(0.31f, 0.78f, 1f, 0.85f);   // Blanco en reposo
    public Color colorFiring = new Color(1f, 1f, 1f, 0.95f);   // Azul al disparar
    public Color colorNoWater = new Color(1f, 0.3f, 0.1f, 0.85f);  // Rojo sin agua

    [Header("Animación")]
    public float pulseSpeed = 2f;     // Velocidad del pulso del anillo
    public float pulseAmount = 0.08f;  // Cuánto crece el anillo

    // ── Privados
    private WaterTank _tank;
    private bool _isFiring = false;

    // ── INICIALIZACIÓN


    private void Start()
    {
        // Buscar el WaterTank automáticamente
        _tank = FindAnyObjectByType<WaterTank>();

        if (_tank == null)
            Debug.LogWarning("[WaterCrosshair] No se encontró WaterTank en la escena.");

    }

    private void Update()
    {
        // Determinar color según estado
        Color color = (_tank != null && _tank.IsEmpty) ? colorNoWater
                    : _isFiring ? colorFiring
                                                       : colorNormal;

        // Aplicar color a todos los elementos
        if (lines != null)
            foreach (var line in lines)
                if (line != null) line.color = color;

    }

    // ── API PÚBLICA 

    /// <summary>
    /// WaterHose llama esto al empezar y parar de disparar.
    /// </summary>
    public void SetFiring(bool firing) => _isFiring = firing;
}