using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace CorePro.UI
{
    /// <summary>
    /// A named set of values for every slot defined in UIStyleSheet.
    /// One ThemePreset per visual mode (Light, Dark, HighContrast, etc.).
    /// Slot list order must match the corresponding slot definitions in UIStyleSheet.
    /// </summary>
    [Serializable]
    public class ThemePreset
    {
        [Tooltip("Unique name used to identify this theme at runtime (e.g. \"Dark\", \"Light\").")]
        public string name;

        [Tooltip("One entry per UI color slot. Order must match UIStyleSheet.colors list.")]
        public List<Color> colors = new List<Color>();

        [Tooltip("One entry per font color slot. Order must match UIStyleSheet.fontColors list.")]
        public List<Color> fontColors = new List<Color>();

        [Tooltip("One entry per font slot. Order must match UIStyleSheet.fonts list.")]
        public List<FontOverride> fonts = new List<FontOverride>();

        /// <summary>
        /// Per-theme font override. Leaving <see cref="font"/> null means
        /// "inherit the font asset from UIStyleSheet; only override the size."
        /// </summary>
        [Serializable]
        public struct FontOverride
        {
            [Tooltip("Leave null to keep the font asset from UIStyleSheet and only override size.")]
            public TMP_FontAsset font;
            [Min(1f)]
            public float size;
        }
    }
}
