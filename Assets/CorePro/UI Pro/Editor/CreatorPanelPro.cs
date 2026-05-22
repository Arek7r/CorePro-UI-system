#if UNITY_EDITOR

namespace CorePro.UI
{
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UI;

    public class CreatePanelPro : UnityEditor.Editor
    {
        [MenuItem("GameObject/UI Pro/PanelPro", false, 7)]
        static void CreatePanelProObject(MenuCommand menuCommand)
        {
            // Find or create a Canvas 
            GameObject parentCanvas = FindOrCreateCanvas();

            // 1. Create the main PanelPro object (RectTransform)
            GameObject panelProObject = new GameObject("PanelPro", typeof(RectTransform));
            RectTransform panelRect = panelProObject.GetComponent<RectTransform>();

            // Set the default size
            panelRect.sizeDelta = new Vector2(200, 200);

            // 2. Create a child Background (Image)
            GameObject backgroundObject = new GameObject("Background", typeof(Image));
            backgroundObject.transform.SetParent(panelProObject.transform, false);

            RectTransform backgroundRect = backgroundObject.GetComponent<RectTransform>();

            // Stretch to Full (Stretch/Stretch)
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;

            // Optional: set the default background colour (e.g. semi-transparent grey/white)
            Image backgroundImage = backgroundObject.GetComponent<Image>();
            backgroundImage.color = new Color(1f, 1f, 1f, 0.4f);

            // Hierarchy and Undo settings
            GameObjectUtility.SetParentAndAlign(panelProObject, menuCommand.context as GameObject ?? parentCanvas);

            Undo.RegisterCreatedObjectUndo(panelProObject, "Create PanelPro");
            Selection.activeObject = panelProObject;
        }

        private static GameObject FindOrCreateCanvas()
        {
            Canvas existingCanvas;

#if UNITY_6000_0_OR_NEWER
            existingCanvas = Object.FindFirstObjectByType<Canvas>();
#else
            existingCanvas = Object.FindObjectOfType<Canvas>();
#endif

            if (existingCanvas != null)
                return existingCanvas.gameObject;

            GameObject newCanvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = newCanvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            Undo.RegisterCreatedObjectUndo(newCanvasObject, "Create Canvas");

            return newCanvasObject;
        }
    }
}
#endif