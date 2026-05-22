using System;
using UnityEngine;

namespace InspectorPro
{
    [AttributeUsage(AttributeTargets.Field)]
    public class ModuleViewAttribute : PropertyAttribute
    {
        public  EditorStyleTag Tag;

        public ModuleViewAttribute(EditorStyleTag tag = EditorStyleTag.None)
        {
            Tag = tag;
        }
    }
}