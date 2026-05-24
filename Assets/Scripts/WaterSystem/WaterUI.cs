using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// HUD del nivel de agua del bombero.
/// Añadir a un GameObject vacío dentro del Canvas.
/// </summary>
public class WaterUI : MonoBehaviour
{
    [Header("Referencias UI")]
    public Slider waterSlider;
    public TMP_Text waterLabel;
    public Image fillImage;      // Arrastrar el GameObject "Fill" del Slider

    [Header("Colores por nivel")]
    public Color colorFull = new Color(0.2f, 0.6f, 1f);
    public Color colorMedium = new Color(0.4f, 0.8f, 0.2f);
    public Color colorLow = new Color(1f, 0.3f, 0.1f);

    [Range(0f, 1f)]
    public float lowThreshold = 0.25f;

    // ── INICIALIZACIÓN


    private void Start()
    {
        // FindAnyObjectByType es el correcto en Unity 6
        WaterTank tank = FindAnyObjectByType<WaterTank>();

        if (tank != null)
        {
            // Inicializar la UI con los valores actuales
            UpdateUI(tank.CurrentWater, tank.MaxCapacity);

            // Suscribirse al evento — se llamará automáticamente cada vez que cambie el agua
            tank.onWaterChanged.AddListener(UpdateUI);
        }
        else
        {
            Debug.LogWarning("[WaterUI] No se encontró ningún WaterTank en la escena.");
        }
    }

    // ── ACTUALIZACIÓN


    public void UpdateUI(float current, float max)
    {
        float percent = (max > 0f) ? current / max : 0f;

        if (waterSlider != null)
        {
            waterSlider.minValue = 0f;
            waterSlider.maxValue = max;
            waterSlider.value = current;
        }

        // Usa la cantidad de agua actual y la maxima del tanque de agua del player
        if (waterLabel != null)
            waterLabel.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)} ml";

        if (fillImage != null)
        {
            fillImage.color = percent <= lowThreshold ? colorLow
                            : percent <= 0.6f ? colorMedium
                                                      : colorFull;
        }
    }
}