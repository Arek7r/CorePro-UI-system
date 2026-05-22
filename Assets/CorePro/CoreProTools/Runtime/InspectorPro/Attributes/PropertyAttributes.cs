using UnityEngine;
using System;

namespace InspectorPro
{
    public class TagAttribute : PropertyAttribute { }
    public class StructViewAttribute : PropertyAttribute { }
    public class ReadOnlyAttribute : PropertyAttribute { }
    public class RuntimeAttribute : PropertyAttribute { }
    public class InlineSOAttribute : PropertyAttribute { }
    public class LineAttribute : PropertyAttribute { }
    
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class ClassViewFlat : PropertyAttribute { }
    
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public class ShowInInspectorAttribute : PropertyAttribute { }
    
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public class SingleModuleAttribute : PropertyAttribute { }
    
    public class DynamicRangeAttribute : PropertyAttribute
    {
        public string MinFieldName;
        public string MaxFieldName;

        public DynamicRangeAttribute(string minFieldName, string maxFieldName)
        {
            MinFieldName = minFieldName;
            MaxFieldName = maxFieldName;
        }
    }
}