using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class WaterHUD : MonoBehaviour
{
    public Image waterImage;
    public Sprite[] waterSprites;

    public int currentWater = 2;
    public int maxWater = 5;

    void Start()
    {
        UpdateWaterHUD();
    }

    public void UseWater(int amount = 1)  // Para bajar el nivel de agua del contador
    {
        currentWater -= amount;

        if (currentWater < 0)
        {
            currentWater = 0;
        }

        UpdateWaterHUD();
    }

    public void AddWater(int amount) // Rellena al completo el contador de agua
    {
        currentWater += amount;

        if (currentWater > maxWater)
        {
            currentWater = maxWater;
        }

        UpdateWaterHUD();
    }

    public void SetWater(int amount)  // Establecer un nivel de agua concreta (checkpoints)
    {
        currentWater = amount;

        if (currentWater < 0)
        {
            currentWater = 0;
        }

        if (currentWater > maxWater)
        {
            currentWater = maxWater;
        }

        UpdateWaterHUD();
    }

    void UpdateWaterHUD()   // Cambia la imagen del HUD
    {
        waterImage.sprite = waterSprites[currentWater];
    }

    // PRUEBAAA
    void Update()
    {
        if (Keyboard.current.hKey.wasPressedThisFrame)
        {
            UseWater(1);
        }

        if (Keyboard.current.jKey.wasPressedThisFrame)
        {
            AddWater(1);
        }
    }
}