using System.Linq;
using UnityEditor;
using UnityEngine;

namespace  InspectorPro.Editor.Drawers
{
    //[CustomEditor(typeof(CoreProEditorStyleSO))]
    public class CoreProEditorStyleSOEditor : UnityEditor.Editor
    {
        private float labelWidth = 220;
        private float colorWidth = 80;

        string[] drawnProps = new[]
        {
            "foldoutH1TitleBackgroundPro", "foldoutH1TitleBackground",
            "foldoutH1TitleFramePro", "foldoutH1TitleFrame",
            "foldoutH1ContentBackgroundDark", "foldoutH1ContentBackground",
           
            //H2
            "foldoutH2TitleBackgroundPro", "foldoutH2TitleBackground",
            "foldoutH2TitleFramePro", "foldoutH2TitleFrame",
  
            // --- Group ---   
            "GroupHeaderPro", "GroupHeader",
            "GroupFramePro", "GroupFrame",
            "GroupContentBackgroundPro", "GroupContentBackground",
            // Tags color
            "blueTagPro", "blueTag",
            "redTagPro", "redTag",
            // Other color
            "titleUnderlineDark", "titleUnderline",
            "headerProBackgroundPro", "headerProBackground",
            
            // BoxEditor color
            "BoxContentBackgroundPro", "BoxContentBackground",
        };


        public override void OnInspectorGUI()
        {
            if (target == null) 
                return;
            
            serializedObject.Update();

            SerializedProperty scriptProp = serializedObject.FindProperty("m_Script");
            if (scriptProp != null)
            {
                GUI.enabled = false;
                EditorGUILayout.PropertyField(scriptProp, true);
                GUI.enabled = true;

            }

            if (GUILayout.Button("Clear Cache"))
            {
                (target as CoreProEditorStyleSO)?.ClearCache();
            }

            DrawHeader("FoldoutH1 H1");
            DrawColorRow("Title Background", "foldoutH1TitleBackgroundDark", "foldoutH1TitleBackground");
            DrawColorRow("Title Frame", "foldoutH1TitleFrameDark", "foldoutH1TitleFrame");
            DrawColorRow("Content Background", "foldoutH1ContentBackgroundDark", "foldoutH1ContentBackground");

            DrawHeader("FoldoutH1 H2");
            DrawColorRow("Title Background", "foldoutH2TitleBackgroundPro", "foldoutH2TitleBackground");
            DrawColorRow("Title Frame", "foldoutH2TitleFramePro", "foldoutH2TitleFrame");
            //DrawColorRow("Content Background", "foldoutH3ContentBackgroundDark", "foldoutH3ContentBackground");

            DrawHeader("Group Attribute");
            DrawColorRow("Group Header", "GroupHeaderPro", "GroupHeader");
            DrawColorRow("Group Frame", "GroupFramePro", "GroupFrame");
            DrawColorRow("Group Content", "GroupContentBackgroundPro", "GroupContentBackground");

            DrawHeader("Tags color");
            DrawColorRow("Blue Tag color", "blueTagPro", "blueTag");
            DrawColorRow("Red Tag color", "redTagPro", "redTag");

            DrawHeader("Other colors");
            DrawColorRow("HeaderPro Background", "headerProBackgroundPro", "headerProBackground");
            DrawColorRow("Title underline", "titleUnderlineDark", "titleUnderline");
            
            DrawHeader("Box Editor");
            DrawColorRow("Box content", "BoxContentBackgroundPro", "BoxContentBackground");
            
            
            GUILayout.Space(20);
            SerializedProperty prop = serializedObject.GetIterator();

            bool first = true;
            while (prop.NextVisible(first))
            {
                first = false;
                if (drawnProps.Contains(prop.name))
                    continue;

                if (prop.name == "m_Script")
                    continue;

                EditorGUILayout.PropertyField(prop, true);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawHeader(string label)
        {
            GUILayout.Space(8);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(label, EditorStyles.boldLabel, GUILayout.Width(labelWidth));
            GUILayout.Label("Pro", EditorStyles.boldLabel, GUILayout.Width(colorWidth));
            GUILayout.Label("Normal", EditorStyles.boldLabel, GUILayout.Width(colorWidth));
            EditorGUILayout.EndHorizontal();
        }

        void DrawColorRow(string label, string darkPropName, string lightPropName)
        {
            var darkProp = serializedObject.FindProperty(darkPropName);
            var lightProp = serializedObject.FindProperty(lightPropName);

            if (darkProp == null || lightProp == null)
                return;

            EditorGUILayout.BeginHorizontal();

            GUILayout.Label(label, GUILayout.Width(labelWidth));
            EditorGUILayout.PropertyField(darkProp, GUIContent.none, GUILayout.Width(colorWidth));
            GUILayout.Space(5);
            EditorGUILayout.PropertyField(lightProp, GUIContent.none, GUILayout.Width(colorWidth));

            EditorGUILayout.EndHorizontal();
        }
    }
}