// Usage: Assets > Create > CorePro > Editor Theme
// One asset per project. Every inspector loads it automatically via
// CoreProEditor (base class). Changing values in this asset updates the look
// of all inspectors without recompilation.

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace CorePro.Editor.Framework
{
    [CreateAssetMenu(
        fileName = "CoreProEditorTheme",
        menuName  = "CorePro/Editor Theme",
        order     = 0)]
    public class CoreProEditorTheme : ScriptableObject
    {
        [Header("Header Bar")]
        [Tooltip("Height of section header bars in pixels.")]
        public float headerHeight = 30f;

        [Tooltip("Vertical gap between section header bars in pixels.")]
        public float sectionSpacing = 0f;

        [Tooltip("Header background color - Dark skin.")]
        public Color headerBgDark       = new Color(0.18f, 0.18f, 0.18f, 1f);

        [Tooltip("Header background color on hover - Dark skin.")]
        public Color headerBgHoverDark  = new Color(0.22f, 0.22f, 0.22f, 1f);

        [Tooltip("Header background color - Light skin.")]
        public Color headerBgLight      = new Color(0.60f, 0.60f, 0.60f, 1f);

        [Tooltip("Header background color on hover - Light skin.")]
        public Color headerBgHoverLight = new Color(0.65f, 0.65f, 0.65f, 1f);

        [Tooltip("Separator line below the header - Dark skin.")]
        public Color headerBorderDark   = new Color(0.10f, 0.10f, 0.10f, 1f);

        [Tooltip("Separator line below the header - Light skin.")]
        public Color headerBorderLight  = new Color(0.45f, 0.45f, 0.45f, 1f);

        [Header("Accent Stripe")]
        [Tooltip("Color of the left accent stripe shown when the section is open.")]
        public Color accentColor        = new Color(0.25f, 0.55f, 1f, 0.85f);

        [Tooltip("Width of the left accent stripe in pixels.")]
        public float accentWidth        = 3f;

        [Header("Typography")]
        public int   headerFontSize     = 11;
        public Color headerTextDark     = new Color(0.85f, 0.85f, 0.85f, 1f);
        public Color headerTextLight    = new Color(0.08f, 0.08f, 0.08f, 1f);

        [Header("Preview Bar")]
        public Color previewBarBg       = new Color(0.15f, 0.15f, 0.15f, 1f);
        public Color previewBarBorder   = new Color(0.40f, 0.40f, 0.40f, 1f);
        public float previewBarHeight   = 22f;

        /// <summary>Returns the correct header background color for the current editor skin.</summary>
        public Color HeaderBg(bool hover) => EditorGUIUtility.isProSkin
            ? (hover ? headerBgHoverDark  : headerBgDark)
            : (hover ? headerBgHoverLight : headerBgLight);

        /// <summary>Returns the separator line color for the current editor skin.</summary>
        public Color HeaderBorder() => EditorGUIUtility.isProSkin
            ? headerBorderDark
            : headerBorderLight;

        /// <summary>Returns the header text color for the current editor skin.</summary>
        public Color HeaderText() => EditorGUIUtility.isProSkin
            ? headerTextDark
            : headerTextLight;

        private static CoreProEditorTheme _instance;

        /// <summary>
        /// Returns the first CoreProEditorTheme asset found in the project.
        /// If none exists, returns an in-memory instance with default values.
        /// </summary>
        public static CoreProEditorTheme Load()
        {
            if (_instance != null) return _instance;

            string[] guids = AssetDatabase.FindAssets("t:CoreProEditorTheme");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                _instance = AssetDatabase.LoadAssetAtPath<CoreProEditorTheme>(path);
            }

            if (_instance == null)
            {
                // Fallback: default values, in-memory only (not saved to disk)
                _instance = CreateInstance<CoreProEditorTheme>();
            }

            return _instance;
        }

        /// <summary>
        /// Clears the cached instance on every domain reload.
        /// </summary>
        [InitializeOnLoadMethod]
        private static void ClearCache()
        {
            _instance = null;
        }
    }
}
#endif
