using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Detecta cuando las partículas del chorro de agua golpean un objeto
/// y notifica al fuego o a cualquier WaterReceiver.
/// Añadir al mismo GameObject que el Particle System del chorro (WaterJet).
/// IMPORTANTE: el módulo Collision del Particle System debe tener
/// "Send Collision Messages" activado para que esto funcione.
/// </summary>
public class WaterParticleCollision : MonoBehaviour
{
    // ── REFERENCIAS
    [Header("Referencias")]
    public WaterTank playerTank;   // El tanque de agua del jugador (se busca automáticamente si se deja vacío)

    // ── CONFIGURACIÓN
    [Header("Daño por partícula")]
    [Tooltip("Agua que se aplica por cada partícula que impacta.")]
    public float waterPerParticle = 0.5f;

    // ── PRIVADOS
    // Lista reutilizable para los eventos de colisión — evita crear basura en memoria cada frame
    private List<ParticleCollisionEvent> _collisionEvents = new List<ParticleCollisionEvent>();
    private ParticleSystem _ps;

    // ── INICIALIZACIÓN
    private void Awake()
    {
        _ps = GetComponent<ParticleSystem>();

        if (playerTank == null)
            playerTank = GetComponentInParent<WaterTank>();

        if (playerTank == null)
            Debug.LogError("[WaterParticleCollision] No se encontró WaterTank en el padre.");
    }

    // ── DETECCIÓN DE COLISIÓN DE PARTÍCULAS

    /// <summary>
    /// Unity llama a este método automáticamente cuando una partícula
    /// colisiona con otro objeto que tiene Collider.
    /// </summary>
    public void OnParticleCollision(GameObject other)
    {
        // Obtener cuántas partículas golpearon este objeto en este frame
        int count = ParticlePhysicsExtensions.GetCollisionEvents(_ps, other, _collisionEvents);
        if (count == 0) return;

        // Calcular el agua total según cuántas partículas impactaron este frame
        float totalAgua = waterPerParticle * count;

        // ── ¿Es un fuego? → apagarlo
        Fuego fuego = other.GetComponent<Fuego>();
        if (fuego != null)
        {
            fuego.RecibirAgua(totalAgua);
            Debug.Log($"[WaterParticleCollision] {count} partículas golpearon fuego {other.name} → {totalAgua:F2} agua.");
        }

        // ── ¿Es un objeto con WaterReceiver? → acumular agua y activar acción
        WaterReceiver receptor = other.GetComponent<WaterReceiver>();
        if (receptor != null)
        {
            receptor.ReceiveWater(totalAgua);
        }
    }
}