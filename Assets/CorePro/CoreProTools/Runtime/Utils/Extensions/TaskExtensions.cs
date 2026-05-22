using System;
using System.Threading.Tasks;
using UnityEngine;

namespace CorePro.Utils.Extensions
{
    public static class TaskExtensions
    {
        /// <summary>
        /// Safely executes a task without awaiting it, logging any potential exceptions.
        /// </summary>
        public static async void FireAndForget(this Task task)
        {
            try
            {
                await task;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TaskExtensions] Exception in async operation: {ex}");
            }
        }
    }
}