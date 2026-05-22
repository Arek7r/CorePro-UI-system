#if UNITY_EDITOR
using CorePro.UI;
using UnityEditor;
using UnityEngine;

namespace CorePro.UITools
{
    [CustomEditor(typeof(SimpleBar))]
    public class SimpleBarEditor : UnityEditor.Editor
    {
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

        private void OnEnable()
        {
            initialValue = serializedObject.FindProperty("initialValue");
            initialMaxValue = serializedObject.FindProperty("initialMaxValue");
            initialOvercharge = serializedObject.FindProperty("initialOvercharge");
        }

        public override void OnInspectorGUI()
        {      
            if (target == null) 
                return;
            
            // Draw default inspector first (UIBar base class properties)
            DrawDefaultInspector();

            serializedObject.Update();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Editor Testing Controls", EditorStyles.boldLabel);

            // Live preview toggle
            EditorGUI.BeginChangeCheck();
            livePreview = EditorGUILayout.Toggle("Live Preview", livePreview);
            if (EditorGUI.EndChangeCheck() == true && livePreview == true)
            {
                needsPreviewUpdate = true;
            }
            
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

            EditorGUILayout.Slider(initialOvercharge, 0f, maxVal * 2f, "Overcharge Value");

            EditorGUILayout.Slider(initialValue, 0f, totalMax, "Current Value");

            float normalized = maxVal > 0f ? initialValue.floatValue / maxVal : 0f;
            EditorGUILayout.LabelField("Normalized", $"{normalized:F2} ({normalized * 100f:F1}%)");

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

            // Quick action buttons
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
            if (GUILayout.Button("Fill 50%"))
            {
                initialValue.floatValue = initialMaxValue.floatValue * 0.5f;
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
            if (GUILayout.Button("Max x2"))
            {
                initialMaxValue.floatValue *= 2f;
                serializedObject.ApplyModifiedProperties();
                if (livePreview == true && Application.isPlaying == false)
                {
                    ApplyLivePreview();
                }
            }
            if (GUILayout.Button("Max x10"))
            {
                initialMaxValue.floatValue *= 10f;
                serializedObject.ApplyModifiedProperties();
                if (livePreview == true && Application.isPlaying == false)
                {
                    ApplyLivePreview();
                }
            }
            if (GUILayout.Button("Max /2"))
            {
                initialMaxValue.floatValue = Mathf.Max(1f, initialMaxValue.floatValue * 0.5f);
                serializedObject.ApplyModifiedProperties();
                if (livePreview == true && Application.isPlaying == false)
                {
                    ApplyLivePreview();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Applies current inspector values to the bar for live preview.
        /// Called only in edit mode when livePreview is enabled.
        /// </summary>
        private void ApplyLivePreview()
        {
            SimpleBar bar = target as SimpleBar;
            if (bar == null) return;

            // Force initialize the bar
            bar.ForceInitialize();
            
            // Get safe values
            float safeMax = Mathf.Max(1f, initialMaxValue.floatValue);
            float safeOvercharge = Mathf.Max(0f, initialOvercharge.floatValue);
            float safeValue = Mathf.Clamp(initialValue.floatValue, 0f, safeMax + safeOvercharge);
            
            // Apply to bar using reflection to call protected UpdateValue
            // Or use the public SetValue method if available
            bar.SetValue(safeValue, safeMax, safeOvercharge);
            
            // Mark scene as dirty for proper undo support
            EditorUtility.SetDirty(bar);
        }
    }
}
#endif
