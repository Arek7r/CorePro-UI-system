namespace InspectorPro.Editor
{
#if UNITY_EDITOR
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;
    using UnityEditor;
    using UnityEngine;

    [CustomPropertyDrawer(typeof(ShowInInspectorAttribute))]
    public class ShowInInspectorDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // This drawer does not support serialized fields!
            EditorGUI.HelpBox(position, "ShowInInspector only works on *non-serialized* private fields. Use [ReadOnly] for serialized fields.",
                MessageType.Info);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight * 2f;
        }
    }

    [CustomEditor(typeof(MonoBehaviourCorePro), true)]
    public class ShowInInspectorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            if (target == null) 
                return;
            
            base.OnInspectorGUI();

            var fields = target.GetType()
                .GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            foreach (var field in fields)
            {
                var attr = Attribute.GetCustomAttribute(field, typeof(ShowInInspectorAttribute), true);
                if (attr == null) continue;
                if (field.IsStatic) continue;

                object value = field.GetValue(target);
                bool isReadonly = true;

                DrawReadonlyField(serializedObject, field, value, isReadonly);
            }
        }

        private void DrawReadonlyField(SerializedObject so, FieldInfo field, object value, bool isReadonly)
        {
            EditorGUI.BeginDisabledGroup(true);

            string displayName = ObjectNames.NicifyVariableName(field.Name);

            if (value == null)
            {
                EditorGUILayout.LabelField(displayName, "null");
            }
            else if (value is IList list && !(value is string))
            {
                DrawReadonlyList(displayName, list);
            }
            else
            {
                EditorGUILayout.LabelField(displayName, value.ToString());
            }

            EditorGUI.EndDisabledGroup();
        }

        private void DrawReadonlyList(string label, IList list)
        {
            bool expanded = SessionState.GetBool("ShowInInspector_" + label, true);
            expanded = EditorGUILayout.Foldout(expanded, $"{label} ({list.Count})", true);

            SessionState.SetBool("ShowInInspector_" + label, expanded);

            if (expanded)
            {
                EditorGUI.indentLevel++;
                for (int i = 0; i < list.Count; i++)
                {
                    object el = list[i];
                    string elLabel = $"Element {i}";
                    if (el == null)
                    {
                        EditorGUILayout.LabelField(elLabel, "null");
                    }
                    else
                    {
                        EditorGUILayout.LabelField(elLabel, el.ToString());
                    }
                }

                EditorGUI.indentLevel--;
            }
        }
    }
#endif
}