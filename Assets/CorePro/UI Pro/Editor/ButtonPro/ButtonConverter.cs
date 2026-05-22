#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using System.Collections.Generic;
using CorePro;

namespace CorePro.Editor
{
    public static class ButtonConverter
    {
        [MenuItem("CONTEXT/Button/Convert to ButtonPro")]
        private static void ConvertToButtonPro(MenuCommand command)
        {
            Button button = command.context as Button;
            
            if (button == null)
            {
                Debug.LogError("[ButtonConverter] Failed to get Button component from context.");
                return;
            }

            GameObject buttonObject = button.gameObject;
            
            // We register Undo for the entire process
            Undo.RegisterFullObjectHierarchyUndo(buttonObject, "Convert Button to ButtonPro");
            
            try
            {
                // We collect data from the original button BEFORE deletion.
                bool wasInteractable = button.interactable;
                var onClick = button.onClick;
                
                // We find Image and Text in the hierarchy
                Image targetImage = button.targetGraphic as Image;
                if (targetImage == null)
                    targetImage = buttonObject.GetComponent<Image>();
                
                Text legacyText = buttonObject.GetComponentInChildren<Text>();
                TextMeshProUGUI tmpText = buttonObject.GetComponentInChildren<TextMeshProUGUI>();
                
                // List of all Images in children
                List<Image> allImages = new List<Image>(buttonObject.GetComponentsInChildren<Image>(true));
                
                // Remove the original Button
                Undo.DestroyObjectImmediate(button);
                
                // We add ButtonPro
                ButtonPro buttonPro = Undo.AddComponent<ButtonPro>(buttonObject);
                
                // We are configuring ButtonPro
                buttonPro.interactable = wasInteractable;
                
                // We copy OnClick events
                if (onClick != null && onClick.GetPersistentEventCount() > 0)
                {
                    CopyUnityEvent(onClick, buttonPro.onClick);
                }
                
                // Add images to the list
                if (allImages.Count > 0)
                {
                    buttonPro.images.Clear();
                    foreach (var img in allImages)
                    {
                        if (img != null)
                            buttonPro.images.Add(img);
                    }
                }
                
                // We add texts to the list
                buttonPro.texts.Clear();
                
                if (tmpText != null)
                {
                    buttonPro.texts.Add(tmpText);
                }
                else if (legacyText != null)
                {
                    // We convert Text to TextMeshProUGUI
                    TextMeshProUGUI newTMP = ConvertTextToTMP(legacyText);
                    if (newTMP != null)
                        buttonPro.texts.Add(newTMP);
                }
                
                // We validate the lists
                buttonPro.ValidateImageColorsList();
                buttonPro.ValidateTextColorsList();
                
                // Set default colours for Image
                if (buttonPro.images.Count > 0)
                {
                    buttonPro.useStateColorsForImage = true;
                    buttonPro.imageColors[0].useNormal = true;
                    buttonPro.imageColors[0].useHighlighted = true;
                    buttonPro.imageColors[0].usePress = true;
                    buttonPro.imageColors[0].useInactive = true;
                    
                    // We synchronise colours with the original Button
                    if (targetImage != null)
                    {
                        buttonPro.imageColors[0].normal = targetImage.color;
                        buttonPro.imageColors[0].highlighted = new Color(
                            targetImage.color.r * 0.9f,
                            targetImage.color.g * 0.9f,
                            targetImage.color.b * 0.9f,
                            targetImage.color.a
                        );
                        buttonPro.imageColors[0].press = new Color(
                            targetImage.color.r * 0.7f,
                            targetImage.color.g * 0.7f,
                            targetImage.color.b * 0.7f,
                            targetImage.color.a
                        );
                        buttonPro.imageColors[0].inactive = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                    }
                }
                
                // We mark the object as changed
                EditorUtility.SetDirty(buttonObject);
                
                // We save the changes
                if (PrefabUtility.IsPartOfPrefabInstance(buttonObject))
                {
                    PrefabUtility.RecordPrefabInstancePropertyModifications(buttonObject);
                }
                
                Debug.Log($"[ButtonConverter] Successfully converted '{buttonObject.name}' to ButtonPro. " +
                          $"Images: {buttonPro.images.Count}, Texts: {buttonPro.texts.Count}");
                
                // Select the object in the hierarchy
                Selection.activeGameObject = buttonObject;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ButtonConverter] Error during conversion: {ex.Message}\n{ex.StackTrace}");
                Undo.PerformUndo(); // We undo changes in case of an error
            }
        }
        
        /// <summary>
        /// Copies UnityEvent from one object to another
        /// </summary>
        private static void CopyUnityEvent(Button.ButtonClickedEvent source, UnityEngine.Events.UnityEvent target)
        {
            if (source == null || target == null)
                return;
            
            int count = source.GetPersistentEventCount();
            
            for (int i = 0; i < count; i++)
            {
                var targetObj = source.GetPersistentTarget(i);
                var methodName = source.GetPersistentMethodName(i);
                
                if (targetObj != null && !string.IsNullOrEmpty(methodName))
                {
                    UnityEditor.Events.UnityEventTools.AddPersistentListener(target, 
                        (UnityEngine.Events.UnityAction)System.Delegate.CreateDelegate(
                            typeof(UnityEngine.Events.UnityAction), 
                            targetObj, 
                            methodName, 
                            false, 
                            false));
                }
            }
        }
        
        /// <summary>
        /// Converts legacy Text to TextMeshProUGUI
        /// </summary>
        private static TextMeshProUGUI ConvertTextToTMP(Text legacyText)
        {
            if (legacyText == null)
                return null;
            
            GameObject textObject = legacyText.gameObject;
            
            // We collect data from Text
            string text = legacyText.text;
            Color color = legacyText.color;
            int fontSize = legacyText.fontSize;
            FontStyle fontStyle = legacyText.fontStyle;
            TextAnchor alignment = legacyText.alignment;
            bool raycastTarget = legacyText.raycastTarget;
            
            // Save RectTransform
            RectTransform rt = textObject.GetComponent<RectTransform>();
            Vector2 anchorMin = rt.anchorMin;
            Vector2 anchorMax = rt.anchorMax;
            Vector2 offsetMin = rt.offsetMin;
            Vector2 offsetMax = rt.offsetMax;
            
            // Remove old text
            Undo.DestroyObjectImmediate(legacyText);
            
            // Add TextMeshProUGUI
            TextMeshProUGUI tmp = Undo.AddComponent<TextMeshProUGUI>(textObject);
            
            // We restore RectTransform (it sometimes breaks)
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            
            // Copy properties
            tmp.text = text;
            tmp.color = color;
            tmp.fontSize = fontSize;
            tmp.fontStyle = ConvertFontStyle(fontStyle);
            tmp.alignment = ConvertTextAlignment(alignment);
            tmp.raycastTarget = raycastTarget;
            tmp.enableAutoSizing = true;
            
            EditorUtility.SetDirty(textObject);
            
            Debug.Log($"[ButtonConverter] Converted Text to TextMeshProUGUI on '{textObject.name}'");
            
            return tmp;
        }
        
        /// <summary>
        /// Converts FontStyle from Text to TMP
        /// </summary>
        private static FontStyles ConvertFontStyle(FontStyle style)
        {
            switch (style)
            {
                case FontStyle.Bold:
                    return FontStyles.Bold;
                case FontStyle.Italic:
                    return FontStyles.Italic;
                case FontStyle.BoldAndItalic:
                    return FontStyles.Bold | FontStyles.Italic;
                default:
                    return FontStyles.Normal;
            }
        }
        
        /// <summary>
        /// Converts TextAnchor from Text to TextAlignmentOptions (TMP)
        /// </summary>
        private static TextAlignmentOptions ConvertTextAlignment(TextAnchor alignment)
        {
            switch (alignment)
            {
                case TextAnchor.UpperLeft:
                    return TextAlignmentOptions.TopLeft;
                case TextAnchor.UpperCenter:
                    return TextAlignmentOptions.Top;
                case TextAnchor.UpperRight:
                    return TextAlignmentOptions.TopRight;
                case TextAnchor.MiddleLeft:
                    return TextAlignmentOptions.Left;
                case TextAnchor.MiddleCenter:
                    return TextAlignmentOptions.Center;
                case TextAnchor.MiddleRight:
                    return TextAlignmentOptions.Right;
                case TextAnchor.LowerLeft:
                    return TextAlignmentOptions.BottomLeft;
                case TextAnchor.LowerCenter:
                    return TextAlignmentOptions.Bottom;
                case TextAnchor.LowerRight:
                    return TextAlignmentOptions.BottomRight;
                default:
                    return TextAlignmentOptions.Center;
            }
        }
        
        /// <summary>
        /// Validation - checks whether the option should be active
        /// </summary>
        [MenuItem("CONTEXT/Button/Convert to ButtonPro", true)]
        private static bool ValidateConvertToButtonPro(MenuCommand command)
        {
            // This option is only available if we have Button (not ButtonPro).
            Button button = command.context as Button;
            return button != null && button.GetType() == typeof(Button);
        }
    }
}
#endif