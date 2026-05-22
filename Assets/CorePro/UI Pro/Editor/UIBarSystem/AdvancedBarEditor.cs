#if UNITY_EDITOR
using CorePro.UI;
using UnityEditor;
using UnityEngine;
using InspectorPro.Editor;

namespace CorePro.UITools
{
    [CustomEditor(typeof(AdvancedBar))]
    public class AdvancedBarEditor : UnityEditor.Editor
    {
        private const string PREFS_PREFIX = "AdvancedBarEditor_";
        
        private SerializedProperty initialValue;
        private SerializedProperty initialMaxValue;
        private SerializedProperty initialOvercharge;
        
        /// <summary>
        /// Live preview state - persisted per-editor instance.
        /// </summary>
        private bool livePreview = true;
        
        /// <summary>
        /// Flag to track if we need to update preview after property changes.
        /// </summary>
        private bool needsPreviewUpdate;
        
        private SerializedProperty mainFill;
        private SerializedProperty overchargeFill;
        private SerializedProperty background;
        private SerializedProperty valueText;
        private SerializedProperty showText;
        private SerializedProperty hideWhenFull;
        private SerializedProperty textMode;
        private SerializedProperty useGradient;
        private SerializedProperty colorGradient;
        private SerializedProperty solidColor;
        private SerializedProperty overchargeColor;
        private SerializedProperty smoothTransition;
        private SerializedProperty transitionSpeed;
        
        private SerializedProperty enableGhostBar;
        private SerializedProperty ghostFill;
        private SerializedProperty ghostDelay;
        private SerializedProperty ghostSpeed;
        private SerializedProperty ghostColor;
        
        private SerializedProperty enableSegments;
        private SerializedProperty segmentCount;
        private SerializedProperty segmentContainer;
        private SerializedProperty segmentPrefab;
        private SerializedProperty segmentSpacing;
        
        private SerializedProperty fillMode;
        
        private SerializedProperty enableThresholds;
        private SerializedProperty thresholds;
        
        private SerializedProperty numberFormat;
        private SerializedProperty animateNumbers;
        private SerializedProperty numberAnimationSpeed;
        private SerializedProperty customPrefix;
        private SerializedProperty customSuffix;
        
        private SerializedProperty enableFlashOnDamage;
        private SerializedProperty flashColor;
        private SerializedProperty flashDuration;
        
        private SerializedProperty enablePulseOnLow;
        private SerializedProperty pulseBelowPercent;
        private SerializedProperty pulseColor;
        private SerializedProperty pulseSpeed;
        
        private SerializedProperty enableShakeOnHit;
        private SerializedProperty shakeIntensity;
        private SerializedProperty shakeDuration;
        
        private SerializedProperty updateMode;
        private SerializedProperty throttleRate;
        
        private SerializedProperty timeMode;
        
        private SerializedProperty onValueChanged;
        private SerializedProperty onEmpty;
        private SerializedProperty onFull;
        private SerializedProperty onThresholdCrossed;

        private bool showReferences;
        private bool showBaseSettings;
        private bool showTestingControls;
        private bool showGhostSettings;
        private bool showSegmentSettings;
        private bool showFillModeSettings;
        private bool showThresholdSettings;
        private bool showTextSettings;
        private bool showEffectSettings;
        private bool showPerformanceSettings;
        private bool showEventSettings;

        private void OnEnable()
        {
            LoadFoldoutStates();
            
            initialValue = serializedObject.FindProperty("initialValue");
            initialMaxValue = serializedObject.FindProperty("initialMaxValue");
            initialOvercharge = serializedObject.FindProperty("initialOvercharge");
            
            mainFill = serializedObject.FindProperty("mainFill");
            overchargeFill = serializedObject.FindProperty("overchargeFill");
            background = serializedObject.FindProperty("background");
            valueText = serializedObject.FindProperty("valueText");
            showText = serializedObject.FindProperty("showText");
            hideWhenFull = serializedObject.FindProperty("hideWhenFull");
            textMode = serializedObject.FindProperty("textMode");
            useGradient = serializedObject.FindProperty("useGradient");
            colorGradient = serializedObject.FindProperty("colorGradient");
            solidColor = serializedObject.FindProperty("solidColor");
            overchargeColor = serializedObject.FindProperty("overchargeColor");
            smoothTransition = serializedObject.FindProperty("smoothTransition");
            transitionSpeed = serializedObject.FindProperty("transitionSpeed");
            
            enableGhostBar = serializedObject.FindProperty("enableGhostBar");
            ghostFill = serializedObject.FindProperty("ghostFill");
            ghostDelay = serializedObject.FindProperty("ghostDelay");
            ghostSpeed = serializedObject.FindProperty("ghostSpeed");
            ghostColor = serializedObject.FindProperty("ghostColor");
            
            enableSegments = serializedObject.FindProperty("enableSegments");
            segmentCount = serializedObject.FindProperty("segmentCount");
            segmentContainer = serializedObject.FindProperty("segmentContainer");
            segmentPrefab = serializedObject.FindProperty("segmentPrefab");
            segmentSpacing = serializedObject.FindProperty("segmentSpacing");
            
            fillMode = serializedObject.FindProperty("fillMode");
            
            enableThresholds = serializedObject.FindProperty("enableThresholds");
            thresholds = serializedObject.FindProperty("thresholds");
            
            numberFormat = serializedObject.FindProperty("numberFormat");
            animateNumbers = serializedObject.FindProperty("animateNumbers");
            numberAnimationSpeed = serializedObject.FindProperty("numberAnimationSpeed");
            customPrefix = serializedObject.FindProperty("customPrefix");
            customSuffix = serializedObject.FindProperty("customSuffix");
            
            enableFlashOnDamage = serializedObject.FindProperty("enableFlashOnDamage");
            flashColor = serializedObject.FindProperty("flashColor");
            flashDuration = serializedObject.FindProperty("flashDuration");
            
            enablePulseOnLow = serializedObject.FindProperty("enablePulseOnLow");
            pulseBelowPercent = serializedObject.FindProperty("pulseBelowPercent");
            pulseColor = serializedObject.FindProperty("pulseColor");
            pulseSpeed = serializedObject.FindProperty("pulseSpeed");
            
            enableShakeOnHit = serializedObject.FindProperty("enableShakeOnHit");
            shakeIntensity = serializedObject.FindProperty("shakeIntensity");
            shakeDuration = serializedObject.FindProperty("shakeDuration");
            
            updateMode = serializedObject.FindProperty("updateMode");
            throttleRate = serializedObject.FindProperty("throttleRate");
            
            timeMode = serializedObject.FindProperty("timeMode");
            
            onValueChanged = serializedObject.FindProperty("onValueChanged");
            onEmpty = serializedObject.FindProperty("onEmpty");
            onFull = serializedObject.FindProperty("onFull");
            onThresholdCrossed = serializedObject.FindProperty("onThresholdCrossed");
        }

        private void OnDisable()
        {
            SaveFoldoutStates();
        }

        private void LoadFoldoutStates()
        {
            showReferences = EditorPrefs.GetBool(PREFS_PREFIX + "showReferences", true);
            showBaseSettings = EditorPrefs.GetBool(PREFS_PREFIX + "showBaseSettings", true);
            showTestingControls = EditorPrefs.GetBool(PREFS_PREFIX + "showTestingControls", true);
            showGhostSettings = EditorPrefs.GetBool(PREFS_PREFIX + "showGhostSettings", false);
            showSegmentSettings = EditorPrefs.GetBool(PREFS_PREFIX + "showSegmentSettings", false);
            showFillModeSettings = EditorPrefs.GetBool(PREFS_PREFIX + "showFillModeSettings", false);
            showThresholdSettings = EditorPrefs.GetBool(PREFS_PREFIX + "showThresholdSettings", false);
            showTextSettings = EditorPrefs.GetBool(PREFS_PREFIX + "showTextSettings", false);
            showEffectSettings = EditorPrefs.GetBool(PREFS_PREFIX + "showEffectSettings", false);
            showPerformanceSettings = EditorPrefs.GetBool(PREFS_PREFIX + "showPerformanceSettings", false);
            showEventSettings = EditorPrefs.GetBool(PREFS_PREFIX + "showEventSettings", false);
        }

        private void SaveFoldoutStates()
        {
            EditorPrefs.SetBool(PREFS_PREFIX + "showReferences", showReferences);
            EditorPrefs.SetBool(PREFS_PREFIX + "showBaseSettings", showBaseSettings);
            EditorPrefs.SetBool(PREFS_PREFIX + "showTestingControls", showTestingControls);
            EditorPrefs.SetBool(PREFS_PREFIX + "showGhostSettings", showGhostSettings);
            EditorPrefs.SetBool(PREFS_PREFIX + "showSegmentSettings", showSegmentSettings);
            EditorPrefs.SetBool(PREFS_PREFIX + "showFillModeSettings", showFillModeSettings);
            EditorPrefs.SetBool(PREFS_PREFIX + "showThresholdSettings", showThresholdSettings);
            EditorPrefs.SetBool(PREFS_PREFIX + "showTextSettings", showTextSettings);
            EditorPrefs.SetBool(PREFS_PREFIX + "showEffectSettings", showEffectSettings);
            EditorPrefs.SetBool(PREFS_PREFIX + "showPerformanceSettings", showPerformanceSettings);
            EditorPrefs.SetBool(PREFS_PREFIX + "showEventSettings", showEventSettings);
        }

        public override void OnInspectorGUI()
        {
            if (target == null) 
                return;
            
            serializedObject.Update();

            //DrawHeader();
            
            EditorGUILayout.Space(5);
            DrawReferences();
            
            EditorGUILayout.Space(5);
            DrawBaseSettings();
            
            EditorGUILayout.Space(5);
            DrawTestingControls();
            
            EditorGUILayout.Space(5);
            DrawGhostBarSettings();
            
            EditorGUILayout.Space(5);
            DrawSegmentSettings();
            
            EditorGUILayout.Space(5);
            DrawFillModeSettings();
            
            EditorGUILayout.Space(5);
            DrawThresholdSettings();
            
            EditorGUILayout.Space(5);
            DrawTextFormattingSettings();
            
            EditorGUILayout.Space(5);
            DrawEffectSettings();
            
            EditorGUILayout.Space(5);
            DrawPerformanceSettings();
            
            EditorGUILayout.Space(5);
            DrawEventSettings();

            serializedObject.ApplyModifiedProperties();
        }

        // private void DrawHeader()
        // {
        //     EditorGUILayout.LabelField("Advanced Bar Configuration", CoreProEditorStyle.GetH1LabelStyle);
        //     EditorGUILayout.Space(2);
        //     CoreProEditorStyle.DrawLine();
        //     EditorGUILayout.Space(3);
        //     CoreProEditorStyle.DrawHelpBox("Professional UI Bar with effects, animations, and advanced features. Optimized for zero GC allocation.");
        // }

        private void DrawReferences()
        {
            CoreProEditorStyle.DrawGroupFoldoutHeader("UI References", showReferences, out showReferences);
            
            if (showReferences == true)
            {
                EditorGUILayout.BeginVertical(CoreProEditorStyle.GroupContentBackground);
                EditorGUILayout.Space(5);
                
                EditorGUILayout.PropertyField(mainFill, new GUIContent("Main Fill"));
                EditorGUILayout.PropertyField(overchargeFill, new GUIContent("Overcharge Fill"));
                EditorGUILayout.PropertyField(background, new GUIContent("Background"));
                EditorGUILayout.PropertyField(valueText, new GUIContent("Value Text (TMP)"));
                
                EditorGUILayout.Space(5);
                
                if (GUILayout.Button("Auto-Find References"))
                {
                    AdvancedBar bar = (AdvancedBar)target;
                    if (bar != null)
                    {
                        Undo.RecordObject(bar, "Auto-Find References");
                        
                        Transform t = bar.transform;
                        
                        if (mainFill.objectReferenceValue == null)
                        {
                            Transform mainFillTransform = t.Find("MainFill");
                            if (mainFillTransform != null)
                            {
                                mainFill.objectReferenceValue = mainFillTransform.GetComponent<UnityEngine.UI.Image>();
                            }
                        }
                        
                        if (overchargeFill.objectReferenceValue == null)
                        {
                            Transform overchargeFillTransform = t.Find("OverchargeFill");
                            if (overchargeFillTransform != null)
                            {
                                overchargeFill.objectReferenceValue = overchargeFillTransform.GetComponent<UnityEngine.UI.Image>();
                            }
                        }
                        
                        if (background.objectReferenceValue == null)
                        {
                            Transform backgroundTransform = t.Find("Background");
                            if (backgroundTransform != null)
                            {
                                background.objectReferenceValue = backgroundTransform.GetComponent<UnityEngine.UI.Image>();
                            }
                        }
                        
                        if (valueText.objectReferenceValue == null)
                        {
                            valueText.objectReferenceValue = bar.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                        }
                        
                        serializedObject.ApplyModifiedProperties();
                        EditorUtility.SetDirty(bar);
                    }
                }
                
                EditorGUILayout.Space(3);

                EditorGUILayout.Space(5);
                EditorGUILayout.EndVertical();
            }
        }

        private void DrawBaseSettings()
        {
            CoreProEditorStyle.DrawGroupFoldoutHeader("Base Display Settings", showBaseSettings, out showBaseSettings);
            
            if (showBaseSettings == true)
            {
                EditorGUILayout.BeginVertical(CoreProEditorStyle.GroupContentBackground);
                EditorGUILayout.Space(5);
                
                EditorGUILayout.LabelField("Text Settings", CoreProEditorStyle.Asset.Header_H1_TitleStyle);
                EditorGUILayout.PropertyField(showText, new GUIContent("Show Text"));
                if (showText.boolValue == true)
                {
                    EditorGUILayout.PropertyField(textMode, new GUIContent("Text Display Mode"));
                }
                
                EditorGUILayout.Space(5);
                
                EditorGUILayout.LabelField("Color Settings", CoreProEditorStyle.Asset.Header_H1_TitleStyle);
                EditorGUILayout.PropertyField(useGradient, new GUIContent("Use Gradient"));
                if (useGradient.boolValue == true)
                {
                    EditorGUILayout.PropertyField(colorGradient, new GUIContent("Color Gradient"));
                }
                else
                {
                    EditorGUILayout.PropertyField(solidColor, new GUIContent("Solid Color"));
                }
                EditorGUILayout.PropertyField(overchargeColor, new GUIContent("Overcharge Color"));
                
                EditorGUILayout.Space(5);
                
                EditorGUILayout.LabelField("Animation Settings", CoreProEditorStyle.Asset.Header_H1_TitleStyle);
                EditorGUILayout.PropertyField(smoothTransition, new GUIContent("Smooth Transition"));
                if (smoothTransition.boolValue == true)
                {
                    EditorGUILayout.PropertyField(transitionSpeed, new GUIContent("Transition Speed"));
                }
                
                EditorGUILayout.Space(5);
                
                EditorGUILayout.LabelField("Visibility Settings", CoreProEditorStyle.Asset.Header_H1_TitleStyle);
                EditorGUILayout.PropertyField(hideWhenFull, new GUIContent("Hide When Full"));
                
                EditorGUILayout.Space(5);
                EditorGUILayout.EndVertical();
            }
        }

        private void DrawTestingControls()
        {
            CoreProEditorStyle.DrawGroupFoldoutHeader("Testing Controls", showTestingControls, out showTestingControls);
            
            if (showTestingControls == true)
            {
                EditorGUILayout.BeginVertical(CoreProEditorStyle.GroupContentBackground);
                EditorGUILayout.Space(5);
                
                // Live preview toggle
                EditorGUI.BeginChangeCheck();
                livePreview = EditorGUILayout.Toggle("Live Preview", livePreview);
                if (EditorGUI.EndChangeCheck() == true && livePreview == true)
                {
                    needsPreviewUpdate = true;
                }
                
                EditorGUILayout.Space(5);
                
                float maxVal = initialMaxValue.floatValue;
                float overVal = initialOvercharge.floatValue;
                float totalMax = maxVal + overVal;

                // Track changes in value fields
                EditorGUI.BeginChangeCheck();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Max Value");
                initialMaxValue.floatValue = EditorGUILayout.FloatField(initialMaxValue.floatValue);
                if (initialMaxValue.floatValue < 1f)
                {
                    initialMaxValue.floatValue = 1f;
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Overcharge Value");
                initialOvercharge.floatValue = EditorGUILayout.FloatField(initialOvercharge.floatValue);
                if (initialOvercharge.floatValue < 0f)
                {
                    initialOvercharge.floatValue = 0f;
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Slider(initialValue, 0f, totalMax, "Current Value");

                float normalized = maxVal > 0f ? initialValue.floatValue / maxVal : 0f;
                EditorGUILayout.LabelField("Normalized", $"{normalized:F2} ({normalized * 100f:F1}%)");
                
                if (overVal > 0f)
                {
                    float overchargePercent = (overVal / maxVal) * 100f;
                    EditorGUILayout.LabelField("Overcharge", $"+{overVal:F0} (+{overchargePercent:F1}%)");
                }

                if (EditorGUI.EndChangeCheck() == true)
                {
                    serializedObject.ApplyModifiedProperties();
                    needsPreviewUpdate = true;
                }

                // Apply live preview if enabled and in edit mode
                if (needsPreviewUpdate == true && livePreview == true && Application.isPlaying == false)
                {
                    ApplyLivePreview();
                    needsPreviewUpdate = false;
                }

                EditorGUILayout.Space(5);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Fill 100%"))
                {
                    initialValue.floatValue = initialMaxValue.floatValue;
                    serializedObject.ApplyModifiedProperties();
                    if (livePreview == true && Application.isPlaying == false)
                    {
                        ApplyLivePreview();
                    }
                }
                if (GUILayout.Button("Fill 75%"))
                {
                    initialValue.floatValue = initialMaxValue.floatValue * 0.75f;
                    serializedObject.ApplyModifiedProperties();
                    if (livePreview == true && Application.isPlaying == false)
                    {
                        ApplyLivePreview();
                    }
                }
                if (GUILayout.Button("Fill 50%"))
                {
                    initialValue.floatValue = initialMaxValue.floatValue * 0.5f;
                    serializedObject.ApplyModifiedProperties();
                    if (livePreview == true && Application.isPlaying == false)
                    {
                        ApplyLivePreview();
                    }
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Fill 25%"))
                {
                    initialValue.floatValue = initialMaxValue.floatValue * 0.25f;
                    serializedObject.ApplyModifiedProperties();
                    if (livePreview == true && Application.isPlaying == false)
                    {
                        ApplyLivePreview();
                    }
                }
                if (GUILayout.Button("Fill 10%"))
                {
                    initialValue.floatValue = initialMaxValue.floatValue * 0.1f;
                    serializedObject.ApplyModifiedProperties();
                    if (livePreview == true && Application.isPlaying == false)
                    {
                        ApplyLivePreview();
                    }
                }
                if (GUILayout.Button("Empty"))
                {
                    initialValue.floatValue = 0f;
                    serializedObject.ApplyModifiedProperties();
                    if (livePreview == true && Application.isPlaying == false)
                    {
                        ApplyLivePreview();
                    }
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(5);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Simulate Damage -10"))
                {
                    initialValue.floatValue = Mathf.Max(0f, initialValue.floatValue - 10f);
                    serializedObject.ApplyModifiedProperties();
                    if (livePreview == true && Application.isPlaying == false)
                    {
                        ApplyLivePreview();
                    }
                }
                if (GUILayout.Button("Simulate Heal +10"))
                {
                    initialValue.floatValue = Mathf.Min(totalMax, initialValue.floatValue + 10f);
                    serializedObject.ApplyModifiedProperties();
                    if (livePreview == true && Application.isPlaying == false)
                    {
                        ApplyLivePreview();
                    }
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Add Overcharge +25%"))
                {
                    initialOvercharge.floatValue += initialMaxValue.floatValue * 0.25f;
                    serializedObject.ApplyModifiedProperties();
                    if (livePreview == true && Application.isPlaying == false)
                    {
                        ApplyLivePreview();
                    }
                }
                if (GUILayout.Button("Clear Overcharge"))
                {
                    initialOvercharge.floatValue = 0f;
                    serializedObject.ApplyModifiedProperties();
                    if (livePreview == true && Application.isPlaying == false)
                    {
                        ApplyLivePreview();
                    }
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(5);
                EditorGUILayout.EndVertical();
            }
        }

        /// <summary>
        /// Applies current inspector values to the bar for live preview.
        /// Called only in edit mode when livePreview is enabled.
        /// </summary>
        private void ApplyLivePreview()
        {
            AdvancedBar bar = target as AdvancedBar;
            if (bar == null) return;

            // Force initialize the bar
            bar.ForceInitialize();
            
            // Get safe values
            float safeMax = Mathf.Max(1f, initialMaxValue.floatValue);
            float safeOvercharge = Mathf.Max(0f, initialOvercharge.floatValue);
            float safeValue = Mathf.Clamp(initialValue.floatValue, 0f, safeMax + safeOvercharge);
            
            // Apply to bar
            bar.SetValue(safeValue, safeMax, safeOvercharge);
            
            // Mark scene as dirty for proper undo support
            EditorUtility.SetDirty(bar);
        }

        private void DrawGhostBarSettings()
        {
            CoreProEditorStyle.DrawGroupFoldoutHeader("Ghost Bar (Delayed Fill)", showGhostSettings, out showGhostSettings);
            
            if (showGhostSettings == true)
            {
                EditorGUILayout.BeginVertical(CoreProEditorStyle.GroupContentBackground);
                EditorGUILayout.Space(5);
                
                EditorGUILayout.PropertyField(enableGhostBar, new GUIContent("Enable Ghost Bar"));
                
                if (enableGhostBar.boolValue == true)
                {
                    EditorGUILayout.PropertyField(ghostFill, new GUIContent("Ghost Fill Image"));
                    EditorGUILayout.PropertyField(ghostDelay, new GUIContent("Delay (seconds)"));
                    EditorGUILayout.PropertyField(ghostSpeed, new GUIContent("Speed"));
                    EditorGUILayout.PropertyField(ghostColor, new GUIContent("Color"));
                    
                    EditorGUILayout.Space(3);
                    CoreProEditorStyle.DrawHelpBox("Ghost bar shows previous value with delay. Useful for damage visualization.");
                    
                }
                
                EditorGUILayout.Space(5);
                EditorGUILayout.EndVertical();
            }
        }

        private void DrawSegmentSettings()
        {
            CoreProEditorStyle.DrawGroupFoldoutHeader("Segmentation", showSegmentSettings, out showSegmentSettings);
            
            if (showSegmentSettings == true)
            {
                EditorGUILayout.BeginVertical(CoreProEditorStyle.GroupContentBackground);
                EditorGUILayout.Space(5);
                
                EditorGUILayout.PropertyField(enableSegments, new GUIContent("Enable Segments"));
                
                if (enableSegments.boolValue == true)
                {
                    EditorGUILayout.PropertyField(segmentCount, new GUIContent("Segment Count"));
                    EditorGUILayout.PropertyField(segmentContainer, new GUIContent("Container"));
                    EditorGUILayout.PropertyField(segmentPrefab, new GUIContent("Segment Prefab"));
                    EditorGUILayout.PropertyField(segmentSpacing, new GUIContent("Spacing"));
                    
                    EditorGUILayout.Space(5);
                    
                    if (GUILayout.Button("Regenerate Segments"))
                    {
                        AdvancedBar bar = (AdvancedBar)target;
                        if (bar != null)
                        {
                            serializedObject.ApplyModifiedProperties();
                            EditorUtility.SetDirty(bar);
                        }
                    }
                    
                    EditorGUILayout.Space(3);
                    CoreProEditorStyle.DrawHelpBox("Divides bar into visual segments. Each segment fills individually.");
                }
                
                EditorGUILayout.Space(5);
                EditorGUILayout.EndVertical();
            }
        }

        private void DrawFillModeSettings()
        {
            CoreProEditorStyle.DrawGroupFoldoutHeader("Fill Direction", showFillModeSettings, out showFillModeSettings);
            
            if (showFillModeSettings == true)
            {
                EditorGUILayout.BeginVertical(CoreProEditorStyle.GroupContentBackground);
                EditorGUILayout.Space(5);
                
                EditorGUILayout.PropertyField(fillMode, new GUIContent("Fill Mode"));
                EditorGUILayout.Space(3);
                CoreProEditorStyle.DrawHelpBox("Controls how the bar fills: horizontal, vertical, or radial.");
                
                EditorGUILayout.Space(5);
                EditorGUILayout.EndVertical();
            }
        }

        private void DrawThresholdSettings()
        {
            CoreProEditorStyle.DrawGroupFoldoutHeader("Thresholds & Colors", showThresholdSettings, out showThresholdSettings);
            
            if (showThresholdSettings == true)
            {
                EditorGUILayout.BeginVertical(CoreProEditorStyle.GroupContentBackground);
                EditorGUILayout.Space(5);
                
                EditorGUILayout.PropertyField(enableThresholds, new GUIContent("Enable Thresholds"));
                
                if (enableThresholds.boolValue == true)
                {
                    EditorGUILayout.PropertyField(thresholds, new GUIContent("Threshold List"), true);
                    
                    EditorGUILayout.Space(5);
                    
                    if (GUILayout.Button("Add Default Thresholds (Low/Mid/High)"))
                    {
                        thresholds.ClearArray();
                        
                        thresholds.InsertArrayElementAtIndex(0);
                        SerializedProperty low = thresholds.GetArrayElementAtIndex(0);
                        low.FindPropertyRelative("percentage").floatValue = 0.25f;
                        low.FindPropertyRelative("color").colorValue = Color.red;
                        low.FindPropertyRelative("triggerEvent").boolValue = true;
                        
                        thresholds.InsertArrayElementAtIndex(1);
                        SerializedProperty mid = thresholds.GetArrayElementAtIndex(1);
                        mid.FindPropertyRelative("percentage").floatValue = 0.5f;
                        mid.FindPropertyRelative("color").colorValue = Color.yellow;
                        mid.FindPropertyRelative("triggerEvent").boolValue = false;
                        
                        thresholds.InsertArrayElementAtIndex(2);
                        SerializedProperty high = thresholds.GetArrayElementAtIndex(2);
                        high.FindPropertyRelative("percentage").floatValue = 0.75f;
                        high.FindPropertyRelative("color").colorValue = Color.green;
                        high.FindPropertyRelative("triggerEvent").boolValue = false;
                    }
                    
                    EditorGUILayout.Space(3);
                    CoreProEditorStyle.DrawHelpBox("Changes bar color at specific percentage thresholds. Can trigger events.");
                }
                
                EditorGUILayout.Space(5);
                EditorGUILayout.EndVertical();
            }
        }

        private void DrawTextFormattingSettings()
        {
            CoreProEditorStyle.DrawGroupFoldoutHeader("Text Formatting", showTextSettings, out showTextSettings);
            
            if (showTextSettings == true)
            {
                EditorGUILayout.BeginVertical(CoreProEditorStyle.GroupContentBackground);
                EditorGUILayout.Space(5);
                
                EditorGUILayout.PropertyField(numberFormat, new GUIContent("Number Format"));
                EditorGUILayout.PropertyField(animateNumbers, new GUIContent("Animate Numbers"));
                
                if (animateNumbers.boolValue == true)
                {
                    EditorGUILayout.PropertyField(numberAnimationSpeed, new GUIContent("Animation Speed"));
                }
                
                EditorGUILayout.PropertyField(customPrefix, new GUIContent("Prefix"));
                EditorGUILayout.PropertyField(customSuffix, new GUIContent("Suffix"));
                
                EditorGUILayout.Space(5);
                
                string example = GetFormattingExample();
                CoreProEditorStyle.DrawHelpBox($"Example output: {example}");
                
                EditorGUILayout.Space(5);
                EditorGUILayout.EndVertical();
            }
        }

        private string GetFormattingExample()
        {
            string prefix = customPrefix.stringValue;
            string suffix = customSuffix.stringValue;
            
            switch (numberFormat.enumValueIndex)
            {
                case 0:
                    return $"{prefix}1234567{suffix}";
                case 1:
                    return $"{prefix}1.2M{suffix}";
                case 2:
                    return $"{prefix}75%{suffix}";
                case 3:
                    return $"{prefix}75.5%{suffix}";
                default:
                    return $"{prefix}1000{suffix}";
            }
        }

        private void DrawEffectSettings()
        {
            CoreProEditorStyle.DrawGroupFoldoutHeader("Visual Effects", showEffectSettings, out showEffectSettings);
            
            if (showEffectSettings == true)
            {
                EditorGUILayout.BeginVertical(CoreProEditorStyle.GroupContentBackground);
                EditorGUILayout.Space(5);
                
                EditorGUILayout.LabelField("Flash Effect", CoreProEditorStyle.Asset.Header_H1_TitleStyle);
                EditorGUILayout.PropertyField(enableFlashOnDamage, new GUIContent("Enable Flash on Damage"));
                if (enableFlashOnDamage.boolValue == true)
                {
                    EditorGUILayout.PropertyField(flashColor, new GUIContent("Flash Color"));
                    EditorGUILayout.PropertyField(flashDuration, new GUIContent("Duration (seconds)"));
                }
                
                EditorGUILayout.Space(5);
                
                EditorGUILayout.LabelField("Pulse Effect", CoreProEditorStyle.Asset.Header_H1_TitleStyle);
                EditorGUILayout.PropertyField(enablePulseOnLow, new GUIContent("Enable Pulse on Low Value"));
                if (enablePulseOnLow.boolValue == true)
                {
                    EditorGUILayout.Slider(pulseBelowPercent, 0f, 1f, "Pulse Below Percent");
                    EditorGUILayout.PropertyField(pulseColor, new GUIContent("Pulse Color"));
                    EditorGUILayout.PropertyField(pulseSpeed, new GUIContent("Pulse Speed"));
                }
                
                EditorGUILayout.Space(5);
                
                EditorGUILayout.LabelField("Shake Effect", CoreProEditorStyle.Asset.Header_H1_TitleStyle);
                EditorGUILayout.PropertyField(enableShakeOnHit, new GUIContent("Enable Shake on Hit"));
                if (enableShakeOnHit.boolValue == true)
                {
                    EditorGUILayout.PropertyField(shakeIntensity, new GUIContent("Shake Intensity"));
                    EditorGUILayout.PropertyField(shakeDuration, new GUIContent("Duration (seconds)"));
                }
                
                EditorGUILayout.Space(5);
                CoreProEditorStyle.DrawHelpBox("Visual effects trigger automatically on value changes. All effects respect Time Mode setting.");
                
                EditorGUILayout.Space(5);
                EditorGUILayout.EndVertical();
            }
        }

        private void DrawPerformanceSettings()
        {
            CoreProEditorStyle.DrawGroupFoldoutHeader("Performance & Time", showPerformanceSettings, out showPerformanceSettings);
            
            if (showPerformanceSettings == true)
            {
                EditorGUILayout.BeginVertical(CoreProEditorStyle.GroupContentBackground);
                EditorGUILayout.Space(5);
                
                EditorGUILayout.PropertyField(updateMode, new GUIContent("Update Mode"));
                
                if (updateMode.enumValueIndex == 2)
                {
                    EditorGUILayout.PropertyField(throttleRate, new GUIContent("Throttle Rate (seconds)"));
                    CoreProEditorStyle.DrawHelpBox("Limits updates to once per throttle rate. Reduces CPU usage.");
                }
                else if (updateMode.enumValueIndex == 1)
                {
                    CoreProEditorStyle.DrawHelpBox("Updates only when value changes. Best performance.");
                }
                else
                {
                    CoreProEditorStyle.DrawWarningBox("Updates every frame. Required for smooth animations.");
                }
                
                EditorGUILayout.Space(5);
                
                EditorGUILayout.PropertyField(timeMode, new GUIContent("Time Mode"));
                
                if (timeMode.enumValueIndex == 0)
                {
                    CoreProEditorStyle.DrawHelpBox("Uses Time.deltaTime. Pauses when Time.timeScale = 0 (game pause).");
                }
                else
                {
                    CoreProEditorStyle.DrawHelpBox("Uses Time.unscaledDeltaTime. Continues during game pause. Use for pause menu UI.");
                }
                
                EditorGUILayout.Space(5);
                EditorGUILayout.EndVertical();
            }
        }

        private void DrawEventSettings()
        {
            CoreProEditorStyle.DrawGroupFoldoutHeader("Unity Events", showEventSettings, out showEventSettings);
            
            if (showEventSettings == true)
            {
                EditorGUILayout.BeginVertical(CoreProEditorStyle.GroupContentBackground);
                EditorGUILayout.Space(5);
                
                EditorGUILayout.PropertyField(onValueChanged, new GUIContent("On Value Changed"));
                EditorGUILayout.PropertyField(onEmpty, new GUIContent("On Empty"));
                EditorGUILayout.PropertyField(onFull, new GUIContent("On Full"));
                EditorGUILayout.PropertyField(onThresholdCrossed, new GUIContent("On Threshold Crossed"));
                
                EditorGUILayout.Space(3);
                CoreProEditorStyle.DrawHelpBox("Events trigger automatically. Use for audio, particles, or gameplay logic.");
                
                EditorGUILayout.Space(5);
                EditorGUILayout.EndVertical();
            }
        }
    }
}
#endif
