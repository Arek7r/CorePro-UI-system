using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CorePro.UI
{
    /// <summary>
    /// Base component for all UIStyle* components.
    ///
    /// Refresh strategy:
    ///   Startup / scene load  – OnEnable calls OnReset() once; reads current
    ///                           theme from the assigned StyleSheet automatically.
    ///   Runtime theme switch  – UIStyleSheet.OnThemeChanged event triggers
    ///                           OnReset() on every subscribed component.
    ///   Editor inspector edit – UIStyleSheetEditor calls ForceApplyInEditor()
    ///                           directly via FindObjectsByType.
    ///
    /// No Update(), no polling, no GC in the hot path.
    /// </summary>
    [ExecuteAlways]
    public abstract class UIStyle : MonoBehaviour
    {
        [SerializeField]
        public UIStyleSheet styleSheet;

        // Unity lifecycle 
        protected virtual void Awake()
        {
#if UNITY_EDITOR
            // In edit mode let OnEnable handle the refresh to avoid double Apply.
            if (!Application.isPlaying)
                return;
#endif
            OnReset();
        }

        protected virtual void OnEnable()
        {
            // Subscribe to theme changes so this component reacts immediately
            // whenever SetTheme() is called from anywhere in the codebase.
            UIStyleSheet.OnThemeChanged += OnThemeChangedHandler;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                OnReset(); return;
            }
#endif
            // Apply the current theme as soon as the component becomes active.
            // This handles: scene load, object activation, theme already set before
            // this scene was loaded.
            OnReset();
        }

        protected virtual void OnDisable()
        {
            // Always unsubscribe to prevent calls on destroyed/inactive components.
            UIStyleSheet.OnThemeChanged -= OnThemeChangedHandler;
        }

        // ==== Theme change handler ====
        private void OnThemeChangedHandler(UIStyleSheet changedSheet)
        {
            // Only react to the sheet assigned to this component.
            if (changedSheet == styleSheet)
                OnReset();
        }

        //  ==== Public API ====

        /// <summary>
        /// Applies colors and fonts from the currently active theme.
        /// Call this from your startup code after loading user settings,
        /// or whenever you need to force a refresh.
        /// </summary>
        public void OnReset()
        {
            if (styleSheet == null)
            {
#if UNITY_EDITOR
                // In edit mode try to auto-assign the first UIStyleSheet found in
                // the project. This saves having to drag the asset onto every
                // component manually when there is only one sheet in the project.
                if (!Application.isPlaying)
                    TryAutoAssignStyleSheet();
#endif
                if (styleSheet == null)
                    return;
            }

            Apply(styleSheet);
        }

#if UNITY_EDITOR
        /// <summary>
        /// Searches the AssetDatabase for the first UIStyleSheet asset in the project
        /// and assigns it. Marks the component dirty so the change is saved with the scene.
        /// Only runs in edit mode – never at runtime.
        /// </summary>
        private void TryAutoAssignStyleSheet()
        {
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:" + nameof(UIStyleSheet));
            if (guids.Length == 0)
                return;

            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
            styleSheet  = UnityEditor.AssetDatabase.LoadAssetAtPath<UIStyleSheet>(path);

            if (styleSheet != null)
                UnityEditor.EditorUtility.SetDirty(this);
        }
#endif

        // ==== Abstract core ====

        /// <summary>
        /// Override in subclasses to push color/font values onto the target component.
        /// Guaranteed: <paramref name="sheet"/> is not null when called.
        /// </summary>
        protected abstract void Apply(UIStyleSheet sheet);

        // ==== Editor-only helpers ====

#if UNITY_EDITOR
        /// <summary>
        /// Called by UIStyleSheetEditor when <paramref name="changedSheet"/> was
        /// modified in the Inspector. Skips components referencing a different asset.
        /// </summary>
        public void ForceApplyInEditor(UIStyleSheet changedSheet)
        {
            if (styleSheet != changedSheet) return;
            Apply(styleSheet);
            EditorUtility.SetDirty(this);
        }
#endif
    }
}