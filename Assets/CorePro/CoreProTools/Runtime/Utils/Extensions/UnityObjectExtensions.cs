using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CorePro.Extensions
{
    public static class UnityObjectExtensions
    {
        /// <summary>
        /// Safely destroys a Unity Object. 
        /// Handles Editor selection issues (clears selection before destroying) 
        /// and switches between Destroy and DestroyImmediate automatically.
        /// </summary>
        public static void SafeDestroy(this Object obj)
        {
            if (obj == null) return;

            // 1. Security for the Editor (fixing your error)
#if UNITY_EDITOR
            // If the object we are removing is currently selected in Unity -> deselect it.
            // This prevents the Inspector from trying to display a "dead" object.
            if (!Application.isPlaying)
            {
                if (Selection.activeObject == obj)
                {
                    Selection.activeObject = null;
                }
                
                // If we are removing a component and the GameObject of that component is selected
                else if (obj is Component component && Selection.activeGameObject == component.gameObject)
                {
                    Selection.activeGameObject = null;
                }
            }
#endif

            // 2. Proper disposal
            if (Application.isPlaying)
            {
                // During the game, we always use the standard Destroy (end of frame).
                Object.Destroy(obj);
            }
            else
            {
                // In edit mode (Editor Mode) we must use DestroyImmediate
                Object.DestroyImmediate(obj);
            }
        }
    }
}