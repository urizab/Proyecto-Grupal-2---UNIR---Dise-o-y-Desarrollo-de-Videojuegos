using UnityEngine;

/// <summary>
/// Mantiene este GameObject apuntando en la misma dirección que la cámara.
/// Añadir al GameObject WaterJet (el Particle System del chorro de agua).
/// </summary>
public class FollowCameraDirection : MonoBehaviour
{
    [Tooltip("Arrastrar la Main Camera del Player.")]
    public Transform cameraTransform;

    private void Awake()
    {
        // Buscar la cámara automáticamente si no se asigna en el Inspector
        if (cameraTransform == null)
        {
            Camera cam = GetComponentInParent<Camera>();
            if (cam == null)
                cam = Camera.main;
            if (cam != null)
                cameraTransform = cam.transform;
        }
    }

    private void LateUpdate()
    {
        if (cameraTransform == null) return;

        // Copiar solo la rotación de la cámara (no la posición)
        transform.rotation = cameraTransform.rotation;
    }
}