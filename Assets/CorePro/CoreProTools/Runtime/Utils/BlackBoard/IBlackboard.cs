using System;
using UnityEngine;

namespace CorePro
{
    /// <summary>
    /// Contract for a centralized data store that can hold dynamic, type-based context payloads.
    /// Ensures that modules can inject their specific data without hardcoding fields.
    /// </summary>
    public interface IBlackboard
    {
        /// <summary>
        /// Injects reference data of a specific type into the context.
        /// </summary>
        void SetContextData<T>(T data) where T : class;

        /// <summary>
        /// Retrieves reference data of a specific type. Returns null if missing.
        /// </summary>
        T GetContextData<T>() where T : class;

        /// <summary>
        /// Clears the specific data type from the context to free memory.
        /// </summary>
        void ClearContextData<T>() where T : class;
        
        // Fired when new context is injected or removed. Passes the Type of the context.
        event Action<Type> OnContextChanged;
    }
}
