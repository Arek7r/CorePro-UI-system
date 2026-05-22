using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace InspectorPro.Editor
{
#if UNITY_EDITOR
    /// <summary>
    /// Custom property drawer that triggers a specified method when the value of a property changes in the inspector.
    /// usage: [OnValueChanged(nameof(MethodName))]
    /// </summary>
    [CustomPropertyDrawer(typeof(OnValueChangedAttribute))]
    public class OnValueChangedDrawer : PropertyDrawer
    {
        /// <summary>
        /// Renders the property field in the Unity Inspector and invokes the specified method when the property value changes.
        /// </summary>
        /// <param name="position">The rectangle on the screen to use for the property GUI.</param>
        /// <param name="property">The serialized property being drawn.</param>
        /// <param name="label">The label of the property.</param>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUI.PropertyField(position, property, label, true);

                if (EditorGUI.EndChangeCheck())
                {
                    var onValueChanged = attribute as OnValueChangedAttribute;

                    property.serializedObject.ApplyModifiedProperties();

                    var obj = property.serializedObject.targetObject;
                    var method = obj.GetType().GetMethod(
                        onValueChanged.MethodName,
                        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                    if (method != null)
                    {
                        method.Invoke(obj, null);
                    }
                    else
                    {
                        Debug.LogError($"Method '{onValueChanged.MethodName}' not found on '{obj.GetType().Name}'");
                    }
                }
            }
    }
#endif

}