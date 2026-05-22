using System;
using UnityEngine;

namespace InspectorPro
{
    /// <summary>
    /// <br/> Usage:
    /// <br/> [DisableIf(nameof(mode), SpreadMode.Simple, invert:true)]
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class DisableIfAttribute : PropertyAttribute
    {
        public string FieldName { get; }
        public object Value { get; }
        public bool Invert { get; }

        public DisableIfAttribute(string fieldName, object value, bool invert = false)
        {
            FieldName = fieldName;
            Value = value;
            Invert = invert;
        }
    }
}