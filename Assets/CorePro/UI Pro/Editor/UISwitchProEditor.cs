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
                EditorGUI.indentLevel++;

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

                EditorGUI.indentLevel--;
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

        // == Helpers ==

        void SnapHandle(bool toOn)
        {
            serializedObject.ApplyModifiedProperties();
            UISwitchPro sw = (UISwitchPro)target;
            sw.Editor_SnapHandle(toOn);
            EditorUtility.SetDirty(sw);
        }

        /// <summary>Lightweight sub-header inside a section. Consistent style with UICheckboxProEditor.</summary>
        new static void DrawSubHeader(string label)
        {
            EditorGUILayout.Space(3);
            Rect r = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(r, label, EditorStyles.boldLabel);
            EditorGUI.DrawRect(new Rect(r.x, r.yMax + 1f, r.width, 1f), new Color(0.35f, 0.35f, 0.35f, 0.6f));
            EditorGUILayout.Space(2);
        }
    }
}
#endif
