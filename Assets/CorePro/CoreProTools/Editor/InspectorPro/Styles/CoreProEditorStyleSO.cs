// https://www.foundations.unity.com/fundamentals/color-palette
#pragma warning disable 0414
using System;
using System.Collections.Generic;
using InspectorPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace InspectorPro.Editor
{
    public enum EditorStyleKey
    {
        FoldoutHeaderH1,
        FoldoutHeaderH2,
        FoldoutFrameH1,
        FoldoutFrameH3,
        FoldoutStatusLabelH1,
        FoldoutHeaderBackgroundH1,
        FoldoutHeaderBackgroundH2,
        FoldoutHeaderBackgroundH3,
        FoldoutContentBackgroundH1,
        FoldoutContentBackgroundH3,
        FoldoutHeaderBackgroundH3_TagMagazine,
        FoldoutHeaderBackgroundH1_TagBlue,
        FoldoutHeaderBackgroundH1_TagRed,
        GroupContentStyle,
        ToolsFoldout_HeaderBackground_H1,
        Foldout_HeaderFrame_H1
        
    }

    //[CreateAssetMenu(menuName = "CorePro/CoreProEditorStyleSO")]
    public class CoreProEditorStyleSO : ScriptableObjectCorePro
    {
        // ========================================================
        // FOLDOUTS H1
        // ========================================================
        [Group("Header H1", true)]
        [SerializeField] private GUIStyle header_H1_LabelStyle_Pro ;
        [SerializeField] private GUIStyle header_H1_LabelStyle ;

        [Group("Foldout H1", true)]
        [SerializeField] public float foldout_Header_H1_Height = 30f;
        [SerializeField] public float foldout_Header_H1_LabelOffset = 30;
        [SerializeField] public float foldout_Header_H1_ArrowOffset = 25;

        [Header("Pro")]
        [SerializeField] private GUIStyle foldout_Header_H1_LabelStyle_Pro ;
        [SerializeField] private Color    foldout_HeaderFrame_H1_Pro = new Color(0.251f, 0.251f, 0.251f, 1f);
        [SerializeField] private Color    foldout_HeaderBackground_H1_Pro = new Color(0.251f, 0.251f, 0.251f, 1f);
        [SerializeField] private Color    foldout_ContentFrame_H1_Pro = new Color(0.251f, 0.251f, 0.251f, 1f);
        [SerializeField] private Color    foldout_ContentBackground_H1_Pro = new Color(0.251f, 0.251f, 0.251f, 1f);
        [Header("Normal")]
        [SerializeField] private GUIStyle foldout_Header_H1_LabelStyle ;
        [SerializeField] private Color    foldout_HeaderFrame_H1 = new Color(0.251f, 0.251f, 0.251f, 1f);
        [SerializeField] private Color    foldout_HeaderBackground_H1 = new Color(0.251f, 0.251f, 0.251f, 1f);
        [SerializeField] private Color    foldout_ContentFrame_H1 = new Color(0.251f, 0.251f, 0.251f, 1f);
        [SerializeField] private Color    foldout_ContentBackground_H1 = new Color(0.251f, 0.251f, 0.251f, 1f);

        // ========================================================
        // FOLDOUTS H2
        // ========================================================
        
        [Group("Foldout H2", true)]
        [SerializeField] public float foldout_Header_H2_Height = 30f;
        [SerializeField] public float foldout_Header_H2_LabelOffset = 30;
        [SerializeField] public float foldout_Header_H2_ArrowOffset = 25;
        
        [Header("Pro")]
        [SerializeField] private GUIStyle foldout_Header_H2_LabelStyle_Pro;
        [SerializeField] private Color    foldout_HeaderFrame_H2_Pro = new Color(0.251f, 0.251f, 0.251f, 1f);
        [SerializeField] private Color    foldout_HeaderBackground_H2_Pro = new Color(0.251f, 0.251f, 0.251f, 1f);
        [SerializeField] private Color    foldout_ContentFrame_H2_Pro = new Color(0.251f, 0.251f, 0.251f, 1f);
        [SerializeField] private Color    foldout_ContentBackground_H2_Pro = new Color(0.251f, 0.251f, 0.251f, 1f);
        [Header("Normal")]
        [SerializeField] private GUIStyle foldout_Header_H2_LabelStyle;
        [SerializeField] private Color    foldout_HeaderFrame_H2 = new Color(0.251f, 0.251f, 0.251f, 1f);
        [SerializeField] private Color    foldout_HeaderBackground_H2 = new Color(0.251f, 0.251f, 0.251f, 1f);
        [SerializeField] private Color    foldout_ContentFrame_H2 = new Color(0.251f, 0.251f, 0.251f, 1f);
        [SerializeField] private Color    foldout_ContentBackground_H2 = new Color(0.251f, 0.251f, 0.251f, 1f);

        // ========================================================
        // FOLDOUTS H3
        // ========================================================
        
        [Group("Foldout H3", true)]
        [SerializeField] public float foldout_Header_H3_Height = 30f;
        [SerializeField] public float foldout_Header_H3_LabelOffset = 30;
        [SerializeField] public float foldout_Header_H3_ArrowOffset = 25;
     
        [Header("Pro")]
        [SerializeField] private GUIStyle foldout_Header_H3_LabelStyle_Pro;
        [SerializeField] private Color    foldout_HeaderFrame_H3_Pro = new Color(0.251f, 0.251f, 0.251f, 1f);
        [SerializeField] private Color    foldout_HeaderBackground_H3_Pro = new Color(0.251f, 0.251f, 0.251f, 1f);
        [SerializeField] private Color    foldout_ContentFrame_H3_Pro = new Color(0.251f, 0.251f, 0.251f, 1f);
        [SerializeField] private Color    foldout_ContentBackground_H3_Pro = new Color(0.251f, 0.251f, 0.251f, 1f);
        [Header("Normal")]
        [SerializeField] private GUIStyle foldout_Header_H3_LabelStyle;
        [SerializeField] private Color    foldout_HeaderFrame_H3 = new Color(0.251f, 0.251f, 0.251f, 1f);
        [SerializeField] private Color    foldout_HeaderBackground_H3 = new Color(0.251f, 0.251f, 0.251f, 1f);
        [SerializeField] private Color    foldout_ContentFrame_H3 = new Color(0.251f, 0.251f, 0.251f, 1f);
        [SerializeField] private Color    foldout_ContentBackground_H3 = new Color(0.251f, 0.251f, 0.251f, 1f);
       
        // ========================================================
        // TOOLS WINDOW FOLDOUTS T1
        // ========================================================
        #region Tools Window Foldouts
        [Group("Foldout Tools T1", true)] [Space(-10)]
        [Header("Pro")]
        [SerializeField] private Color foldout_HeaderFrame_T1_Pro = new Color(0.251f, 0.251f, 0.251f, 1f);
        [SerializeField] private Color foldout_HeaderBackground_T1_Pro = new Color(0.251f, 0.251f, 0.251f, 1f);
        [SerializeField] private Color foldout_ContentFrame_T1_Pro = new Color(0.251f, 0.251f, 0.251f, 1f);
        [SerializeField] private Color foldout_ContentBackground_T1_Pro = new Color(0.251f, 0.251f, 0.251f, 1f);
        [Header("Normal")]
        [SerializeField] private Color foldout_HeaderFrame_T1 = new Color(0.251f, 0.251f, 0.251f, 1f);
        [SerializeField] private Color foldout_HeaderBackground_T1 = new Color(0.251f, 0.251f, 0.251f, 1f);
        [SerializeField] private Color foldout_ContentFrame_T1 = new Color(0.251f, 0.251f, 0.251f, 1f);
        [SerializeField] private Color foldout_ContentBackground_T1 = new Color(0.251f, 0.251f, 0.251f, 1f);
        
        // ========================================================
        // TOOLS WINDOW FOLDOUTS T2
        // ========================================================
        
        [Group("Foldout Tools T2", true)] [Space(-10)]
        [Header("Pro")]
        [SerializeField] private Color foldout_HeaderFrame_T2_Pro = new Color(0.251f, 0.251f, 0.251f, 1f);
        [SerializeField] private Color foldout_HeaderBackground_T2_Pro = new Color(0.251f, 0.251f, 0.251f, 1f);
        [SerializeField] private Color foldout_ContentFrame_T2_Pro = new Color(0.251f, 0.251f, 0.251f, 1f);
        [SerializeField] private Color foldout_ContentBackground_T2_Pro = new Color(0.251f, 0.251f, 0.251f, 1f);
        [Header("Normal")]
        [SerializeField] private Color foldout_HeaderFrame_T2 = new Color(0.251f, 0.251f, 0.251f, 1f);
        [SerializeField] private Color foldout_HeaderBackground_T2 = new Color(0.251f, 0.251f, 0.251f, 1f);
        [SerializeField] private Color foldout_ContentFrame_T2 = new Color(0.251f, 0.251f, 0.251f, 1f);
        [SerializeField] private Color foldout_ContentBackground_T2 = new Color(0.251f, 0.251f, 0.251f, 1f);

        // ========================================================
        // TOOLS WINDOW FOLDOUTS T3
        // ========================================================
        [Group("Foldout Tools T3", true)] [Space(-10)]
        [Header("Pro")]
        [SerializeField] private Color foldout_HeaderFrame_T3_Pro = new Color(0.251f, 0.251f, 0.251f, 1f);
        [SerializeField] private Color foldout_HeaderBackground_T3_Pro = new Color(0.251f, 0.251f, 0.251f, 1f);
        [SerializeField] private Color foldout_ContentFrame_T3_Pro = new Color(0.251f, 0.251f, 0.251f, 1f);
        [SerializeField] private Color foldout_ContentBackground_T3_Pro = new Color(0.251f, 0.251f, 0.251f, 1f);
        [Header("Normal")]
        [SerializeField] private Color foldout_HeaderFrame_T3 = new Color(0.251f, 0.251f, 0.251f, 1f);
        [SerializeField] private Color foldout_HeaderBackground_T3 = new Color(0.251f, 0.251f, 0.251f, 1f);
        [SerializeField] private Color foldout_ContentFrame_T3 = new Color(0.251f, 0.251f, 0.251f, 1f);
        [SerializeField] private Color foldout_ContentBackground_T3 = new Color(0.251f, 0.251f, 0.251f, 1f);
        #endregion
        
        // ========================================================
        //  FOLDOUTS WIDE H1
        // ========================================================
        [Group("Foldout Wide H1", true)]
        [SerializeField] public float foldoutWide_Header_H1_Height = 30f;
        [SerializeField] public float foldoutWide_Header_H1_LabelOffset = 30;
        [SerializeField] public float foldoutWide_Header_H1_ArrowOffset = 25;
        [Header("Pro")]
        [SerializeField] private GUIStyle foldoutWide_Header_H1_LabelStyle_Pro = new GUIStyle();
        [SerializeField] private Color    foldoutWide_HeaderFrame_H1_Pro = new Color(0.251f, 0.251f, 0.251f, 1f);
        [SerializeField] private Color    foldoutWide_HeaderBackground_H1_Pro = new Color(0.251f, 0.251f, 0.251f, 1f);
        [SerializeField] private Color    foldoutWide_ContentFrame_H1_Pro = new Color(0.251f, 0.251f, 0.251f, 1f);
        [SerializeField] private Color    foldoutWide_ContentBackground_H1_Pro = new Color(0.251f, 0.251f, 0.251f, 1f);
        [Header("Normal")]
        [SerializeField] private GUIStyle foldoutWide_Header_H1_LabelStyle = new GUIStyle();
        [SerializeField] private Color    foldoutWide_HeaderFrame_H1 = new Color(0.251f, 0.251f, 0.251f, 1f);
        [SerializeField] private Color    foldoutWide_HeaderBackground_H1 = new Color(0.251f, 0.251f, 0.251f, 1f);
        [SerializeField] private Color    foldoutWide_ContentFrame_H1 = new Color(0.251f, 0.251f, 0.251f, 1f);
        [SerializeField] private Color    foldoutWide_ContentBackground_H1 = new Color(0.251f, 0.251f, 0.251f, 1f);

        // ========================================================
        //  FOLDOUTS WIDE H2
        // ========================================================
        [Group("Foldout Wide H2", true)] 
        [SerializeField] public float foldoutWide_H2Height = 30f;
        [SerializeField] public float foldoutWide_H2LabelOffset = 10;
        [SerializeField] public float foldoutWide_H2Offset = 25;
        [Header("Pro")]
        [SerializeField] private GUIStyle foldoutWide_Header_H2_LabelStyle_Pro = new GUIStyle();
        [SerializeField] private Color    foldoutWide_HeaderFrame_H2_Pro = new Color(0.251f, 0.251f, 0.251f, 1f);
        [SerializeField] private Color    foldoutWide_HeaderBackground_H2_Pro = new Color(0.251f, 0.251f, 0.251f, 1f);
        [SerializeField] private Color    foldoutWide_ContentFrame_H2_Pro = new Color(0.251f, 0.251f, 0.251f, 1f);
        [SerializeField] private Color    foldoutWide_ContentBackground_H2_Pro = new Color(0.251f, 0.251f, 0.251f, 1f);
        [Header("Normal")]
        [SerializeField] private GUIStyle foldoutWide_Header_H2_LabelStyle = new GUIStyle();
        [SerializeField] private Color    foldoutWide_HeaderFrame_H2 = new Color(0.251f, 0.251f, 0.251f, 1f);
        [SerializeField] private Color    foldoutWide_HeaderBackground_H2 = new Color(0.251f, 0.251f, 0.251f, 1f);
        [SerializeField] private Color    foldoutWide_ContentFrame_H2 = new Color(0.251f, 0.251f, 0.251f, 1f);
        [SerializeField] private Color    foldoutWide_ContentBackground_H2 = new Color(0.251f, 0.251f, 0.251f, 1f);
       
        // ========================================================
        //  FOLDOUTS WIDE H3
        // ========================================================
        [Group("Foldout Wide H3", true)]
        [SerializeField] public float foldoutWide_H3Height = 30f;
        [SerializeField] public float foldoutWide_H3LabelOffset = 10;
        [SerializeField] public float foldoutWide_H3Offset = 25;
        [Header("Pro")]
        [SerializeField] private GUIStyle foldoutWide_Header_H3_LabelStyle_Pro = new GUIStyle();
        [SerializeField] private Color    foldoutWide_HeaderFrame_H3_Pro = new Color(0.251f, 0.251f, 0.251f, 1f);
        [SerializeField] private Color    foldoutWide_HeaderBackground_H3_Pro = new Color(0.251f, 0.251f, 0.251f, 1f);
        [SerializeField] private Color    foldoutWide_ContentFrame_H3_Pro = new Color(0.251f, 0.251f, 0.251f, 1f);
        [SerializeField] private Color    foldoutWide_ContentBackground_H3_Pro = new Color(0.251f, 0.251f, 0.251f, 1f);
        [Header("Normal")]
        [SerializeField] private GUIStyle foldoutWide_Header_H3_LabelStyle = new GUIStyle();
        [SerializeField] private Color    foldoutWide_HeaderFrame_H3 = new Color(0.251f, 0.251f, 0.251f, 1f);
        [SerializeField] private Color    foldoutWide_HeaderBackground_H3 = new Color(0.251f, 0.251f, 0.251f, 1f);
        [SerializeField] private Color    foldoutWide_ContentFrame_H3 = new Color(0.251f, 0.251f, 0.251f, 1f);
        [SerializeField] private Color    foldoutWide_ContentBackground_H3 = new Color(0.251f, 0.251f, 0.251f, 1f);
        
        // ========================================================
        //  FOLDOUTS Module
        // ========================================================
        [Group("Foldout Module", true)]
        [SerializeField] public float foldoutModule_Height = 30f;
        [SerializeField] public float foldoutModule_LabelOffset = 10;
        [SerializeField] public float foldoutModule_ArrowOffset = 20;
        
        [Header("Pro")]
        [SerializeField] private GUIStyle foldoutModule_Header_LabelStyle_Pro = new GUIStyle();
        [SerializeField] private Color    foldoutModule_HeaderFrame_Pro = new Color(0.251f, 0.251f, 0.251f, 1f);
        [SerializeField] private Color    foldoutModule_HeaderBackground_Pro = new Color(0.251f, 0.251f, 0.251f, 1f);
        [SerializeField] private Color    foldoutModule_ContentFrame_Pro = new Color(0.251f, 0.251f, 0.251f, 1f);
        [SerializeField] private Color    foldoutModule_ContentBackground_Pro = new Color(0.251f, 0.251f, 0.251f, 1f);
        [Header("Normal")]
        [SerializeField] private GUIStyle foldoutModule_Header_LabelStyle = new GUIStyle();
        [SerializeField] private Color    foldoutModule_HeaderFrame = new Color(0.251f, 0.251f, 0.251f, 1f);
        [SerializeField] private Color    foldoutModule_HeaderBackground = new Color(0.251f, 0.251f, 0.251f, 1f);
        [SerializeField] private Color    foldoutModule_ContentFrame = new Color(0.251f, 0.251f, 0.251f, 1f);
        [SerializeField] private Color    foldoutModule_ContentBackground = new Color(0.251f, 0.251f, 0.251f, 1f);
        
        #region Groups
        [Group("Group", true)] [Space(-10)]
        [Header("Pro")]
        [SerializeField] private Color GroupFramePro = new Color(0.251f, 0.251f, 0.251f, 1f);
        [SerializeField] private Color GroupHeaderPro = new Color(0.251f, 0.251f, 0.251f, 1f);
        [SerializeField] private Color GroupContentBackgroundPro = new Color(0.251f, 0.251f, 0.251f, 1f);
        [Header("Normal")]
        [SerializeField] private Color GroupFrame = new Color(0.251f, 0.251f, 0.251f, 1f);
        [SerializeField] private Color GroupHeader = new Color(0.251f, 0.251f, 0.251f, 1f);
        [SerializeField] private Color GroupContentBackground = new Color(0.251f, 0.251f, 0.251f, 1f);
        #endregion

        #region SingleModule
        [Group("SingleModule", true)] [Space(-10)]
        [Header("Pro")]
        [SerializeField] private Color singleModuleFramePro = new Color(0.251f, 0.251f, 0.251f, 1f);
        [SerializeField] private Color singleModuleHeaderPro = new Color(0.251f, 0.251f, 0.251f, 1f);
        [SerializeField] private Color singleModuleContentBackgroundPro = new Color(0.251f, 0.251f, 0.251f, 1f);
        [Header("Normal")]
        [SerializeField] private Color singleModuleFrame = new Color(0.251f, 0.251f, 0.251f, 1f);
        [SerializeField] private Color singleModuleHeader = new Color(0.251f, 0.251f, 0.251f, 1f);
        [SerializeField] private Color singleModuleContentBackground = new Color(0.251f, 0.251f, 0.251f, 1f);
        #endregion
        
        #region Box
        [Group("Box", true)] [Space(-10)]
        [Header("Pro")]
        [SerializeField] private Color BoxContentBackgroundPro = new Color(0.251f, 0.251f, 0.251f, 1f);
        [Header("Normal")]
        [SerializeField] private Color BoxContentBackground = new Color(0.251f, 0.251f, 0.251f, 1f);
        #endregion

        #region Title
        [Group("Title", true)]
        [SerializeField] private Color titleUnderlineDark = new Color(0.251f, 0.251f, 0.251f, 1f);
        [SerializeField] private Color titleUnderline= new Color(0.251f, 0.251f, 0.251f, 1f);
        [SerializeField] public float titleUnderlineHeight = 2f;
        [SerializeField] public int titleUnderlineFontSize = 16;
        [SerializeField] public int titleSpaceBefore = 2;
        [SerializeField] public FontStyle titleUnderlineFont;
        #endregion
        
        [Group("Other", true)]
        [SerializeField] public Color ButtonStyle1Color = new Color(0.5f, 0.2f, 0.2f, 1f);
        [SerializeField] public Color SelectedGreen = new Color(0.690f, 0.824f, 0.988f, 1f);

        // --- Styles ---
        [SerializeField] public float arrowOffsetX = 25;
        [SerializeField] public float arrowSizeFoldout = 15;

        // --- Tag colors ---
        [Group("Tag Colors", true)]
        [SerializeField] public Color blueTagPro = new Color(0.18f, 0.26f, 0.45f, 1f);
        [SerializeField] public Color blueTag = new Color(0.18f, 0.26f, 0.45f, 1f);
        [SerializeField] public Color redTagPro = new Color(0.5f, 0.2f, 0.2f, 1f);
        [SerializeField] public Color redTag = new Color(0.5f, 0.2f, 0.2f, 1f);

        // --- Atributes colors ---
        [Group("Module Vew", true)]
        [SerializeField] public float moduleView_InspectorRenderMarginLeft = 10;
        [SerializeField] public float moduleView_InspectorRenderMarginRight = 0;

        [Space] 
        [SerializeField] public float spaceBetweenModulesFoldouts = 1;
        
        private Dictionary<EditorStyleKey, GUIStyle> stylePool = new();
        private Dictionary<Color32, Texture2D> colorTexturePool = new();
        
        /// <summary>
        /// Gets read-only access to the style pool for debugging purposes.
        /// </summary>
        public System.Collections.Generic.IReadOnlyDictionary<EditorStyleKey, GUIStyle> StylePool => stylePool;
        private Color tempColor;
    
        // private void OnValidate()
        // {
        //     ClearCache();
        // }

        private void CheckNulls()  
        {   
            if (colorTexturePool == null) 
            {
                colorTexturePool = new Dictionary<Color32, Texture2D>();
            }
            
            if (stylePool == null) 
            {
                stylePool = new Dictionary<EditorStyleKey, GUIStyle>();
            }
        }
        
        public Texture2D GetTexture2D(Color color)
        {
            CheckNulls();

            tempColor = color;
            tempColor.a = 1f;
            var c32 = (Color32)tempColor;

            if (colorTexturePool.TryGetValue(c32, out var texture2D))
            {
                if (texture2D != null && !texture2D.Equals(null))
                    return texture2D;

                // Damaged - remove from cache
                colorTexturePool.Remove(c32);
            }

            // We create a new one because there was no or it was damaged
            texture2D = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture2D.hideFlags = HideFlags.HideAndDontSave; 
            var pixels = new Color[4] { tempColor, tempColor, tempColor, tempColor };
            texture2D.SetPixels(pixels);
            texture2D.Apply();
            colorTexturePool[c32] = texture2D;
            return texture2D;
        }
        
        public GUIStyle GetOrCreateStyle(EditorStyleKey key,bool requireBackground, Func<GUIStyle> creator)
        {
            CheckNulls();

            if (stylePool.TryGetValue(key, out var style))
            {
                if (style == null)
                {
                    Debug.LogError($"[CoreProEditorStyleSO] (key: {key}) Style is NULL in stylePool! (Possible cache issue)");
                    ClearCache();
                }
                else if (requireBackground && style.normal.background == null)
                {
                    //Debug.LogError($"[CoreProEditorStyleSO] Style expected background but has NONE! Forcing recreate. (key: {key})");
                    style = null;
                    stylePool.Remove(key);
                }
                else
                {
                    return style;
                }
            }

            style = creator();
            stylePool[key] = style;

            // if (requireBackground && style.normal.background == null)
            //     Debug.LogError($"[CoreProEditorStyleSO] (key: {key}) NEW style also has NO background, check your creator!");

            return style;
        }
        
        // ======================================================================================================================
        // H1 Foldout
        // ======================================================================================================================
        
        public GUIStyle GetStyle_Foldout_HeaderFrame_H1(EditorStyleTag tag = EditorStyleTag.None)
        {
            CheckNulls();
            
            return CoreProEditorStyle.Asset.GetOrCreateStyle(EditorStyleKey.FoldoutFrameH1, true,
                () => new GUIStyle
                {
                    padding = new RectOffset(0, 0, 0, 0),
                    margin = new RectOffset(0, 0, 0, 0),
                    normal = { background = CoreProEditorStyle.Asset.GetTexture2D(CoreProEditorStyle.Asset.Foldout_HeaderFrame_H1) }
                });
        }
        
        public GUIStyle GetStyle_Foldout_HeaderBackground_H1(EditorStyleTag tag = EditorStyleTag.None)
        {
            CheckNulls();
            
            if (tag == EditorStyleTag.Blue)
                return GetOrCreateStyle(EditorStyleKey.FoldoutHeaderBackgroundH1_TagBlue, true,
                    () => new GUIStyle
                    {
                        padding = new RectOffset(10, 10, 5, 5),
                        margin = new RectOffset(1, 1, 1, 1),
                        normal = { background = GetTexture2D(BlueTagColor) }
                    });
            else if (tag == EditorStyleTag.Red)
                return GetOrCreateStyle(EditorStyleKey.FoldoutHeaderBackgroundH1_TagRed, true,
                    () => new GUIStyle
                    {
                        padding = new RectOffset(10, 10, 5, 5),
                        margin = new RectOffset(1, 1, 1, 1),
                        normal = { background = GetTexture2D(RedTagColor) }
                    });

            return GetOrCreateStyle(EditorStyleKey.FoldoutHeaderBackgroundH1, true,
                () => new GUIStyle
                {
                    padding = new RectOffset(0, 0, 0, 0),
                    margin = new RectOffset(0, 0, 0, 0),
                    normal = { background = GetTexture2D(Foldout_HeaderBackground_H1) }
                });
        }

        public GUIStyle GetStyle_Foldout_ContentBackground_H1(EditorStyleTag tag= EditorStyleTag.None)
        {
            CheckNulls();
            
            return CoreProEditorStyle.Asset.GetOrCreateStyle(EditorStyleKey.FoldoutContentBackgroundH1, true,
                () => new GUIStyle
                {
                    padding = new RectOffset(0, 0, 0, 0),
                    margin = new RectOffset(0, 0, 0, 0),
                    normal = { background = CoreProEditorStyle.Asset.GetTexture2D(CoreProEditorStyle.Asset.Foldout_ContentBackground_H1) }
                });
        }
        

        // ======================================================================================================================
        // H3 Foldout
        // ======================================================================================================================
        public GUIStyle GetStyleFoldout_HeaderFrame_H3(EditorStyleTag tag = EditorStyleTag.None)
        {
            if (tag == EditorStyleTag.Blue)
                return GetOrCreateStyle(EditorStyleKey.FoldoutHeaderBackgroundH3_TagMagazine, true,
                    () => new GUIStyle { normal = { background = GetTexture2D(BlueTagColor) } });

            if (tag == EditorStyleTag.Red)
                return GetOrCreateStyle(EditorStyleKey.FoldoutHeaderBackgroundH3_TagMagazine, true,
                    () => new GUIStyle { normal = { background = GetTexture2D(BlueTagColor) } });
            
            CheckNulls();

            return GetOrCreateStyle(EditorStyleKey.FoldoutFrameH3, true,
                () => new GUIStyle
                {
                    padding = new RectOffset(0, 0, 0, 0),
                    margin = new RectOffset(0, 0, 0, 0),
                    normal = { background = GetTexture2D(Foldout_HeaderFrame_H3) }
                });
        }

        public GUIStyle GetStyleFoldout_HeaderBackground_H3(EditorStyleTag tag= EditorStyleTag.None)
        {
            CheckNulls();
            
            if (tag == EditorStyleTag.Blue)
                return GetOrCreateStyle(EditorStyleKey.FoldoutHeaderBackgroundH3_TagMagazine, true,
                    () => new GUIStyle { normal = { background = GetTexture2D(BlueTagColor) } });
            
            if (tag == EditorStyleTag.Red)
                return GetOrCreateStyle(EditorStyleKey.FoldoutHeaderBackgroundH3_TagMagazine, true,
                    () => new GUIStyle { normal = { background = GetTexture2D(RedTagColor) } });
            
            return GetOrCreateStyle(EditorStyleKey.FoldoutContentBackgroundH3, true,
                () => new GUIStyle
                {
                    padding = new RectOffset(0,0,0,0),
                    margin = new RectOffset(0,0,0,0),
                    normal = { background = GetTexture2D(Foldout_HeaderBackground_H3) }
                });
        }
        
 
        [Button]
        [InitializeOnEnterPlayMode]
        #if UNITY_EDITOR
        
        public void ClearCache()   
        {
            if (colorTexturePool != null)
            {
                foreach (var kvp in colorTexturePool)
                {
                    if (kvp.Value != null)
                        DestroyImmediate(kvp.Value, true);
                }
                
                colorTexturePool.Clear();
            }
            stylePool.Clear();
            
            CoreProEditorStyle.ResetCacheOnEnterPlaymode();
        } 
        #endif

        #region TagColors
        
        public Color BlueTagColor
        {
            get
            {
                if (EditorGUIUtility.isProSkin)
                    return blueTagPro;
                else
                    return blueTag;
            }
        }
        
        public Color RedTagColor
        {
            get
            {
                if (EditorGUIUtility.isProSkin)
                    return redTagPro;
                else
                    return redTag;
            }
        }
        #endregion

        #region Group
        public Color GroupHeaderColor => EditorGUIUtility.isProSkin ? GroupHeaderPro : GroupHeader;
        public Color GroupFrameColor => EditorGUIUtility.isProSkin ? GroupFramePro : GroupFrame;
        public Color GroupContentBackgroundColor => EditorGUIUtility.isProSkin ? GroupContentBackgroundPro : GroupContentBackground;
        #endregion
        
        #region SingleModule
        public Color SingleModuleHeaderColor => EditorGUIUtility.isProSkin ? singleModuleHeaderPro : singleModuleHeader;
        public Color SingleModuleFrameColor => EditorGUIUtility.isProSkin ? singleModuleFramePro : singleModuleFrame;
        public Color SingleModuleContentBackgroundColor => EditorGUIUtility.isProSkin ? singleModuleContentBackgroundPro : singleModuleContentBackground;
        #endregion
        
        public GUIStyle Header_H1_TitleStyle => EditorGUIUtility.isProSkin ? header_H1_LabelStyle_Pro : header_H1_LabelStyle;
      
        // ========================================================
        // FOLDOUTS GETTERS
        // ========================================================
        
        public GUIStyle Foldout_Header_H1_TitleStyle => EditorGUIUtility.isProSkin ? foldout_Header_H1_LabelStyle_Pro : foldout_Header_H1_LabelStyle;
        public Color Foldout_HeaderFrame_H1 => EditorGUIUtility.isProSkin ? foldout_HeaderFrame_H1_Pro : foldout_HeaderFrame_H1;
        public Color Foldout_HeaderBackground_H1 => EditorGUIUtility.isProSkin ? foldout_HeaderBackground_H1_Pro : foldout_HeaderBackground_H1;
        public Color Foldout_ContentFrame_H1 => EditorGUIUtility.isProSkin ? foldout_ContentFrame_H1_Pro : foldout_ContentFrame_H1;
        public Color Foldout_ContentBackground_H1 => EditorGUIUtility.isProSkin ? foldout_ContentBackground_H1_Pro : foldout_ContentBackground_H1;
        
        public GUIStyle Foldout_Header_H2_TitleStyle => EditorGUIUtility.isProSkin ? foldout_Header_H2_LabelStyle_Pro : foldout_Header_H2_LabelStyle;
        public Color Foldout_HeaderFrame_H2 => EditorGUIUtility.isProSkin ? foldout_HeaderFrame_H2_Pro : foldout_HeaderFrame_H2;
        public Color Foldout_HeaderBackground_H2 => EditorGUIUtility.isProSkin ? foldout_HeaderBackground_H2_Pro : foldout_HeaderBackground_H2;
        public Color Foldout_ContentFrame_H2 => EditorGUIUtility.isProSkin ? foldout_ContentFrame_H2_Pro : foldout_ContentFrame_H2;
        public Color Foldout_ContentBackground_H2 => EditorGUIUtility.isProSkin ? foldout_ContentBackground_H2_Pro : foldout_ContentBackground_H2;

        
        public GUIStyle Foldout_Header_H3_TitleStyle => EditorGUIUtility.isProSkin ? foldout_Header_H3_LabelStyle_Pro : foldout_Header_H3_LabelStyle;
        public Color Foldout_HeaderFrame_H3 => EditorGUIUtility.isProSkin ? foldout_HeaderFrame_H3_Pro : foldout_HeaderFrame_H3;
        public Color Foldout_HeaderBackground_H3 => EditorGUIUtility.isProSkin ? foldout_HeaderBackground_H3_Pro : foldout_HeaderBackground_H3;
        public Color Foldout_ContentFrame_H3 => EditorGUIUtility.isProSkin ? foldout_ContentFrame_H3_Pro : foldout_ContentFrame_H3;
        public Color Foldout_ContentBackground_H3 => EditorGUIUtility.isProSkin ? foldout_ContentBackground_H3_Pro : foldout_ContentBackground_H3;

        // ========================================================
        // TOOLS WINDOW FOLDOUTS GETTERS (T1, T2, T3)
        // ========================================================

        public Color Foldout_HeaderFrame_T1 => EditorGUIUtility.isProSkin ? foldout_HeaderFrame_T1_Pro : foldout_HeaderFrame_T1;
        public Color Foldout_HeaderBackground_T1 => EditorGUIUtility.isProSkin ? foldout_HeaderBackground_T1_Pro : foldout_HeaderBackground_T1;
        public Color Foldout_ContentFrame_T1 => EditorGUIUtility.isProSkin ? foldout_ContentFrame_T1_Pro : foldout_ContentFrame_T1;
        public Color Foldout_ContentBackground_T1 => EditorGUIUtility.isProSkin ? foldout_ContentBackground_T1_Pro : foldout_ContentBackground_T1;

        public Color Foldout_HeaderFrame_T2 => EditorGUIUtility.isProSkin ? foldout_HeaderFrame_T2_Pro : foldout_HeaderFrame_T2;
        public Color Foldout_HeaderBackground_T2 => EditorGUIUtility.isProSkin ? foldout_HeaderBackground_T2_Pro : foldout_HeaderBackground_T2;
        public Color Foldout_ContentFrame_T2 => EditorGUIUtility.isProSkin ? foldout_ContentFrame_T2_Pro : foldout_ContentFrame_T2;
        public Color Foldout_ContentBackground_T2 => EditorGUIUtility.isProSkin ? foldout_ContentBackground_T2_Pro : foldout_ContentBackground_T2;

        public Color Foldout_HeaderFrame_T3 => EditorGUIUtility.isProSkin ? foldout_HeaderFrame_T3_Pro : foldout_HeaderFrame_T3;
        public Color Foldout_HeaderBackground_T3 => EditorGUIUtility.isProSkin ? foldout_HeaderBackground_T3_Pro : foldout_HeaderBackground_T3;
        public Color Foldout_ContentFrame_T3 => EditorGUIUtility.isProSkin ? foldout_ContentFrame_T3_Pro : foldout_ContentFrame_T3;
        public Color Foldout_ContentBackground_T3 => EditorGUIUtility.isProSkin ? foldout_ContentBackground_T3_Pro : foldout_ContentBackground_T3;
          
        // ========================================================
        // FOLDOUTS WIDE GETTERS (T1, T2, T3)
        // ========================================================
        
        public GUIStyle FoldoutWide_H1_TitleStyle => EditorGUIUtility.isProSkin ? foldoutWide_Header_H1_LabelStyle_Pro : foldoutWide_Header_H1_LabelStyle;
        public Color FoldoutWide_HeaderFrame_H1 => EditorGUIUtility.isProSkin ? foldoutWide_HeaderFrame_H1_Pro : foldoutWide_HeaderFrame_H1;
        public Color FoldoutWide_HeaderBackground_H1 => EditorGUIUtility.isProSkin ? foldoutWide_HeaderBackground_H1_Pro : foldoutWide_HeaderBackground_H1;
        public Color FoldoutWide_ContentFrame_H1 => EditorGUIUtility.isProSkin ? foldoutWide_ContentFrame_H1_Pro : foldoutWide_ContentFrame_H1;
        public Color FoldoutWide_ContentBackground_H1 => EditorGUIUtility.isProSkin ? foldoutWide_ContentBackground_H1_Pro : foldoutWide_ContentBackground_H1;
        
        public GUIStyle FoldoutWide_H2_TitleStyle => EditorGUIUtility.isProSkin ? foldoutWide_Header_H2_LabelStyle_Pro : foldoutWide_Header_H2_LabelStyle;
        public Color FoldoutWide_HeaderFrame_H2 => EditorGUIUtility.isProSkin ? foldoutWide_HeaderFrame_H2_Pro : foldoutWide_HeaderFrame_H2;
        public Color FoldoutWide_HeaderBackground_H2 => EditorGUIUtility.isProSkin ? foldoutWide_HeaderBackground_H2_Pro : foldoutWide_HeaderBackground_H2;
        public Color FoldoutWide_ContentFrame_H2 => EditorGUIUtility.isProSkin ? foldoutWide_ContentFrame_H2_Pro : foldoutWide_ContentFrame_H2;
        public Color FoldoutWide_ContentBackground_H2 => EditorGUIUtility.isProSkin ? foldoutWide_ContentBackground_H2_Pro : foldoutWide_ContentBackground_H2;
        
        public GUIStyle FoldoutWide_H3_TitleStyle => EditorGUIUtility.isProSkin ? foldoutWide_Header_H3_LabelStyle_Pro : foldoutWide_Header_H3_LabelStyle;
        public Color FoldoutWide_HeaderFrame_H3 => EditorGUIUtility.isProSkin ? foldoutWide_HeaderFrame_H3_Pro : foldoutWide_HeaderFrame_H3;
        public Color FoldoutWide_HeaderBackground_H3 => EditorGUIUtility.isProSkin ? foldoutWide_HeaderBackground_H3_Pro : foldoutWide_HeaderBackground_H3;
        public Color FoldoutWide_ContentFrame_H3 => EditorGUIUtility.isProSkin ? foldoutWide_ContentFrame_H3_Pro : foldoutWide_ContentFrame_H3;
        public Color FoldoutWide_ContentBackground_H3 => EditorGUIUtility.isProSkin ? foldoutWide_ContentBackground_H3_Pro : foldoutWide_ContentBackground_H3;

        // ========================================================
        // FOLDOUT MODULE GETTERS
        // ========================================================
        public GUIStyle FoldoutModule_LabelStyle => EditorGUIUtility.isProSkin ? foldoutModule_Header_LabelStyle_Pro : foldoutModule_Header_LabelStyle;
        public Color FoldoutModule_HeaderFrame => EditorGUIUtility.isProSkin ? foldoutModule_HeaderFrame_Pro : foldoutModule_HeaderFrame;
        public Color FoldoutModule_HeaderBackground => EditorGUIUtility.isProSkin ? foldoutModule_HeaderBackground_Pro : foldoutModule_HeaderBackground;
        public Color FoldoutModule_ContentFrame => EditorGUIUtility.isProSkin ? foldoutModule_ContentFrame_Pro : foldoutModule_ContentFrame;
        public Color FoldoutModule_ContentBackground => EditorGUIUtility.isProSkin ? foldoutModule_ContentBackground_Pro : foldoutModule_ContentBackground;

               
        public Color BoxContentBackgroundColor => EditorGUIUtility.isProSkin ? BoxContentBackgroundPro : BoxContentBackground;

        
        public Color TitleUnderline
        {
            get
            {
                if (EditorGUIUtility.isProSkin)
                    return titleUnderlineDark;
                else
                    return titleUnderline;
            }
        }
    }
}
#pragma warning restore 0414