#if UNITY_EDITOR
using CorePro.Editor.Framework;
using UnityEditor;
using UnityEngine;

namespace CorePro.UI.Editor
{
    [CustomEditor(typeof(UICheckboxPro))]
    public class UICheckboxProEditor : CoreProEditor
    {
        // == SerializedProperties ==

        // References
        SerializedProperty _stateOnGO;
        SerializedProperty _stateOffGO;
        SerializedProperty _checkmarkCG;
        SerializedProperty _highlightCG;

        // Text Groups
        SerializedProperty _textGroups;

        // Animation
        SerializedProperty _animationMode;
        SerializedProperty _checkboxAnimator;
        SerializedProperty _useFade;
        SerializedProperty _fadeInDuration;
        SerializedProperty _fadeOutDuration;
        SerializedProperty _fadeInCurve;
        SerializedProperty _fadeOutCurve;
        SerializedProperty _stateGoMode;
        SerializedProperty _stateGoFadeInDuration;
        SerializedProperty _stateGoFadeOutDuration;
        SerializedProperty _useColorTransition;
        SerializedProperty _useColorStyleSheet;
        SerializedProperty _colorTransitionSheet;
        SerializedProperty _colorTransitions;
        SerializedProperty _colorDuration;
        SerializedProperty _colorCurve;

        // Save
        SerializedProperty _saveValue;
        SerializedProperty _saveKey;

        // Settings
        SerializedProperty _isOn;
        SerializedProperty _isInteractable;
        SerializedProperty _invokeOnAwake;
        SerializedProperty _useSounds;
        SerializedProperty _highlightSpeed;

        // Events
        SerializedProperty _onValueChanged;
        SerializedProperty _onChecked;
        SerializedProperty _onUnchecked;

        // == Foldouts ==

        bool _foldReferences;
        bool _foldTextGroups;
        bool _foldAnimation;
        bool _foldSave;
        bool _foldSettings;
        bool _foldEvents;

        // == Lifecycle ==

        protected override void OnEnable()
        {
            base.OnEnable();

            // References
            _stateOnGO        = serializedObject.FindProperty("stateOnGO");
            _stateOffGO       = serializedObject.FindProperty("stateOffGO");
            _checkmarkCG      = serializedObject.FindProperty("checkmarkCG");
            _highlightCG      = serializedObject.FindProperty("highlightCG");

            // Text Groups
            _textGroups       = serializedObject.FindProperty("textGroups");

            // Animation
            _animationMode    = serializedObject.FindProperty("animationMode");
            _checkboxAnimator = serializedObject.FindProperty("checkboxAnimator");
            _useFade          = serializedObject.FindProperty("useFade");
            _fadeInDuration   = serializedObject.FindProperty("fadeInDuration");
            _fadeOutDuration  = serializedObject.FindProperty("fadeOutDuration");
            _fadeInCurve            = serializedObject.FindProperty("fadeInCurve");
            _fadeOutCurve           = serializedObject.FindProperty("fadeOutCurve");
            _stateGoMode            = serializedObject.FindProperty("stateGoMode");
            _stateGoFadeInDuration  = serializedObject.FindProperty("stateGoFadeInDuration");
            _stateGoFadeOutDuration = serializedObject.FindProperty("stateGoFadeOutDuration");
            _useColorTransition     = serializedObject.FindProperty("useColorTransition");
            _useColorStyleSheet     = serializedObject.FindProperty("useColorStyleSheet");
            _colorTransitionSheet   = serializedObject.FindProperty("colorTransitionSheet");
            _colorTransitions       = serializedObject.FindProperty("colorTransitions");
            _colorDuration          = serializedObject.FindProperty("colorDuration");
            _colorCurve             = serializedObject.FindProperty("colorCurve");

            // Save
            _saveValue        = serializedObject.FindProperty("saveValue");
            _saveKey          = serializedObject.FindProperty("saveKey");

            // Settings
            _isOn             = serializedObject.FindProperty("isOn");
            _isInteractable   = serializedObject.FindProperty("isInteractable");
            _invokeOnAwake    = serializedObject.FindProperty("invokeOnAwake");
            _useSounds        = serializedObject.FindProperty("useSounds");
            _highlightSpeed   = serializedObject.FindProperty("highlightSpeed");

            // Events
            _onValueChanged   = serializedObject.FindProperty("onValueChanged");
            _onChecked        = serializedObject.FindProperty("onChecked");
            _onUnchecked      = serializedObject.FindProperty("onUnchecked");

            // Foldouts
            _foldReferences = LoadFoldout("References",  true);
            _foldTextGroups = LoadFoldout("TextGroups",  true);
            _foldAnimation  = LoadFoldout("Animation",   true);
            _foldSave       = LoadFoldout("Save",        false);
            _foldSettings   = LoadFoldout("Settings",    true);
            _foldEvents     = LoadFoldout("Events",      false);
        }

        // == Main Draw ==

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            UICheckboxPro cb = (UICheckboxPro)target;

            DrawStatePreview(cb);
            EditorGUILayout.Space(4);

            DrawSection("References",   ref _foldReferences, "References",  DrawReferences);
            DrawSection("Text Groups",   ref _foldTextGroups, "TextGroups",  DrawTextGroups);
            DrawSection("Animation",    ref _foldAnimation,  "Animation",   DrawAnimation);
            DrawSection("Save",         ref _foldSave,       "Save",        DrawSave);
            DrawSection("Settings",     ref _foldSettings,   "Settings",    DrawSettings);
            DrawSection("Events",       ref _foldEvents,     "Events",      DrawEvents);

            if (Application.isPlaying)
                DrawRuntimeControls(cb);

            serializedObject.ApplyModifiedProperties();
        }

        // == State Preview ==

        void DrawStatePreview(UICheckboxPro cb)
        {
            bool isOn           = Application.isPlaying ? cb.IsOn           : _isOn.boolValue;
            bool isInteractable = Application.isPlaying ? cb.IsInteractable : _isInteractable.boolValue;

            Rect rect = GUILayoutUtility.GetRect(
                GUIContent.none, GUIStyle.none,
                GUILayout.Height(Theme.previewBarHeight),
                GUILayout.ExpandWidth(true));

            rect = EditorGUI.IndentedRect(rect);

            // Background
            EditorGUI.DrawRect(rect, Theme.previewBarBg);

            // Kolor stanu
            Color stateColor = !isInteractable
                ? new Color(0.4f, 0.4f, 0.4f, 1f)
                : isOn
                    ? new Color(0.2f, 0.75f, 0.3f, 1f)
                    : new Color(0.75f, 0.25f, 0.2f, 1f);

            // Fill: full width when checked, zero when unchecked
            Rect fillRect = rect;
            fillRect.width *= isOn ? 1f : 0f;
            EditorGUI.DrawRect(fillRect, stateColor);

            DrawBorder(rect, Theme.previewBarBorder);

            // Icon + text
            string icon  = isOn ? "✔" : "✕";
            string label = isOn ? "CHECKED" : "UNCHECKED";
            if (!isInteractable) label += "  (not interactable)";

            DrawLabelWithShadow(rect, $"{icon}  {label}");

            // Handle click to toggle state
            if (Event.current.type == EventType.MouseDown &&
                Event.current.button == 0 &&
                rect.Contains(Event.current.mousePosition) &&
                isInteractable)
            {
                if (Application.isPlaying)
                    cb.Toggle();
                else
                {
                    _isOn.boolValue = !_isOn.boolValue;
                    serializedObject.ApplyModifiedProperties();
                    cb.ForceSetValue(_isOn.boolValue, animated: false);
                    cb.Editor_SnapColors(_isOn.boolValue);
                    EditorUtility.SetDirty(cb);
                    Repaint();
                }
                Event.current.Use();
            }
        }

        // == Section Bodies ==

        void DrawReferences()
        {
            var mode = (UICheckboxPro.AnimationMode)_animationMode.enumValueIndex;

            EditorGUILayout.PropertyField(_stateOnGO,   new GUIContent("State ON  GameObject",
                "Active when the checkbox is checked (Generic mode)."));
            EditorGUILayout.PropertyField(_stateOffGO,  new GUIContent("State OFF GameObject",
                "Active when the checkbox is unchecked (Generic mode)."));
            EditorGUILayout.PropertyField(_checkmarkCG, new GUIContent("Checkmark CanvasGroup",
                "Faded in/out in Generic mode. Optional."));
            EditorGUILayout.PropertyField(_highlightCG, new GUIContent("Highlight CanvasGroup",
                "Faded in/out on hover and select. Optional."));

            if (mode == UICheckboxPro.AnimationMode.Generic && _stateOnGO.objectReferenceValue == null)
                EditorGUILayout.HelpBox("State ON GameObject is not assigned. Checked state will have no visual.", MessageType.Warning);

            if (mode == UICheckboxPro.AnimationMode.Animator && _checkboxAnimator.objectReferenceValue == null)
                EditorGUILayout.HelpBox("Animator mode requires an Animator component to be assigned.", MessageType.Warning);
        }

        void DrawTextGroups()
        {
            // Header info
            // EditorGUILayout.HelpBox(
            //     "Each group holds any number of TMP labels.\n" +
            //     "• SetText(groupName, text)           - sets text in a group\n" +
            //     "• SetText(text)                      - sets text in ALL groups\n" +
            //     "• SetActiveTextGroup(groupName, bool) - show / hide a group\n" +
            //     "• SetActiveTextGroupExclusive(name)  - show one, hide the rest",
            //     MessageType.None);

            EditorGUILayout.Space(2);

            // Array size control
            int currentSize = _textGroups.arraySize;
            int newSize = EditorGUILayout.IntField(
                new GUIContent("Group count", "Number of text groups."),
                currentSize);

            if (newSize != currentSize && newSize >= 0)
                _textGroups.arraySize = newSize;

            EditorGUILayout.Space(2);

            // Draw each group
            for (int i = 0; i < _textGroups.arraySize; i++)
            {
                SerializedProperty groupProp = _textGroups.GetArrayElementAtIndex(i);
                SerializedProperty nameProp  = groupProp.FindPropertyRelative("groupName");
                SerializedProperty labsProp  = groupProp.FindPropertyRelative("labels");

                // Foldout per group using the group's own name
                string foldoutLabel = string.IsNullOrWhiteSpace(nameProp.stringValue)
                    ? $"Group {i}"
                    : nameProp.stringValue;

                groupProp.isExpanded = EditorGUILayout.Foldout(groupProp.isExpanded, foldoutLabel, true);

                if (!groupProp.isExpanded)
                    continue;

                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(nameProp, new GUIContent("Group Name"));

                // Labels array - inline without default header
                int labCount = labsProp.arraySize;
                int newLabCount = EditorGUILayout.IntField(
                    new GUIContent("Labels", "TMP objects in this group."),
                    labCount);

                if (newLabCount != labCount && newLabCount >= 0)
                    labsProp.arraySize = newLabCount;

                EditorGUI.indentLevel++;
                for (int j = 0; j < labsProp.arraySize; j++)
                {
                    EditorGUILayout.PropertyField(
                        labsProp.GetArrayElementAtIndex(j),
                        new GUIContent($"Label {j}"));
                }
                EditorGUI.indentLevel--;

                // Validation - warn about empty or null labels
                bool hasEmpty = false;
                for (int j = 0; j < labsProp.arraySize; j++)
                {
                    if (labsProp.GetArrayElementAtIndex(j).objectReferenceValue == null)
                    { hasEmpty = true; break; }
                }
                if (hasEmpty)
                    EditorGUILayout.HelpBox("One or more labels are not assigned.", MessageType.Warning);

                // Remove button
                EditorGUILayout.Space(2);
                if (GUILayout.Button($"Remove '{foldoutLabel}'"))
                    _textGroups.DeleteArrayElementAtIndex(i);

                EditorGUILayout.Space(2);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(2);
            if (GUILayout.Button("+ Add Text Group"))
            {
                _textGroups.arraySize++;
                SerializedProperty newGroup = _textGroups.GetArrayElementAtIndex(_textGroups.arraySize - 1);
                newGroup.FindPropertyRelative("groupName").stringValue = $"Group {_textGroups.arraySize - 1}";
                newGroup.FindPropertyRelative("labels").arraySize      = 0;
                newGroup.isExpanded = true;
            }

            // Runtime quick-test controls
            if (Application.isPlaying && _textGroups.arraySize > 0)
            {
                EditorGUILayout.Space(4);
                DrawSubHeader("Runtime - Quick Test");

                UICheckboxPro cb = (UICheckboxPro)target;

                for (int i = 0; i < _textGroups.arraySize; i++)
                {
                    SerializedProperty groupProp = _textGroups.GetArrayElementAtIndex(i);
                    string gName = groupProp.FindPropertyRelative("groupName").stringValue;

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(gName, GUILayout.Width(100));

                    if (GUILayout.Button("Show"))
                        cb.SetActiveTextGroup(gName, true);

                    if (GUILayout.Button("Hide"))
                        cb.SetActiveTextGroup(gName, false);

                    if (GUILayout.Button("Exclusive"))
                        cb.SetActiveTextGroupExclusive(gName);

                    EditorGUILayout.EndHorizontal();
                }
            }
        }

        void DrawAnimation()
        {
            EditorGUILayout.PropertyField(_animationMode, new GUIContent("Animation Mode"));

            var mode = (UICheckboxPro.AnimationMode)_animationMode.enumValueIndex;

            EditorGUILayout.Space(2);

            if (mode == UICheckboxPro.AnimationMode.Animator)
            {
                EditorGUILayout.PropertyField(_checkboxAnimator, new GUIContent("Animator",
                    "Must have states: On / Off / On Instant / Off Instant."));
            }
            else if (mode == UICheckboxPro.AnimationMode.Generic)
            {
                // Checkmark CanvasGroup 
                DrawSubHeader("Checkmark CanvasGroup");

                EditorGUILayout.PropertyField(_useFade, new GUIContent("Use Fade",
                    "Fades checkmarkCG in/out. If false - alpha snaps instantly."));

                if (_useFade.boolValue)
                {
                    if (_checkmarkCG.objectReferenceValue == null)
                        EditorGUILayout.HelpBox("Use Fade requires a Checkmark CanvasGroup to be assigned.", MessageType.Warning);


                    DrawSubHeaderH3("Fade In");
                    EditorGUILayout.PropertyField(_fadeInDuration, new GUIContent("Duration (s)"));
                    EditorGUILayout.PropertyField(_fadeInCurve,    new GUIContent("Curve"));

                    DrawSubHeaderH3("Fade Out");
                    EditorGUILayout.PropertyField(_fadeOutDuration, new GUIContent("Duration (s)"));
                    EditorGUILayout.PropertyField(_fadeOutCurve,    new GUIContent("Curve"));
                }

                EditorGUILayout.Space(4);

                // State GameObjects (stateOnGO / stateOffGO) 
                DrawSubHeader("ON/OFF GameObjects");

                EditorGUILayout.PropertyField(_stateGoMode, new GUIContent("Transition Mode",
                    "Instant: SetActive on/off immediately.\nFade: crossfade via CanvasGroup alpha, then SetActive(false)."));

                var stateGoMode = (UICheckboxPro.StateGoMode)_stateGoMode.enumValueIndex;
                if (stateGoMode == UICheckboxPro.StateGoMode.Fade)
                {
                    if (_stateOnGO.objectReferenceValue == null && _stateOffGO.objectReferenceValue == null)
                        EditorGUILayout.HelpBox("Fade mode needs at least one State GO (CanvasGroup) assigned in References.", MessageType.Warning);

                    EditorGUILayout.PropertyField(_stateGoFadeInDuration,  new GUIContent("Fade In Duration (s)",
                        "Duration of the fade when switching to the ON state."));
                    EditorGUILayout.PropertyField(_stateGoFadeOutDuration, new GUIContent("Fade Out Duration (s)",
                        "Duration of the fade when switching to the OFF state."));
                }

                EditorGUILayout.Space(4);

                // Color Transition
                DrawSubHeader("Color Transition");

                EditorGUILayout.PropertyField(_useColorTransition, new GUIContent("Use Color Transition",
                    "Tint target Images from Unchecked color to Checked color."));

                if (_useColorTransition.boolValue)
                {
                    EditorGUILayout.Space(2);
                    EditorGUILayout.PropertyField(_colorDuration, new GUIContent("Color Duration (s)"));
                    EditorGUILayout.PropertyField(_colorCurve,    new GUIContent("Color Curve"));
                    EditorGUILayout.HelpBox(
                        "If a target has ImageRounded, its 'Use Own Colors' is forced off so the tint applies.",
                        MessageType.None);

                    EditorGUILayout.Space(10);
                    EditorGUILayout.PropertyField(_useColorStyleSheet, new GUIContent("Use Style Sheet",
                        "Pick colors from a UIStyleSheet instead of custom Unchecked / Checked values."));

                    if (_useColorStyleSheet.boolValue)
                    {
                        EditorGUILayout.PropertyField(_colorTransitionSheet, new GUIContent("Style Sheet",
                            "Color slots are read from this sheet by index."));

                        if (_colorTransitionSheet.objectReferenceValue == null)
                            EditorGUILayout.HelpBox("Assign a Style Sheet to pick color slots.", MessageType.Warning);
                    }

                    EditorGUILayout.PropertyField(_colorTransitions,
                        new GUIContent("Targets (Image / Unchecked / Checked)"), true);
                }
            }
        }

        void DrawSave()
        {
            EditorGUILayout.PropertyField(_saveValue, new GUIContent("Save Value",
                "Persists the checkbox state in PlayerPrefs between sessions."));

            if (_saveValue.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(_saveKey, new GUIContent("Save Key",
                    "PlayerPrefs key. Must be unique across the project."));
                if (EditorGUI.EndChangeCheck() && string.IsNullOrWhiteSpace(_saveKey.stringValue))
                    _saveKey.stringValue = "Checkbox";

                EditorGUILayout.HelpBox($"PlayerPrefs key: \"Checkbox_{_saveKey.stringValue}\"", MessageType.None);
                EditorGUI.indentLevel--;
            }
        }

        void DrawSettings()
        {
            EditorGUI.BeginChangeCheck();
            bool newIsOn = EditorGUILayout.Toggle(
                new GUIContent("Is On", "Initial state of the checkbox."),
                _isOn.boolValue);
            if (EditorGUI.EndChangeCheck())
                _isOn.boolValue = newIsOn;

            EditorGUILayout.PropertyField(_isInteractable, new GUIContent("Is Interactable"));
            EditorGUILayout.PropertyField(_invokeOnAwake,  new GUIContent("Invoke On Awake",
                "Whether events should be fired on Awake."));
            EditorGUILayout.PropertyField(_useSounds,      new GUIContent("Use Sounds",
                "Requires a UIAudio.Instance in the scene."));

            EditorGUILayout.Space(2);
            DrawSubHeader("Highlight");
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_highlightSpeed, new GUIContent("Speed",
                "Interpolation speed of the Highlight CanvasGroup."));
            EditorGUI.indentLevel--;

            if (_useSounds.boolValue)
                EditorGUILayout.HelpBox("Sounds require UIAudio.Instance in the scene.", MessageType.None);
        }

        void DrawEvents()
        {
            EditorGUILayout.PropertyField(_onValueChanged, new GUIContent("On Value Changed (bool)"));
            EditorGUILayout.PropertyField(_onChecked,      new GUIContent("On Checked"));
            EditorGUILayout.PropertyField(_onUnchecked,    new GUIContent("On Unchecked"));
        }

        // == Runtime Controls ==

        void DrawRuntimeControls(UICheckboxPro cb)
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Runtime Controls", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Toggle"))
                cb.Toggle();

            using (new EditorGUI.DisabledScope(cb.IsOn))
                if (GUILayout.Button("Set Checked"))
                    cb.SetChecked();

            using (new EditorGUI.DisabledScope(!cb.IsOn))
                if (GUILayout.Button("Set Unchecked"))
                    cb.SetUnchecked();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button(cb.IsInteractable ? "Disable" : "Enable"))
                cb.SetInteractable(!cb.IsInteractable);

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField(
                $"IsOn: {cb.IsOn}   Interactable: {cb.IsInteractable}",
                EditorStyles.centeredGreyMiniLabel);
        }

    }

    // Per-element drawer for UICheckboxPro color transition targets.
    // Drawn through the native list so it keeps the standard reorderable look,
    // indentation and foldout. Reads the sibling useColorStyleSheet / colorTransitionSheet
    // fields to decide between custom Color fields and style-sheet slot dropdowns.
    [CustomPropertyDrawer(typeof(UICheckboxPro.ColorTransitionEntry))]
    public class CheckboxColorTransitionEntryDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float line    = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;

            if (!property.isExpanded)
                return line;

            // Foldout + Target + Unchecked + Checked
            return (line + spacing) * 4f;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            float line    = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;

            Rect foldRect = new Rect(position.x, position.y, position.width, line);
            property.isExpanded = EditorGUI.Foldout(foldRect, property.isExpanded, label, true);

            if (!property.isExpanded)
                return;

            EditorGUI.indentLevel++;

            float y = position.y + line + spacing;
            Rect r0 = new Rect(position.x, y, position.width, line);
            EditorGUI.PropertyField(r0, property.FindPropertyRelative("target"), new GUIContent("Target"));

            SerializedObject so = property.serializedObject;
            bool useSheet = so.FindProperty("useColorStyleSheet").boolValue;
            UIStyleSheet sheet = so.FindProperty("colorTransitionSheet").objectReferenceValue as UIStyleSheet;

            y += line + spacing;
            Rect r1 = new Rect(position.x, y, position.width, line);

            y += line + spacing;
            Rect r2 = new Rect(position.x, y, position.width, line);

            if (useSheet && sheet != null)
            {
                string[] names = BuildColorNames(sheet);
                DrawSlotRow(r1, "Unchecked", sheet, names, property.FindPropertyRelative("slotUnchecked"));
                DrawSlotRow(r2, "Checked",   sheet, names, property.FindPropertyRelative("slotChecked"));
            }
            else
            {
                EditorGUI.PropertyField(r1, property.FindPropertyRelative("colorUnchecked"), new GUIContent("Unchecked"));
                EditorGUI.PropertyField(r2, property.FindPropertyRelative("colorChecked"),   new GUIContent("Checked"));
            }

            EditorGUI.indentLevel--;
        }

        static void DrawSlotRow(Rect r, string label, UIStyleSheet sheet, string[] colorNames, SerializedProperty slotProp)
        {
            const float swatchW = 18f;
            const float gap     = 4f;

            Rect swatchRect = new Rect(r.xMax - swatchW, r.y, swatchW, r.height);
            Rect popupRect  = new Rect(r.x, r.y, swatchRect.x - r.x - gap, r.height);

            int cur  = Mathf.Clamp(slotProp.intValue, 0, colorNames.Length - 1);
            int next = EditorGUI.Popup(popupRect, label, cur, colorNames);
            if (next != cur)
                slotProp.intValue = next;

            EditorGUI.DrawRect(swatchRect, sheet.GetColor(next));
        }

        static string[] BuildColorNames(UIStyleSheet sheet)
        {
            if (sheet == null)
                return new[] { "-" };

            var names = new string[sheet.Colors.Count];
            for (int i = 0; i < sheet.Colors.Count; i++)
            {
                names[i] = string.IsNullOrWhiteSpace(sheet.Colors[i].name)
                    ? $"Slot {i}"
                    : $"{i}: {sheet.Colors[i].name}";
            }
            return names;
        }
    }
}
#endif