using UnityEditor;
using UnityEngine;

namespace InspectorPro.Editor
{
    [CustomPropertyDrawer(typeof(HeaderFrameAttribute))]
    public class HeaderFrameDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attr = (HeaderFrameAttribute)attribute;
            float frameHeight = Mathf.Max(24f, attr.FontSize + 16f);

            // Frame (on top)
            var frameRect = new Rect(position.x, position.y + attr.TopSpacing, position.width, frameHeight);
            var padding = new RectOffset(4, 4, 6, 6);
            
            if (attr.Alignment == TextAnchor.MiddleLeft)
                 padding = new RectOffset(10, 4, 6, 6);
            
            var style = new GUIStyle(EditorStyles.helpBox)
            {
                alignment = attr.Alignment,
                fontStyle = attr.FontStyle,
                fontSize = attr.FontSize,
                padding = padding,
                margin = new RectOffset(0, 0, 6, 6)
            };
            EditorGUI.LabelField(frameRect, attr.Text, style);

            // Property (with default label)
            float propY = frameRect.yMax + attr.BottomSpacing;
            var propRect = new Rect(position.x, propY, position.width, EditorGUI.GetPropertyHeight(property, null, true));

            // Create a custom label with the property name (e.g., "isMagazineEnabled" → "Is Blue Enabled")
            GUIContent propertyLabel = new GUIContent(ObjectNames.NicifyVariableName(property.name));
            EditorGUI.PropertyField(propRect, property, propertyLabel, true);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var attr = (HeaderFrameAttribute)attribute;
            float frame = Mathf.Max(24f, attr.FontSize + 16f);
            float prop = EditorGUI.GetPropertyHeight(property, null, true);
            return attr.TopSpacing + frame + attr.BottomSpacing + prop;
        }
    }
}