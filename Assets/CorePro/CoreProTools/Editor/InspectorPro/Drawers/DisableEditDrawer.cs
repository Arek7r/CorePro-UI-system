using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace InspectorPro.Editor
{
#if UNITY_EDITOR
    //[CustomPropertyDrawer(typeof(DisableEditAttribute))]
    [CustomPropertyDrawer(typeof(DisableEditIfAttribute), true)]
    public class DisableEditDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            bool shouldDisable = false;

            if (attribute is DisableEditAttribute)
            {
                shouldDisable = true;
            }
            else if (attribute is DisableEditIfAttribute cond)
            {
                // Find a field in the target object
                var targetObj = GetTargetObjectOfProperty(property);
                var fieldInfo = targetObj.GetType().GetField(cond.fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (fieldInfo != null) {
                    var comparedValue = fieldInfo.GetValue(targetObj);
                    bool equals = EqualsBoxed(comparedValue, cond.value);
                    if (cond.invert) equals = !equals;
                    shouldDisable = equals;
                }
            }

            bool prev = GUI.enabled;
            GUI.enabled = !shouldDisable && prev;

            EditorGUI.PropertyField(position, property, label, true);

            GUI.enabled = prev;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }
        
        object GetTargetObjectOfProperty(SerializedProperty prop)
        {
            object obj = prop.serializedObject.targetObject;
            var path = prop.propertyPath.Replace(".Array.data[", "[");
            var elements = path.Split('.');

            foreach (var element in elements.Take(elements.Length - 1))
            {
                if (element.Contains("["))
                {
                    var elementName = element.Substring(0, element.IndexOf("["));
                    var index = int.Parse(element.Substring(element.IndexOf("[")).Replace("[", "").Replace("]", ""));
                    var field = obj.GetType().GetField(elementName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    var list = field.GetValue(obj) as System.Collections.IList;
                    obj = list[index];
                }
                else
                {
                    var field = obj.GetType().GetField(element, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    obj = field.GetValue(obj);
                }
            }
            return obj;
        }


        // Helper for comparison
        private static bool EqualsBoxed(object a, object b)
        {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;
            if (a.GetType().IsEnum && b.GetType().IsEnum)
                return (int)a == (int)b;
            if (a is bool && b is bool)
                return (bool)a == (bool)b;
            if (a is int && b is int)
                return (int)a == (int)b;
            if (a is float && b is float)
                return Mathf.Approximately((float)a, (float)b);
            return a.Equals(b);
        }
    }
#endif
}