using UnityEngine;
using UnityEngine.UI;

namespace CorePro.Utils.Extensions
{
    public static class SelectableExtensions
    {
        /// <summary>
        /// Sets normal color directly on Selectable component.
        /// Usage: button.SetNormalColor(Color.black);
        /// </summary>
        public static T SetNormalColor<T>(this T selectable, Color color) where T : UnityEngine.UI.Selectable
        {
            var colors = selectable.colors;
            colors.normalColor = color;
            selectable.colors = colors;
            return selectable;
        }

        public static T SetHighlightedColor<T>(this T selectable, Color color) where T : UnityEngine.UI.Selectable
        {
            var colors = selectable.colors;
            colors.highlightedColor = color;
            selectable.colors = colors;
            return selectable;
        }

        public static T SetPressedColor<T>(this T selectable, Color color) where T : UnityEngine.UI.Selectable
        {
            var colors = selectable.colors;
            colors.pressedColor = color;
            selectable.colors = colors;
            return selectable;
        }

        public static T SetSelectedColor<T>(this T selectable, Color color) where T : UnityEngine.UI.Selectable
        {
            var colors = selectable.colors;
            colors.selectedColor = color;
            selectable.colors = colors;
            return selectable;
        }

        public static T SetDisabledColor<T>(this T selectable, Color color) where T : UnityEngine.UI.Selectable
        {
            var colors = selectable.colors;
            colors.disabledColor = color;
            selectable.colors = colors;
            return selectable;
        }

        public static T SetColorMultiplier<T>(this T selectable, float multiplier) where T : UnityEngine.UI.Selectable
        {
            var colors = selectable.colors;
            colors.colorMultiplier = multiplier;
            selectable.colors = colors;
            return selectable;
        }

        public static T SetFadeDuration<T>(this T selectable, float duration) where T : UnityEngine.UI.Selectable
        {
            var colors = selectable.colors;
            colors.fadeDuration = duration;
            selectable.colors = colors;
            return selectable;
        }

        /// <summary>
        /// Sets all colors at once.
        /// </summary>
        public static T SetColors<T>(this T selectable, Color normal, Color highlighted, Color pressed, Color selected, Color disabled) where T : UnityEngine.UI.Selectable
        {
            var colors = selectable.colors;
            colors.normalColor = normal;
            colors.highlightedColor = highlighted;
            colors.pressedColor = pressed;
            colors.selectedColor = selected;
            colors.disabledColor = disabled;
            selectable.colors = colors;
            return selectable;
        }

        /// <summary>
        /// Sets all colors to the same value.
        /// </summary>
        public static T SetUniformColor<T>(this T selectable, Color color) where T : UnityEngine.UI.Selectable
        {
            var colors = selectable.colors;
            colors.normalColor = color;
            colors.highlightedColor = color;
            colors.pressedColor = color;
            colors.selectedColor = color;
            colors.disabledColor = color;
            selectable.colors = colors;
            return selectable;
        }

        /// <summary>
        /// Modifies colors with optional parameters.
        /// </summary>
        public static T ModifyColors<T>(this T selectable,
            Color? normal = null,
            Color? highlighted = null,
            Color? pressed = null,
            Color? selected = null,
            Color? disabled = null,
            float? multiplier = null,
            float? fadeDuration = null) where T : UnityEngine.UI.Selectable
        {
            var colors = selectable.colors;
            
            if (normal.HasValue) colors.normalColor = normal.Value;
            if (highlighted.HasValue) colors.highlightedColor = highlighted.Value;
            if (pressed.HasValue) colors.pressedColor = pressed.Value;
            if (selected.HasValue) colors.selectedColor = selected.Value;
            if (disabled.HasValue) colors.disabledColor = disabled.Value;
            if (multiplier.HasValue) colors.colorMultiplier = multiplier.Value;
            if (fadeDuration.HasValue) colors.fadeDuration = fadeDuration.Value;
            
            selectable.colors = colors;
            return selectable;
        }

        /// <summary>
        /// Creates a darker version of current normal color.
        /// </summary>
        public static T DarkenColors<T>(this T selectable, float factor = 0.7f) where T : UnityEngine.UI.Selectable
        {
            var colors = selectable.colors;
            colors.normalColor = colors.normalColor * factor;
            colors.highlightedColor = colors.highlightedColor * factor;
            colors.pressedColor = colors.pressedColor * factor;
            colors.selectedColor = colors.selectedColor * factor;
            selectable.colors = colors;
            return selectable;
        }

        /// <summary>
        /// Creates a lighter version of current normal color.
        /// </summary>
        public static T LightenColors<T>(this T selectable, float factor = 1.3f) where T : UnityEngine.UI.Selectable
        {
            var colors = selectable.colors;
            colors.normalColor = colors.normalColor * factor;
            colors.highlightedColor = colors.highlightedColor * factor;
            colors.pressedColor = colors.pressedColor * factor;
            colors.selectedColor = colors.selectedColor * factor;
            selectable.colors = colors;
            return selectable;
        }
    }
}
