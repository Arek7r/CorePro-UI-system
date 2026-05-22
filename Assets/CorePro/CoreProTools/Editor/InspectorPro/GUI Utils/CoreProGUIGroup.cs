using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace InspectorPro.Editor
{
    public static class CoreProGUIGroup
    {
        public static void BeginGroup(string groupName)
        {
            CoreProEditorStyle.DrawGroupHeader(groupName);

            EditorGUILayout.BeginVertical(CoreProEditorStyle.Frames);
            EditorGUILayout.BeginVertical(CoreProEditorStyle.GroupContentBackground);
            EditorGUILayout.BeginVertical(CoreProEditorStyle.Content);
        }

        public static void EndGroup()
        {
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndVertical();
            CoreProEditorStyle.DrawGroupFrames(GUILayoutUtility.GetLastRect());
            
            GUILayout.Space(-1);
        }

        
        
        /// <summary>
        /// Draws group foldout header and returns the expanded state.
        /// NOTE: State management is handled by the caller (editor).
        /// </summary>
        public static bool BeginGroupFoldout(string groupName, bool isExpanded, out bool newState)
        {
            CoreProEditorStyle.DrawGroupFoldoutHeader(groupName, isExpanded, out newState);
            
            if (newState)
            {
                EditorGUILayout.BeginVertical(CoreProEditorStyle.GroupContentBackground); // its for draw background
                EditorGUILayout.BeginVertical(CoreProEditorStyle.Content); // its for margin
            }
            return newState;
        }
        
        /// <summary>
        /// Ends group foldout layout if it was expanded.
        /// </summary>
        public static void EndGroupFoldout(bool wasExpanded)
        {
            if (wasExpanded)
            {
                EditorGUILayout.EndVertical(); // close GroupContentBackground
                EditorGUILayout.EndVertical(); // close Content
                CoreProEditorStyle.DrawGroupFrames(GUILayoutUtility.GetLastRect());
            }
            
            GUILayout.Space( -1);
        }
            
        public static GroupScope Foldout(string groupName, ref bool isExpanded)
        {
            bool newState;
            BeginGroupFoldout(groupName, isExpanded, out newState);
            isExpanded = newState;
            return new GroupScope(newState);
        }

        public ref struct GroupScope
        {
            private readonly bool _expanded;
            public bool IsOpen => _expanded;

            public GroupScope(bool expanded)
            {
                _expanded = expanded;
            }

            public void Dispose()
            {
                EndGroupFoldout(_expanded);
            }
        }
        
    }
}