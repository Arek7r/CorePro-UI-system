#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace InspectorPro.Editor
{
    //[CustomEditor(typeof(MonoBehaviourCorePro), true)]
    public class ButtonDrawer : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            if (target == null) 
                return;
            
            base.OnInspectorGUI();

            MonoBehaviour monoBehaviour = (MonoBehaviour)target;
            MethodInfo[] methods = monoBehaviour.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

            foreach (var method in methods)
            {
                // Checking if the method has the [Button] attribute.
                var attributes = method.GetCustomAttributes(typeof(ButtonAttribute), true);
                if (attributes.Length > 0)
                {
                    var buttonAttr = (ButtonAttribute)attributes[0];
                    string label = string.IsNullOrEmpty(buttonAttr.Label) ? method.Name : buttonAttr.Label;

                    if (GUILayout.Button(label))
                    {
                        method.Invoke(monoBehaviour, null);
                    }
                }
            }
        }
    }
}
#endif