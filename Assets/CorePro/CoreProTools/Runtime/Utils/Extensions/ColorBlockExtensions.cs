using UnityEngine;
using UnityEngine.UI;

namespace CorePro.Utils.Extensions
{
    public static class ColorBlockExtensions
    {
        /// <summary>
        /// Sets normal color and returns modified ColorBlock.
        /// Usage: button.colors = button.colors.SetNormalColor(Color.black);
        /// </summary>
        public static ColorBlock SetNormalColor(this ColorBlock colors, Color color)
        {
            colors.normalColor = color;
            return colors;
        }

        /// <summary>
        /// Sets highlighted color and returns modified ColorBlock.
        /// </summary>
        public static ColorBlock SetHighlightedColor(this ColorBlock colors, Color color)
        {
            colors.highlightedColor = color;
            return colors;
        }

        /// <summary>
        /// Sets pressed color and returns modified ColorBlock.
        /// </summary>
        public static ColorBlock SetPressedColor(this ColorBlock colors, Color color)
        {
            colors.pressedColor = color;
            return colors;
        }

        /// <summary>
        /// Sets selected color and returns modified ColorBlock.
        /// </summary>
        public static ColorBlock SetSelectedColor(this ColorBlock colors, Color color)
        {
            colors.selectedColor = color;
            return colors;
        }

        /// <summary>
        /// Sets disabled color and returns modified ColorBlock.
        /// </summary>
        public static ColorBlock SetDisabledColor(this ColorBlock colors, Color color)
        {
            colors.disabledColor = color;
            return colors;
        }

        /// <summary>
        /// Sets color multiplier and returns modified ColorBlock.
        /// </summary>
        public static ColorBlock SetColorMultiplier(this ColorBlock colors, float multiplier)
        {
            colors.colorMultiplier = multiplier;
            return colors;
        }

        /// <summary>
        /// Sets fade duration and returns modified ColorBlock.
        /// </summary>
        public static ColorBlock SetFadeDuration(this ColorBlock colors, float duration)
        {
            colors.fadeDuration = duration;
            return colors;
        }

        /// <summary>
        /// Chainable method to set all colors at once.
        /// </summary>
        public static ColorBlock SetAllColors(this ColorBlock colors, Color normal, Color highlighted, Color pressed, Color selected, Color disabled)
        {
            colors.normalColor = normal;
            colors.highlightedColor = highlighted;
            colors.pressedColor = pressed;
            colors.selectedColor = selected;
            colors.disabledColor = disabled;
            return colors;
        }

        /// <summary>
        /// Sets all colors to the same value.
        /// </summary>
        public static ColorBlock SetUniformColor(this ColorBlock colors, Color color)
        {
            colors.normalColor = color;
            colors.highlightedColor = color;
            colors.pressedColor = color;
            colors.selectedColor = color;
            colors.disabledColor = color;
            return colors;
        }

        /// <summary>
        /// Chainable method to set all colors and multiplier.
        /// </summary>
        public static ColorBlock With(this ColorBlock colors, 
            Color? normal = null, 
            Color? highlighted = null, 
            Color? pressed = null, 
            Color? selected = null, 
            Color? disabled = null,
            float? multiplier = null,
            float? fadeDuration = null)
        {
            if (normal.HasValue) colors.normalColor = normal.Value;
            if (highlighted.HasValue) colors.highlightedColor = highlighted.Value;
            if (pressed.HasValue) colors.pressedColor = pressed.Value;
            if (selected.HasValue) colors.selectedColor = selected.Value;
            if (disabled.HasValue) colors.disabledColor = disabled.Value;
            if (multiplier.HasValue) colors.colorMultiplier = multiplier.Value;
            if (fadeDuration.HasValue) colors.fadeDuration = fadeDuration.Value;
            return colors;
        }
    }
}
