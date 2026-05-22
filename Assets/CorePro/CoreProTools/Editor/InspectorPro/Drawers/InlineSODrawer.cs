using UnityEditor;
#if UNITY_EDITOR
using UnityEngine;
#endif

#if UNITY_EDITOR
namespace InspectorPro.Editor
{
    [CustomPropertyDrawer(typeof(InlineSOAttribute))]
    public class InlineSODrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float totalHeight = EditorGUIUtility.singleLineHeight;
            if (property.objectReferenceValue != null && property.isExpanded)
            {
                SerializedObject targetObject = new SerializedObject(property.objectReferenceValue);
                SerializedProperty prop = targetObject.GetIterator();
                prop.NextVisible(true); // m_Script

                while (prop.NextVisible(false))
                {
                    totalHeight += EditorGUI.GetPropertyHeight(prop, true) + EditorGUIUtility.standardVerticalSpacing;
                }
                totalHeight += EditorGUIUtility.standardVerticalSpacing;
            }
            return totalHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // --- Custom Label Logic ---
            string foldoutLabel = label.text; // Default to "Element X"
            
            if (property.objectReferenceValue != null)
            {
                SerializedObject so = new SerializedObject(property.objectReferenceValue);
                // Try to find a property that could serve as a better title
                // We check for "DisplayName" as seen in your screenshot
                SerializedProperty nameProp = so.FindProperty("DisplayName") ?? 
                                             so.FindProperty("displayName") ?? 
                                             so.FindProperty("m_Name");

                if (nameProp != null && !string.IsNullOrEmpty(nameProp.stringValue))
                {
                    foldoutLabel = nameProp.stringValue;
                }
            }

            // FoldoutH1 + Custom Label
            property.isExpanded = EditorGUI.Foldout(
                new Rect(position.x, position.y, EditorGUIUtility.labelWidth, EditorGUIUtility.singleLineHeight),
                property.isExpanded, new GUIContent(foldoutLabel), true
            );

            // ObjectField (to the right of the label).
            EditorGUI.ObjectField(
                new Rect(position.x + EditorGUIUtility.labelWidth, position.y,
                    position.width - EditorGUIUtility.labelWidth, EditorGUIUtility.singleLineHeight),
                property, GUIContent.none
            );

            // Draw inline SO
            if (property.objectReferenceValue != null && property.isExpanded)
            {
                EditorGUI.indentLevel++;
                SerializedObject targetObject = new SerializedObject(property.objectReferenceValue);
                SerializedProperty prop = targetObject.GetIterator();
                float y = position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                prop.NextVisible(true);
                while (prop.NextVisible(false))
                {
                    if (prop.name == "m_Script") continue;

                    float h = EditorGUI.GetPropertyHeight(prop, true);
                    EditorGUI.PropertyField(
                        new Rect(position.x, y, position.width, h),
                        prop, true
                    );
                    y += h + EditorGUIUtility.standardVerticalSpacing;
                }
                targetObject.ApplyModifiedProperties();
                EditorGUI.indentLevel--;
            }
        }
    }
}
#endif