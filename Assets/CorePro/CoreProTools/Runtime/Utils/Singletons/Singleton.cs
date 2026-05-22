using UnityEngine;

namespace CorePro.Utils
{
    /// <summary>
    /// GC-safe, build-safe singleton base. No Editor refs. 
    /// Enforces single instance, optional DontDestroyOnLoad, no forced activation.
    /// </summary>
    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        /// <summary>Return true to persist across scenes.</summary>
        protected virtual bool DoNotDestroyOnLoad
        {
            get => doNotDestroyOnLoad;
            set => doNotDestroyOnLoad = value;
        }

        protected bool doNotDestroyOnLoad;

        private static volatile T instanceReference;
        private static bool isShuttingDown; // per-closed-generic
        private static readonly object synchronizationLock = new object();
        private static readonly System.Type componentType = typeof(T);

        /// <summary>
        /// Access instance. Creates a new GameObject if not found.
        /// </summary>
        public static T Instance
        {
            get
            {
                if (isShuttingDown == true)
                    return null;

                if (instanceReference != null)
                    return instanceReference;

                lock (synchronizationLock)
                {
                    if (instanceReference != null)
                    {
                        return instanceReference;
                    }

                    // Find existing (including inactive), no allocations beyond search.
#if UNITY_2023_1_OR_NEWER
                    instanceReference = Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
#else
                instanceReference = Object.FindObjectOfType<T>(); // fallback for older Unity
#endif
                    if (instanceReference == null)
                    {
                        var newGameObject = new GameObject(componentType.Name);
                        instanceReference = (T)newGameObject.AddComponent(componentType);
                    }

                    var singletonComponent = instanceReference as Singleton<T>;

                    if (singletonComponent != null)
                    {
                        if (singletonComponent.DoNotDestroyOnLoad == true)
                        {
                            DontDestroyOnLoad(singletonComponent.gameObject);
                        }
                    }

                    return instanceReference;
                }
            }
        }

        /// <summary>
        /// Try get instance without creating a new object.
        /// </summary>
        public static bool TryGetInstance(out T outputInstance)
        {
            outputInstance = instanceReference;

            if (outputInstance != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        protected virtual void Awake()
        {
            isShuttingDown = false;

            // Enforce single instance if prefab placed in scene.
            if (instanceReference == null)
            {
                instanceReference = this as T;

                if (DoNotDestroyOnLoad == true)
                {
                    DontDestroyOnLoad(gameObject);
                }

                return;
            }

            if (instanceReference == this as T)
            {
                return;
            }

            // Duplicate - destroy silently to prevent leaks / double-updates.
            // No GC allocs from string concat here.
            Destroy(gameObject);
        }

        private void OnApplicationQuit()
        {
            isShuttingDown = true;
        }

        protected virtual void OnDestroy()
        {
            if (instanceReference == this as T)
            {
                instanceReference = null;
                // Do not touch synchronizationLock.
            }
        }

        /// <summary>
        /// Manual shutdown to clear reference early, e.g., in custom quit logic.
        /// </summary>
        public static void ShutdownSingleton()
        {
            if (instanceReference != null)
            {
                Destroy(instanceReference.gameObject);
                instanceReference = null;
            }
        }

#if UNITY_EDITOR
        // Put this in your singleton
        // #region Singleton
        // #if UNITY_EDITOR
        //         [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        //         private static void Clear() => ResetSingleton(); 
        // #endif
        // #endregion
        
        /// <summary>
        /// Clean up for Enter Play Mode (No Domain Reload).
        /// </summary>
        protected static void ResetSingleton()
        {
            isShuttingDown = false;
            instanceReference = null;
        }
#endif
    }
}