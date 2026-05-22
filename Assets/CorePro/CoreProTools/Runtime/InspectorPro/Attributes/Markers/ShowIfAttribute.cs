using System;
using System.Collections.Generic;
using UnityEngine;

namespace InspectorPro
{
    /// <summary>
    /// Shows a field in the inspector only if the specified condition is met.
    /// </summary>
    /// <remarks>
    /// Usage:
    /// <br/>[ShowIf(nameof(isEnabled), true)] // Show when isEnabled is true
    /// <br/>[ShowIf(nameof(weaponType), WeaponType.Melee)] // Show when weaponType equals Melee
    /// <br/>[ShowIf(nameof(isEnabled), true, true)] // Show when isEnabled is false (inverted)
    /// </remarks>
    /// <param name="fieldName">Name of the field to compare against</param>
    /// <param name="value">Value to compare with</param>
    /// <param name="invert">If true, inverts the condition</param>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class ShowIfAttribute : PropertyAttribute
    {
        public string fieldName;
        public object value;
        public object[] values;
        public bool invert;

        public ShowIfAttribute(string fieldName, object value, bool invert = false)
        {
            this.fieldName = fieldName;
            this.value = value;
            this.invert = invert;
        }
        
        public ShowIfAttribute(string fieldName, bool value = true, bool invert = false)
        {
            this.fieldName = fieldName;
            this.value = value;
            this.invert = invert;
        }
        
        public ShowIfAttribute(string fieldName, params object[] values)
        {
            this.fieldName = fieldName;
            this.values = values;
            this.invert = false;
        }
    }
    
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class ShowIfAllAttribute : PropertyAttribute
    {
        public string[] fieldNames;
        public object[] values;

        public ShowIfAllAttribute(params object[] args)
        {
            var names = new List<string>();
            var vals = new List<object>();
            for (int i = 0; i + 1 < args.Length; i += 2)
            {
                names.Add((string)args[i]);
                vals.Add(args[i + 1]);
            }
            fieldNames = names.ToArray();
            values = vals.ToArray();
        }
    } 
}