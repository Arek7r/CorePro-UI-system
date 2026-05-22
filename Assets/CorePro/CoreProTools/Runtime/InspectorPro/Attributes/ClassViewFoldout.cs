using UnityEngine;
    using System;

namespace InspectorPro
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class ClassViewFoldout : PropertyAttribute
    {
        public string Header;
        public ClassViewFoldout(string header = null)
        {
            Header = header;
        }
    }
}