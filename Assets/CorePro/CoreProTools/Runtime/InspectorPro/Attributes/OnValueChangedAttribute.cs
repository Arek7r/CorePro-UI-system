using UnityEngine;

namespace InspectorPro
{
    public class OnValueChangedAttribute : PropertyAttribute
    {
        public string MethodName;
        public OnValueChangedAttribute(string methodName) => MethodName = methodName;
    }
}