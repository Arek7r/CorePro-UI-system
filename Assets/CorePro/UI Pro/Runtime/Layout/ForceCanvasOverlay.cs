using UnityEngine;

namespace CorePro.UI
{
    /// <summary>
    /// Forces Canvas to use Screen Space - Overlay render mode on awake.
    /// Useful for UI canvases that need to always render on top.
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public class ForceCanvasOverlay : MonoBehaviour
    {
        [SerializeField]
        private Canvas canvas;

        private void Awake()
        {
            canvas ??= GetComponent<Canvas>();
            
            if (canvas == null)
            {
                Debug.LogError("ForceCanvasOverlay: Canvas component not found!", this);
                return;
            }

            // Force overlay mode
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }
    }
}