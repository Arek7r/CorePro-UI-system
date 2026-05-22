namespace InspectorPro.Editor
{
    using UnityEditor;
    using UnityEngine;
    using System.Reflection;

    [CustomPropertyDrawer(typeof(StructViewAttribute))]
    public class StructViewDrawer : PropertyDrawer
    {
        private const float ArrowWidth = 4;
        private const float FoldoutPadding = 12f;
        private const float HeaderSpacing = 6f; // Additional spacing for headers

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded)
                return EditorGUIUtility.singleLineHeight;

            float totalHeight = EditorGUIUtility.singleLineHeight;   // Main label

            SerializedProperty prop = property.Copy();
            prop.NextVisible(true);

            do
            {
                if (!prop.propertyPath.StartsWith(property.propertyPath))
                    break;

                // The height of the property itself
                float propertyHeight = EditorGUI.GetPropertyHeight(prop, true);
                totalHeight += propertyHeight;

                // Check if the field has the Header attribute
                if (HasHeaderAttribute(property.serializedObject.targetObject.GetType(), prop.name))
                {
                    totalHeight += HeaderSpacing; // Additional spacing for the header
                    totalHeight += EditorGUIUtility.singleLineHeight; // Height of the header itself
                }
            } while (prop.NextVisible(false));

            return totalHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // FoldoutH1 rect with padding from the left
            Rect foldoutRect = new Rect(position.x + FoldoutPadding, position.y, ArrowWidth, EditorGUIUtility.singleLineHeight);

            // Label rect moved even further to the right
            Rect labelRect = new Rect(position.x + FoldoutPadding + ArrowWidth, position.y, position.width - ArrowWidth - FoldoutPadding,
                EditorGUIUtility.singleLineHeight);

            // Draw foldout (arrow only, no label)
            bool expanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, GUIContent.none, true);
            if (expanded != property.isExpanded)
                property.isExpanded = expanded;

            // Draw label
            EditorGUI.LabelField(labelRect, label);

            // Click on the label
            if (Event.current.type == EventType.MouseDown && labelRect.Contains(Event.current.mousePosition))
            {
                property.isExpanded = !property.isExpanded;
                Event.current.Use();
            }

            if (!property.isExpanded)
                return;

            EditorGUI.indentLevel++;
            float y = position.y + EditorGUIUtility.singleLineHeight;
            SerializedProperty prop = property.Copy();
            prop.NextVisible(true);

            string lastDrawnHeader = null;

            do
            {
                if (!prop.propertyPath.StartsWith(property.propertyPath))
                    break;

                // Check that this field has the Header attribute
                string headerText = GetHeaderText(property.serializedObject.targetObject.GetType(), prop.name);

                // Draw header only once (do not duplicate)
                if (!string.IsNullOrEmpty(headerText) && headerText != lastDrawnHeader)
                {
                    y += HeaderSpacing; // Add spacing before the header

                    // Style for the header (similar to Unity's Header)
                    var headerStyle = new GUIStyle(EditorStyles.boldLabel);
                    headerStyle.fontSize = 12;

                    Rect headerRect = new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight);
                    EditorGUI.LabelField(headerRect, headerText, headerStyle);

                    y += EditorGUIUtility.singleLineHeight;
                    lastDrawnHeader = headerText;
                }

                // Draw field (without enabling disabled group - struct is read-only via PropertyDrawer)
                float propertyHeight = EditorGUI.GetPropertyHeight(prop, true);
                Rect propRect = new Rect(position.x, y, position.width, propertyHeight);

                EditorGUI.BeginDisabledGroup(true);
                EditorGUI.PropertyField(propRect, prop, true);
                EditorGUI.EndDisabledGroup();

                y += propertyHeight;
            } while (prop.NextVisible(false));

            EditorGUI.indentLevel--;
        }

        private bool HasHeaderAttribute(System.Type targetType, string fieldName)
        {
            return !string.IsNullOrEmpty(GetHeaderText(targetType, fieldName));
        }

        private string GetHeaderText(System.Type targetType, string fieldName)
        {
            try
            {
                // Search across the type hierarchy
                var currentType = targetType;
                while (currentType != null && currentType != typeof(object))
                {
                    var field = currentType.GetField(fieldName,
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);

                    if (field != null)
                    {
                        var headerAttr = field.GetCustomAttribute<HeaderAttribute>();
                        if (headerAttr != null)
                            return headerAttr.header;

                        // Also check other header attributes with InspectorPro
                        var customAttrs = field.GetCustomAttributes(typeof(System.Attribute), false);
                        foreach (var attr in customAttrs)
                        {
                            if (attr.GetType().Name.Contains("Header"))
                            {
                                // Attempt to retrieve text from properties/fields
                                var headerProp = attr.GetType().GetProperty("header") ??
                                                 attr.GetType().GetProperty("text") ??
                                                 attr.GetType().GetProperty("Header");
                                if (headerProp != null)
                                {
                                    var value = headerProp.GetValue(attr);
                                    return value?.ToString();
                                }
                            }
                        }
                    }

                    currentType = currentType.BaseType;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Error getting header for field {fieldName}: {ex.Message}");
            }

            return null;
        }
    }
}