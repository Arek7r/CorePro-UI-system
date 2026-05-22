#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace InspectorPro.Editor
{
    [CustomPropertyDrawer(typeof(ClassViewFlat))]
    public class FlatDisplayDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // We skip the foldout, display all childe "inline"
            SerializedProperty prop = property.Copy();
            SerializedProperty endProp = prop.GetEndProperty();
            float y = position.y;
            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel++;

            bool first = true;
            while (prop.NextVisible(first) && !SerializedProperty.EqualContents(prop, endProp))
            {
                float h = EditorGUI.GetPropertyHeight(prop, true);
                Rect r = new Rect(position.x, y, position.width, h);
                EditorGUI.PropertyField(r, prop, true);
                y += h + EditorGUIUtility.standardVerticalSpacing;
                first = false;
            }

            EditorGUI.indentLevel = indent;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = 0f;
            SerializedProperty prop = property.Copy();
            SerializedProperty endProp = prop.GetEndProperty();
            bool first = true;
            while (prop.NextVisible(first) && !SerializedProperty.EqualContents(prop, endProp))
            {
                height += EditorGUI.GetPropertyHeight(prop, true) + EditorGUIUtility.standardVerticalSpacing;
                first = false;
            }
            return Mathf.Max(0, height - EditorGUIUtility.standardVerticalSpacing); // delete last spacing
        }
    }
}
#endif