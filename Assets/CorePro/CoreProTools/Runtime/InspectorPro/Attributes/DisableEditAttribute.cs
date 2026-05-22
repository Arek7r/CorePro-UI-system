using System;
using UnityEngine;

namespace InspectorPro
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class DisableEditAttribute : PropertyAttribute
    {
        public DisableEditAttribute() { }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class DisableEditIfAttribute : PropertyAttribute
    {
        public string fieldName;
        public object value;
        public bool invert;

        public DisableEditIfAttribute(string fieldName, object value, bool invert = false)
        {
            this.fieldName = fieldName;
            this.value = value;
            this.invert = invert;
        }
    }

}