using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Añadir al mismo GameObject que el Particle System del chorro (WaterJet).
/// Detecta cuando las partículas impactan y notifica al fuego.
/// Requiere que el módulo Collision del Particle System tenga
/// "Send Collision Messages" activado.
/// </summary>
public class WaterParticleCollision : MonoBehaviour
{
    [Header("Referencias")]
    public WaterTank playerTank;          // Arrastrar el WaterTank del Player para que funcione

    [Header("Daño por partícula")]
    [Tooltip("Agua que se aplica al fuego por cada partícula que impacta.")]
    public float waterPerParticle = 0.5f;

    // Buffer reutilizable para evitar allocations en cada frame
    private List<ParticleCollisionEvent> _collisionEvents = new List<ParticleCollisionEvent>();
    private ParticleSystem _ps;

    // ── INICIALIZACIÓN

    private void Awake()
    {
        _ps = GetComponent<ParticleSystem>();

        // Buscar el WaterTank automáticamente si no se asignó
        if (playerTank == null)
            playerTank = GetComponentInParent<WaterTank>();

        if (playerTank == null)
            Debug.LogError("[WaterParticleCollision] No se encontró WaterTank en el padre.");
    }

    // ── DETECCIÓN DE COLISIÓN DE PARTÍCULAS

    /// <summary>
    /// Unity llama a este método en el GameObject que RECIBE el impacto
    /// Y también en el GameObject que TIENE el Particle System.
    /// Aquí lo gestionamos desde el lado del Particle System.
    /// </summary>
    private void OnParticleCollision(GameObject other)
    {
        // Obtener todos los eventos de colisión de este frame
        int count = ParticlePhysicsExtensions.GetCollisionEvents(_ps, other, _collisionEvents);

        if (count == 0) return;

        // Comprueba si el objeto golpeado tiene el script del fuego
        // Sustir "FireScript" por el nombre real del script de Uri
        var fire = other.GetComponent<Fuego>();

        if (fire != null)
        {
            // Aplicar agua proporcional al número de partículas que impactaron
            float totalWater = waterPerParticle * count;
            fire.RecibirAgua(totalWater);

            Debug.Log($"[WaterParticleCollision] {count} partículas golpearon {other.name} → {totalWater:F2} agua aplicada.");
        }
    }
}