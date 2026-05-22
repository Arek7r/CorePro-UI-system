using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace InspectorPro.Editor
{
    public class ModuleCreatePopup : PopupWindowContent
    {
        private List<Type> moduleTypes;
        private Action<Type> onTypeSelected;
        private Vector2 scroll;
        private Type baseType;

        public ModuleCreatePopup(Type baseType, Action<Type> onTypeSelected)
        {
            this.baseType = baseType;
            moduleTypes = TypeCache.GetTypesDerivedFrom(baseType)
                .Where(t => !t.IsAbstract && t.IsClass && typeof(MonoBehaviour).IsAssignableFrom(t))
                .OrderBy(t => t.Name)
                .ToList();
            this.onTypeSelected = onTypeSelected;
        }

        public override Vector2 GetWindowSize() => new Vector2(320, 320);

        public override void OnGUI(Rect rect)
        {
            EditorGUILayout.HelpBox(
                $"Below are all MonoBehaviour scripts that derive from: {baseType.Name}", 
                MessageType.Info);

            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("Select Module Type:", EditorStyles.boldLabel);
        
            EditorGUILayout.Space(6);

            scroll = EditorGUILayout.BeginScrollView(scroll);
            string className;
            
            foreach (var type in moduleTypes)
            {
                className = ObjectNames.NicifyVariableName(type.Name);

                if (GUILayout.Button(className, GUILayout.Height(24)))
                {
                    onTypeSelected?.Invoke(type);
                    editorWindow.Close();
                }
            }
            EditorGUILayout.EndScrollView();
        }
    }
}