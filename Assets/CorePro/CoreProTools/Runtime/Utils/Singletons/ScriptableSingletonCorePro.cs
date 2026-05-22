using InspectorPro;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CorePro.Utils
{
    /// <summary>
    /// Thread-safe, build-safe singleton for ScriptableObjects.
    /// Automatically loads the asset from Resources or finds it in the Project.
    /// </summary>
    public class ScriptableSingletonCorePro<T> : ScriptableObjectCorePro where T : ScriptableObject
    {
        private static volatile T instanceReference;
        private static readonly object synchronizationLock = new object();

        /// <summary>
        /// Access the singleton instance. Loads from Resources if not already in memory.
        /// </summary>
        public static T Instance
        {
            get
            {
                if (instanceReference != null)
                {
                    return instanceReference;
                }

                lock (synchronizationLock)
                {
                    if (instanceReference != null)
                    {
                        return instanceReference;
                    }

                    // 1. Try to load from Resources
                    // The asset must be named exactly like the class or placed in a specific path
                    instanceReference = Resources.Load<T>(typeof(T).Name);

                    // 2. Editor Fallback: Search the entire project if not in Resources
#if UNITY_EDITOR
                    if (instanceReference == null)
                    {
                        string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
                        if (guids.Length > 0)
                        {
                            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                            instanceReference = AssetDatabase.LoadAssetAtPath<T>(path);
                        }
                    }
#endif

                    if (instanceReference == null)
                    {
                        Debug.LogError($"[ScriptableSingleton] No instance of {typeof(T).Name} found! " +
                                       $"Ensure the asset exists and its name matches the class name.");
                    }

                    return instanceReference;
                }
            }
        }

        /// <summary>
        /// Check if instance exists without triggering a load/search.
        /// </summary>
        public static bool TryGetInstance(out T outputInstance)
        {
            outputInstance = instanceReference;
            return outputInstance != null;
        }

        protected virtual void OnEnable()
        {
            // Self-assign if Unity loads this asset first
            if (instanceReference == null)
            {
                instanceReference = this as T;
            }
        }

#if UNITY_EDITOR
        // Put this in your SO
        // #if UNITY_EDITOR
        //         [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        //         private static void Clear() => ResetSingleton(); 
        // #endif
        
        /// <summary>
        /// Clean up for Enter Play Mode (No Domain Reload).
        /// </summary>
        protected static void ResetSingleton()
        {
            instanceReference = null;
        }
#endif
    }
}