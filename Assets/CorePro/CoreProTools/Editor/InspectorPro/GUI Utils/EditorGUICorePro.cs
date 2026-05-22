using UnityEditor;
using UnityEngine;

namespace InspectorPro.Editor
{
    public static class EditorGUICorePro
    {
        public static void Debug(Rect testedRect)   
        {   
            EditorGUI.DrawRect( testedRect, new Color(1f, 0f, 0f, 0.3f));
        }

        public static void DrawHeader(string groupName)
        {
            CoreProEditorStyle.DrawGroupHeader(groupName);
        }
        
        public static void DrawBannerSimple(string groupName)
        {
            CoreProEditorStyle.DrawBannerSimple(groupName);
        }
        
        public static void Test(ref bool isExpanded)
        {
            CoreProEditorStyle.DrawFoldoutH1Wide(ref isExpanded,null,"test");
        } 
    }
}