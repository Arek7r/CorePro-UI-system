// Base class for all custom inspectors in the CorePro project.
// Provides:
//   - Automatic theme loading (CoreProEditorTheme)
//   - Ready-made DrawSection / DrawBorder / DrawLabelWithShadow methods
//   - Foldout system with EditorPrefs persistence
//   - Lazy-initialized styles (safe after domain reload)

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace CorePro.Editor.Framework
{
    public abstract class CoreProEditor : UnityEditor.Editor
    {
        // == Theme ==

        protected CoreProEditorTheme Theme { get; private set; }

        // == Styles (lazy-init) ==

        GUIStyle _sectionStyle;
        GUIStyle _shadowStyle;
        GUIStyle _labelStyle;

        /// <summary>Text style used for section header labels.</summary>
        protected GUIStyle SectionStyle   => _sectionStyle;

        /// <summary>White bold text style, used for example in the preview bar label.</summary>
        protected GUIStyle LabelStyle     => _labelStyle;

        /// <summary>Dark semi-transparent text style used as a drop shadow.</summary>
        protected GUIStyle ShadowStyle    => _shadowStyle;

        void EnsureStyles()
        {
            if (_sectionStyle != null) return;

            _sectionStyle = new GUIStyle(EditorStyles.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize  = Theme.headerFontSize,
                alignment = TextAnchor.MiddleLeft,
                imagePosition = ImagePosition.ImageLeft
            };
            _sectionStyle.normal.textColor = Theme.HeaderText();

            _labelStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
            {
                fontSize  = 10,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            _labelStyle.normal.textColor = Color.white;

            _shadowStyle = new GUIStyle(_labelStyle);
            _shadowStyle.normal.textColor = new Color(0f, 0f, 0f, 0.75f);
        }

        // == Lifecycle ==

        protected virtual void OnEnable()
        {
            Theme = CoreProEditorTheme.Load();
            // Subclasses override OnEnable and must call base.OnEnable()
            // to ensure Theme is loaded before any FindProperty calls.
        }

        protected void InvalidateStyles() => _sectionStyle = null;

        // == Foldout persistence ==

        /// <summary>
        /// Returns an EditorPrefs key unique to this component instance and section name.
        /// </summary>
        protected string PrefKey(string section) =>
            $"CoreProEditor_{GetType().Name}_{target.GetInstanceID()}_{section}";

        protected bool LoadFoldout(string section, bool defaultValue) =>
            EditorPrefs.GetBool(PrefKey(section), defaultValue);

        protected void SaveFoldout(string section, bool value) =>
            EditorPrefs.SetBool(PrefKey(section), value);

        // == Section drawing ==

        /// <summary>Delegate for the content drawn inside a section body.</summary>
        protected delegate void SectionBody();

        /// <summary>
        /// Draws a dark header bar spanning the full inspector width.
        /// Supports click to toggle the foldout and hover highlight.
        /// </summary>
        protected void DrawSection(
            string      label,
            ref bool    foldout,
            string      prefKey,
            SectionBody body)
        {
            EnsureStyles();

            EditorGUILayout.Space(Theme.sectionSpacing);

            Rect headerRect = GUILayoutUtility.GetRect(
                GUIContent.none, GUIStyle.none,
                GUILayout.Height(Theme.headerHeight),
                GUILayout.ExpandWidth(true));

            // Expand to the full inspector width
            float leftPad       = headerRect.x;
            headerRect.x        = 0f;
            headerRect.width    = EditorGUIUtility.currentViewWidth;

            bool isHover = headerRect.Contains(Event.current.mousePosition);

            // Background
            EditorGUI.DrawRect(headerRect, Theme.HeaderBg(isHover));

            // Bottom separator line
            EditorGUI.DrawRect(
                new Rect(headerRect.x, headerRect.yMax - 1f, headerRect.width, 1f),
                Theme.HeaderBorder());

            // Left accent stripe (visible when section is open)
            if (foldout)
                EditorGUI.DrawRect(
                    new Rect(headerRect.x, headerRect.y, Theme.accentWidth, headerRect.height),
                    Theme.accentColor);

            // Arrow and label text
            string arrow   = foldout ? "▾" : "▸";
            float  padLeft = leftPad + Theme.accentWidth + 6f;
            Rect   txtRect = new Rect(
                padLeft,
                headerRect.y,
                headerRect.width - padLeft - 6f,
                headerRect.height);

            GUI.Label(txtRect, $"{arrow}  {label}", _sectionStyle);

            // Trigger repaint on hover for smooth color transition
            if (isHover && Event.current.type == EventType.MouseMove)
                Repaint();

            // Click to toggle
            if (Event.current.type    == EventType.MouseDown &&
                Event.current.button  == 0 &&
                headerRect.Contains(Event.current.mousePosition))
            {
                foldout = !foldout;
                SaveFoldout(prefKey, foldout);
                Event.current.Use();
            }

            // Section body content
            if (foldout)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.Space(4);
                body();
                EditorGUI.indentLevel--;
                EditorGUILayout.Space(2);
            }
        }

        /// <summary>
        /// Draws a dark header bar spanning the full inspector width, supporting icons.
        /// Supports click to toggle the foldout and hover highlight.
        /// Allocates no GC during layout by separating foldout arrow from the header content.
        /// </summary>
        protected void DrawSection(
            GUIContent  headerContent,
            ref bool    foldout,
            string      prefKey,
            SectionBody body)
        {
            EnsureStyles();

            EditorGUILayout.Space(Theme.sectionSpacing);

            Rect headerRect = GUILayoutUtility.GetRect(
                GUIContent.none, GUIStyle.none,
                GUILayout.Height(Theme.headerHeight),
                GUILayout.ExpandWidth(true));

            // Expand to the full inspector width
            float leftPad       = headerRect.x;
            headerRect.x        = 0f;
            headerRect.width    = EditorGUIUtility.currentViewWidth;

            bool isHover = headerRect.Contains(Event.current.mousePosition);

            // Background
            EditorGUI.DrawRect(headerRect, Theme.HeaderBg(isHover));

            // Bottom separator line
            EditorGUI.DrawRect(
                new Rect(headerRect.x, headerRect.yMax - 1f, headerRect.width, 1f),
                Theme.HeaderBorder());

            // Left accent stripe (visible when section is open)
            if (foldout)
                EditorGUI.DrawRect(
                    new Rect(headerRect.x, headerRect.y, Theme.accentWidth, headerRect.height),
                    Theme.accentColor);

            // Separate arrow and content drawing to prevent string allocation (Zero-GC)
            string arrow   = foldout ? "▾" : "▸";
            float  padLeft = leftPad + Theme.accentWidth + 6f;
            
            // Draw arrow
            Rect arrowRect = new Rect(padLeft, headerRect.y, 14f, headerRect.height);
            GUI.Label(arrowRect, arrow, _sectionStyle);

            // Draw icon and text label
            Rect txtRect = new Rect(
                padLeft + 14f,
                headerRect.y,
                headerRect.width - padLeft - 20f,
                headerRect.height);

            GUI.Label(txtRect, headerContent, _sectionStyle);

            // Trigger repaint on hover for smooth color transition
            if (isHover && Event.current.type == EventType.MouseMove)
                Repaint();

            // Click to toggle
            if (Event.current.type    == EventType.MouseDown &&
                Event.current.button  == 0 &&
                headerRect.Contains(Event.current.mousePosition))
            {
                foldout = !foldout;
                SaveFoldout(prefKey, foldout);
                Event.current.Use();
            }

            // Section body content
            if (foldout)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.Space(4);
                body();
                EditorGUI.indentLevel--;
                EditorGUILayout.Space(2);
            }
        }
        
        /// <summary>
        /// Draws a static dark header bar spanning the full inspector width.
        /// Identical visual style to DrawSection but always expanded - no arrow, no click, no foldout.
        /// </summary>
        protected void DrawHeader(string label, SectionBody body)
        {
            EnsureStyles();

            EditorGUILayout.Space(Theme.sectionSpacing);

            Rect headerRect = GUILayoutUtility.GetRect(
                GUIContent.none, GUIStyle.none,
                GUILayout.Height(Theme.headerHeight),
                GUILayout.ExpandWidth(true));

            float leftPad    = headerRect.x;
            headerRect.x     = 0f;
            headerRect.width = EditorGUIUtility.currentViewWidth;

            // Background (no hover state - not interactive)
            EditorGUI.DrawRect(headerRect, Theme.HeaderBg(false));

            // Bottom separator line
            EditorGUI.DrawRect(
                new Rect(headerRect.x, headerRect.yMax - 1f, headerRect.width, 1f),
                Theme.HeaderBorder());

            // Label only - no arrow, no accent stripe
            float  padLeft = leftPad + 10f;
            Rect   txtRect = new Rect(
                padLeft,
                headerRect.y,
                headerRect.width - padLeft - 6f,
                headerRect.height);

            GUI.Label(txtRect, label, _sectionStyle);

            // Body always drawn
            EditorGUI.indentLevel++;
            EditorGUILayout.Space(4);
            body();
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(2);
        }

        // == Section with enable toggle ==

        /// <summary>
        /// Like DrawSection but with an interactive enable/disable toggle on the right side of the header.
        /// The accent stripe changes color based on enabled state even when collapsed.
        /// </summary>
        protected void DrawSectionWithToggle(
            string label,
            ref bool foldout,
            string prefKey,
            SerializedProperty enabledProp,
            SectionBody body)
        {
            EnsureStyles();

            EditorGUILayout.Space(Theme.sectionSpacing);

            Rect headerRect = GUILayoutUtility.GetRect(
                GUIContent.none, GUIStyle.none,
                GUILayout.Height(Theme.headerHeight),
                GUILayout.ExpandWidth(true));

            float leftPad    = headerRect.x;
            headerRect.x     = 0f;
            headerRect.width = EditorGUIUtility.currentViewWidth;

            // Toggle and status label rects (right side)
            float toggleW    = 20f;
            float statusW    = 58f;
            float toggleGap  = 4f;
            float rightEdge  = headerRect.xMax - 8f;
            Rect  toggleRect = new Rect(rightEdge - toggleW, headerRect.y + (headerRect.height - 16f) * 0.5f, toggleW, 16f);
            Rect  statusRect = new Rect(rightEdge - toggleW - toggleGap - statusW, headerRect.y, statusW, headerRect.height);

            bool isEnabled = enabledProp.boolValue;
            bool isHover   = headerRect.Contains(Event.current.mousePosition);

            // Background
            EditorGUI.DrawRect(headerRect, Theme.HeaderBg(isHover));

            // Bottom separator
            EditorGUI.DrawRect(
                new Rect(headerRect.x, headerRect.yMax - 1f, headerRect.width, 1f),
                Theme.HeaderBorder());

            // Accent stripe - blue when open, green when enabled+closed, gray when disabled
            Color accentCol = isEnabled
                ? (foldout ? Theme.accentColor : new Color(0.25f, 0.75f, 0.25f, 0.85f))
                : new Color(0.4f, 0.4f, 0.4f, 0.4f);
            EditorGUI.DrawRect(
                new Rect(headerRect.x, headerRect.y, Theme.accentWidth, headerRect.height),
                accentCol);

            // Arrow + label
            string arrow   = foldout ? "▾" : "▸";
            float  padLeft = leftPad + Theme.accentWidth + 6f;
            Rect   txtRect = new Rect(padLeft, headerRect.y, statusRect.x - padLeft - 4f, headerRect.height);
            GUI.Label(txtRect, $"{arrow}  {label}", _sectionStyle);

            // Status label
            var statusStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleRight,
                fontStyle = FontStyle.Bold
            };
            statusStyle.normal.textColor = isEnabled
                ? new Color(0.3f, 0.85f, 0.3f)
                : new Color(0.55f, 0.55f, 0.55f);
            GUI.Label(statusRect, isEnabled ? "enabled" : "disabled", statusStyle);

            // Interactive toggle checkbox
            EditorGUI.BeginChangeCheck();
            bool newVal = GUI.Toggle(toggleRect, isEnabled, GUIContent.none);
            if (EditorGUI.EndChangeCheck())
            {
                enabledProp.boolValue = newVal;
                enabledProp.serializedObject.ApplyModifiedProperties();
            }

            // Hover repaint
            if (isHover && Event.current.type == EventType.MouseMove)
                Repaint();

            // Click header (outside toggle) → toggle foldout
            if (Event.current.type   == EventType.MouseDown &&
                Event.current.button == 0 &&
                headerRect.Contains(Event.current.mousePosition) &&
                !toggleRect.Contains(Event.current.mousePosition))
            {
                foldout = !foldout;
                SaveFoldout(prefKey, foldout);
                Event.current.Use();
            }

            // Body
            if (foldout)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.Space(4);
                if (!enabledProp.boolValue)
                    DrawDisabledWarning();
                body();
                EditorGUI.indentLevel--;
                EditorGUILayout.Space(2);
            }
        }

        /// <summary>
        /// Like DrawSectionWithToggle but the enabled state is a computed bool (read-only indicator, no interactive checkbox).
        /// Used for sections whose active state is derived from other properties (e.g. Animations).
        /// </summary>
        protected void DrawSectionWithToggle(
            string label,
            ref bool foldout,
            string prefKey,
            bool isEnabled,
            SectionBody body)
        {
            EnsureStyles();

            EditorGUILayout.Space(Theme.sectionSpacing);

            Rect headerRect = GUILayoutUtility.GetRect(
                GUIContent.none, GUIStyle.none,
                GUILayout.Height(Theme.headerHeight),
                GUILayout.ExpandWidth(true));

            float leftPad    = headerRect.x;
            headerRect.x     = 0f;
            headerRect.width = EditorGUIUtility.currentViewWidth;

            float statusW   = 58f;
            float rightEdge = headerRect.xMax - 8f;
            Rect  statusRect = new Rect(rightEdge - statusW, headerRect.y, statusW, headerRect.height);

            bool isHover = headerRect.Contains(Event.current.mousePosition);

            EditorGUI.DrawRect(headerRect, Theme.HeaderBg(isHover));
            EditorGUI.DrawRect(
                new Rect(headerRect.x, headerRect.yMax - 1f, headerRect.width, 1f),
                Theme.HeaderBorder());

            Color accentCol = isEnabled
                ? (foldout ? Theme.accentColor : new Color(0.25f, 0.75f, 0.25f, 0.85f))
                : new Color(0.4f, 0.4f, 0.4f, 0.4f);
            EditorGUI.DrawRect(
                new Rect(headerRect.x, headerRect.y, Theme.accentWidth, headerRect.height),
                accentCol);

            string arrow   = foldout ? "▾" : "▸";
            float  padLeft = leftPad + Theme.accentWidth + 6f;
            Rect   txtRect = new Rect(padLeft, headerRect.y, statusRect.x - padLeft - 4f, headerRect.height);
            GUI.Label(txtRect, $"{arrow}  {label}", _sectionStyle);

            var statusStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleRight,
                fontStyle = FontStyle.Bold
            };
            statusStyle.normal.textColor = isEnabled
                ? new Color(0.3f, 0.85f, 0.3f)
                : new Color(0.55f, 0.55f, 0.55f);
            GUI.Label(statusRect, isEnabled ? "enabled" : "disabled", statusStyle);

            if (isHover && Event.current.type == EventType.MouseMove)
                Repaint();

            if (Event.current.type   == EventType.MouseDown &&
                Event.current.button == 0 &&
                headerRect.Contains(Event.current.mousePosition))
            {
                foldout = !foldout;
                SaveFoldout(prefKey, foldout);
                Event.current.Use();
            }

            if (foldout)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.Space(4);
                if (!isEnabled)
                    DrawDisabledWarning();
                body();
                EditorGUI.indentLevel--;
                EditorGUILayout.Space(2);
            }
        }

        // == Disabled warning ==

        /// <summary>
        /// Draws a red semi-transparent banner warning the user that the module is disabled
        /// and changes will not take effect at runtime.
        /// </summary>
        protected void DrawDisabledWarning()
        {
            EnsureStyles();

            Rect r = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none,
                GUILayout.Height(24f), GUILayout.ExpandWidth(true));
            r = EditorGUI.IndentedRect(r);

            // Red semi-transparent fill + border
            EditorGUI.DrawRect(r, new Color(0.75f, 0.1f, 0.08f, 0.28f));
            DrawBorder(r, new Color(0.9f, 0.25f, 0.2f, 0.55f), 1f);

            DrawLabelWithShadow(r, "⚠   Module disabled - changes will not take effect");

            EditorGUILayout.Space(4);
        }

        // == Sub-header ==

        /// <summary>Lightweight divider with bold label, used inside section bodies.</summary>
        protected static void DrawSubHeaderH3(string label)
        {
            EditorGUILayout.Space(3);
            Rect r = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(r, label, EditorStyles.boldLabel);
            //EditorGUI.DrawRect(new Rect(r.x, r.yMax + 1f, r.width, 1f), new Color(0.35f, 0.35f, 0.35f, 0.6f));
        }
        private static GUIStyle _subHeaderStyle;

        protected static void DrawSubHeader(string label)
        {
            if (_subHeaderStyle == null)
            {
                _subHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.MiddleLeft,
                    padding   = new RectOffset(12, 6, 0, 0)
                };
            }

            EditorGUILayout.Space(4);

            Rect line = EditorGUILayout.GetControlRect(false, 20f);

            // Full inspector width, ignoring indent and the section margins
            Rect bg = new Rect(0f, line.y, EditorGUIUtility.currentViewWidth, line.height);

            EditorGUI.DrawRect(bg, new Color(0.18f, 0.18f, 0.18f, 1f));                       // background bar
            // EditorGUI.DrawRect(new Rect(bg.x, bg.y, 3f, bg.height),                            // left accent stripe
            //     new Color(0.30f, 0.55f, 0.95f, 1f));

            EditorGUI.LabelField(bg, label, _subHeaderStyle);
            EditorGUILayout.Space(3);
        }
        
        // == Drawing utilities ==

        /// <summary>Draws a rectangular border outline.</summary>
        protected static void DrawBorder(Rect r, Color c, float thickness = 1f)
        {
            EditorGUI.DrawRect(new Rect(r.x,              r.y,              r.width,    thickness), c);
            EditorGUI.DrawRect(new Rect(r.x,              r.yMax - thickness, r.width,  thickness), c);
            EditorGUI.DrawRect(new Rect(r.x,              r.y,              thickness,  r.height),  c);
            EditorGUI.DrawRect(new Rect(r.xMax - thickness, r.y,            thickness,  r.height),  c);
        }

        /// <summary>
        /// Draws text with a 4-direction drop shadow, creating an outline effect.
        /// Readable on any background color including the boundary between fill and empty.
        /// </summary>
        protected void DrawLabelWithShadow(Rect r, string text)
        {
            EnsureStyles();

            Vector2[] offsets =
            {
                new Vector2(-1f, -1f), new Vector2( 1f, -1f),
                new Vector2(-1f,  1f), new Vector2( 1f,  1f)
            };

            foreach (var o in offsets)
                GUI.Label(new Rect(r.x + o.x, r.y + o.y, r.width, r.height), text, _shadowStyle);

            GUI.Label(r, text, _labelStyle);
        }
    }
}
#endif