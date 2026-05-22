using System;
using CorePro.Editor.Framework;
using InspectorPro.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace CorePro.Editor
{
    [CustomEditor(typeof(ButtonPro), true)]
    [CanEditMultipleObjects]
    public class ButtonProEditor : CoreProEditor
    {
        #region Variables

        private ButtonPro buttonPro;
        private SerializedProperty _isSelected;
        private SerializedProperty buttonPressStateAt;

        // State colors image
        private SerializedProperty useStateColorsForImage;
        private SerializedProperty useIndividualImageColors;
        private SerializedProperty imageColors;

        // State colors Text
        private SerializedProperty useStateColorsForText;
        private SerializedProperty useIndividualTextColors;
        private SerializedProperty textColors;

        // State Sprites
        private SerializedProperty useStateSprites;
        private SerializedProperty useIndividualSprites;
        private SerializedProperty disableObjOnEmptySprite;
        private SerializedProperty stateSprites;

        // State Texts
        private SerializedProperty useStateTexts;
        private SerializedProperty useIndividualTexts;
        private SerializedProperty disableObjOnEmptyText;
        private SerializedProperty stateTexts;

        // State Group objects
        private SerializedProperty useStateObjectGroup;
        private SerializedProperty stateObjectsGroups;

        // Animation
        private SerializedProperty animationMode;
        private SerializedProperty stateAnimator;
        private SerializedProperty animNormalTrigger;
        private SerializedProperty animHighlightTrigger;
        private SerializedProperty animPressTrigger;
        private SerializedProperty animInactiveTrigger;
        private SerializedProperty genericClickAnim;
        private SerializedProperty genericClickScale;
        private SerializedProperty genericClickDuration;
        private SerializedProperty genericHoverAnim;
        private SerializedProperty genericHoverScale;
        private SerializedProperty genericHoverDuration;
        private SerializedProperty genericInactiveAnim;
        private SerializedProperty genericInactiveAlpha;
        private SerializedProperty genericInactiveDuration;

        // Groups
        private SerializedProperty useInteractableGroups;
        private SerializedProperty enableWhenInteractable;
        private SerializedProperty enableWhenNotInteractable;

        // Events
        private SerializedProperty onClick;
        private SerializedProperty onClickDown;
        private SerializedProperty onHighlighted;
        private SerializedProperty onInteractable;
        private SerializedProperty onNoInteractable;
        private SerializedProperty onInteractableChanged;

        // Lists
        private SerializedProperty images;
        private SerializedProperty texts;
        private GUIListPro ListProImages;
        private GUIListPro ListProTexts;

        // Foldout states
        private bool _foldColorsImage;
        private bool _foldColorsText;
        private bool _foldSprites;
        private bool _foldTexts;
        private bool _foldStateGroups;
        private bool _foldInteractable;
        private bool _foldAnim;
        private bool _foldEvents;
        private bool _foldDebug;

        // Temp
        private Sprite tempSprite;

        #endregion

        protected override void OnEnable()
        {
            if (target == null) return;

            base.OnEnable();

            buttonPro = (ButtonPro)target;
            _isSelected       = serializedObject.FindProperty("_isSelected");
            buttonPressStateAt = serializedObject.FindProperty("buttonPressStateAt");

            AssignSerializedProperties();

            ListProImages = new GUIListPro(buttonPro.images, serializedObject, serializedObject.FindProperty("images"), "Images");
            ListProTexts  = new GUIListPro(buttonPro.texts,  serializedObject, serializedObject.FindProperty("texts"),  "Texts");

            _foldColorsImage  = LoadFoldout("ColorsImage",        false);
            _foldColorsText   = LoadFoldout("ColorsText",         false);
            _foldSprites      = LoadFoldout("Sprites",            false);
            _foldTexts        = LoadFoldout("Texts",              false);
            _foldStateGroups  = LoadFoldout("StateGroups",        false);
            _foldInteractable = LoadFoldout("InteractableGroups", false);
            _foldAnim         = LoadFoldout("Anim",               false);
            _foldEvents       = LoadFoldout("Events",             false);
            _foldDebug        = LoadFoldout("Debug",              false);
        }

        private void AssignSerializedProperties()
        {
            var fields = GetType().GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            foreach (var field in fields)
            {
                if (field.FieldType == typeof(SerializedProperty))
                {
                    var property = serializedObject.FindProperty(field.Name);
                    if (property != null)
                        field.SetValue(this, property);
                }
            }
        }

        public override void OnInspectorGUI()
        {
            if (target == null) return;

            try
            {
                serializedObject.Update();

                DrawStatePreview();
                EditorGUILayout.Space(4);
                DrawLists();

                serializedObject.ApplyModifiedProperties();

                bool isAnimActive = ComputeIsAnimActive();

                DrawSectionWithToggle("Colors Image",        ref _foldColorsImage,   "ColorsImage",        useStateColorsForImage,  DrawColorsImageBody);
                DrawSectionWithToggle("Colors Text",         ref _foldColorsText,    "ColorsText",         useStateColorsForText,   DrawColorsTextBody);
                DrawSectionWithToggle("Sprites",             ref _foldSprites,       "Sprites",            useStateSprites,         DrawSpritesBody);
                DrawSectionWithToggle("Texts",               ref _foldTexts,         "Texts",              useStateTexts,           DrawTextsBody);
                DrawSectionWithToggle("State Groups",        ref _foldStateGroups,   "StateGroups",        useStateObjectGroup,     DrawStateGroupsBody);
                DrawSectionWithToggle("Interactable Groups", ref _foldInteractable,  "InteractableGroups", useInteractableGroups,   DrawGroupsBody);
                DrawSectionWithToggle("Animations",          ref _foldAnim,          "Anim",               isAnimActive,              DrawAnimBody);
                DrawSectionWithToggle("Events",              ref _foldEvents,        "Events",             HasAnyEventListeners(),    DrawEventsBody);
                DrawSection          ("Debug",               ref _foldDebug,         "Debug",              DrawDebugBody);

                DrawMiniButtons();

                serializedObject.ApplyModifiedProperties();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        #region State Preview

        private void DrawStatePreview()
        {
            Rect rect = GUILayoutUtility.GetRect(
                GUIContent.none, GUIStyle.none,
                GUILayout.Height(Theme.previewBarHeight),
                GUILayout.ExpandWidth(true));
            rect = EditorGUI.IndentedRect(rect);

            Color stateColor = buttonPro.currentState switch
            {
                ButtonPro.ButtonState.Normal      => new Color(0.2f,  0.75f, 0.3f,  1f),
                ButtonPro.ButtonState.Highlighted => new Color(0.25f, 0.55f, 1.0f,  1f),
                ButtonPro.ButtonState.Pressed     => new Color(0.85f, 0.45f, 0.1f,  1f),
                ButtonPro.ButtonState.Inactive    => new Color(0.4f,  0.4f,  0.4f,  1f),
                _ => new Color(0.2f, 0.75f, 0.3f, 1f)
            };

            EditorGUI.DrawRect(rect, Theme.previewBarBg);
            EditorGUI.DrawRect(rect, new Color(stateColor.r, stateColor.g, stateColor.b, 0.25f));
            DrawBorder(rect, stateColor, 2f);
            DrawLabelWithShadow(rect, $"●  {buttonPro.currentState.ToString().ToUpper()}");

            EditorGUILayout.Space(6);

            EditorGUI.BeginChangeCheck();
            bool newInteractable = EditorGUILayout.Toggle("Interactable", buttonPro.interactable);
            if (buttonPressStateAt != null)
                EditorGUILayout.PropertyField(buttonPressStateAt, new GUIContent("Invoke OnClick at"));
            bool newSelected = EditorGUILayout.Toggle("Is Selected (Lock)", buttonPro.IsSelected);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(buttonPro, "Change ButtonPro Basics");
                buttonPro.interactable = newInteractable;
                buttonPro.IsSelected   = newSelected;
                EditorUtility.SetDirty(buttonPro);
            }
        }

        #endregion

        #region Lists

        private void DrawLists()
        {
            try
            {
                EditorGUILayout.Space(2);
                ListProImages.DoLayoutList();
                EditorGUILayout.Space(2);
                ListProTexts.DoLayoutList();
                EditorGUILayout.Space(2);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        #endregion

        #region Section Bodies

        private void DrawColorsImageBody()
        {
            buttonPro.ValidateImageColorsList();

            if (!IsArrayElementValid(images, 0)) return;

            DrawSubHeader("Options");
            EditorInspectorExtensions.DrawCustomToggle(useIndividualImageColors, "Use Individual Image Colors", null, 5f, 5, 5);

            int count = useIndividualImageColors.boolValue ? imageColors.arraySize : (imageColors.arraySize > 0 ? 1 : 0);

            for (int i = 0; i < count; i++)
            {
                if (i >= buttonPro.images.Count || buttonPro.images[i] == null) continue;

                SerializedProperty propSet = imageColors.GetArrayElementAtIndex(i);
                string label = useIndividualImageColors.boolValue ? $"Image: {buttonPro.images[i].name}" : "All images";

                EditorGUI.BeginChangeCheck();
                DrawPropertySetIndividual(propSet, false, label);

                if (!Application.isPlaying && EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();
                    SyncImageColorWithState(buttonPro.images[i], propSet);
                }
            }
        }

        private void DrawColorsTextBody()
        {
            buttonPro.ValidateTextColorsList();

            if (!IsArrayElementValid(texts, 0)) return;

            DrawSubHeader("Options");
            EditorInspectorExtensions.DrawCustomToggle(useIndividualTextColors, "Use Individual Text Colors", null, 5f);

            int count = useIndividualTextColors.boolValue ? textColors.arraySize : (textColors.arraySize > 0 ? 1 : 0);

            for (int i = 0; i < count; i++)
            {
                if (i >= buttonPro.texts.Count || buttonPro.texts[i] == null) continue;

                SerializedProperty propSet = textColors.GetArrayElementAtIndex(i);
                string label = useIndividualTextColors.boolValue ? $"Text: {buttonPro.texts[i].name}" : "All texts";

                EditorGUI.BeginChangeCheck();
                DrawPropertySetIndividual(propSet, true, label);

                if (!Application.isPlaying && EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();
                    SyncTextColorWithState(buttonPro.texts[i], propSet);
                }
            }
        }

        private void DrawSpritesBody()
        {
            buttonPro.ValidateStateSpriteList();

            if (!IsArrayElementValid(images, 0)) return;

            DrawSubHeader("Options");
            EditorInspectorExtensions.DrawCustomToggle(useIndividualSprites, "Use Individual Sprites", null, 5f, 0);
            EditorInspectorExtensions.DrawCustomToggle(disableObjOnEmptySprite, "Disable object if no sprite is set for the used state", null, 5f, 5, 0);

            if (useIndividualSprites.boolValue)
            {
                for (int i = 0; i < stateSprites.arraySize; i++)
                {
                    if (i >= buttonPro.stateSprites.Count || i >= buttonPro.images.Count || buttonPro.images[i] == null) continue;

                    SerializedProperty propSet   = stateSprites.GetArrayElementAtIndex(i);
                    SerializedProperty normalProp = propSet.FindPropertyRelative("normal");

                    if (!Application.isPlaying)
                    {
                        // Pre-fill normal only if it's not set yet - don't override existing assignment
                        if (normalProp.objectReferenceValue == null && buttonPro.images[i].sprite != null)
                        {
                            normalProp.objectReferenceValue = buttonPro.images[i].sprite;
                            serializedObject.ApplyModifiedProperties();
                        }

                        EditorGUI.BeginChangeCheck();
                    }

                    DrawPropertySetIndividual(propSet, true, $"Image: {buttonPro.images[i].name}");

                    if (!Application.isPlaying && EditorGUI.EndChangeCheck() && useStateSprites.boolValue)
                    {
                        if ((Sprite)normalProp.objectReferenceValue != buttonPro.images[i].sprite)
                        {
                            Undo.RecordObject(buttonPro.images[i], "Change ButtonPro Image Sprite");
                            buttonPro.images[i].sprite = (Sprite)normalProp.objectReferenceValue;

                            if (disableObjOnEmptySprite.boolValue)
                                buttonPro.images[i].gameObject.SetActive(buttonPro.images[i].sprite != null);

                            serializedObject.ApplyModifiedProperties();
                            EditorUtility.SetDirty(buttonPro.images[i]);
                        }
                    }
                }
            }
            else if (stateSprites.arraySize > 0)
            {
                DrawPropertySetIndividual(stateSprites.GetArrayElementAtIndex(0), false, "All images");
            }
        }

        private void DrawTextsBody()
        {
            buttonPro.ValidateStateTextList();

            if (!IsArrayElementValid(texts, 0)) return;

            DrawSubHeader("Options");
            EditorInspectorExtensions.DrawCustomToggle(useIndividualTexts, "Use Individual Texts", null, 5f, 0);
            EditorInspectorExtensions.DrawCustomToggle(disableObjOnEmptyText, "Disable object if no text is set for the used state", null, 5f);

            if (useIndividualTexts.boolValue)
            {
                for (int i = 0; i < stateTexts.arraySize; i++)
                {
                    if (i >= buttonPro.stateTexts.Count || buttonPro.texts[i] == null) continue;

                    if (!Application.isPlaying)
                    {
                        if (!string.IsNullOrWhiteSpace(buttonPro.texts[i].text))
                        {
                            stateTexts.GetArrayElementAtIndex(i).FindPropertyRelative("normal").stringValue = buttonPro.texts[i].text;
                            serializedObject.ApplyModifiedProperties();
                            EditorGUI.BeginChangeCheck();
                        }
                    }

                    DrawPropertySetIndividual(stateTexts.GetArrayElementAtIndex(i), true, $"Texts: {buttonPro.texts[i].name}");

                    if (!Application.isPlaying && EditorGUI.EndChangeCheck() && useStateTexts.boolValue)
                    {
                        SerializedProperty normalProp = stateTexts.GetArrayElementAtIndex(i).FindPropertyRelative("normal");
                        if (normalProp.stringValue != buttonPro.texts[i].text)
                        {
                            Undo.RecordObject(buttonPro.texts[i], "Change ButtonPro Text");
                            buttonPro.texts[i].text = normalProp.stringValue;

                            if (disableObjOnEmptyText.boolValue)
                                buttonPro.texts[i].gameObject.SetActive(!string.IsNullOrWhiteSpace(buttonPro.texts[i].text));

                            serializedObject.ApplyModifiedProperties();
                            EditorUtility.SetDirty(buttonPro.texts[i]);
                        }
                    }
                }
            }
            else
            {
                if (stateTexts.arraySize > 0)
                {
                    SerializedProperty propSet = stateTexts.GetArrayElementAtIndex(0);

                    if (!Application.isPlaying)
                    {
                        if (!string.IsNullOrWhiteSpace(buttonPro.texts[0].text))
                        {
                            propSet.FindPropertyRelative("normal").stringValue = buttonPro.texts[0].text;
                            serializedObject.ApplyModifiedProperties();
                            EditorGUI.BeginChangeCheck();
                        }
                    }

                    DrawPropertySetIndividual(propSet, false, "All texts");

                    if (!Application.isPlaying && EditorGUI.EndChangeCheck() && useStateTexts.boolValue)
                    {
                        SerializedProperty normalProp = stateTexts.GetArrayElementAtIndex(0).FindPropertyRelative("normal");
                        if (normalProp.stringValue != buttonPro.texts[0].text)
                        {
                            Undo.RecordObject(buttonPro.texts[0], "Change ButtonPro Text");
                            buttonPro.texts[0].text = normalProp.stringValue;

                            if (disableObjOnEmptyText.boolValue)
                                buttonPro.texts[0].gameObject.SetActive(!string.IsNullOrWhiteSpace(buttonPro.texts[0].text));

                            serializedObject.ApplyModifiedProperties();
                            EditorUtility.SetDirty(buttonPro.texts[0]);
                        }
                    }
                }
            }
        }

        private void DrawStateGroupsBody()
        {
            buttonPro.ValidateObjectsList();

            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox("Each group will be switched according to the state", MessageType.None);
            EditorGUILayout.Space(4);

            EditorInspectorExtensions.DrawPropertyWithToggle(stateObjectsGroups.FindPropertyRelative("normal"),      stateObjectsGroups.FindPropertyRelative("useNormal"),      "Normal");
            EditorInspectorExtensions.DrawPropertyWithToggle(stateObjectsGroups.FindPropertyRelative("highlighted"), stateObjectsGroups.FindPropertyRelative("useHighlighted"), "Highlighted");
            EditorInspectorExtensions.DrawPropertyWithToggle(stateObjectsGroups.FindPropertyRelative("press"),       stateObjectsGroups.FindPropertyRelative("usePress"),       "Pressed");
            EditorInspectorExtensions.DrawPropertyWithToggle(stateObjectsGroups.FindPropertyRelative("inactive"),    stateObjectsGroups.FindPropertyRelative("useInactive"),    "Inactive");

            EditorGUILayout.Space(4);
        }

        private void DrawGroupsBody()
        {
            EditorGUILayout.PropertyField(enableWhenInteractable,    true);
            EditorGUILayout.PropertyField(enableWhenNotInteractable, true);
        }

        private void DrawAnimBody()
        {
            var mode = (ButtonPro.AnimationMode)animationMode.enumValueIndex;
            EditorGUILayout.PropertyField(animationMode, new GUIContent("Mode"));

            if (mode == ButtonPro.AnimationMode.Animator)
            {
                EditorGUILayout.PropertyField(stateAnimator);
                EditorGUILayout.PropertyField(animNormalTrigger,    new GUIContent("Normal Trigger"));
                EditorGUILayout.PropertyField(animHighlightTrigger, new GUIContent("Highlighted Trigger"));
                EditorGUILayout.PropertyField(animPressTrigger,     new GUIContent("Pressed Trigger"));
                EditorGUILayout.PropertyField(animInactiveTrigger,  new GUIContent("Inactive Trigger"));
            }
            else if (mode == ButtonPro.AnimationMode.Generic)
            {
                EditorGUILayout.Space(4);
                DrawSubHeader("Click");
                EditorInspectorExtensions.DrawCustomToggle(genericClickAnim, "Enable Click Punch", null, 5f, 0);
                if (genericClickAnim.boolValue)
                {
                    EditorGUILayout.PropertyField(genericClickScale,    new GUIContent("Scale"));
                    EditorGUILayout.PropertyField(genericClickDuration, new GUIContent("Duration"));
                }

                EditorGUILayout.Space(4);
                DrawSubHeader("Hover");
                EditorInspectorExtensions.DrawCustomToggle(genericHoverAnim, "Enable Hover Scale", null, 5f, 0);
                if (genericHoverAnim.boolValue)
                {
                    EditorGUILayout.PropertyField(genericHoverScale,    new GUIContent("Scale"));
                    EditorGUILayout.PropertyField(genericHoverDuration, new GUIContent("Duration"));
                }

                EditorGUILayout.Space(4);
                DrawSubHeader("Inactive");
                EditorInspectorExtensions.DrawCustomToggle(genericInactiveAnim, "Enable Inactive Fade", null, 5f, 0);
                if (genericInactiveAnim.boolValue)
                {
                    EditorGUILayout.PropertyField(genericInactiveAlpha,    new GUIContent("Target Alpha"));
                    EditorGUILayout.PropertyField(genericInactiveDuration, new GUIContent("Duration"));
                }
            }
        }

        private void DrawEventsBody()
        {
            EditorGUILayout.PropertyField(onClick);
            EditorGUILayout.PropertyField(onClickDown);
            EditorGUILayout.PropertyField(onHighlighted);
            EditorGUILayout.PropertyField(onInteractable);
            EditorGUILayout.PropertyField(onNoInteractable);
            EditorGUILayout.PropertyField(onInteractableChanged);
        }

        private void DrawDebugBody()
        {
            if (GUILayout.Button("Set Normal State"))      SetButtonState(ButtonPro.ButtonState.Normal);
            if (GUILayout.Button("Set Highlighted State")) SetButtonState(ButtonPro.ButtonState.Highlighted);
            if (GUILayout.Button("Set Pressed State"))     SetButtonState(ButtonPro.ButtonState.Pressed);
            if (GUILayout.Button("Set Disabled State"))    SetButtonState(ButtonPro.ButtonState.Inactive);
        }

        #endregion

        #region Mini Buttons

        private void DrawMiniButtons()
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Set new state", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            GUIStyle s = EditorStyles.miniButton;
            if (GUILayout.Button("Normal",  s)) SetButtonState(ButtonPro.ButtonState.Normal);
            if (GUILayout.Button("Hover",   s)) SetButtonState(ButtonPro.ButtonState.Highlighted);
            if (GUILayout.Button("Press",   s)) SetButtonState(ButtonPro.ButtonState.Pressed);
            if (GUILayout.Button("Disable", s)) SetButtonState(ButtonPro.ButtonState.Inactive);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(6);
        }

        private void SetButtonState(ButtonPro.ButtonState state)
        {
            buttonPro.SetState(state);
            EditorUtility.SetDirty(buttonPro);
            if (!Application.isPlaying)
                SceneView.RepaintAll();
        }

        #endregion

        #region Sync helpers

        private void SyncImageColorWithState(Image image, SerializedProperty propSet)
        {
            string propertyName = buttonPro.currentState switch
            {
                ButtonPro.ButtonState.Normal      => "normal",
                ButtonPro.ButtonState.Highlighted => "highlighted",
                ButtonPro.ButtonState.Pressed     => "press",
                ButtonPro.ButtonState.Inactive    => "inactive",
                _ => "normal"
            };

            Color targetColor = propSet.FindPropertyRelative(propertyName).colorValue;
            if (image.color != targetColor)
            {
                Undo.RecordObject(image, "Change ButtonPro Image Color");
                image.color = targetColor;
                EditorUtility.SetDirty(image);
                SceneView.RepaintAll();
            }
        }

        private void SyncTextColorWithState(TMPro.TMP_Text text, SerializedProperty propSet)
        {
            string propertyName = buttonPro.currentState switch
            {
                ButtonPro.ButtonState.Normal      => "normal",
                ButtonPro.ButtonState.Highlighted => "highlighted",
                ButtonPro.ButtonState.Pressed     => "press",
                ButtonPro.ButtonState.Inactive    => "inactive",
                _ => "normal"
            };

            Color targetColor = propSet.FindPropertyRelative(propertyName).colorValue;
            if (text.color != targetColor)
            {
                Undo.RecordObject(text, "Change ButtonPro Text Color");
                text.color = targetColor;
                EditorUtility.SetDirty(text);
                SceneView.RepaintAll();
            }
        }

        #endregion

        #region Draw utilities

        private void DrawPropertySetIndividual(SerializedProperty propertySet, bool useIndentLevel, string title = null,
            bool disabled = false, string info = "")
        {
            if (propertySet == null) return;

            if (title != null)
            {
                EditorGUILayout.Space(4);
                DrawSubHeader(title);
            }

            if (disabled)
            {
                if (!string.IsNullOrWhiteSpace(info))
                    EditorGUILayout.LabelField(info, EditorStyles.helpBox);
                GUI.enabled = false;
            }

            EditorInspectorExtensions.DrawPropertyWithToggle(propertySet.FindPropertyRelative("normal"),      null,                                              "Normal");
            EditorInspectorExtensions.DrawPropertyWithToggle(propertySet.FindPropertyRelative("highlighted"), propertySet.FindPropertyRelative("useHighlighted"), "Highlighted");
            EditorInspectorExtensions.DrawPropertyWithToggle(propertySet.FindPropertyRelative("press"),       propertySet.FindPropertyRelative("usePress"),       "Press");
            EditorInspectorExtensions.DrawPropertyWithToggle(propertySet.FindPropertyRelative("inactive"),    propertySet.FindPropertyRelative("useInactive"),    "Inactive");

            if (disabled)
                GUI.enabled = true;

            EditorGUILayout.Space(2);
        }

        private bool IsArrayElementValid(SerializedProperty arrayProperty, int index)
        {
            if (arrayProperty == null || arrayProperty.arraySize == 0)
            {
                EditorGUILayout.LabelField("The reference list is empty or not assigned.");
                return false;
            }

            if (index < 0 || index >= arrayProperty.arraySize)
            {
                EditorGUILayout.LabelField($"Invalid index: {index}.");
                return false;
            }

            SerializedProperty element = arrayProperty.GetArrayElementAtIndex(index);
            if (element == null || element.objectReferenceValue == null)
            {
                EditorGUILayout.LabelField($"Element at index {index} is null. Please assign a valid reference.");
                return false;
            }

            return true;
        }

        private bool ComputeIsAnimActive()
        {
            if (animationMode == null) return false;
            var mode = (ButtonPro.AnimationMode)animationMode.enumValueIndex;
            return mode == ButtonPro.AnimationMode.Animator
                ? stateAnimator != null && stateAnimator.objectReferenceValue != null
                : mode == ButtonPro.AnimationMode.Generic &&
                  (genericClickAnim.boolValue || genericHoverAnim.boolValue || genericInactiveAnim.boolValue);
        }

        private bool HasAnyEventListeners()
        {
            SerializedProperty[] eventProps = { onClick, onClickDown, onHighlighted, onInteractable, onNoInteractable, onInteractableChanged };
            foreach (var ep in eventProps)
            {
                if (ep == null) continue;
                var calls = ep.FindPropertyRelative("m_PersistentCalls.m_Calls");
                if (calls != null && calls.arraySize > 0) return true;
            }
            return false;
        }

        #endregion
    }
}
