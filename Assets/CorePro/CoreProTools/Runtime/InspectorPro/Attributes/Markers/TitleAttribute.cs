namespace InspectorPro
{
    using UnityEngine;

    public class TitleAttribute : PropertyAttribute
    {
        public readonly string Title;
        public TitleAttribute(string title)
        {
            Title = title;
        }
    }
}