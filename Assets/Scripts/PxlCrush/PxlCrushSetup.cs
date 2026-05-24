using UnityEngine;
using UnityEngine.UI;

namespace PxlCrush
{
    [ExecuteInEditMode]
    public class PxlCrushSetup : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("The resolution width for the retro effect (e.g. 320 for 320x180)")]
        public int targetWidth = 320;
        [Tooltip("The resolution height for the retro effect (e.g. 180 for 320x180)")]
        public int targetHeight = 180;
        
        [Header("References")]
        public Camera mainCamera;
        public RenderTexture renderTexture;
        public Material pxlCrushMaterial;

        [ContextMenu("Setup Pxl Crush Automatically")]
        public void SetupEffect()
        {
            // 1. Ensure Main Camera exists
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera == null)
                {
                    Debug.LogError("[PxlCrushSetup] No Main Camera found in the scene! Please assign it manually.");
                    return;
                }
            }

            // 2. Locate or Create Render Texture
            if (renderTexture == null)
            {
                // Try to load the existing one from the project
#if UNITY_EDITOR
                string rtPath = "Assets/Shader/Textura Shader.renderTexture";
                renderTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<RenderTexture>(rtPath);
#endif
                if (renderTexture == null)
                {
                    Debug.LogWarning("[PxlCrushSetup] Render Texture not found at default path. Creating a temporary runtime RenderTexture. For saving settings, create a Render Texture Asset and assign it.");
                    renderTexture = new RenderTexture(targetWidth, targetHeight, 24);
                }
            }

            // Configure RenderTexture settings for pixel art
            renderTexture.width = targetWidth;
            renderTexture.height = targetHeight;
            renderTexture.filterMode = FilterMode.Point;
            renderTexture.antiAliasing = 1;

            // 3. Attach Render Texture to Main Camera
            mainCamera.targetTexture = renderTexture;

            // 4. Create UI Camera
            GameObject uiCamObj = GameObject.Find("UI Camera (PxlCrush)");
            Camera uiCamera = null;
            if (uiCamObj == null)
            {
                uiCamObj = new GameObject("UI Camera (PxlCrush)");
                uiCamera = uiCamObj.AddComponent<Camera>();
            }
            else
            {
                uiCamera = uiCamObj.GetComponent<Camera>();
            }

            uiCamObj.transform.SetParent(transform);
            uiCamera.clearFlags = CameraClearFlags.Depth;
            uiCamera.cullingMask = 1 << LayerMask.NameToLayer("UI");
            uiCamera.orthographic = true;
            uiCamera.depth = mainCamera.depth + 1; // Make sure it renders after Main Camera

            // 5. Create or Find Canvas
            Canvas canvas = FindFirstObjectByType<Canvas>();
            GameObject canvasObj = null;
            if (canvas == null)
            {
                canvasObj = new GameObject("PxlCrush Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }
            else
            {
                canvasObj = canvas.gameObject;
            }

            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = uiCamera;

            // 6. Create Raw Image inside the Canvas
            GameObject rawImageObj = GameObject.Find("PxlCrush Screen");
            RawImage rawImage = null;
            if (rawImageObj == null)
            {
                rawImageObj = new GameObject("PxlCrush Screen");
                rawImageObj.transform.SetParent(canvas.transform, false);
                rawImage = rawImageObj.AddComponent<RawImage>();
            }
            else
            {
                rawImage = rawImageObj.GetComponent<RawImage>();
            }

            // Set raw image to stretch and cover full screen
            RectTransform rectTransform = rawImage.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            // Assign texture and material
            rawImage.texture = renderTexture;

            if (pxlCrushMaterial == null)
            {
#if UNITY_EDITOR
                string matPath = "Assets/Shader/Pxl Crush Effect/Pxl Crush Material.mat";
                pxlCrushMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(matPath);
#endif
            }

            if (pxlCrushMaterial != null)
            {
                rawImage.material = pxlCrushMaterial;
            }
            else
            {
                Debug.LogWarning("[PxlCrushSetup] Pxl Crush Material could not be loaded automatically. Please assign it manually to the Raw Image.");
            }

            Debug.Log("[PxlCrushSetup] Pixelation system set up successfully! Press Play to test.");
        }
    }
}
