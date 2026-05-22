#pragma warning disable 0414
using System;
using UnityEngine;

namespace InspectorPro.Editor
{
#if UNITY_EDITOR
    using UnityEditor;
#endif

    /// <summary>
    /// Static utility class providing cached GUIStyles and drawing methods for CorePro Editor extensions.
    /// All styles are cached to prevent GC allocations during editor rendering.
    /// </summary>
    public static class CoreProEditorStyle
    {
        private const string EditorPrefsKey = "CoreProEditorStyleSO_GUID";
        private static CoreProEditorStyleSO asset;

        /// <summary>
        /// Gets the cached CoreProEditorStyleSO asset instance.
        /// Automatically loads from EditorPrefs or performs fallback search if needed.
        /// </summary>
        public static CoreProEditorStyleSO Asset
        {
            get
            {
                if (asset == null || asset.Equals(null))
                {
#if UNITY_EDITOR
                    asset = null;
                    string guid = EditorPrefs.GetString(EditorPrefsKey, null);
                    string path = null;

                    if (!string.IsNullOrEmpty(guid))
                    {
                        path = AssetDatabase.GUIDToAssetPath(guid);
                        if (string.IsNullOrEmpty(path))
                        {
                            //Debug.LogError($"[CoreProEditorStyleSO] EditorPrefs has GUID '{guid}' but AssetDatabase.GUIDToAssetPath returned null or empty. The asset may have been moved or deleted.");
                        }
                        else
                        {
                            asset = AssetDatabase.LoadAssetAtPath<CoreProEditorStyleSO>(path);
                        }
                    }

                    // Fallback if not in prefs or asset removed
                    if (asset == null)
                    {
                        string[] guids = AssetDatabase.FindAssets("t:CoreProEditorStyleSO");
                        if (guids.Length > 0)
                        {
                            guid = guids[0];
                            path = AssetDatabase.GUIDToAssetPath(guid);
                            if (!string.IsNullOrEmpty(path))
                            {
                                asset = AssetDatabase.LoadAssetAtPath<CoreProEditorStyleSO>(path);
                                if (asset != null)
                                {
                                    EditorPrefs.SetString(EditorPrefsKey, guid);
                                }
                            }
                        }
                    }
#endif
                }

                return asset;
            }
        }

        private static GUIStyle _h1Style;
        private static GUIStyle _h2Style;
        private static GUIStyle _h3Style;
        private static GUIStyle _h3FoldoutStyle;
        private static GUIStyle _h1FoldoutStyle;
        private static GUIStyle _Frames;
        private static GUIStyle _GroupContentBackground;
        private static GUIStyle _BoxContent;
        private static GUIStyle _BoxContent2;
        private static GUIStyle _GroupContentBackgroundNoPad;
        private static GUIStyle _helpBoxStyleNoBorder;
        private static GUIStyle _helpBoxStyleWithBorder;
        private static GUIStyle _bannerStyle;
        private static GUIStyle _customHelpBoxStyle;
        private static Texture2D _customHelpBoxTexture;

        // Cache for colored HelpBox styles (background color -> style)
        private static System.Collections.Generic.Dictionary<int, GUIStyle> _coloredHelpBoxStyles;

#if UNITY_EDITOR
        [InitializeOnEnterPlayMode]
        public static void ResetCacheOnEnterPlaymode()
        {
            asset = null;
            DestroyStaticTextures();

            _h1Style = null;
            _h2Style = null;
            _h3Style = null;
            _content = null;
            _Frames = null;
            _h3FoldoutStyle = null;
            _h1FoldoutStyle = null;
            _GroupContentBackground = null;
            _GroupContentBackgroundNoPad = null;
            _BoxContent = null;
            _BoxContent2 = null;
            _helpBoxStyleNoBorder = null;
            _helpBoxStyleWithBorder = null;
            _bannerStyle = null;
            _customHelpBoxStyle = null;
            _customHelpBoxTexture = null;
            _coloredHelpBoxStyles?.Clear();
        }
#endif

        private static void DestroyStaticTextures()
        {
            if (_Frames?.normal.background != null)
                UnityEngine.Object.DestroyImmediate(_Frames.normal.background, true);

            if (_GroupContentBackground?.normal.background != null)
                UnityEngine.Object.DestroyImmediate(_GroupContentBackground.normal.background, true);

            if (_GroupContentBackgroundNoPad?.normal.background != null)
                UnityEngine.Object.DestroyImmediate(_GroupContentBackgroundNoPad.normal.background, true);

            if (_BoxContent?.normal.background != null)
                UnityEngine.Object.DestroyImmediate(_BoxContent.normal.background, true);

            if (_BoxContent2?.normal.background != null)
                UnityEngine.Object.DestroyImmediate(_BoxContent2.normal.background, true);

            if (_helpBoxStyleNoBorder?.normal.background != null)
                UnityEngine.Object.DestroyImmediate(_helpBoxStyleNoBorder.normal.background, true);

            if (_helpBoxStyleWithBorder?.normal.background != null)
                UnityEngine.Object.DestroyImmediate(_helpBoxStyleWithBorder.normal.background, true);

            if (_customHelpBoxTexture != null)
                UnityEngine.Object.DestroyImmediate(_customHelpBoxTexture, true);

            if (_coloredHelpBoxStyles != null)
            {
                foreach (var kvp in _coloredHelpBoxStyles)
                {
                    if (kvp.Value?.normal.background != null)
                        UnityEngine.Object.DestroyImmediate(kvp.Value.normal.background, true);
                }
                _coloredHelpBoxStyles.Clear();
            }
        }

      
        /// <summary>
        /// Gets a cached GUIStyle for frame rendering with GroupFrameColor background.
        /// </summary>
        public static GUIStyle Frames
        {
            get
            {
                if (_Frames == null)
                {
                    _Frames = new GUIStyle();
                    _Frames.normal.background = MakeTex(2, 2, Asset.GroupFrameColor);
                    _Frames.margin = new RectOffset(0, 0, 0, 0);
                    _Frames.padding = new RectOffset(0, 0, 0, 0);
                }

                return _Frames;
            }
        }

        /// <summary>
        /// Gets a cached GUIStyle for group content background with padding.
        /// </summary>
        public static GUIStyle GroupContentBackground
        {
            get
            {
                if (_GroupContentBackground == null)
                {
                    _GroupContentBackground = new GUIStyle();
                    _GroupContentBackground.normal.background = MakeTex(2, 2, Asset.GroupContentBackgroundColor);
                    _GroupContentBackground.margin = new RectOffset(0, 0, 0, 0);
                    _GroupContentBackground.padding = new RectOffset(1, 1, 1, 1);
                }

                return _GroupContentBackground;
            }
        }

        /// <summary>
        /// Gets a cached GUIStyle for group content background without padding.
        /// </summary>
        public static GUIStyle GroupContentBackgroundNoPad
        {
            get
            {
                if (_GroupContentBackgroundNoPad == null)
                {
                    _GroupContentBackgroundNoPad = new GUIStyle();
                    _GroupContentBackgroundNoPad.normal.background = MakeTex(2, 2, Asset.GroupContentBackgroundColor);
                    _GroupContentBackgroundNoPad.margin = new RectOffset(0, 0, 0, 0);
                    _GroupContentBackgroundNoPad.padding = new RectOffset(0, 0, 0, 0);
                }

                return _GroupContentBackgroundNoPad;
            }
        }

        /// <summary>
        /// Gets a cached GUIStyle for box content with background color.
        /// </summary>
        public static GUIStyle BoxContentStyle1
        {
            get
            {
                if (_BoxContent == null)
                {
                    _BoxContent = new GUIStyle();
                    _BoxContent.normal.background = MakeTex(2, 2, Asset.BoxContentBackgroundColor);
                    _BoxContent.margin = new RectOffset(0, 0, 1, 1);
                    _BoxContent.padding = new RectOffset(0, 0, 0, 0);
                }
                return _BoxContent;
            }
        }

        /// <summary>
        /// Gets a cached GUIStyle for box content with colored border.
        /// </summary>
        public static GUIStyle BoxContentStyle2
        {
            get
            {
                if (_BoxContent2 == null)
                {
                    _BoxContent2 = new GUIStyle();
                    var tex = MakeTexWithBorder(4, 4, Asset.GroupContentBackgroundColor, Asset.GroupFrameColor, 1);
                    tex.filterMode = FilterMode.Point;
                    tex.wrapMode = TextureWrapMode.Repeat;
                    _BoxContent2.normal.background = tex;
                    _BoxContent2.border = new RectOffset(1, 1, 1, 1);
                    _BoxContent2.margin = new RectOffset(0, 0, 0, 0);
                    _BoxContent2.padding = new RectOffset(4, 4, 4, 4);
                    _BoxContent2.stretchWidth = false;
                    _BoxContent2.stretchHeight = false;
                }

                return _BoxContent2;
            }
        }

        /// <summary>
        /// Creates a 2x2 texture with a colored border and background.
        /// Texture is created with HideFlags.HideAndDontSave.
        /// </summary>
        /// <param name="w">Width of the texture.</param>
        /// <param name="h">Height of the texture.</param>
        /// <param name="bgColor">Background color.</param>
        /// <param name="borderColor">Border color.</param>
        /// <param name="borderThickness">Thickness of the border in pixels.</param>
        /// <returns>A new Texture2D with the specified border pattern.</returns>
        private static Texture2D MakeTexWithBorder(int w, int h, Color bgColor, Color borderColor, int borderThickness)
        {
            Color[] pix = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (x < borderThickness || x >= w - borderThickness || y < borderThickness || y >= h - borderThickness)
                        pix[y * w + x] = borderColor;
                    else
                        pix[y * w + x] = bgColor;
                }
            }

            var result = new Texture2D(w, h, TextureFormat.RGBA32, false);
            result.SetPixels(pix);
            result.Apply();
            result.wrapMode = TextureWrapMode.Repeat;
            result.hideFlags = HideFlags.HideAndDontSave;
            return result;
        }

        private static GUIStyle _content;

        /// <summary>
        /// Gets a cached GUIStyle for content with left margin.
        /// </summary>
        public static GUIStyle Content
        {
            get
            {
                if (_content == null)
                {
                    _content = new GUIStyle();
                    _content.margin = new RectOffset(12, 0, 0, 0);
                    _content.padding = new RectOffset(0, 0, 0, 0);
                }

                return _content;
            }
        }

        /// <summary>
        /// Creates a solid color texture. Texture is created with HideFlags.HideAndDontSave.
        /// </summary>
        /// <param name="w">Width of the texture.</param>
        /// <param name="h">Height of the texture.</param>
        /// <param name="col">Color to fill the texture with.</param>
        /// <returns>A new Texture2D filled with the specified color.</returns>
        private static Texture2D MakeTex(int w, int h, Color col)
        {
            Color[] pix = new Color[w * h];
            for (int i = 0; i < pix.Length; i++)
                pix[i] = col;

            var result = new Texture2D(w, h);
            result.SetPixels(pix);
            result.Apply();
            result.hideFlags = HideFlags.HideAndDontSave;
            return result;
        }

        /// <summary>
        /// Draws a group header with label at specified dimensions.
        /// </summary>
        /// <param name="headerLabel">The header text to display.</param>
        /// <param name="headerHeight">Height of the header.</param>
        /// <param name="headerWidth">Width of the header.</param>
        /// <param name="rect">Output rect for the header area.</param>
        public static void DrawGroupHeader(string headerLabel, float headerHeight, float headerWidth, out Rect rect)
        {
            rect = GUILayoutUtility.GetRect(headerWidth, headerHeight);
            DrawGroupHeaderPro(rect);
            var headerRect = new Rect(rect.x, rect.y, rect.width, headerHeight);
            DrawLabel(headerRect, out var labelRect, headerLabel);
        }
        
        
        public static bool DrawFoldoutH1Wide(ref bool foldout, SerializedProperty enabledProperty, string title, string prefsKey = null, float headerHeight = 35, bool hideArrow = false)
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
                Rect arrowRect = new Rect(bgRect.x + CoreProEditorStyle.Asset.foldoutWide_Header_H1_ArrowOffset, bgRect.y + (bgRect.height - Asset.arrowSizeFoldout) / 2, Asset.arrowSizeFoldout, Asset.arrowSizeFoldout);
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
        public static bool DrawFoldoutH2Wide(ref bool foldout, SerializedProperty enabledProperty, string title, string prefsKey = null, float headerHeight = 28, bool hideArrow = false)
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
                        Rect arrowRect = new Rect(bgRect.x + arrowOffsetX, bgRect.y + (bgRect.height - Asset.arrowSizeFoldout) / 2, Asset.arrowSizeFoldout, Asset.arrowSizeFoldout);
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
        
        public static bool DrawFoldoutH3Wide(ref bool foldout, SerializedProperty enabledProperty, string title, string prefsKey = null, float headerHeight = 35, bool hideArrow = false)
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
                        Rect arrowRect = new Rect(bgRect.x + CoreProEditorStyle.Asset.foldoutWide_Header_H1_ArrowOffset, bgRect.y + (bgRect.height - Asset.arrowSizeFoldout) / 2, Asset.arrowSizeFoldout, Asset.arrowSizeFoldout);
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
        /// Draws a group header with label using default height.
        /// </summary>
        /// <param name="headerLabel">The header text to display.</param>
        public static void DrawGroupHeader(string headerLabel)
        {
            var rect = GUILayoutUtility.GetRect(0, GroupHeaderHeight, GUILayout.ExpandWidth(true));
            DrawGroupHeaderPro(rect);
            var headerRect = new Rect(rect.x, rect.y, rect.width, GroupHeaderHeight);
            DrawLabel(headerRect, out var labelRect, headerLabel);
        }

        /// <summary>
        /// Draws a group header with label only (no background frame).
        /// </summary>
        /// <param name="headerLabel">The header text to display.</param>
        public static void DrawGroupHeaderPro(string headerLabel)
        {
            var rect = GUILayoutUtility.GetRect(0, GroupHeaderHeight, GUILayout.ExpandWidth(true));
            var headerRect = new Rect(rect.x, rect.y, rect.width, GroupHeaderHeight);
            DrawLabel(headerRect, out var labelRect, headerLabel, true);
        }

        private const float ArrowSize = 16;
        private const float FrameThickness = 1;
        public const float GroupHeaderHeight = 24;
        private static float headerLabelHeight => EditorStyles.boldLabel.lineHeight;

        /// <summary>
        /// Draws a foldout group header with expand/collapse functionality.
        /// </summary>
        /// <param name="headerLabel">The header text to display.</param>
        /// <param name="isExpanded">Current expanded state.</param>
        /// <param name="newIsExpanded">Output new expanded state after user interaction.</param>
        public static void DrawGroupFoldoutHeader(string headerLabel, bool isExpanded, out bool newIsExpanded)
        {
            var rect = GUILayoutUtility.GetRect(0, GroupHeaderHeight, GUILayout.ExpandWidth(true));
            DrawGroupFoldoutHeader(headerLabel, isExpanded, out newIsExpanded, rect, out var contentRect);
        }

        /// <summary>
        /// Draws a foldout group header for EditorWindow with expand/collapse functionality.
        /// </summary>
        /// <param name="headerLabel">The header text to display.</param>
        /// <param name="isExpanded">Current expanded state.</param>
        /// <param name="newIsExpanded">Output new expanded state after user interaction.</param>
        /// <param name="contentRect">Output rect for content area.</param>
        public static void DrawGroupFoldoutHeaderEditorWin(string headerLabel, bool isExpanded, out bool newIsExpanded, out Rect contentRect)
        {
            var rect = GUILayoutUtility.GetRect(0, GroupHeaderHeight, GUILayout.ExpandWidth(true));
            DrawGroupFoldoutHeader(headerLabel, isExpanded, out newIsExpanded, rect, out contentRect, true);
        }

        /// <summary>
        /// Draws a foldout group header with expand/collapse functionality at specified rect.
        /// </summary>
        /// <param name="headerLabel">The header text to display.</param>
        /// <param name="isExpanded">Current expanded state.</param>
        /// <param name="newIsExpanded">Output new expanded state after user interaction.</param>
        /// <param name="rect">Rect position for the header.</param>
        /// <param name="contentRect">Output rect for content area.</param>
        /// <param name="ignoreArrowMarginForArrow">Whether to ignore arrow margin.</param>
        public static void DrawGroupFoldoutHeader(string headerLabel, bool isExpanded, out bool newIsExpanded, Rect rect, out Rect contentRect,
            bool ignoreArrowMarginForArrow = false)
        {
            var headerRect = new Rect(rect.x, rect.y, rect.width, GroupHeaderHeight);

            DrawGroupHeaderPro(headerRect);
            DrawLabel(headerRect, out var labelRect, headerLabel);
            DrawFoldoutArrow(isExpanded, headerRect, out newIsExpanded, ignoreArrowMarginForArrow);

            contentRect = new Rect(labelRect.x, headerRect.y + Asset.foldout_Header_H2_Height, rect.width - ArrowSize,
                rect.height - headerRect.height);
        }

        /// <summary>
        /// Draws the body of a group with background and frame.
        /// </summary>
        /// <param name="rect">Rect position for the body.</param>
        /// <param name="newIsExpanded">Whether the group is expanded.</param>
        public static void DrawGroupBody(Rect rect, bool newIsExpanded)
        {
            if (newIsExpanded == false)
                return;

            // Background
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, rect.height), Asset.GroupContentBackgroundColor);
            // Lower frame
            EditorGUI.DrawRect(new Rect(rect.xMin, rect.yMax - FrameThickness, rect.width, FrameThickness / 2), Asset.GroupFrameColor);
            // Left frame
            EditorGUI.DrawRect(new Rect(rect.xMin, rect.yMin, FrameThickness, rect.height), Asset.GroupFrameColor);
            // Right frame
            EditorGUI.DrawRect(new Rect(rect.xMax - FrameThickness / 2, rect.yMin, FrameThickness, rect.height), Asset.GroupFrameColor);
        }

        /// <summary>
        /// Draws a group header background with frame at specified rect using default colors.
        /// </summary>
        /// <param name="headerPosRect">Rect position for the header.</param>
        public static void DrawGroupHeaderPro(Rect headerPosRect)
        {
            DrawGroupHeaderPro(headerPosRect, Asset.GroupHeaderColor, Asset.GroupFrameColor);
        }

        /// <summary>
        /// Draws a group header background with frame at specified rect using custom colors.
        /// </summary>
        /// <param name="headerPosRect">Rect position for the header.</param>
        /// <param name="headBgColor">Header background color.</param>
        /// <param name="frameColor">Frame color.</param>
        public static void DrawGroupHeaderPro(Rect headerPosRect, Color headBgColor, Color frameColor)
        {
            EditorGUI.DrawRect(headerPosRect, headBgColor);

            // Upper frame
            EditorGUI.DrawRect(new Rect(headerPosRect.xMin, headerPosRect.yMin, headerPosRect.width, FrameThickness / 2), frameColor);

            // Lower frame
            EditorGUI.DrawRect(new Rect(headerPosRect.xMin, headerPosRect.yMax - FrameThickness, headerPosRect.width, FrameThickness / 2), frameColor);

            // Left frame
            EditorGUI.DrawRect(new Rect(headerPosRect.xMin, headerPosRect.yMin, FrameThickness, headerPosRect.height), frameColor);

            // Right frame
            EditorGUI.DrawRect(new Rect(headerPosRect.xMax - FrameThickness / 2, headerPosRect.yMin, FrameThickness, headerPosRect.height), frameColor);
        }

        /// <summary>
        /// Draws a header background without frame using default color.
        /// </summary>
        /// <param name="headerPosRect">Rect position for the header.</param>
        public static void DrawHeaderProNoFrame(Rect headerPosRect)
        {
            EditorGUI.DrawRect(headerPosRect, Asset.GroupHeaderColor);
        }

        /// <summary>
        /// Draws a header background without frame using custom color.
        /// </summary>
        /// <param name="headerPosRect">Rect position for the header.</param>
        /// <param name="headBgColor">Header background color.</param>
        public static void DrawHeaderProNoFrame(Rect headerPosRect, Color headBgColor)
        {
            EditorGUI.DrawRect(headerPosRect, headBgColor);
        }

        /// <summary>
        /// Draws frame borders around the specified body rect.
        /// </summary>
        /// <param name="bodyRect">Rect position for the body.</param>
        public static void DrawGroupFrames(Rect bodyRect)
        {
            // Lower frame
            EditorGUI.DrawRect(new Rect(bodyRect.xMin, bodyRect.yMax - FrameThickness, bodyRect.width, FrameThickness / 2), Asset.GroupFrameColor);

            // Left frame
            EditorGUI.DrawRect(new Rect(bodyRect.xMin, bodyRect.yMin, FrameThickness, bodyRect.height), Asset.GroupFrameColor);

            // Right frame
            EditorGUI.DrawRect(new Rect(bodyRect.xMax - FrameThickness / 2, bodyRect.yMin, FrameThickness, bodyRect.height), Asset.GroupFrameColor);
        }

        private static void DrawFoldoutArrow(bool isExpanded, Rect headerRect, out bool newIsExpanded, bool ignoreArrowMargin = false)
        {
            float offset = ignoreArrowMargin ? 2f : ArrowSize;
            Rect arrowRect = new Rect(
                headerRect.x + offset,
                headerRect.y + (headerRect.height - ArrowSize) * 0.5f,
                ArrowSize, ArrowSize
            );

            Event e = Event.current;
            if (e.type == EventType.MouseDown && headerRect.Contains(e.mousePosition))
            {
                newIsExpanded = !isExpanded;
                e.Use();
            }
            else
            {
                newIsExpanded = isExpanded;
            }

            EditorGUI.Foldout(arrowRect, isExpanded, GUIContent.none, true);
        }

        private static Rect DrawLabel(Rect headerRect, string headerLabel)
        {
            Rect labelRect = new Rect(
                headerRect.x + ArrowSize,
                headerRect.y + (headerRect.height - headerLabelHeight) * 0.5f,
                headerRect.width - ArrowSize,
                headerLabelHeight
            );

            EditorGUI.LabelField(labelRect, headerLabel, EditorStyles.boldLabel);
            return labelRect;
        }

        private static void DrawLabel(Rect headerRect, out Rect labelRect, string headerLabel, bool ignoreArrowMargin = false)
        {
            float arrowSize = ignoreArrowMargin ? 2f : ArrowSize;
            labelRect = new Rect(
                headerRect.x + arrowSize,
                headerRect.y + (headerRect.height - headerLabelHeight) * 0.5f,
                headerRect.width - arrowSize,
                headerLabelHeight
            );

            EditorGUI.LabelField(labelRect, headerLabel, EditorStyles.boldLabel);
        }

        /// <summary>
        /// Draws a button with style 1 (colored background with frame).
        /// </summary>
        /// <param name="headerPosRect">Rect position for the button.</param>
        public static void DrawButtonStyle1(Rect headerPosRect)
        {
            EditorGUI.DrawRect(headerPosRect, Asset.ButtonStyle1Color);

            // Upper frame
            EditorGUI.DrawRect(new Rect(headerPosRect.xMin, headerPosRect.yMin, headerPosRect.width, FrameThickness / 2), Asset.GroupFrameColor);

            // Lower frame
            EditorGUI.DrawRect(new Rect(headerPosRect.xMin, headerPosRect.yMax - FrameThickness, headerPosRect.width, FrameThickness / 2),
                Asset.GroupFrameColor);

            // Left frame
            EditorGUI.DrawRect(new Rect(headerPosRect.xMin, headerPosRect.yMin, FrameThickness, headerPosRect.height), Asset.GroupFrameColor);

            // Right frame
            EditorGUI.DrawRect(new Rect(headerPosRect.xMax - FrameThickness / 2, headerPosRect.yMin, FrameThickness, headerPosRect.height), Asset.GroupFrameColor);
        }

        /// <summary>
        /// Draws a horizontal line separator.
        /// </summary>
        public static void DrawLine()
        {
            var headerPosRect = GUILayoutUtility.GetRect(0, 1, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(headerPosRect, Color.black);
        }

        #region HelpBox

        /// <summary>
        /// Gets a cached HelpBox GUIStyle with border.
        /// </summary>
        public static GUIStyle HelpBoxStyleWithBorder
        {
            get
            {
                if (_helpBoxStyleWithBorder == null)
                {
                    _helpBoxStyleWithBorder = new GUIStyle(EditorStyles.label)
                    {
                        wordWrap = true,
                        fontSize = 11,
                        padding = new RectOffset(8, 8, 6, 6),
                        margin = new RectOffset(0, 0, 2, 2),
                        alignment = TextAnchor.UpperLeft,
                        normal =
                        {
                            background = MakeTexWithBorder(4, 4, Asset.GroupContentBackgroundColor, Asset.GroupFrameColor, 1),
                            textColor = EditorStyles.label.normal.textColor
                        }
                    };

                    _helpBoxStyleWithBorder.normal.background.filterMode = FilterMode.Point;
                    _helpBoxStyleWithBorder.normal.background.wrapMode = TextureWrapMode.Repeat;
                    _helpBoxStyleWithBorder.border = new RectOffset(1, 1, 1, 1);
                }

                return _helpBoxStyleWithBorder;
            }
        }

        /// <summary>
        /// Gets a cached HelpBox GUIStyle without border.
        /// </summary>
        public static GUIStyle HelpBoxStyleNoBorder
        {
            get
            {
                if (_helpBoxStyleNoBorder == null)
                {
                    _helpBoxStyleNoBorder = new GUIStyle(EditorStyles.label)
                    {
                        wordWrap = true,
                        fontSize = 11,
                        padding = new RectOffset(8, 8, 6, 6),
                        margin = new RectOffset(0, 0, 2, 2),
                        alignment = TextAnchor.UpperLeft,
                        normal =
                        {
                            background = MakeTex(2, 2, Asset.GroupContentBackgroundColor),
                            textColor = EditorStyles.label.normal.textColor
                        }
                    };
                }

                return _helpBoxStyleNoBorder;
            }
        }

        /// <summary>
        /// Draws a help box without icon - simple text in frame.
        /// </summary>
        /// <param name="text">The text to display.</param>
        /// <param name="withBorder">Whether to draw with border.</param>
        public static void DrawHelpBox(string text, bool withBorder = true)
        {
            GUIStyle style = withBorder ? HelpBoxStyleWithBorder : HelpBoxStyleNoBorder;
            EditorGUILayout.LabelField(text, style);
        }

        /// <summary>
        /// Draws a help box without icon with custom colors. Uses caching to prevent GC allocations.
        /// </summary>
        /// <param name="text">The text to display.</param>
        /// <param name="backgroundColor">Background color.</param>
        /// <param name="borderColor">Border color.</param>
        public static void DrawHelpBox(string text, Color backgroundColor, Color borderColor)
        {
            // Create cache dictionary if needed
            if (_coloredHelpBoxStyles == null)
                _coloredHelpBoxStyles = new System.Collections.Generic.Dictionary<int, GUIStyle>();

            // Create a hash key from colors
            int hashKey = HashCode.Combine(backgroundColor.GetHashCode(), borderColor.GetHashCode());

            if (!_coloredHelpBoxStyles.TryGetValue(hashKey, out var style))
            {
                style = new GUIStyle(EditorStyles.label)
                {
                    wordWrap = true,
                    fontSize = 11,
                    padding = new RectOffset(8, 8, 6, 6),
                    margin = new RectOffset(0, 0, 2, 2),
                    alignment = TextAnchor.UpperLeft,
                    normal =
                    {
                        background = MakeTexWithBorder(4, 4, backgroundColor, borderColor, 1),
                        textColor = EditorStyles.label.normal.textColor
                    }
                };

                style.normal.background.filterMode = FilterMode.Point;
                style.normal.background.wrapMode = TextureWrapMode.Repeat;
                style.border = new RectOffset(1, 1, 1, 1);

                _coloredHelpBoxStyles[hashKey] = style;
            }

            EditorGUILayout.LabelField(text, style);
        }

        /// <summary>
        /// Draws an info box with blue tint.
        /// </summary>
        /// <param name="text">The text to display.</param>
        public static void DrawInfoBox(string text)
        {
            Color bgColor = EditorGUIUtility.isProSkin
                ? new Color(0.15f, 0.20f, 0.30f, 1f)
                : new Color(0.85f, 0.90f, 0.95f, 1f);

            Color borderColor = EditorGUIUtility.isProSkin
                ? new Color(0.20f, 0.35f, 0.55f, 1f)
                : new Color(0.50f, 0.65f, 0.85f, 1f);

            DrawHelpBox(text, bgColor, borderColor);
        }

        /// <summary>
        /// Draws a warning box with yellow tint.
        /// </summary>
        /// <param name="text">The text to display.</param>
        public static void DrawWarningBox(string text)
        {
            Color bgColor = EditorGUIUtility.isProSkin
                ? new Color(0.30f, 0.25f, 0.15f, 1f)
                : new Color(0.95f, 0.92f, 0.85f, 1f);

            Color borderColor = EditorGUIUtility.isProSkin
                ? new Color(0.60f, 0.50f, 0.20f, 1f)
                : new Color(0.85f, 0.75f, 0.40f, 1f);

            DrawHelpBox(text, bgColor, borderColor);
        }

        /// <summary>
        /// Draws an error box with red tint.
        /// </summary>
        /// <param name="text">The text to display.</param>
        public static void DrawErrorBox(string text)
        {
            Color bgColor = EditorGUIUtility.isProSkin
                ? new Color(0.30f, 0.15f, 0.15f, 1f)
                : new Color(0.95f, 0.85f, 0.85f, 1f);

            Color borderColor = EditorGUIUtility.isProSkin
                ? new Color(0.60f, 0.20f, 0.20f, 1f)
                : new Color(0.85f, 0.40f, 0.40f, 1f);

            DrawHelpBox(text, bgColor, borderColor);
        }

        #endregion

        /// <summary>
        /// Draws a banner with CorePro branding and header text.
        /// </summary>
        /// <param name="Header">The header text to display.</param>
        public static void DrawBanner(string Header)
        {
            // Initialize style once
            if (_bannerStyle == null)
            {
                _bannerStyle = new GUIStyle(EditorStyles.whiteLargeLabel)
                {
                    alignment = TextAnchor.MiddleLeft,
                    fontStyle = FontStyle.Normal,
                    fontSize = 15,
                    richText = true,
                    padding = new RectOffset(0, 0, 0, 0)
                };
            }

            // 1. Prepare rects
            Rect totalRect = EditorGUILayout.GetControlRect(false, 35);
            Rect bgRect = new Rect(totalRect.x - 15, totalRect.y, totalRect.width + 30, totalRect.height);
            Rect bottomLineRect = new Rect(bgRect.x, bgRect.yMax - 1, bgRect.width, 1);
            Rect accentRect = new Rect(bgRect.x, bgRect.y, 4, bgRect.height);

            // 2. Draw Backgrounds
            EditorGUI.DrawRect(bgRect, new Color(0.12f, 0.12f, 0.12f));
            EditorGUI.DrawRect(bottomLineRect, new Color(0.3f, 0.3f, 0.3f, 0.5f));
            EditorGUI.DrawRect(accentRect, new Color(0.15f, 0.45f, 0.85f));

            // 3. Draw Label with Rich Text
            string headerText = $" <color=#aaaaaa><b>CORE</b></color><color=#888888>PRO</color> <color=#4F97FF>{Header.ToUpper()}</color>";
            EditorGUI.LabelField(totalRect, headerText, _bannerStyle);

            EditorGUILayout.Space(2);
        }
        
        /// <summary>
        /// Draws a banner without CorePro branding and header text.
        /// </summary>
        /// <param name="Header">The header text to display.</param>
        public static void DrawBannerSimple(string Header)
        {
            // Initialize style once
            if (_bannerStyle == null)
            {
                _bannerStyle = new GUIStyle(EditorStyles.whiteLargeLabel)
                {
                    alignment = TextAnchor.MiddleLeft,
                    fontStyle = FontStyle.Normal,
                    fontSize = 15,
                    richText = true,
                    padding = new RectOffset(0, 0, 0, 0)
                };
            }

            // 1. Prepare rects
            Rect totalRect = EditorGUILayout.GetControlRect(false, 35);
            Rect bgRect = new Rect(totalRect.x - 15, totalRect.y, totalRect.width + 30, totalRect.height);
            Rect bottomLineRect = new Rect(bgRect.x, bgRect.yMax - 1, bgRect.width, 1);
            Rect accentRect = new Rect(bgRect.x, bgRect.y, 4, bgRect.height);

            // 2. Draw Backgrounds
            EditorGUI.DrawRect(bgRect, new Color(0.12f, 0.12f, 0.12f));
            EditorGUI.DrawRect(bottomLineRect, new Color(0.3f, 0.3f, 0.3f, 0.5f));
            EditorGUI.DrawRect(accentRect, new Color(0.15f, 0.45f, 0.85f));

            // 3. Draw Label with Rich Text
            float labelOffsetX = 6;
            string headerText = $"<color=#4F97FF>{Header.ToUpper()}</color>";
            totalRect.x += labelOffsetX;
            EditorGUI.LabelField(totalRect, headerText, _bannerStyle);

            EditorGUILayout.Space(2);
        }
    }
}
