#if UNITY_EDITOR
using CorePro.Editor.Framework;
using UnityEditor;
using UnityEngine;

namespace CorePro.UI.Editor
{
    [CustomEditor(typeof(UIProgressBar))]
    public class UIProgressBarEditor : CoreProEditor
    {
        // == SerializedProperties ==

        // Visual References
        SerializedProperty _mainFill;
        SerializedProperty _background;
        SerializedProperty _valueText;
        SerializedProperty _valueText2;

        // Initial Values
        SerializedProperty _initialValue;
        SerializedProperty _initialMaxValue;
        SerializedProperty _currentValue;

        // Display Options
        SerializedProperty _showText;
        SerializedProperty _hideWhenFull;
        SerializedProperty _textMode;
        SerializedProperty _customPrefix;
        SerializedProperty _customSuffix;

        // Visual Styling
        SerializedProperty _useGradient;
        SerializedProperty _colorGradient;
        SerializedProperty _solidColor;

        // Smooth Transition
        SerializedProperty _smoothTransition;
        SerializedProperty _transitionSpeed;

        // Checkpoints
        SerializedProperty _enableCheckpoints;
        SerializedProperty _checkpoints;
        SerializedProperty _indicatorPrefab;
        SerializedProperty _indicatorContainer;
        SerializedProperty _indicatorSize;

        // Indeterminate
        SerializedProperty _indeterminateMode;
        SerializedProperty _indeterminateStrip;
        SerializedProperty _stripWidth;
        SerializedProperty _indeterminateSpeed;

        // ETA
        SerializedProperty _showEta;
        SerializedProperty _etaText;
        SerializedProperty _etaFormat;
        SerializedProperty _etaSampleWindow;

        // Auto Complete
        SerializedProperty _autoComplete;
        SerializedProperty _autoCompleteDelay;
        SerializedProperty _autoCompleteDuration;

        // Time Settings
        SerializedProperty _timeMode;

        // Events
        SerializedProperty _onComplete;
        SerializedProperty _onReset;
        SerializedProperty _onValueChanged;
        SerializedProperty _onCheckpointReached;

        // == Foldouts ==

        bool _foldVisual;
        bool _foldInitial;
        bool _foldDisplay;
        bool _foldStyling;
        bool _foldSmooth;
        bool _foldCheckpoints;
        bool _foldIndeterminate;
        bool _foldEta;
        bool _foldAutoComplete;
        bool _foldTime;
        bool _foldEvents;

        // == Lifecycle ==

        protected override void OnEnable()
        {
            base.OnEnable(); // loads Theme

            // Visual References
            _mainFill       = serializedObject.FindProperty("mainFill");
            _background     = serializedObject.FindProperty("background");
            _valueText      = serializedObject.FindProperty("valueText");
            _valueText2     = serializedObject.FindProperty("valueText2");

            // Initial Values
            _initialValue    = serializedObject.FindProperty("initialValue");
            _initialMaxValue = serializedObject.FindProperty("initialMaxValue");
            _currentValue    = serializedObject.FindProperty("currentValue");

            // Display Options
            _showText      = serializedObject.FindProperty("showText");
            _hideWhenFull  = serializedObject.FindProperty("hideWhenFull");
            _textMode      = serializedObject.FindProperty("textMode");
            _customPrefix  = serializedObject.FindProperty("customPrefix");
            _customSuffix  = serializedObject.FindProperty("customSuffix");

            // Visual Styling
            _useGradient   = serializedObject.FindProperty("useGradient");
            _colorGradient = serializedObject.FindProperty("colorGradient");
            _solidColor    = serializedObject.FindProperty("solidColor");

            // Smooth Transition
            _smoothTransition = serializedObject.FindProperty("smoothTransition");
            _transitionSpeed  = serializedObject.FindProperty("transitionSpeed");

            // Checkpoints
            _enableCheckpoints  = serializedObject.FindProperty("enableCheckpoints");
            _checkpoints        = serializedObject.FindProperty("checkpoints");
            _indicatorPrefab    = serializedObject.FindProperty("indicatorPrefab");
            _indicatorContainer = serializedObject.FindProperty("indicatorContainer");
            _indicatorSize      = serializedObject.FindProperty("indicatorSize");

            // Indeterminate
            _indeterminateMode  = serializedObject.FindProperty("indeterminateMode");
            _indeterminateStrip = serializedObject.FindProperty("indeterminateStrip");
            _stripWidth         = serializedObject.FindProperty("stripWidth");
            _indeterminateSpeed = serializedObject.FindProperty("indeterminateSpeed");

            // ETA
            _showEta         = serializedObject.FindProperty("showEta");
            _etaText         = serializedObject.FindProperty("etaText");
            _etaFormat       = serializedObject.FindProperty("etaFormat");
            _etaSampleWindow = serializedObject.FindProperty("etaSampleWindow");

            // Auto Complete
            _autoComplete         = serializedObject.FindProperty("autoComplete");
            _autoCompleteDelay    = serializedObject.FindProperty("autoCompleteDelay");
            _autoCompleteDuration = serializedObject.FindProperty("autoCompleteDuration");

            // Time
            _timeMode = serializedObject.FindProperty("timeMode");

            // Events
            _onComplete          = serializedObject.FindProperty("onComplete");
            _onReset             = serializedObject.FindProperty("onReset");
            _onValueChanged      = serializedObject.FindProperty("onValueChanged");
            _onCheckpointReached = serializedObject.FindProperty("onCheckpointReached");

            // Foldouts - key sections open by default
            _foldVisual        = LoadFoldout("Visual",        true);
            _foldInitial       = LoadFoldout("Initial",       true);
            _foldDisplay       = LoadFoldout("Display",       true);
            _foldStyling       = LoadFoldout("Styling",       true);
            _foldSmooth        = LoadFoldout("Smooth",        true);
            _foldCheckpoints   = LoadFoldout("Checkpoints",   false);
            _foldIndeterminate = LoadFoldout("Indeterminate", false);
            _foldEta           = LoadFoldout("ETA",           false);
            _foldAutoComplete  = LoadFoldout("AutoComplete",  false);
            _foldTime          = LoadFoldout("Time",          false);
            _foldEvents        = LoadFoldout("Events",        false);
        }

        // == Main Draw ==

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            UIProgressBar bar = (UIProgressBar)target;

            DrawPreviewBar(bar);
            EditorGUILayout.Space(4);

            EditorGUI.BeginChangeCheck();

            DrawSection("Visual References",  ref _foldVisual,        "Visual",        DrawVisualReferences);
            DrawSection("Initial Values",      ref _foldInitial,       "Initial",       DrawInitialValues);
            DrawSection("Display Options",     ref _foldDisplay,       "Display",       DrawDisplayOptions);
            DrawSection("Visual Styling",      ref _foldStyling,       "Styling",       DrawVisualStyling);
            DrawSection("Smooth Transition",   ref _foldSmooth,        "Smooth",        DrawSmoothTransition);
            DrawSection("Checkpoints",         ref _foldCheckpoints,   "Checkpoints",   DrawCheckpoints);
            DrawSection("Indeterminate Mode",  ref _foldIndeterminate, "Indeterminate", DrawIndeterminate);
            DrawSection("ETA Display",         ref _foldEta,           "ETA",           DrawEta);
            DrawSection("Auto Complete",       ref _foldAutoComplete,  "AutoComplete",  DrawAutoComplete);
            DrawSection("Time Settings",       ref _foldTime,          "Time",          DrawTimeSettings);
            DrawSection("Events",             ref _foldEvents,        "Events",        DrawEvents);

            bool changed = EditorGUI.EndChangeCheck();

            if (Application.isPlaying)
                DrawRuntimeControls(bar);

            serializedObject.ApplyModifiedProperties();

            // After properties are written back to the object, refresh scene visuals (Edit Mode only)
            if (changed && !Application.isPlaying)
                bar.Editor_Refresh();
        }

        // == Preview Bar ==

        void DrawPreviewBar(UIProgressBar bar)
        {
            float pct = _initialMaxValue.floatValue > 0f
                ? _currentValue.floatValue / _initialMaxValue.floatValue
                : 0f;
            pct = Mathf.Clamp01(pct);

            Rect outerRect = GUILayoutUtility.GetRect(
                GUIContent.none, GUIStyle.none,
                GUILayout.Height(Theme.previewBarHeight),
                GUILayout.ExpandWidth(true));

            outerRect = EditorGUI.IndentedRect(outerRect);

            // Background
            EditorGUI.DrawRect(outerRect, Theme.previewBarBg);

            // Fill
            Color fillColor = _useGradient.boolValue
                ? Color.Lerp(Color.red, Color.green, pct)
                : _solidColor.colorValue;

            Rect fillRect  = outerRect;
            fillRect.width *= pct;
            EditorGUI.DrawRect(fillRect, fillColor);

            // Border
            DrawBorder(outerRect, Theme.previewBarBorder);

            // Label with shadow outline - readable on any background color
            string label = Application.isPlaying
                ? $"{bar.CurrentValue:F1} / {bar.MaxValue:F1}  ({pct * 100f:F0}%)"
                : $"{_currentValue.floatValue:F1} / {_initialMaxValue.floatValue:F1}  ({pct * 100f:F0}%)";

            DrawLabelWithShadow(outerRect, label);
        }

        // == Section Bodies ==

        void DrawVisualReferences()
        {
            EditorGUILayout.PropertyField(_mainFill,   new GUIContent("Main Fill Image"));
            EditorGUILayout.PropertyField(_background, new GUIContent("Background Image"));
            EditorGUILayout.PropertyField(_valueText,  new GUIContent("Value Text (TMP)"));
            EditorGUILayout.PropertyField(_valueText2, new GUIContent("Value Text 2 (TMP)",
                "Optional. Receives the same formatted value as Value Text. " +
                "Use when a second label sits on a different background layer."));

            if (_mainFill.objectReferenceValue == null)
                EditorGUILayout.HelpBox("Main Fill Image is required for the bar to render.", MessageType.Warning);
        }

        void DrawInitialValues()
        {
            EditorGUILayout.PropertyField(_initialMaxValue, new GUIContent("Max Value"));

            float max = Mathf.Max(1f, _initialMaxValue.floatValue);

            // Initial value slider
            EditorGUI.BeginChangeCheck();
            float newInit = EditorGUILayout.Slider("Initial Value", _initialValue.floatValue, 0f, max);
            if (EditorGUI.EndChangeCheck())
                _initialValue.floatValue = newInit;

            // Current value slider - updates the preview bar and triggers Editor_Refresh via global change check
            EditorGUI.BeginChangeCheck();
            float newCurrent = EditorGUILayout.Slider(
                new GUIContent("Current Value", "Shown in the preview bar above."),
                _currentValue.floatValue, 0f, max);
            if (EditorGUI.EndChangeCheck())
                _currentValue.floatValue = Mathf.Clamp(newCurrent, 0f, max);
        }

        void DrawDisplayOptions()
        {
            EditorGUILayout.PropertyField(_showText,     new GUIContent("Show Text"));
            EditorGUILayout.PropertyField(_hideWhenFull, new GUIContent("Hide When Full"));
            EditorGUILayout.PropertyField(_textMode,     new GUIContent("Text Mode"));

            var mode = (UIProgressBar.TextDisplayMode)_textMode.enumValueIndex;
            if (mode != UIProgressBar.TextDisplayMode.None)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_customPrefix, new GUIContent("Prefix"));
                EditorGUILayout.PropertyField(_customSuffix, new GUIContent("Suffix"));
                EditorGUI.indentLevel--;
            }

            if (_valueText.objectReferenceValue == null && _showText.boolValue)
                EditorGUILayout.HelpBox("Value Text reference is missing. Text will not appear.", MessageType.Warning);
        }

        void DrawVisualStyling()
        {
            EditorGUILayout.PropertyField(_useGradient, new GUIContent("Use Gradient"));
            EditorGUILayout.PropertyField(
                _useGradient.boolValue ? _colorGradient : _solidColor,
                new GUIContent(_useGradient.boolValue ? "Color Gradient" : "Solid Color"));
        }

        void DrawSmoothTransition()
        {
            EditorGUILayout.PropertyField(_smoothTransition, new GUIContent("Enable Smooth Transition"));
            if (_smoothTransition.boolValue)
                EditorGUILayout.PropertyField(_transitionSpeed, new GUIContent("Transition Speed"));
        }

        void DrawCheckpoints()
        {
            EditorGUILayout.PropertyField(_enableCheckpoints, new GUIContent("Enable Checkpoints"));

            if (_enableCheckpoints.boolValue)
            {
                EditorGUILayout.PropertyField(_checkpoints,        new GUIContent("Checkpoint List"), true);
                EditorGUILayout.PropertyField(_indicatorPrefab,    new GUIContent("Indicator Prefab"));
                EditorGUILayout.PropertyField(_indicatorContainer, new GUIContent("Indicator Container"));
                EditorGUILayout.PropertyField(_indicatorSize,      new GUIContent("Indicator Size"));

                if (_indicatorPrefab.objectReferenceValue == null)
                    EditorGUILayout.HelpBox("Indicator Prefab is required to show checkpoint markers.", MessageType.Info);
            }
        }

        void DrawIndeterminate()
        {
            EditorGUILayout.PropertyField(_indeterminateMode, new GUIContent("Indeterminate Mode"));

            if (_indeterminateMode.boolValue)
            {
                EditorGUILayout.PropertyField(_indeterminateStrip, new GUIContent("Bouncing Strip Image"));
                EditorGUILayout.PropertyField(_stripWidth,         new GUIContent("Strip Width (0-1)"));
                EditorGUILayout.PropertyField(_indeterminateSpeed, new GUIContent("Speed"));

                if (_indeterminateStrip.objectReferenceValue == null)
                    EditorGUILayout.HelpBox("Indeterminate Strip image is not assigned.", MessageType.Warning);
            }
        }

        void DrawEta()
        {
            EditorGUILayout.PropertyField(_showEta, new GUIContent("Show ETA"));

            if (_showEta.boolValue)
            {
                EditorGUILayout.PropertyField(_etaText,         new GUIContent("ETA Text (TMP)"));
                EditorGUILayout.PropertyField(_etaFormat,       new GUIContent("Format String"));
                EditorGUILayout.PropertyField(_etaSampleWindow, new GUIContent("Sample Window (s)"));
                EditorGUILayout.HelpBox("Use {0} in the format string for seconds. Example: \"~{0}s remaining\"", MessageType.None);
            }
        }

        void DrawAutoComplete()
        {
            EditorGUILayout.PropertyField(_autoComplete, new GUIContent("Auto Complete"));

            if (_autoComplete.boolValue)
            {
                EditorGUILayout.PropertyField(_autoCompleteDelay,    new GUIContent("Delay (s)"));
                EditorGUILayout.PropertyField(_autoCompleteDuration, new GUIContent("Duration (s)"));
            }
        }

        void DrawTimeSettings()
        {
            EditorGUILayout.PropertyField(_timeMode, new GUIContent("Time Mode"));
            string hint = ((UIProgressBar.TimeMode)_timeMode.enumValueIndex) == UIProgressBar.TimeMode.Unscaled
                ? "Uses UnscaledDeltaTime. The bar continues filling when Time.timeScale is 0."
                : "Uses ScaledDeltaTime. The bar pauses when Time.timeScale is 0.";
            EditorGUILayout.HelpBox(hint, MessageType.None);
        }

        void DrawEvents()
        {
            EditorGUILayout.PropertyField(_onComplete,          new GUIContent("On Complete"));
            EditorGUILayout.PropertyField(_onReset,             new GUIContent("On Reset"));
            EditorGUILayout.PropertyField(_onValueChanged,      new GUIContent("On Value Changed"));
            EditorGUILayout.PropertyField(_onCheckpointReached, new GUIContent("On Checkpoint Reached"));
        }

        // == Runtime Controls ==

        void DrawRuntimeControls(UIProgressBar bar)
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Runtime Controls", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset"))        bar.ResetProgress();
            if (GUILayout.Button("Set Complete")) bar.SetComplete();
            if (GUILayout.Button("+10"))          bar.ModifyValue(10f);
            if (GUILayout.Button("-10"))          bar.ModifyValue(-10f);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(2);

            EditorGUI.BeginChangeCheck();
            float live = EditorGUILayout.Slider("Scrub Value", bar.CurrentValue, 0f, bar.MaxValue);
            if (EditorGUI.EndChangeCheck())
                bar.SetValue(live);

            // Live state readout
            EditorGUILayout.LabelField(
                $"CurrentValue: {bar.CurrentValue:F2}   MaxValue: {bar.MaxValue:F2}   {bar.CurrentPercent * 100f:F0}%",
                EditorStyles.centeredGreyMiniLabel);
        }
    }
}
#endif