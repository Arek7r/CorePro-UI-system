using System.Collections.Generic;
using System.Reflection;
using InspectorPro.Editor;

namespace InspectorPro.Attributes.Editor
{
    using UnityEditor;
    using UnityEngine;
    using System;

    [CustomPropertyDrawer(typeof(ClassViewFoldout))]
    public class FoldoutBoxDrawer : PropertyDrawer
    {
        private const int FieldSpacing = 2;
        private float HeaderHeight => CoreProEditorStyle.GroupHeaderHeight;
        private const int SpaceAtBoxEnd = 6;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            bool expanded = property.isExpanded;
            float height = HeaderHeight;

            if (expanded)
            {
                // Find all direct children of this property
                var childProperties = GetDirectChildren(property);

                foreach (var childProp in childProperties)
                {
                    if (childProp.name == "m_Script") continue;

                    var parentObj = property.serializedObject.targetObject;
                    var ownerInstance = InspectorProFieldHelper.GetTargetObjectOfProperty(childProp);
                    var parentType = parentObj.GetType();
                    var ownerType = ownerInstance?.GetType() ?? parentType;
                    var field = ownerType.GetField(childProp.name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                    float fieldHeight = EditorGUI.GetPropertyHeight(childProp, true);
                    if (field != null)
                        fieldHeight = InspectorProFieldHelper.GetConditionalFieldHeight(field, childProp);

                    height += fieldHeight + FieldSpacing;
                }

                height += FieldSpacing;
                height += SpaceAtBoxEnd;
            }

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            ClassViewFoldout classViewFoldoutBox = attribute as ClassViewFoldout;
            string headerLabel = classViewFoldoutBox.Header ?? ObjectNames.NicifyVariableName(property.displayName);

            // Header
            CoreProEditorStyle.DrawGroupBody(position, property.isExpanded);
            CoreProEditorStyle.DrawGroupFoldoutHeader(headerLabel, property.isExpanded, out bool newIsExpanded, position, out var contentRect);
            property.isExpanded = newIsExpanded;
            
            if (property.isExpanded)
            {
                var childProperties = GetDirectChildren(property);
                
                float y = position.y + HeaderHeight + FieldSpacing;
                foreach (var childProp in childProperties)
                {
                    if (childProp.name == "m_Script") continue;

                    var parentObj = property.serializedObject.targetObject;
                    var parentType = parentObj.GetType();
                    var ownerInstance = InspectorProFieldHelper.GetTargetObjectOfProperty(childProp);
                    var ownerType = ownerInstance?.GetType() ?? parentType;
                    var field = ownerType.GetField(childProp.name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                    float h = EditorGUI.GetPropertyHeight(childProp, true);
                    if (field != null)
                        h = InspectorProFieldHelper.GetConditionalFieldHeight(field, childProp);

                    var fieldRect = new Rect(contentRect.x, y, contentRect.width, h);

                    if (ReferenceEquals(ownerInstance, parentObj))
                    {
                        if (InspectorProFieldHelper.DrawFieldWithConditionalAttributes(childProp, parentObj, parentType, fieldRect, true))
                            y += h + FieldSpacing;
                    }
                    else
                    {
                        if (InspectorProFieldHelper.DrawFieldWithConditionalAttributes(childProp, ownerInstance, ownerType, fieldRect, true))
                            y += h + FieldSpacing;
                    }
                }
            }
        }


        private List<SerializedProperty> GetDirectChildren(SerializedProperty parent)
        {
            List<SerializedProperty> children = new List<SerializedProperty>();

            // Copy the property and move on to the first child
            var prop = parent.Copy();
            string parentPath = parent.propertyPath;

            // Go to the first child
            if (prop.Next(true))
            {
                do
                {
                    // Check if it is a direct child
                    if (prop.propertyPath.StartsWith(parentPath + "."))
                    {
                        // Check that this is exactly one level down
                        string relativePath = prop.propertyPath.Substring(parentPath.Length + 1);
                        if (!relativePath.Contains("."))
                        {
                            children.Add(prop.Copy());
                        }
                    }
                    else if (!prop.propertyPath.StartsWith(parentPath))
                    {
                        // We have gone beyond the parent
                        break;
                    }
                } while (prop.Next(false));
            }

            return children;
        }
    }
}