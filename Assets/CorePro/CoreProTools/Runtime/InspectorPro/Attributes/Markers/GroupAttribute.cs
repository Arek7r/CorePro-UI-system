using System;
using UnityEngine;

namespace InspectorPro
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class GroupAttribute : PropertyAttribute
    {
        public string GroupName;
        public bool Foldout { get; set; } = false; 
        public int Order { get; set; } = 0; 

        public GroupAttribute(string groupName, bool foldout = false, int order = 0)
        {
            GroupName = groupName;
            Foldout = foldout;
            Order = order;
        }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = false)]
    public class EndGroupAttribute : PropertyAttribute
    {
        public string GroupName;
        public int Order { get; set; } = 0;

        public EndGroupAttribute(string groupName, int order = 0) 
        {
            GroupName = groupName;
            Order = order;
        }
        public EndGroupAttribute() 
        {
            GroupName = null;
            Order = 0; 
        }
    }
}