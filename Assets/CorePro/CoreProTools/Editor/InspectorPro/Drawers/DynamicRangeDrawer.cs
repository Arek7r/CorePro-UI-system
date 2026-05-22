using InspectorPro;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(DynamicRangeAttribute))]
public class DynamicRangeDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        DynamicRangeAttribute range = (DynamicRangeAttribute)attribute;

        SerializedProperty minProp = property.serializedObject.FindProperty(range.MinFieldName);
        SerializedProperty maxProp = property.serializedObject.FindProperty(range.MaxFieldName);

        float min = minProp != null ? minProp.floatValue : 0f;
        float max = maxProp != null ? maxProp.floatValue : 1f;

        property.floatValue = EditorGUI.Slider(position, label, property.floatValue, min, max);
    }
}