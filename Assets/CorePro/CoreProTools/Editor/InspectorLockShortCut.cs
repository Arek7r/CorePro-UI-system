#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace CorePro.EditorTools
{
    // MenuItem shortcut syntax:
    // %  = Ctrl (Windows) / Cmd (Mac)
    // #  = Shift
    // &  = Alt
    // _  = No special key

    // Examples:
    //[MenuItem("Edit/Command %l")]        // Ctrl+L / Cmd+L
    //[MenuItem("Edit/Command %#p")]       // Ctrl+Shift+P / Cmd+Shift+P
    //[MenuItem("Edit/Command %#l")]       // Ctrl+Shift+L / Cmd+Shift+L
    //[MenuItem("Edit/Command &#x")]       // Ctrl+Alt+X / Cmd+Alt+X
    /// <summary>
    /// Provides keyboard shortcuts for toggling Inspector lock and Transform constrain proportions.
    /// Shortcuts: 
    /// - Ctrl+L / Cmd+L: Toggle Inspector Lock
    /// - Ctrl+Shift+P / Cmd+Shift+P: Toggle Constrain Proportions
    /// </summary>
    public static class InspectorLockShortcut
    {
        private const BindingFlags BINDING_FLAGS = BindingFlags.NonPublic | BindingFlags.Instance;
        
        private static MethodInfo flipLockedMethod;
        private static PropertyInfo constrainProportionsProperty;
        
        static InspectorLockShortcut()
        {
            InitializeReflectionCache();
        }

        private static void InitializeReflectionCache()
        {
#if UNITY_2023_2_OR_NEWER
            var editorLockTrackerType = typeof(EditorGUIUtility).Assembly.GetType("UnityEditor.EditorGUIUtility+EditorLockTracker");
            if (editorLockTrackerType != null)
            {
                flipLockedMethod = editorLockTrackerType.GetMethod("FlipLocked", BINDING_FLAGS);
                
                if (flipLockedMethod == null)
                {
                    Debug.LogWarning("[InspectorLockShortcut] FlipLocked method not found. Inspector lock toggle may not work.");
                }
            }
            else
            {
                Debug.LogWarning("[InspectorLockShortcut] EditorLockTracker type not found.");
            }
#endif

            constrainProportionsProperty = typeof(Transform).GetProperty("constrainProportionsScale", BINDING_FLAGS);
            
            if (constrainProportionsProperty == null)
            {
                Debug.LogWarning("[InspectorLockShortcut] constrainProportionsScale property not found.");
            }
        }

        #region Inspector Lock Toggle

        [MenuItem("Edit/Toggle Inspector Lock %q")]
        private static void ToggleInspectorLock()
        {
            ToggleInspectorLockState();
            ForceInspectorRefresh();
        }

        [MenuItem("Edit/Toggle Inspector Lock %q", true)]
        private static bool ValidateToggleInspectorLock()
        {
            return ActiveEditorTracker.sharedTracker.activeEditors.Length > 0;
        }

        private static void ToggleInspectorLockState()
        {
#if UNITY_2023_2_OR_NEWER
            ToggleModernInspectorLock();
#else
            ToggleLegacyInspectorLock();
#endif
        }

#if UNITY_2023_2_OR_NEWER
        private static void ToggleModernInspectorLock()
        {
            if (flipLockedMethod == null)
            {
                Debug.LogError("[InspectorLockShortcut] Cannot toggle inspector lock - reflection method not found.");
                return;
            }

            var inspectorWindowType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.InspectorWindow");
            if (inspectorWindowType == null)
            {
                Debug.LogError("[InspectorLockShortcut] InspectorWindow type not found.");
                return;
            }

            var inspectorWindows = Resources.FindObjectsOfTypeAll(inspectorWindowType);
            if (inspectorWindows.Length == 0)
            {
                Debug.LogWarning("[InspectorLockShortcut] No inspector windows found.");
                return;
            }

            foreach (var inspectorWindow in inspectorWindows)
            {
                var lockTrackerField = inspectorWindowType.GetField("m_LockTracker", BINDING_FLAGS);
                if (lockTrackerField == null)
                {
                    continue;
                }

                var lockTracker = lockTrackerField.GetValue(inspectorWindow);
                if (lockTracker != null)
                {
                    flipLockedMethod.Invoke(lockTracker, null);
                }
            }
        }
#else
        private static void ToggleLegacyInspectorLock()
        {
            var tracker = ActiveEditorTracker.sharedTracker;
            tracker.isLocked = tracker.isLocked == false;
        }
#endif

        #endregion

        #region Constrain Proportions Toggle

        [MenuItem("Edit/Toggle Constrain Proportions %#p")]
        private static void ToggleConstrainProportions()
        {
            ToggleConstrainProportionsForTransforms();
            ForceInspectorRefresh();
        }

        [MenuItem("Edit/Toggle Constrain Proportions %#p", true)]
        private static bool ValidateToggleConstrainProportions()
        {
            var activeEditors = ActiveEditorTracker.sharedTracker.activeEditors;
            
            // Enable only if at least one Transform is selected
            foreach (var editor in activeEditors)
            {
                if (editor.target is Transform)
                {
                    return true;
                }
            }
            
            return false;
        }

        private static void ToggleConstrainProportionsForTransforms()
        {
            if (constrainProportionsProperty == null)
            {
                Debug.LogWarning("[InspectorLockShortcut] Cannot toggle constrain proportions - property not found.");
                return;
            }

            var activeEditors = ActiveEditorTracker.sharedTracker.activeEditors;
            int toggledCount = 0;
            
            foreach (var activeEditor in activeEditors)
            {
                if (activeEditor.target is Transform target)
                {
                    if (ToggleConstrainProportionsForTransform(target) == true)
                    {
                        toggledCount++;
                    }
                }
            }

            if (toggledCount > 0)
            {
                Debug.Log($"[InspectorLockShortcut] Toggled constrain proportions for {toggledCount} Transform(s).");
            }
        }

        private static bool ToggleConstrainProportionsForTransform(Transform target)
        {
            try
            {
                var currentValue = (bool)constrainProportionsProperty.GetValue(target, null);
                constrainProportionsProperty.SetValue(target, currentValue == false, null);
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[InspectorLockShortcut] Failed to toggle constrain proportions for {target.name}: {e.Message}");
                return false;
            }
        }

        #endregion

        #region Combined Toggle (Original Behavior)

        [MenuItem("Edit/Toggle Inspector Lock + Proportions %#l")]
        private static void ToggleBoth()
        {
            ToggleInspectorLockState();
            ToggleConstrainProportionsForTransforms();
            ForceInspectorRefresh();
        }

        [MenuItem("Edit/Toggle Inspector Lock + Proportions %#l", true)]
        private static bool ValidateToggleBoth()
        {
            return ActiveEditorTracker.sharedTracker.activeEditors.Length > 0;
        }

        #endregion

        #region Helpers

        private static void ForceInspectorRefresh()
        {
            ActiveEditorTracker.sharedTracker.ForceRebuild();
        }

        #endregion
    }
}
#endif
