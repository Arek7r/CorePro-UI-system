using System;

namespace InspectorPro
{
    public enum ButtonSize
    {
        Normal,
        Medium,
        Big
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
    public class ButtonAttribute : Attribute
    {
        public string Label { get; }
        public ButtonSize Size { get; }

        public ButtonAttribute()
        {
            Label = null;
            Size = ButtonSize.Normal;
        }

        public ButtonAttribute(string label)
        {
            Label = label;
            Size = ButtonSize.Normal;
        }

        public ButtonAttribute(string label, ButtonSize size)
        {
            Label = label;
            Size = size;
        }

        public ButtonAttribute(ButtonSize size)
        {
            Label = null;
            Size = size;
        }
    }
}