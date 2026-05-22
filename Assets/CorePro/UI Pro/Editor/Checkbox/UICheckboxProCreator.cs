#if UNITY_EDITOR

namespace CorePro.UI
{
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UI;

    public class UICheckboxProCreator : UnityEditor.Editor
    {
        [MenuItem("GameObject/UI Pro/CheckboxPro", false, 7)]
        static void CreateCheckboxProObject(MenuCommand menuCommand)
        {
            GameObject parentCanvas = FindOrCreateCanvas();

            // 1. Root - CheckboxPro
            GameObject checkboxProObject = new GameObject(
                "CheckboxPro",
                typeof(RectTransform),
                typeof(Animator),
                typeof(UICheckboxPro)
            );

            RectTransform rootRect = checkboxProObject.GetComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(160, 40);

            // 2. OFF - empty object with CanvasGroup (active = true, default state)
            GameObject stateOffObject = new GameObject("OFF", typeof(RectTransform), typeof(CanvasGroup));
            stateOffObject.transform.SetParent(checkboxProObject.transform, false);
            StretchToFill(stateOffObject.GetComponent<RectTransform>());
            stateOffObject.SetActive(true);

            // 2a. Background under OFF
            GameObject offBackground = new GameObject("Background", typeof(Image));
            offBackground.transform.SetParent(stateOffObject.transform, false);
            StretchToFill(offBackground.GetComponent<RectTransform>());

            // 3. ON - empty object with CanvasGroup (active = false, checkbox is unchecked)
            GameObject stateOnObject = new GameObject("ON", typeof(RectTransform), typeof(CanvasGroup));
            stateOnObject.transform.SetParent(checkboxProObject.transform, false);
            StretchToFill(stateOnObject.GetComponent<RectTransform>());
            stateOnObject.SetActive(false);

            // 3a. Background under ON
            GameObject onBackground = new GameObject("Background", typeof(Image));
            onBackground.transform.SetParent(stateOnObject.transform, false);
            StretchToFill(onBackground.GetComponent<RectTransform>());

            // 3b. Checkbox under ON - Image with built-in Checkmark sprite + CanvasGroup → checkmarkCG
            GameObject checkboxMark = new GameObject("Checkbox", typeof(Image), typeof(CanvasGroup));
            checkboxMark.transform.SetParent(stateOnObject.transform, false);
            RectTransform checkboxMarkRect = checkboxMark.GetComponent<RectTransform>();
            checkboxMarkRect.anchorMin        = new Vector2(0.5f, 0.5f);
            checkboxMarkRect.anchorMax        = new Vector2(0.5f, 0.5f);
            checkboxMarkRect.pivot            = new Vector2(0.5f, 0.5f);
            checkboxMarkRect.sizeDelta        = new Vector2(20f, 20f);
            checkboxMarkRect.anchoredPosition = new Vector2(50f, 0f);
            checkboxMark.GetComponent<Image>().sprite =
                AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd");

            // 4. Link CanvasGroup references to UICheckboxPro
            UICheckboxPro checkboxPro = checkboxProObject.GetComponent<UICheckboxPro>();
            SerializedObject so = new SerializedObject(checkboxPro);
            so.FindProperty("stateOnGO")  .objectReferenceValue = stateOnObject.GetComponent<CanvasGroup>();
            so.FindProperty("stateOffGO") .objectReferenceValue = stateOffObject.GetComponent<CanvasGroup>();
            so.FindProperty("checkmarkCG").objectReferenceValue = checkboxMark.GetComponent<CanvasGroup>();
            so.FindProperty("isOn")       .boolValue            = false;
            so.ApplyModifiedPropertiesWithoutUndo();

            // 5. Hierarchy, Undo, Selection
            GameObjectUtility.SetParentAndAlign(
                checkboxProObject,
                menuCommand.context as GameObject ?? parentCanvas
            );

            Undo.RegisterCreatedObjectUndo(checkboxProObject, "Create CheckboxPro");
            Selection.activeObject = checkboxProObject;
        }

        private static void StretchToFill(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static GameObject FindOrCreateCanvas()
        {
            Canvas existingCanvas;

#if UNITY_6000_0_OR_NEWER
            existingCanvas = Object.FindAnyObjectByType<Canvas>();
#else
            existingCanvas = Object.FindObjectOfType<Canvas>();
#endif

            if (existingCanvas != null)
                return existingCanvas.gameObject;

            GameObject newCanvasObject = new GameObject(
                "Canvas",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster)
            );

            Canvas canvas = newCanvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            Undo.RegisterCreatedObjectUndo(newCanvasObject, "Create Canvas");

            return newCanvasObject;
        }
    }
}
#endif
