using System;
using UnityEngine;

namespace InspectorPro
{
    /// <summary>
    /// Hides a field in the inspector if the specified condition is met.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class HideIfAttribute : PropertyAttribute
    {
        public string fieldName;
        public object value;
        public bool invert;

        public HideIfAttribute(string fieldName, object value, bool invert = false)
        {
            this.fieldName = fieldName;
            this.value = value;
            this.invert = invert;
        }
    }
}