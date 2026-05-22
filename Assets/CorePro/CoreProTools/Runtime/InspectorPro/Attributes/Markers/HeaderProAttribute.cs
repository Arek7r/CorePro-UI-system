namespace InspectorPro
{
    using UnityEngine;
    
    public class HeaderProAttribute  : PropertyAttribute
    {
        public readonly string Title;
        
        public HeaderProAttribute(string title)
        {
            Title = title;
        }
    }
}