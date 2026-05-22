
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace CorePro.Utils
{
    public static class UIRaycastDebugger
    {
        /// <summary>
        /// Checks and logs all UI elements under the cursor
        /// </summary>
        public static bool IsPointerOverUIWithDebug(bool enableDebug = false)
        {
            if (EventSystem.current == null)
            {
                if (enableDebug)
                    Debug.LogWarning("[UI Raycast] EventSystem is NULL!");
                return false;
            }

            bool isOverUI = EventSystem.current.IsPointerOverGameObject();

            if (enableDebug && isOverUI)
            {
                LogUIElementsUnderPointer();
            }

            return isOverUI;
        }

        /// <summary>
        /// Logs all UI elements under the cursor
        /// </summary>
        public static void LogUIElementsUnderPointer()
        {
            Vector2 pointerPosition = Pointer.current?.position.ReadValue() ?? Vector2.zero;
            
            PointerEventData pointerData = new PointerEventData(EventSystem.current)
            {
                position = pointerPosition
            };

            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);

            if (results.Count == 0)
            {
                Debug.Log("<color=yellow>[UI Raycast] No UI elements found under pointer</color>");
                return;
            }

            Debug.Log($"<color=cyan>[UI Raycast] Found {results.Count} UI element(s) under pointer:</color>");

            for (int i = 0; i < results.Count; i++)
            {
                var result = results[i];
                string hierarchy = GetGameObjectPath(result.gameObject);
                
                Debug.Log($"<color=lime>  [{i}] {result.gameObject.name}</color>\n" +
                          $"      Path: {hierarchy}\n" +
                          $"      Layer: {LayerMask.LayerToName(result.gameObject.layer)}\n" +
                          $"      Canvas: {result.gameObject.GetComponentInParent<Canvas>()?.name ?? "None"}\n" +
                          $"      Distance: {result.distance}", result.gameObject);
            }
        }

        /// <summary>
        /// Returns the full path of the GameObject hierarchy
        /// </summary>
        private static string GetGameObjectPath(GameObject obj)
        {
            string path = obj.name;
            Transform current = obj.transform.parent;

            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }

            return path;
        }

        /// <summary>
        /// Checks whether the pointer is over a specific UI type
        /// </summary>
        public static T GetUIComponentUnderPointer<T>() where T : Component
        {
            if (EventSystem.current == null)
                return null;

            // Use new Input System
            Vector2 pointerPosition = Pointer.current?.position.ReadValue() ?? Vector2.zero;
            
            PointerEventData pointerData = new PointerEventData(EventSystem.current)
            {
                position = pointerPosition
            };

            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);

            foreach (var result in results)
            {
                T component = result.gameObject.GetComponent<T>();
                if (component != null)
                    return component;
            }

            return null;
        }
    }
}