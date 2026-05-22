using System;
using UnityEngine;

namespace InspectorPro
{
    /// <summary>
    /// Displays a customizable header frame (section label) above a field in the Unity Inspector.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <see cref="HeaderFrameAttribute"/> draws a stylized frame with custom text above a serialized field,
    /// making it easy to visually separate and organize sections in your inspector, both in MonoBehaviours
    /// and inside serializable classes/structs.
    /// </para>
    /// <para>
    /// Supports optional parameters for alignment, font size, and font style.
    /// </para>
    /// <para>Usage examples:</para>
    /// <code>
    /// // Basic usage with default style (centered, bold, font size 12)
    /// [HeaderFrame("Blue Settings")]
    /// public int capacity;
    ///
    /// // Change alignment and font size
    /// [HeaderFrame("Ammo Info", TextAnchor.MiddleLeft, 14)]
    /// public int maxMagsInReserve;
    ///
    /// // Full customization (left, size 16, italic)
    /// [HeaderFrame("Advanced Options", TextAnchor.MiddleLeft, 16, FontStyle.Italic)]
    /// public float advancedMultiplier;
    ///
    /// // Use inside serializable classes/structs
    /// [System.Serializable]
    /// public class WeaponData
    /// {
    ///     [HeaderFrame("BaseDamage Section")]
    ///     public int damage;
    /// }
    /// </code>
    /// </remarks>

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class HeaderFrameAttribute : PropertyAttribute
    {
        public string Text;
        public TextAnchor Alignment;
        public int FontSize;
        public FontStyle FontStyle;
        public float BottomSpacing;
        public float TopSpacing;
        public float LeftSpacing;

        /// <summary>
        /// Creates a frame header in the inspector.
        /// </summary>
        /// <param name="text">Displayed text.</param>
        /// <param name="alignment">Text alignment.</param>
        /// <param name="fontSize">Font size.</param>
        /// <param name="fontStyle">Font style.</param>
        /// <param name="bottomSpacing">Additional vertical space after the header (in pixels).</param>
        public HeaderFrameAttribute(
            string text,
            TextAnchor alignment = TextAnchor.MiddleCenter,
            int fontSize = 12,
            FontStyle fontStyle = FontStyle.Bold,
            float bottomSpacing = 4f,
            float topSpacing = 4f
            )
        {
            Text = text;
            Alignment = alignment;
            FontSize = fontSize;
            FontStyle = fontStyle;
            BottomSpacing = bottomSpacing;
            TopSpacing  = topSpacing ;
        }
    }
}