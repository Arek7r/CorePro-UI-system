using InspectorPro;
using UnityEngine;

namespace CorePro.Utils
{
    /// <summary>
    /// Singleton scoped to current scene only.
    /// Automatically resets when scene unloads.
    /// </summary>
    public abstract class SingletonScene<T> : MonoBehaviourCorePro where T : MonoBehaviour
    {
        private static T instanceReference;
        private static bool isShuttingDown;

        public static T Instance
        {
            get
            {
                if (isShuttingDown)
                    return null;

                if (instanceReference != null)
                    return instanceReference;

                // Find existing (including inactive), no allocations beyond search.
#if UNITY_2023_1_OR_NEWER
                instanceReference = Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
#else
                instanceReference = Object.FindObjectOfType<T>();
#endif
                if (instanceReference == null)
                {
                    var go = new GameObject(typeof(T).Name);
                    instanceReference = go.AddComponent<T>();
                }

                return instanceReference;
            }
        }

        protected virtual void Awake()
        {
            if (instanceReference == null)
            {
                instanceReference = this as T;
                return;
            }

            if (instanceReference != this as T)
            {
                Destroy(gameObject);
            }
        }

        protected virtual void OnDestroy()
        {
            if (instanceReference == this as T)
            {
                instanceReference = null;
            }
        }

        private void OnApplicationQuit()
        {
            isShuttingDown = true;
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