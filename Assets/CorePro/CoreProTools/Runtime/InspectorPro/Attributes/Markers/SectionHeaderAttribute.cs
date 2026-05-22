using System;
using UnityEngine;

namespace InspectorPro
{
    /// <summary>
    /// Draws a section header in the inspector above the field.
    /// <br/> Usage:
    /// <br/> [SectionHeader("SETTINGS")]
    /// <br/> [SectionHeader("Is not Burst!", nameof(firingStyle), FiringStyle.Burst, invert: true)]
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class SectionHeaderAttribute : PropertyAttribute
    {
        public string Text;
        public string ComparedField;
        public object ComparedValue;
        public bool Invert;
        public bool HasCondition;

        // Text only (no condition)
        public SectionHeaderAttribute(string text)
        {
            Text = text;
            HasCondition = false;
        }

        // Text + condition
        public SectionHeaderAttribute(string text, string comparedField, object comparedValue, bool invert = false)
        {
            Text = text;
            ComparedField = comparedField;
            ComparedValue = comparedValue;
            Invert = invert;
            HasCondition = true;
        }
    }
}