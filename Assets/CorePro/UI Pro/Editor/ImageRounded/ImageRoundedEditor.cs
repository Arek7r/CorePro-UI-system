#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace CorePro.UI.Editor
{
    [CustomEditor(typeof(ImageRounded))]
    public class ImageRoundedEditor : UnityEditor.Editor
    {
        // Serialized properties 
        SerializedProperty _uniformRadius;
        SerializedProperty _radius;
        SerializedProperty _radiusTL, _radiusTR, _radiusBR, _radiusBL;
        SerializedProperty _borderWidth;
        SerializedProperty _softness;
        SerializedProperty _styleSheet;
        SerializedProperty _fillColorSlot;
        SerializedProperty _borderColorSlot;
        SerializedProperty _useCustomFill;
        SerializedProperty _useCustomBorder;
        SerializedProperty _customFill;
        SerializedProperty _customBorder;

        private void OnEnable()
        {
            _uniformRadius   = serializedObject.FindProperty("uniformRadius");
            _radius          = serializedObject.FindProperty("radius");
            _radiusTL        = serializedObject.FindProperty("radiusTopLeft");
            _radiusTR        = serializedObject.FindProperty("radiusTopRight");
            _radiusBR        = serializedObject.FindProperty("radiusBottomRight");
            _radiusBL        = serializedObject.FindProperty("radiusBottomLeft");
            _borderWidth     = serializedObject.FindProperty("borderWidth");
            _softness        = serializedObject.FindProperty("softness");
            _styleSheet      = serializedObject.FindProperty("styleSheet");
            _fillColorSlot   = serializedObject.FindProperty("fillColorSlot");
            _borderColorSlot = serializedObject.FindProperty("borderColorSlot");
            _useCustomFill   = serializedObject.FindProperty("useCustomFillColor");
            _useCustomBorder = serializedObject.FindProperty("useCustomBorderColor");
            _customFill      = serializedObject.FindProperty("customFillColor");
            _customBorder    = serializedObject.FindProperty("customBorderColor");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var el    = (ImageRounded)target;
            var sheet = _styleSheet.objectReferenceValue as UIStyleSheet;

            var noticeStyle = new GUIStyle(EditorStyles.helpBox)
            {
                fontSize    = 11,
                wordWrap    = true,
                richText    = true,
                padding     = new RectOffset(10, 10, 8, 8),
            };
            noticeStyle.normal.textColor = new Color(0.85f, 0.65f, 0.1f);

            using (new EditorGUILayout.HorizontalScope())
            {
                // Left accent bar
                var barRect = GUILayoutUtility.GetRect(3f, 38f,
                    GUILayout.Width(3), GUILayout.ExpandHeight(true));
                
                EditorGUI.DrawRect(barRect, new Color(0.85f, 0.65f, 0.1f));

                GUILayout.Space(3);

                EditorGUILayout.LabelField(
                    "<b>Additional shader channels required</b>\n" +
                    "Enable on parent Canvas:  TexCoord1  TexCoord2  TexCoord3\n" +
                    "Add shader to build:  Project Settings > Graphics > Always Included Shaders",
                    noticeStyle);
            }

            EditorGUILayout.Space(4);

            // Corner radii 

            EditorGUILayout.LabelField("Corner Radii", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_uniformRadius, new GUIContent("Uniform Radius"));

            if (_uniformRadius.boolValue)
            {
                EditorGUILayout.PropertyField(_radius, new GUIContent("Radius (px)"));
            }
            else
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    // 2×2 grid layout
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        _radiusTL.floatValue = FloatField("↖ TL", _radiusTL.floatValue);
                        _radiusTR.floatValue = FloatField("↗ TR", _radiusTR.floatValue);
                    }
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        _radiusBL.floatValue = FloatField("↙ BL", _radiusBL.floatValue);
                        _radiusBR.floatValue = FloatField("↘ BR", _radiusBR.floatValue);
                    }
                }
            }

            EditorGUILayout.Space(6);

            // Border 
            EditorGUILayout.LabelField("Border", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_borderWidth, new GUIContent("Width (px)"));
            EditorGUILayout.PropertyField(_softness,    new GUIContent("AA Softness (px)"));

            EditorGUILayout.Space(6);

            // Colors 
            EditorGUILayout.LabelField("Colors", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_styleSheet, new GUIContent("Style Sheet"));

            EditorGUILayout.Space(2);

            // Fill color
            DrawColorSlot(
                label:           "Fill",
                sheet:           sheet,
                useColors:       true,
                slotProp:        _fillColorSlot,
                useCustomProp:   _useCustomFill,
                customColorProp: _customFill);

            EditorGUILayout.Space(2);

            // Border color (only relevant when borderWidth > 0)
            using (new EditorGUI.DisabledScope(_borderWidth.floatValue <= 0f))
            {
                DrawColorSlot(
                    label:           "Border",
                    sheet:           sheet,
                    useColors:       true,
                    slotProp:        _borderColorSlot,
                    useCustomProp:   _useCustomBorder,
                    customColorProp: _customBorder);
            }

            serializedObject.ApplyModifiedProperties();
        }

        // Color slot row 

        private void DrawColorSlot(
            string label,
            UIStyleSheet sheet,
            bool useColors,
            SerializedProperty slotProp,
            SerializedProperty useCustomProp,
            SerializedProperty customColorProp)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                // Label
                EditorGUILayout.LabelField(label, GUILayout.Width(50));

                // "Custom" toggle
                bool isCustom = useCustomProp.boolValue;
                bool nextCustom = EditorGUILayout.ToggleLeft("Custom", isCustom, GUILayout.Width(65));
                if (nextCustom != isCustom)
                    useCustomProp.boolValue = nextCustom;

                if (nextCustom || sheet == null)
                {
                    // Freeform color field
                    EditorGUILayout.PropertyField(customColorProp, GUIContent.none);

                    // Preview swatch from sheet (greyed out when custom)
                    if (sheet != null)
                    {
                        int slot = slotProp.intValue;
                        if (slot >= 0 && slot < sheet.Colors.Count)
                        {
                            using (new EditorGUI.DisabledScope(true))
                                DrawColorSwatch(sheet.Colors[slot].color, "(sheet)");
                        }
                    }
                }
                else
                {
                    // Sheet slot dropdown
                    string[] names  = BuildColorNames(sheet, useColors);
                    int      curIdx = Mathf.Clamp(slotProp.intValue, 0, names.Length - 1);
                    int      newIdx = EditorGUILayout.Popup(curIdx, names);
                    if (newIdx != curIdx)
                        slotProp.intValue = newIdx;

                    // Live color swatch
                    Color preview = sheet.GetColor(newIdx);
                    DrawColorSwatch(preview, null);
                }
            }
        }

        // Helpers 
        private static string[] BuildColorNames(UIStyleSheet sheet, bool useColors)
        {
            var list = useColors ? sheet.Colors : sheet.FontColors;
            var names = new string[list.Count];
            for (int i = 0; i < list.Count; i++)
                names[i] = string.IsNullOrWhiteSpace(list[i].name)
                    ? $"Slot {i}"
                    : $"{i}: {list[i].name}";
            
            return names;
        }

        private static void DrawColorSwatch(Color color, string tooltip)
        {
            var rect = GUILayoutUtility.GetRect(18, 18, GUILayout.Width(18));
            EditorGUI.DrawRect(rect, color);
            if (tooltip != null)
                GUI.Label(rect, new GUIContent(string.Empty, tooltip));
        }

        private static float FloatField(string label, float value)
        {
            return EditorGUILayout.FloatField(label, Mathf.Max(0f, value));
        }
    }

    // Minimal shader GUI (suppresses default shader inspector noise) 
    public class RoundedUIShaderGUI : ShaderGUI
    {
        public override void OnGUI(MaterialEditor editor, MaterialProperty[] props)
        {
            EditorGUILayout.HelpBox(
                "This shader is driven by ImageRounded.cs via UV channels.\n" +
                "Do not set properties here - they have no effect at runtime.",
                MessageType.Info);
        }
    }
}
#endif
