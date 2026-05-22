using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
namespace InspectorPro.Editor
{
    [CustomPropertyDrawer(typeof(TagAttribute))]
    public class TagAttributeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            if (string.IsNullOrEmpty(property.stringValue))
                property.stringValue = "Untagged";
    
            property.stringValue = EditorGUI.TagField(position, label, property.stringValue);
            EditorGUI.EndProperty();
        }
    }
}
#endif
