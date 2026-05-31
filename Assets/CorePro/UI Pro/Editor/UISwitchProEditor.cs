#if UNITY_EDITOR
using CorePro.Editor.Framework;
using UnityEditor;
using UnityEngine;

namespace CorePro.UI.Editor
{
    [CustomEditor(typeof(UISwitchPro))]
    public class UISwitchProEditor : CoreProEditor
    {
        // == SerializedProperties ==

        // References
        SerializedProperty _switchAnimator;
        SerializedProperty _highlightCG;
        SerializedProperty _handleRect;

        // Animation
        SerializedProperty _animationMode;
        SerializedProperty _handlePosOff;
        SerializedProperty _handlePosOn;
        SerializedProperty _handleDuration;
        SerializedProperty _handleCurve;
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
        SerializedProperty _useUINavigation;
        SerializedProperty _highlightSpeed;

        // Events
        SerializedProperty _onValueChanged;
        SerializedProperty _onEvents;
        SerializedProperty _offEvents;

        // == Foldouts ==

        bool _foldReferences;
        bool _foldAnimation;
        bool _foldSave;
        bool _foldSettings;
        bool _foldEvents;

        // == Lifecycle ==

        protected override void OnEnable()
        {
            base.OnEnable();

            // References
            _switchAnimator  = serializedObject.FindProperty("switchAnimator");
            _highlightCG     = serializedObject.FindProperty("highlightCG");
            _handleRect      = serializedObject.FindProperty("handleRect");

            // Animation
            _animationMode   = serializedObject.FindProperty("animationMode");
            _handlePosOff    = serializedObject.FindProperty("handlePosOff");
            _handlePosOn     = serializedObject.FindProperty("handlePosOn");
            _handleDuration  = serializedObject.FindProperty("handleDuration");
            _handleCurve     = serializedObject.FindProperty("handleCurve");
            _useColorTransition = serializedObject.FindProperty("useColorTransition");
            _useColorStyleSheet   = serializedObject.FindProperty("useColorStyleSheet");
            _colorTransitionSheet = serializedObject.FindProperty("colorTransitionSheet");
            _colorTransitions   = serializedObject.FindProperty("colorTransitions");
            _colorDuration      = serializedObject.FindProperty("colorDuration");
            _colorCurve         = serializedObject.FindProperty("colorCurve");

            // Save
            _saveValue       = serializedObject.FindProperty("saveValue");
            _saveKey         = serializedObject.FindProperty("saveKey");

            // Settings
            _isOn            = serializedObject.FindProperty("isOn");
            _isInteractable  = serializedObject.FindProperty("isInteractable");
            _invokeOnAwake   = serializedObject.FindProperty("invokeOnAwake");
            _useSounds       = serializedObject.FindProperty("useSounds");
            _useUINavigation = serializedObject.FindProperty("useUINavigation");
            _highlightSpeed  = serializedObject.FindProperty("highlightSpeed");

            // Events
            _onValueChanged  = serializedObject.FindProperty("onValueChanged");
            _onEvents        = serializedObject.FindProperty("onEvents");
            _offEvents       = serializedObject.FindProperty("offEvents");

            // Foldouts
            _foldReferences = LoadFoldout("References", true);
            _foldAnimation  = LoadFoldout("Animation",  true);
            _foldSave       = LoadFoldout("Save",        false);
            _foldSettings   = LoadFoldout("Settings",    true);
            _foldEvents     = LoadFoldout("Events",      false);
        }

        // == Main Draw ==

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            UISwitchPro sw = (UISwitchPro)target;

            DrawSwitchPreview(sw);
            EditorGUILayout.Space(4);

            DrawSection("References",         ref _foldReferences, "References", DrawReferences);
            DrawSection("Animation",           ref _foldAnimation,  "Animation",  DrawAnimation);
            DrawSection("Save",                ref _foldSave,       "Save",       DrawSave);
            DrawSection("Settings",            ref _foldSettings,   "Settings",   DrawSettings);
            DrawSection("Events",              ref _foldEvents,     "Events",     DrawEvents);

            if (Application.isPlaying)
                DrawRuntimeControls(sw);

            serializedObject.ApplyModifiedProperties();
        }

        // == Switch Preview ==
        // Two ON | OFF buttons side by side.
        // Active state is highlighted, inactive is greyed out.
        // Clicking changes the state in both Edit Mode and Play Mode.

        void DrawSwitchPreview(UISwitchPro sw)
        {
            bool isOn           = Application.isPlaying ? sw.IsOn           : _isOn.boolValue;
            bool isInteractable = Application.isPlaying ? sw.IsInteractable : _isInteractable.boolValue;

            float btnHeight = Theme.previewBarHeight;

            Rect totalRect = GUILayoutUtility.GetRect(
                GUIContent.none, GUIStyle.none,
                GUILayout.Height(btnHeight),
                GUILayout.ExpandWidth(true));
            totalRect = EditorGUI.IndentedRect(totalRect);

            float half = totalRect.width * 0.5f;
            float gap   = 2f;

            Rect rectON  = new Rect(totalRect.x,            totalRect.y, half - gap, btnHeight);
            Rect rectOFF = new Rect(totalRect.x + half + gap, totalRect.y, half - gap, btnHeight);

            // Colors
            Color activeOn   = isInteractable ? new Color(0.18f, 0.62f, 0.28f, 1f) : new Color(0.30f, 0.45f, 0.32f, 1f);
            Color activeOff  = isInteractable ? new Color(0.72f, 0.22f, 0.18f, 1f) : new Color(0.45f, 0.30f, 0.30f, 1f);
            Color inactive   = new Color(0.22f, 0.22f, 0.22f, 1f);
            Color borderCol  = Theme.previewBarBorder;

            // Draw ON button
            EditorGUI.DrawRect(rectON,  isOn  ? activeOn  : inactive);
            DrawBorder(rectON, borderCol);
            DrawLabelWithShadow(rectON, "▶  ON");

            // Draw OFF button
            EditorGUI.DrawRect(rectOFF, !isOn ? activeOff : inactive);
            DrawBorder(rectOFF, borderCol);
            DrawLabelWithShadow(rectOFF, "■  OFF");

            // Click - toggle state
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                if (rectON.Contains(Event.current.mousePosition) && !isOn)
                {
                    if (Application.isPlaying)
                        sw.SetOn();
                    else
                    {
                        _isOn.boolValue = true;
                        serializedObject.ApplyModifiedProperties();
                        sw.Editor_SnapHandle(true);
                        sw.Editor_SnapColors(true);
                        EditorUtility.SetDirty(sw);
                    }
                    Event.current.Use();
                    Repaint();
                }
                else if (rectOFF.Contains(Event.current.mousePosition) && isOn)
                {
                    if (Application.isPlaying)
                        sw.SetOff();
                    else
                    {
                        _isOn.boolValue = false;
                        serializedObject.ApplyModifiedProperties();
                        sw.Editor_SnapHandle(false);
                        sw.Editor_SnapColors(false);
                        EditorUtility.SetDirty(sw);
                    }
                    Event.current.Use();
                    Repaint();
                }
            }
        }

        // == Section Bodies ==

        void DrawReferences()
        {
            EditorGUILayout.PropertyField(_handleRect, new GUIContent("Handle RectTransform",
                "The knob or circle that slides between the OFF and ON positions."));
            EditorGUILayout.PropertyField(_highlightCG, new GUIContent("Highlight CanvasGroup",
                "Optional. Faded in/out on hover and select."));
        }

        void DrawAnimation()
        {
            EditorGUILayout.PropertyField(_animationMode, new GUIContent("Animation Mode"));

            var mode = (UISwitchPro.AnimationMode)_animationMode.enumValueIndex;

            EditorGUILayout.Space(4);

            // -- Animator
            if (mode == UISwitchPro.AnimationMode.Animator || mode == UISwitchPro.AnimationMode.Both)
            {
                DrawSubHeader("Animator");
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_switchAnimator, new GUIContent("Animator",
                    "Plays On / Off / On Instant / Off Instant states."));

                if (_switchAnimator.objectReferenceValue == null)
                    EditorGUILayout.HelpBox("Animator is not assigned.", MessageType.Warning);
                else
                    EditorGUILayout.HelpBox(
                        "Required states: On  /  Off  /  On Instant  /  Off Instant",
                        MessageType.None);
                EditorGUI.indentLevel--;
                EditorGUILayout.Space(4);
            }

            // -- Generic Handle Slide
            if (mode == UISwitchPro.AnimationMode.GenericHandle || mode == UISwitchPro.AnimationMode.Both)
            {
                DrawSubHeader("Generic Handle Slide");
                // EditorGUI.indentLevel++;
                //EditorGUI.indentLevel--;

                bool hasHandle = _handleRect.objectReferenceValue != null;

                if (hasHandle)
                {
                    EditorGUILayout.Space(2);

                    EditorGUILayout.PropertyField(_handlePosOff, new GUIContent("Handle Pos OFF",
                        "anchoredPosition.X when Off (px from HandleArea centre)."));
                    EditorGUILayout.PropertyField(_handlePosOn, new GUIContent("Handle Pos ON",
                        "anchoredPosition.X when On (px from HandleArea centre)."));

                    EditorGUILayout.Space(2);
                    EditorGUILayout.PropertyField(_handleDuration, new GUIContent("Slide Duration (s)"));
                    EditorGUILayout.PropertyField(_handleCurve,    new GUIContent("Slide Curve"));

                    if (!Application.isPlaying)
                    {
                        EditorGUILayout.Space(2);
                        EditorGUILayout.BeginHorizontal();
                        if (GUILayout.Button("Snap Handle to OFF"))
                            SnapHandle(false);
                        if (GUILayout.Button("Snap Handle to ON"))
                            SnapHandle(true);
                        EditorGUILayout.EndHorizontal();
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox(
                        "Handle Rect is not assigned. Assign the knob RectTransform in the References section.",
                        MessageType.Warning);
                }

               // EditorGUI.indentLevel--;
            }

            // -- Color Transition (GenericHandle only)
            if (mode == UISwitchPro.AnimationMode.GenericHandle)
            {
                EditorGUILayout.Space(4);
                DrawSubHeader("Color Transition");
                //EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(_useColorTransition, new GUIContent("Use Color Transition",
                    "Tint target Images from OFF color to ON color."));

                if (_useColorTransition.boolValue)
                {
                    EditorGUILayout.Space(2);
                    EditorGUILayout.PropertyField(_colorDuration, new GUIContent("Color Duration (s)"));
                    EditorGUILayout.PropertyField(_colorCurve,    new GUIContent("Color Curve"));
                    EditorGUILayout.HelpBox("If a target has ImageRounded, its 'Use Own Colors' is forced off so the tint applies.",
                        MessageType.None);
                    
                    EditorGUILayout.Space(10);
                    EditorGUILayout.PropertyField(_useColorStyleSheet, new GUIContent("Use Style Sheet",
                        "Pick colors from a UIStyleSheet instead of custom Color Off / Color On values."));

                    DrawColorTargets();
                
                }

                //EditorGUI.indentLevel--;
            }
        }

        void DrawSave()
        {
            EditorGUILayout.PropertyField(_saveValue, new GUIContent("Save Value",
                "Persists the switch state in PlayerPrefs between sessions."));

            if (_saveValue.boolValue)
            {
                EditorGUI.indentLevel++;

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(_saveKey, new GUIContent("Save Key"));
                if (EditorGUI.EndChangeCheck() && string.IsNullOrWhiteSpace(_saveKey.stringValue))
                    _saveKey.stringValue = "Switch";

                EditorGUILayout.HelpBox($"PlayerPrefs key: \"Switch_{_saveKey.stringValue}\"", MessageType.None);
                EditorGUI.indentLevel--;
            }
        }

        void DrawSettings()
        {
            EditorGUILayout.PropertyField(_isOn,            new GUIContent("Is On",           "Initial state of the switch."));
            EditorGUILayout.PropertyField(_isInteractable,  new GUIContent("Is Interactable"));
            EditorGUILayout.PropertyField(_invokeOnAwake,   new GUIContent("Invoke On Awake", "Whether events should be fired on Awake."));
            EditorGUILayout.PropertyField(_useSounds,       new GUIContent("Use Sounds",      "Requires a UIAudio.Instance in the scene."));
            EditorGUILayout.PropertyField(_useUINavigation, new GUIContent("Use UI Navigation",
                "Adds a Button component (Transition: None) to enable keyboard and gamepad navigation."));

            if (_useUINavigation.boolValue)
                EditorGUILayout.HelpBox(
                    "UI Navigation automatically adds or configures a Button on this GameObject at Awake.",
                    MessageType.None);

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

            EditorGUILayout.Space(2);
            DrawSubHeader("ON / OFF");
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_onEvents,  new GUIContent("On Events  (fired when ON)"));
            EditorGUILayout.PropertyField(_offEvents, new GUIContent("Off Events (fired when OFF)"));
            EditorGUI.indentLevel--;
        }

        // == Runtime Controls ==

        void DrawRuntimeControls(UISwitchPro sw)
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Runtime Controls", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Toggle"))
                sw.Toggle();

            using (new EditorGUI.DisabledScope(sw.IsOn))
                if (GUILayout.Button("Set ON"))
                    sw.SetOn();

            using (new EditorGUI.DisabledScope(!sw.IsOn))
                if (GUILayout.Button("Set OFF"))
                    sw.SetOff();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button(sw.IsInteractable ? "Disable" : "Enable"))
                sw.SetInteractable(!sw.IsInteractable);

            if (GUILayout.Button("Force Refresh"))
                sw.ForceSetValue(sw.IsOn, animated: false);

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField(
                $"IsOn: {sw.IsOn}   Interactable: {sw.IsInteractable}",
                EditorStyles.centeredGreyMiniLabel);
        }

        // == Color Transition targets ==
        // Native list (default reorderable look + per-element foldout). The per-element
        // editor is provided by ColorTransitionEntryDrawer, which swaps the Color fields
        // for style-sheet slot dropdowns when Use Style Sheet is on.

        void DrawColorTargets()
        {
            if (_useColorStyleSheet.boolValue)
            {
                EditorGUILayout.PropertyField(_colorTransitionSheet, new GUIContent("Style Sheet",
                    "Color slots are read from this sheet by index."));

                if (_colorTransitionSheet.objectReferenceValue == null)
                    EditorGUILayout.HelpBox("Assign a Style Sheet to pick color slots.", MessageType.Warning);
            }

            EditorGUILayout.PropertyField(_colorTransitions, new GUIContent("Targets (Image / OFF / ON)"), true);
        }

        // == Helpers ==

        void SnapHandle(bool toOn)
        {
            serializedObject.ApplyModifiedProperties();
            UISwitchPro sw = (UISwitchPro)target;
            sw.Editor_SnapHandle(toOn);
            EditorUtility.SetDirty(sw);
        }

        /// <summary>Lightweight sub-header inside a section. Consistent style with UICheckboxProEditor.</summary>
        // new static void DrawSubHeader(string label)
        // {
        //     EditorGUILayout.Space(3);
        //     Rect r = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
        //     EditorGUI.LabelField(r, label, EditorStyles.boldLabel);
        //     EditorGUI.DrawRect(new Rect(r.x, r.yMax + 1f, r.width, 1f), new Color(0.35f, 0.35f, 0.35f, 0.6f));
        //     EditorGUILayout.Space(2);
        // }
    }

    // Per-element drawer for UISwitchPro color transition targets.
    // Drawn through the native list so it keeps the standard reorderable look,
    // indentation and foldout. Reads the sibling useColorStyleSheet / colorTransitionSheet
    // fields to decide between custom Color fields and style-sheet slot dropdowns.
    [CustomPropertyDrawer(typeof(UISwitchPro.ColorTransitionEntry))]
    public class ColorTransitionEntryDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float line    = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;

            if (!property.isExpanded)
                return line;

            // Foldout + Target + OFF + ON
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
                DrawSlotRow(r1, "Color OFF", sheet, names, property.FindPropertyRelative("slotOff"));
                DrawSlotRow(r2, "Color ON",  sheet, names, property.FindPropertyRelative("slotOn"));
            }
            else
            {
                EditorGUI.PropertyField(r1, property.FindPropertyRelative("colorOff"), new GUIContent("Color OFF"));
                EditorGUI.PropertyField(r2, property.FindPropertyRelative("colorOn"),  new GUIContent("Color ON"));
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
