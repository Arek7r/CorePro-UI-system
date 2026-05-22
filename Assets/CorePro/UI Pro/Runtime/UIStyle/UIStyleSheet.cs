using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace CorePro.UI
{
    [CreateAssetMenu(fileName = "UIStyleSheet", menuName = "CorePro/UI/Style Sheet")]
    public class UIStyleSheet : ScriptableObject
    {
        // === UI Color slots ===

        [SerializeField] private List<ColorEntry> colors = new List<ColorEntry>
        {
            new ColorEntry { name = "Accent",      color = new Color32( 80, 140, 255, 255) },
            new ColorEntry { name = "AccentMatch",  color = new Color32(255, 255, 255, 255) },
            new ColorEntry { name = "Primary",      color = new Color32(255, 255, 255, 255) },
            new ColorEntry { name = "Secondary",    color = new Color32(200, 200, 200, 255) },
            new ColorEntry { name = "Background",   color = new Color32( 18,  18,  18, 255) },
            new ColorEntry { name = "Negative",     color = new Color32(220,  60,  60, 255) },
        };

        // === Font Color slots ===
        [SerializeField] private List<ColorEntry> fontColors = new List<ColorEntry>
        {
            new ColorEntry { name = "Text1",     color = new Color32( 10,  10,  10, 255) },
            new ColorEntry { name = "Text2",     color = new Color32( 90,  90,  90, 255) },
            new ColorEntry { name = "Text3",     color = new Color32(150, 150, 150, 255) },
            new ColorEntry { name = "Text1Dark", color = new Color32(245, 245, 245, 255) },
            new ColorEntry { name = "Text2Dark", color = new Color32(180, 180, 180, 255) },
        };

        // === Font slots === 

        [SerializeField] private List<FontEntry> fonts = new List<FontEntry>
        {
            new FontEntry { name = "Heading", font = null, size = 32f },
            new FontEntry { name = "Body",    font = null, size = 16f },
            new FontEntry { name = "Button",  font = null, size = 14f },
            new FontEntry { name = "Caption", font = null, size = 12f },
            new FontEntry { name = "Quote",   font = null, size = 18f },
        };

        // === Audio ===

        [SerializeField] private AudioClip clickSound;
        [SerializeField] private AudioClip hoverSound;
        [SerializeField] private AudioClip notificationSound;

        // === Themes ===

        [SerializeField] private List<ThemePreset> themes = new List<ThemePreset>
        {
            new ThemePreset { name = "Light" },
            new ThemePreset { name = "Dark"  },
        };

        /// <summary>
        /// Index of the currently active theme.
        /// -1 means no theme active – slot default values are used directly.
        /// Set via <see cref="SetTheme"/>, <see cref="SetThemeIndex"/>, or <see cref="SetThemeNone"/>.
        /// </summary>
        [SerializeField] private int currentThemeIndex = -1;

        // === Entry definitions ===

        [Serializable]
        public struct ColorEntry
        {
            [Tooltip("Slot name – becomes an enum value after code generation.")]
            public string name;
            [Tooltip("Default color used when no theme is active.")]
            public Color color;
            /// <summary>
            /// Stable identifier – written as the enum integer value.
            /// Assigned once; never changes on reorder so enum values stay valid
            /// even after the slot list is reordered in the inspector.
            /// </summary>
            [HideInInspector] public int stableId;
        }

        [Serializable]
        public struct FontEntry
        {
            [Tooltip("Slot name – becomes an enum value after code generation.")]
            public string name;
            public TMP_FontAsset font;
            [Min(1f)] public float size;
            /// <inheritdoc cref="ColorEntry.stableId"/>
            [HideInInspector] public int stableId;
        }

        // === Read-only views ===

        public IReadOnlyList<ColorEntry>  Colors      => colors;
        public IReadOnlyList<ColorEntry>  FontColors  => fontColors;
        public IReadOnlyList<FontEntry>   Fonts       => fonts;
        public IReadOnlyList<ThemePreset> Themes      => themes;
        public int                        CurrentThemeIndex => currentThemeIndex;

        public AudioClip ClickSound        => clickSound;
        public AudioClip HoverSound        => hoverSound;
        public AudioClip NotificationSound => notificationSound;

        /// <summary>Returns the active ThemePreset, or null when index is -1 (defaults-only mode).</summary>
        public ThemePreset CurrentTheme =>
            themes != null && currentThemeIndex >= 0 && currentThemeIndex < themes.Count
                ? themes[currentThemeIndex]
                : null;

        // === Runtime cache ===

        private Dictionary<int, Color>                            _colorById;
        private Dictionary<int, Color>                            _fontColorById;
        private Dictionary<int, (TMP_FontAsset font, float size)> _fontById;
        private bool                                               _cacheDirty = true;

        // === Theme switching ===

        /// <summary>
        /// Switches to the theme with the given name.
        /// Returns false and logs a warning when the name is not found.
        /// </summary>
        public bool SetTheme(string themeName)
        {
            for (int i = 0; i < themes.Count; i++)
            {
                if (!string.Equals(themes[i].name, themeName, StringComparison.OrdinalIgnoreCase))
                    continue;

                SetThemeIndex(i);
                return true;
            }

            Debug.LogWarning($"[UIStyleSheet] Theme \"{themeName}\" not found.", this);
            return false;
        }

        /// <summary>
        /// Switches to the theme at the given index.
        /// Pass -1 to disable all themes and use slot default values only.
        /// </summary>
        public void SetThemeIndex(int index)
        {
            if (index != -1 && (index < 0 || index >= themes.Count))
            {
                Debug.LogWarning($"[UIStyleSheet] Theme index {index} out of range.", this);
                return;
            }

            currentThemeIndex = index;
            NotifyChanged();
        }

        /// <summary>Disables all themes and falls back to slot default values.</summary>
        public void SetThemeNone()
        {
            currentThemeIndex = -1;
            NotifyChanged();
        }

        // O(1) lookups 

        /// <summary>
        /// Returns the UI color for the given slot id (cast your ColorSlot enum to int).
        /// Returns Color.magenta when the id is not found.
        /// </summary>
        public Color GetColor(int id)
        {
            BuildCacheIfDirty();
            return _colorById != null && _colorById.TryGetValue(id, out Color c) ? c : Color.magenta;
        }

        /// <summary>
        /// Returns the font color for the given slot id (cast your FontColorSlot enum to int).
        /// Returns Color.magenta when the id is not found.
        /// </summary>
        public Color GetFontColor(int id)
        {
            BuildCacheIfDirty();
            return _fontColorById != null && _fontColorById.TryGetValue(id, out Color c) ? c : Color.magenta;
        }

        /// <summary>
        /// Returns the font asset and size for the given slot id (cast your FontSlot enum to int).
        /// Returns false when the id is not found.
        /// </summary>
        public bool TryGetFont(int id, out TMP_FontAsset font, out float size)
        {
            BuildCacheIfDirty();
            if (_fontById != null && _fontById.TryGetValue(id, out var val))
            {
                font = val.font;
                size = val.size;
                return true;
            }
            font = null;
            size = 0f;
            return false;
        }

        // === Cache ===

        private void BuildCacheIfDirty()
        {
            // Also rebuild when any dict is null – happens after domain reload or
            // SO deserialization where _cacheDirty is false but dicts are not yet allocated.
            if (!_cacheDirty && _colorById != null && _fontColorById != null && _fontById != null) return;

            ThemePreset theme = CurrentTheme;

            _colorById = new Dictionary<int, Color>(colors.Count);
            for (int i = 0; i < colors.Count; i++)
            {
                Color c = (theme != null && i < theme.colors.Count) ? theme.colors[i] : colors[i].color;
                _colorById[colors[i].stableId] = c;
            }

            _fontColorById = new Dictionary<int, Color>(fontColors.Count);
            for (int i = 0; i < fontColors.Count; i++)
            {
                Color c = (theme != null && i < theme.fontColors.Count) ? theme.fontColors[i] : fontColors[i].color;
                _fontColorById[fontColors[i].stableId] = c;
            }

            _fontById = new Dictionary<int, (TMP_FontAsset, float)>(fonts.Count);
            for (int i = 0; i < fonts.Count; i++)
            {
                TMP_FontAsset font = fonts[i].font;
                float         size = fonts[i].size;

                if (theme != null && i < theme.fonts.Count)
                {
                    var ovr = theme.fonts[i];
                    if (ovr.font != null) font = ovr.font;
                    if (ovr.size >= 1f)   size = ovr.size;
                }

                _fontById[fonts[i].stableId] = (font, size);
            }

            _cacheDirty = false;
        }

        public void InvalidateCache() => _cacheDirty = true;

        // === Change notification ===

        /// <summary>
        /// Fired whenever the active theme changes or any slot value is edited.
        /// UIStyle components subscribe in OnEnable and react immediately.
        /// </summary>
        public static event Action<UIStyleSheet> OnThemeChanged;

        private void NotifyChanged()
        {
            InvalidateCache();
            OnThemeChanged?.Invoke(this);
        }

#if UNITY_EDITOR
        public void NotifyChangedFromEditor() => NotifyChanged();
#endif
    }
}