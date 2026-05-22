using System;
using UnityEngine;

namespace InspectorPro
{
    public interface IClassSelectorNameProvider
    {
        string GetClassSelectorName(); // e.g., "Fireball (lvl 3)"
    }

    namespace ClassSelector
    {
        [AttributeUsage(AttributeTargets.Field)]
        public sealed class ClassSelectorAttribute : PropertyAttribute
        {
            public bool IncludeAbstract { get; }
            public string[] AssemblyStartsWith { get; }

            public string NameMember { get; set; } // Field/Property to read (string/int/enum -> ToString)
            public bool UseTypeNameAsLabel { get; set; } = true; // Fallback to Type.Name
            public int MaxNameLength { get; set; } = 64; // Truncate long names

            public ClassSelectorAttribute(bool includeAbstract = false, params string[] assemblyStartsWith)
            {
                IncludeAbstract = includeAbstract;
                AssemblyStartsWith = assemblyStartsWith;
            }
        }
    }
}