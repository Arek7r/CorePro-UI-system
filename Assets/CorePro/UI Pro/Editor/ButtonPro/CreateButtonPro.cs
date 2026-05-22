using CorePro;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;

namespace CorePro
{
    public class CreateButtonPro : MonoBehaviour
    {
        [MenuItem("GameObject/UI Pro/ButtonPro", false, 7)] 
        static void CreateButtonProObject(MenuCommand menuCommand)
        {
            // Ensure the ButtonPro is created under a Canvas
            GameObject parentCanvas = FindOrCreateCanvas();

            // Create a new UI ButtonPro object
            GameObject buttonProObject = new GameObject("ButtonPro", typeof(RectTransform));
            RectTransform buttonRectTransform = buttonProObject.GetComponent<RectTransform>();
            buttonRectTransform.sizeDelta = new Vector2(160, 30); // Set button dimensions

            // Add the ButtonPro component
            ButtonPro buttonPro = buttonProObject.AddComponent<ButtonPro>();

            // Create a child object with an Image component (Background)
            GameObject backgroundObject = new GameObject("Background", typeof(Image));
            backgroundObject.transform.SetParent(buttonProObject.transform, false);
            RectTransform backgroundRectTransform = backgroundObject.GetComponent<RectTransform>();
            backgroundRectTransform.anchorMin = Vector2.zero;
            backgroundRectTransform.anchorMax = Vector2.one;
            backgroundRectTransform.offsetMin = Vector2.zero;
            backgroundRectTransform.offsetMax = Vector2.zero;

            // Set the background color to white
            Image backgroundImage = backgroundObject.GetComponent<Image>();
            backgroundImage.color = Color.white; // White color
            buttonPro.images.Add(backgroundImage); // Add the background to the ButtonPro images list

            // Create a child object with a TextMeshProUGUI component (Text)
            GameObject textObject = new GameObject("Text", typeof(TextMeshProUGUI));
            textObject.transform.SetParent(buttonProObject.transform, false);
            RectTransform textRectTransform = textObject.GetComponent<RectTransform>();
            textRectTransform.anchorMin = Vector2.zero;
            textRectTransform.anchorMax = Vector2.one;
            textRectTransform.offsetMin = Vector2.zero;
            textRectTransform.offsetMax = Vector2.zero;

            // Configure the TextMeshProUGUI component
            TextMeshProUGUI tmp = textObject.GetComponent<TextMeshProUGUI>();
            tmp.text = "ButtonPro"; // Default text
            tmp.alignment = TextAlignmentOptions.Center; // Center align the text
            tmp.color = Color.black; // Black text color
            tmp.enableAutoSizing = true;
            buttonPro.texts.Add(tmp); // Add the text to the ButtonPro texts list

            //Update ButtonPro Images States:
            buttonPro.ValidateImageColorsList();
            buttonPro.useStateColorsForImage = true;
            buttonPro.imageColors[0].useNormal = true;
            buttonPro.imageColors[0].useHighlighted = true;
            buttonPro.imageColors[0].usePress = true;
            buttonPro.imageColors[0].useInactive = true;

            //Update ButtonPro Text States:
            buttonPro.useStateTexts = true;
            
            // Set the ButtonPro object as a child of the Canvas
            GameObjectUtility.SetParentAndAlign(buttonProObject, parentCanvas);

            // Register Undo for editor functionality
            Undo.RegisterCreatedObjectUndo(buttonProObject, "Create ButtonPro");

            // Select the newly created object
            Selection.activeObject = buttonProObject;
        }

        /// <summary>
        /// Finds an existing Canvas in the scene or creates a new one if none exists.
        /// </summary>
        /// <returns>The Canvas GameObject.</returns>
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

            // Create a new Canvas and set its default properties
            GameObject newCanvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = newCanvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            // Register Undo for the new Canvas
            Undo.RegisterCreatedObjectUndo(newCanvasObject, "Create Canvas");

            return newCanvasObject;
        }
    }
}