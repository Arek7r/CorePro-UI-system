using System;
using System.Linq;
using System.Reflection;
using InspectorPro;
using UnityEditor;
using UnityEngine;

namespace InspectorPro.Editor
{
    public static class InspectorProFieldHelper
    {
        public static bool DrawFieldWithConditionalAttributes(SerializedProperty prop, object parentObj, Type parentType, Rect rect, bool includeChildren = true)
        {
            GUI.enabled = true;
            var field = parentType.GetField(prop.name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field == null)
            {
                EditorGUI.PropertyField(rect, prop, includeChildren);
                return true; // Always drawn
            }

            // --- Title ---
            var title = field.GetCustomAttribute<TitleAttribute>();
            if (title != null)
            {
                float spaceBeforeTitle = CoreProEditorStyle.Asset.titleSpaceBefore;
                float spaceAfterTitle = 0f;

                rect.y += spaceBeforeTitle;

                var titleStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = CoreProEditorStyle.Asset.titleUnderlineFontSize,
                    fontStyle = CoreProEditorStyle.Asset.titleUnderlineFont,
                    alignment = TextAnchor.MiddleLeft
                };
    
                var titleRect = new Rect(rect.x, rect.y, rect.width, 18f);
                EditorGUI.LabelField(titleRect, title.Title, titleStyle);

                // Line underneath
                float lineY = titleRect.yMax + 2f;
                var lineRect = new Rect(rect.x, lineY, rect.width, 1f);
                EditorGUI.DrawRect(lineRect,CoreProEditorStyle.Asset.TitleUnderline);
    
                rect.y = lineRect.yMax + spaceAfterTitle;
            }

            // // --- Header ---
            // var header = field.GetCustomAttribute<HeaderAttribute>();
            // if (header != null)
            // {
            //     var headerStyle = new GUIStyle(EditorStyles.label)
            //     {
            //         fontSize = 12,
            //         fontStyle = FontStyle.Bold,
            //         alignment = TextAnchor.MiddleLeft
            //     };
            //     var headerRect = new Rect(rect.x, rect.y, rect.width, 18f);
            //     EditorGUI.LabelField(headerRect, header.header, headerStyle);
            //
            //     rect.y = headerRect.yMax + 4f;
            // }


            // ShowIf
            var showIf = (ShowIfAttribute)Attribute.GetCustomAttribute(field, typeof(ShowIfAttribute));
            if (showIf != null)
            {
                var compared = parentType.GetField(showIf.fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (compared != null)
                {
                    var val = compared.GetValue(parentObj);
                    bool equals = Equals(val, showIf.value);
                    if (showIf.invert ? equals : !equals)
                        return false; // Hide property
                }
            }

            // HideIf
            var hideIf = (HideIfAttribute)Attribute.GetCustomAttribute(field, typeof(HideIfAttribute));
            if (hideIf != null)
            {
                var compared = parentType.GetField(hideIf.fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (compared != null)
                {
                    var val = compared.GetValue(parentObj);
                    bool equals = Equals(val, hideIf.value);
                    if (hideIf.invert ? !equals : equals)
                        return false; // Hide property
                }
            }

            // DisableIf
            bool prevEnabled = GUI.enabled;
            var disableIf = (DisableIfAttribute)Attribute.GetCustomAttribute(field, typeof(DisableIfAttribute));
            if (disableIf != null)
            {
                var compared = parentType.GetField(disableIf.FieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (compared != null)
                {
                    var val = compared.GetValue(parentObj);
                    bool equals = Equals(val, disableIf.Value);
                    if (disableIf.Invert ? !equals : equals)
                        GUI.enabled = false;
                }
            }

            var inlineSO = field?.GetCustomAttribute<InlineSOAttribute>();
            if (inlineSO != null)
            {
                // --- Below all the drawing logic of InlineSO as in InlineSODrawer ---
                prop.isExpanded = EditorGUI.Foldout(
                    new Rect(rect.x, rect.y, EditorGUIUtility.labelWidth, EditorGUIUtility.singleLineHeight),
                    prop.isExpanded, prop.displayName, true
                );

                EditorGUI.ObjectField(
                    new Rect(rect.x + EditorGUIUtility.labelWidth, rect.y,
                        rect.width - EditorGUIUtility.labelWidth, EditorGUIUtility.singleLineHeight),
                    prop, GUIContent.none
                );

                if (prop.objectReferenceValue != null && prop.isExpanded)
                {
                    EditorGUI.indentLevel++;
                    SerializedObject targetObject = new SerializedObject(prop.objectReferenceValue);
                    SerializedProperty subProp = targetObject.GetIterator();
                    float y = rect.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                    subProp.NextVisible(true);
                    while (subProp.NextVisible(false))
                    {
                        if (subProp.name == "m_Script") continue;
                        float h = EditorGUI.GetPropertyHeight(subProp, true);
                        EditorGUI.PropertyField(
                            new Rect(rect.x, y, rect.width, h),
                            subProp, true
                        );
                        y += h + EditorGUIUtility.standardVerticalSpacing;
                    }

                    targetObject.ApplyModifiedProperties();
                    EditorGUI.indentLevel--;
                }

                return true;
            }


            EditorGUI.PropertyField(rect, prop, includeChildren);
            GUI.enabled = prevEnabled;
            return true;
        }

        public static object GetTargetObjectOfProperty(SerializedProperty prop)
        {
            if (prop == null) return null;
            string path = prop.propertyPath.Replace(".Array.data[", "[");
            object obj = prop.serializedObject.targetObject;
            var elements = path.Split('.');
            foreach (var element in elements.Take(elements.Length - 1))
            {
                if (element.Contains("["))
                {
                    string fieldName = element.Substring(0, element.IndexOf("["));
                    int index = Convert.ToInt32(element.Substring(element.IndexOf("[")).Replace("[", "").Replace("]", ""));
                    var field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                    var list = field.GetValue(obj) as System.Collections.IList;
                    obj = list[index];
                }
                else
                {
                    var field = obj.GetType().GetField(element, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                    if (field == null) return null;
                    obj = field.GetValue(obj);
                }
            }

            return obj;
        }
        public static float GetConditionalFieldHeight(FieldInfo field, SerializedProperty prop)
        {
            float height = EditorGUI.GetPropertyHeight(prop, true);

            if (field != null)
            {
                if (field.GetCustomAttribute<TitleAttribute>() != null)
                    height += 18f + 2f + 4f;
                // if (field.GetCustomAttribute<HeaderAttribute>() != null)
                //     height += 18f + 4f;
            }

            return height;
        }
    }
}