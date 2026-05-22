using System;
using UnityEditor;
using UnityEngine;

namespace InspectorPro.Editor
{
    [Serializable]
    public class CoreProGUIFoldout
    {
        #region Variables

        private GUIStyle foldoutTitleStyleH1;
        private GUIStyle frames;
        private GUIStyle foldoutContentBackground;
        private GUIStyle statusLabelStyle;

        private const int IndentLevel = 0;
        private const string Enabled = "enabled";
        private const string Disabled = "disabled";

        private static Color _enabledColor = Color.green;
        private static Color _disabledColor = Color.gray;
        float arrowSize => CoreProEditorStyle.Asset.arrowSizeFoldout;

        private Color cachedColor;

        #endregion
        
        /// <summary>
        /// Initialises GUI styles H1.
        /// </summary>
        private void InitializeStylesH1(EditorStyleTag tag = EditorStyleTag.None)
        {
            if (tag != EditorStyleTag.None)
            {
                foldoutTitleStyleH1 = CoreProEditorStyle.Asset.GetStyle_Foldout_HeaderBackground_H1(tag);
            }
            else
            {
                foldoutTitleStyleH1 = CoreProEditorStyle.Asset.GetStyle_Foldout_HeaderBackground_H1();
            }

            frames = CoreProEditorStyle.Asset.GetStyle_Foldout_HeaderFrame_H1();
            foldoutContentBackground = CoreProEditorStyle.Asset.GetStyle_Foldout_ContentBackground_H1();

            statusLabelStyle = CoreProEditorStyle.Asset.GetOrCreateStyle(EditorStyleKey.FoldoutStatusLabelH1, false,
                () => new GUIStyle
                {
                    fontSize = 11,
                    alignment = TextAnchor.MiddleRight,
                    normal = { textColor = _disabledColor }
                });
        }

        public void UpdateColorEnabled(Color newColor)
        {
            _enabledColor = newColor;
        }

        private void UpdateStatusLabelStyle(bool isEnabled)
        {
            statusLabelStyle.normal.textColor = isEnabled ? _enabledColor : _disabledColor;
        }

        /// <summary>
        /// Draws a wide foldout header spanning the full inspector width.
        /// </summary>
        /// <param name="foldout">Reference to the foldout expansion state.</param>
        /// <param name="enabledProperty">Optional SerializedProperty for enabled/disabled toggle.</param>
        /// <param name="title">The title text to display.</param>
        /// <param name="prefsKey">Optional EditorPrefs key for persisting foldout state.</param>
        /// <param name="headerHeight">Height of the header (default 35).</param>
        /// <returns>The current foldout expansion state.</returns>
        /// <summary>
        /// Draws a wide foldout header spanning the full inspector width.
        /// </summary>
        /// <param name="foldout">Reference to the foldout expansion state.</param>
        /// <param name="enabledProperty">Optional SerializedProperty for enabled/disabled toggle.</param>
        /// <param name="title">The title text to display.</param>
        /// <param name="prefsKey">Optional EditorPrefs key for persisting foldout state.</param>
        /// <param name="headerHeight">Height of the header (default 35).</param>
        /// <returns>The current foldout expansion state.</returns>
        public bool DrawFoldoutH1Wide(ref bool foldout, SerializedProperty enabledProperty, string title, string prefsKey = null, float headerHeight = 35, bool hideArrow = false)
        {
            // 1. Prepare rects
            Rect totalRect = EditorGUILayout.GetControlRect(false, CoreProEditorStyle.Asset.foldoutWide_Header_H1_Height);
            Rect bgRect = new Rect(totalRect.x - 20, totalRect.y, totalRect.width + 30, totalRect.height);
            Rect upperLineRect = new Rect(bgRect.x, bgRect.y, bgRect.width, 1);
            Rect bottomLineRect = new Rect(bgRect.x, bgRect.yMax - 1, bgRect.width, 1);
            //Rect accentRect = new Rect(bgRect.x, bgRect.y, 4, bgRect.height);
 
            // 2. Draw Backgrounds
            EditorGUI.DrawRect(bgRect,         CoreProEditorStyle.Asset.FoldoutWide_HeaderBackground_H1);
            EditorGUI.DrawRect(upperLineRect,  CoreProEditorStyle.Asset.FoldoutWide_HeaderFrame_H1);
            EditorGUI.DrawRect(bottomLineRect, CoreProEditorStyle.Asset.FoldoutWide_HeaderFrame_H1);
            //EditorGUI.DrawRect(accentRect, new Color(0.15f, 0.45f, 0.85f));
 
            // 3. Restore foldout state from EditorPrefs
            if (prefsKey != null && EditorPrefs.HasKey(prefsKey))
            {
                foldout = EditorPrefs.GetBool(prefsKey, foldout);
            }
 
            // 4. Handle click events for foldout toggle
            Event e = Event.current;
            if (e.type == EventType.MouseDown && bgRect.Contains(e.mousePosition) && e.button == 0)
            {
                foldout = !foldout;
                if (prefsKey != null)
                {
                    EditorPrefs.SetBool(prefsKey, foldout);
                }
                e.Use();
                GUI.changed = true;
            }
            // 5. Draw foldout arrow indicator
            if (!hideArrow)
            {
                Rect arrowRect = new Rect(bgRect.x + CoreProEditorStyle.Asset.foldoutWide_Header_H1_ArrowOffset, bgRect.y + (bgRect.height - arrowSize) / 2, arrowSize, arrowSize);
                EditorGUI.Foldout(arrowRect, foldout, GUIContent.none, false);
            }
 
            // 6. Draw Label with Rich Text
            float h1LabelOffset = hideArrow ? 8f : CoreProEditorStyle.Asset.foldoutWide_Header_H1_LabelOffset;
            Rect labelRect = new Rect(totalRect.x + h1LabelOffset, totalRect.y, totalRect.width, totalRect.height);
            EditorGUI.LabelField(labelRect, title.ToUpper(), CoreProEditorStyle.Asset.FoldoutWide_H1_TitleStyle);
 
            // Color debugColor = new Color(1f, 0f, 0f, 0.3f); 
            // EditorGUI.DrawRect(labelRect, debugColor);
            
            // 7. Draw enabled toggle if property is provided
            if (enabledProperty != null)
            {
                float toggleWidth = 16f;
                Rect toggleRect = new Rect(bgRect.xMax - toggleWidth - 10, bgRect.y + (bgRect.height - toggleWidth) / 2, toggleWidth, toggleWidth);
                enabledProperty.boolValue = EditorGUI.Toggle(toggleRect, enabledProperty.boolValue);
            }
 
            EditorGUILayout.Space(2);
            
            return foldout;
        }
        
        /// <summary>
        /// Draws a wide foldout header spanning the full inspector width.
        /// </summary>
        /// <param name="foldout">Reference to the foldout expansion state.</param>
        /// <param name="enabledProperty">Optional SerializedProperty for enabled/disabled toggle.</param>
        /// <param name="title">The title text to display.</param>
        /// <param name="prefsKey">Optional EditorPrefs key for persisting foldout state.</param>
        /// <param name="headerHeight">Height of the header (default 35).</param>
        /// <returns>The current foldout expansion state.</returns>
        public bool DrawFoldoutH2Wide(ref bool foldout, SerializedProperty enabledProperty, string title, string prefsKey = null, float headerHeight = 28, bool hideArrow = false)
        {
            // 1. Prepare rects
            Rect totalRect = EditorGUILayout.GetControlRect(false, CoreProEditorStyle.Asset.foldoutWide_H2Height);
            Rect bgRect = new Rect(totalRect.x - 20, totalRect.y, totalRect.width + 30, totalRect.height);
            Rect upperLineRect = new Rect(bgRect.x, bgRect.y, bgRect.width, 1);
            Rect bottomLineRect = new Rect(bgRect.x, bgRect.yMax - 1, bgRect.width, 1);
            //Rect accentRect = new Rect(bgRect.x, bgRect.y, 4, bgRect.height);
 
            // 2. Draw Backgrounds
            EditorGUI.DrawRect(bgRect,         CoreProEditorStyle.Asset.FoldoutWide_HeaderBackground_H2);
            EditorGUI.DrawRect(upperLineRect,  CoreProEditorStyle.Asset.FoldoutWide_HeaderFrame_H2);
            EditorGUI.DrawRect(bottomLineRect, CoreProEditorStyle.Asset.FoldoutWide_HeaderFrame_H2);
 
            // 3. Restore foldout state from EditorPrefs
            if (prefsKey != null && EditorPrefs.HasKey(prefsKey))
            {
                foldout = EditorPrefs.GetBool(prefsKey, foldout);
            }
 
            // 4. Handle click events for foldout toggle
            Event e = Event.current;
            if (e.type == EventType.MouseDown && bgRect.Contains(e.mousePosition) && e.button == 0)
            {
                foldout = !foldout;
                if (prefsKey != null)
                {
                    EditorPrefs.SetBool(prefsKey, foldout);
                }
                e.Use();
                GUI.changed = true;
            }
 
            // 5. Draw foldout arrow indicator
            if (!hideArrow)
            {
                float arrowOffsetX = 20f;
                Rect arrowRect = new Rect(bgRect.x + arrowOffsetX, bgRect.y + (bgRect.height - arrowSize) / 2, arrowSize, arrowSize);
                EditorGUI.Foldout(arrowRect, foldout, GUIContent.none, false);
            }
 
            // 6. Draw Label with Rich Text
            float h2LabelOffset = hideArrow ? 8f : CoreProEditorStyle.Asset.foldoutWide_H2LabelOffset;
            string headerText = $" <color=#D2D2D2>{title.ToUpper()}</color>";
            Rect labelRect = new Rect(totalRect.x + h2LabelOffset, totalRect.y, totalRect.width, totalRect.height);
            EditorGUI.LabelField(labelRect, headerText, CoreProEditorStyle.Asset.FoldoutWide_H2_TitleStyle);
 
            // Color debugColor = new Color(1f, 0f, 0f, 0.3f); 
            // EditorGUI.DrawRect(labelRect, debugColor);
            
            // 7. Draw enabled toggle if property is provided
            if (enabledProperty != null)
            {
                float toggleWidth = 16f;
                Rect toggleRect = new Rect(bgRect.xMax - toggleWidth - 10, bgRect.y + (bgRect.height - toggleWidth) / 2, toggleWidth, toggleWidth);
                enabledProperty.boolValue = EditorGUI.Toggle(toggleRect, enabledProperty.boolValue);
            }
 
            EditorGUILayout.Space(2);
            
            return foldout;
        }
        
        public bool DrawFoldoutModule(ref bool foldout, SerializedProperty enabledProperty, string title, string prefsKey = null, float headerHeight = 35)
        {
            // 1. Prepare rects
            Rect totalRect = EditorGUILayout.GetControlRect(false, CoreProEditorStyle.Asset.foldoutModule_Height);
            Rect bgRect = new Rect(totalRect.x - 20, totalRect.y, totalRect.width + 30, totalRect.height);
            Rect upperLineRect = new Rect(bgRect.x, bgRect.y, bgRect.width, 1);
            Rect bottomLineRect = new Rect(bgRect.x, bgRect.yMax - 1, bgRect.width, 1);
            //Rect accentRect = new Rect(bgRect.x, bgRect.y, 4, bgRect.height);

            // 2. Draw Backgrounds
            EditorGUI.DrawRect(bgRect,         CoreProEditorStyle.Asset.FoldoutModule_HeaderBackground);
            EditorGUI.DrawRect(upperLineRect,  CoreProEditorStyle.Asset.FoldoutModule_HeaderFrame);
            EditorGUI.DrawRect(bottomLineRect, CoreProEditorStyle.Asset.FoldoutModule_HeaderFrame);
            //EditorGUI.DrawRect(accentRect, new Color(0.15f, 0.45f, 0.85f));

            // 3. Restore foldout state from EditorPrefs
            if (prefsKey != null && EditorPrefs.HasKey(prefsKey))
            {
                foldout = EditorPrefs.GetBool(prefsKey, foldout);
            }

            // 4. Handle click events for foldout toggle
            Event e = Event.current;
            if (e.type == EventType.MouseDown && bgRect.Contains(e.mousePosition) && e.button == 0)
            {
                foldout = !foldout;
                if (prefsKey != null)
                {
                    EditorPrefs.SetBool(prefsKey, foldout);
                }
                e.Use();
                GUI.changed = true;
            }

            // 5. Draw foldout arrow indicator
            Rect arrowRect = new Rect(bgRect.x + CoreProEditorStyle.Asset.foldoutModule_ArrowOffset, bgRect.y + (bgRect.height - arrowSize) / 2, arrowSize, arrowSize);
            EditorGUI.Foldout(arrowRect, foldout, GUIContent.none, false);

            // 6. Draw Label with Rich Text
            Rect labelRect = new Rect(totalRect.x + CoreProEditorStyle.Asset.foldoutModule_LabelOffset, totalRect.y, totalRect.width, totalRect.height);
            EditorGUI.LabelField(labelRect, title.ToUpper(), CoreProEditorStyle.Asset.FoldoutModule_LabelStyle);

            // Color debugColor = new Color(1f, 0f, 0f, 0.3f); 
            // EditorGUI.DrawRect(labelRect, debugColor);
            
            // 7. Draw enabled toggle if property is provided
            if (enabledProperty != null)
            {
                float toggleWidth = 16f;
                Rect toggleRect = new Rect(bgRect.xMax - toggleWidth - 10, bgRect.y + (bgRect.height - toggleWidth) / 2, toggleWidth, toggleWidth);
                enabledProperty.boolValue = EditorGUI.Toggle(toggleRect, enabledProperty.boolValue);
            }

            EditorGUILayout.Space(2);
            
            return foldout;
        }
        
        
        // =================================================================================
        // =============================== SECTION H1 =======================================
        // =================================================================================
        
        /// <summary>
        /// Draws H1 foldout header with 1px frame on all sides.
        /// </summary>
        public bool DrawFoldoutH1(ref bool foldout, SerializedProperty enabledProperty, string title, string prefsKey = null, float headerHeight = 0)
        {
            if (headerHeight == 0)
                headerHeight = CoreProEditorStyle.Asset.foldout_Header_H1_Height;

            EditorStyleTag tag = EditorStyleTag.None;

            if (title == "Magazines")
                tag = EditorStyleTag.Blue;
            else if (title == "Fire modes")
                tag = EditorStyleTag.Red;

            InitializeStylesH1(tag);

            // 1. Get rect for the header (now with consistent indentLevel = 0)
            Rect totalRect = EditorGUILayout.GetControlRect(false, headerHeight);
            
            // 2. Frame rect (outer) and Background rect (inner, 1px smaller on each side)
            Rect frameRect = new Rect(totalRect.x,     totalRect.y,     totalRect.width,     headerHeight);
            Rect bgRect    = new Rect(totalRect.x + 1, totalRect.y + 1, totalRect.width - 2, headerHeight - 2);

            // 3. Draw frame (1px border) then background
            GUI.Box(frameRect, GUIContent.none, CoreProEditorStyle.Asset.GetStyle_Foldout_HeaderFrame_H1(tag));
            GUI.Box(bgRect,    GUIContent.none, CoreProEditorStyle.Asset.GetStyle_Foldout_HeaderBackground_H1(tag));

            // 4. Restore foldout state from EditorPrefs
            if (prefsKey != null && EditorPrefs.HasKey(prefsKey))
            {
                foldout = EditorPrefs.GetBool(prefsKey, foldout);
            }
            
            // 5. Handle click events for foldout toggle
            Event e = Event.current;
            if (e.type == EventType.MouseDown && frameRect.Contains(e.mousePosition) && e.button == 0)
            {
                foldout = !foldout;
                if (prefsKey != null)
                {
                    EditorPrefs.SetBool(prefsKey, foldout);
                }
                e.Use();
                GUI.changed = true;
            }
            
            // 6. Draw foldout arrow indicator
            float arrowOffsetX = CoreProEditorStyle.Asset.foldout_Header_H1_ArrowOffset;
            Rect arrowRect = new Rect(frameRect.x + arrowOffsetX, frameRect.y + (frameRect.height - arrowSize) / 2, arrowSize, arrowSize);
            EditorGUI.Foldout(arrowRect, foldout, GUIContent.none, false);
            
            // 7. Draw enabled toggle if property is provided
            float toggleOffsetX = arrowRect.xMax + 5f;
            if (enabledProperty != null)
            {
                float toggleWidth = 16f;
                Rect toggleRect = new Rect(toggleOffsetX, bgRect.y + (bgRect.height - toggleWidth) / 2, toggleWidth, toggleWidth);
                enabledProperty.boolValue = EditorGUI.Toggle(toggleRect, enabledProperty.boolValue);
            }
            
            // 8. Draw title label
            float labelOffset = CoreProEditorStyle.Asset.foldout_Header_H1_LabelOffset;
            Rect labelRect = new Rect(frameRect.x + labelOffset, frameRect.y, frameRect.width - labelOffset, frameRect.height);
            EditorGUI.LabelField(labelRect, title, CoreProEditorStyle.Asset.Foldout_Header_H1_TitleStyle);
            
            // 9. Status display (enabled/disabled)
            if (enabledProperty != null)
            {
                UpdateStatusLabelStyle(enabledProperty.boolValue);
                string statusText = enabledProperty.boolValue ? Enabled : Disabled;
                Rect statusRect = new Rect(bgRect.xMax - 50, bgRect.y, 45, bgRect.height);
                EditorGUI.LabelField(statusRect, statusText, statusLabelStyle);
            }
            
            return foldout;
        }



        /// <summary>
        /// Full method for drawing H3 (based on your code)
        /// </summary>
        public bool DrawFoldoutH1(ref bool expanded, string title, Rect? rect, float height = 30, float leftPadding = 0, float rightPadding = 0, Texture icon = null, EditorStyleTag tag = EditorStyleTag.None)
        {
            InitializeStylesH1();

            float frameThicknessHorizontal = 1f;
            float frameThicknessVertical = 1f;
            Rect frameRect;

            // If Rect is not specified, we retrieve it from the layout.
            if (rect.HasValue == false)
            {
                frameRect = GUILayoutUtility.GetRect(0, height, GUILayout.ExpandWidth(true));
                frameRect.x += leftPadding;
                frameRect.width -= leftPadding;
            }
            else
            {
                frameRect = (Rect)rect;
            }
     
            Rect backgroundRect = new Rect(
                frameRect.x + frameThicknessHorizontal,
                frameRect.y + frameThicknessVertical,
                frameRect.width - 2 * frameThicknessHorizontal,
                frameRect.height - 2 * frameThicknessVertical);

            // --- Drawing Background (Title Frame & Background) ---
             GUI.Box(frameRect, GUIContent.none, CoreProEditorStyle.Asset.GetStyle_Foldout_HeaderFrame_H1());
             GUI.Box(backgroundRect, GUIContent.none, CoreProEditorStyle.Asset.GetStyle_Foldout_HeaderBackground_H1());

            // ===== CLICKABLE HEADER =====
            #region Event

            Event e = Event.current;
            if (e.type == EventType.MouseDown && frameRect.Contains(e.mousePosition) && e.button == 0)
            {
                expanded = !expanded;
                e.Use();
                GUI.changed = true;
            }
            
            // Color debugColor = new Color(1f, 0f, 0f, 0.3f); 
            // EditorGUI.DrawRect(frameRect, debugColor);
            
            #endregion

            float arrowOffsetX = CoreProEditorStyle.Asset.arrowOffsetX;
            Rect arrowRect = new Rect(frameRect.x + arrowOffsetX, frameRect.y + (frameRect.height - arrowSize) / 2, arrowSize, arrowSize);
            EditorGUI.Foldout(arrowRect, expanded, GUIContent.none, false);
           
            // ===== LABEL =====
            var oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            float foldoutH3LabelOffset = CoreProEditorStyle.Asset.foldout_Header_H3_LabelOffset;
            Rect labelRect = new Rect(backgroundRect.x + foldoutH3LabelOffset, backgroundRect.y, backgroundRect.width - foldoutH3LabelOffset, height);

            EditorGUI.LabelField(labelRect, title, CoreProEditorStyle.Asset.Foldout_Header_H3_TitleStyle);
            EditorGUI.indentLevel = oldIndent;
            
            return expanded;
        }

        /// <summary>
        /// Ends the H1 foldout content area.
        /// </summary>
        /// <param name="foldout">Whether the foldout is expanded.</param>
        public void EndFoldoutH1(bool foldout = false)
        {
            if (foldout)
            {
               // EditorGUI.indentLevel -= IndentLevel; // undo indentation
               // EditorGUILayout.EndVertical(); // close foldoutContentBackground
            }
        }
        

        private Texture2D GetCachedTexture(Color col)
        {
            return CoreProEditorStyle.Asset.GetTexture2D(col);
        }

        public void Dispose()
        {
            foldoutTitleStyleH1 = null;
            frames = null;
            foldoutContentBackground = null;
            statusLabelStyle = null;
        }

        // =================================================================================
        // =============================== SECTION H2 =======================================
        // =================================================================================

        
        // =================================================================================
        // =============================== SECTION H3 =======================================
        // =================================================================================

        /// <summary>
        /// Simple version of drawing method (uses automatic layout)
        /// </summary>
        public bool DrawFoldoutH3(ref bool expanded, string title,  EditorStyleTag tag = EditorStyleTag.None)
        {
            // We pass null as Rect so the method calculates it itself from GUILayout
            return DrawFoldoutH3(ref expanded, title, null, CoreProEditorStyle.Asset.foldout_Header_H3_Height, 0, 0, null, tag);
        }

        /// <summary>
        /// Full method for drawing H3 (based on your code)
        /// </summary>
        public bool DrawFoldoutH3(ref bool expanded, string title, Rect? rect, float height = 30, float leftPadding = 0, float rightPadding = 0, Texture icon = null, EditorStyleTag tag = EditorStyleTag.None)
        {
            InitializeStylesH1();

            float frameThicknessHorizontal = 1f;
            float frameThicknessVertical = 1f;
            Rect frameRect;

            // If Rect is not specified, we retrieve it from the layout.
            if (rect.HasValue == false)
            {
                frameRect = GUILayoutUtility.GetRect(0, height, GUILayout.ExpandWidth(true));
                frameRect.x += leftPadding;
                frameRect.width -= leftPadding;
            }
            else
            {
                frameRect = (Rect)rect;
            }
     
            Rect backgroundRect = new Rect(
                frameRect.x + frameThicknessHorizontal,
                frameRect.y + frameThicknessVertical,
                frameRect.width - 2 * frameThicknessHorizontal,
                frameRect.height - 2 * frameThicknessVertical);

            // --- Drawing Background (Title Frame & Background) ---
             GUI.Box(frameRect, GUIContent.none, CoreProEditorStyle.Asset.GetStyleFoldout_HeaderFrame_H3(tag));
             GUI.Box(backgroundRect, GUIContent.none, CoreProEditorStyle.Asset.GetStyleFoldout_HeaderBackground_H3(tag));

            // ===== CLICKABLE HEADER =====
            #region Event

            Event e = Event.current;
            if (e.type == EventType.MouseDown && frameRect.Contains(e.mousePosition) && e.button == 0)
            {
                expanded = !expanded;
                e.Use();
                GUI.changed = true;
            }
            
            // Color debugColor = new Color(1f, 0f, 0f, 0.3f); 
            // EditorGUI.DrawRect(frameRect, debugColor);
            
            #endregion

            float arrowOffsetX = CoreProEditorStyle.Asset.arrowOffsetX;
            Rect arrowRect = new Rect(frameRect.x + arrowOffsetX, frameRect.y + (frameRect.height - arrowSize) / 2, arrowSize, arrowSize);
            EditorGUI.Foldout(arrowRect, expanded, GUIContent.none, false);
           
            // ===== LABEL =====
            var oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            float foldoutH3LabelOffset = CoreProEditorStyle.Asset.foldout_Header_H3_LabelOffset;
            Rect labelRect = new Rect(backgroundRect.x + foldoutH3LabelOffset, backgroundRect.y, backgroundRect.width - foldoutH3LabelOffset, height);

            EditorGUI.LabelField(labelRect, title, CoreProEditorStyle.Asset.Foldout_Header_H3_TitleStyle);
            EditorGUI.indentLevel = oldIndent;
            
            return expanded;
        }

     
    }
}