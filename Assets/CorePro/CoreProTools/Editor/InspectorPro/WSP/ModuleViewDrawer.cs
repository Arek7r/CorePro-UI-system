using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using InspectorPro.Editor;
using UnityEditor;
using UnityEngine;

namespace InspectorPro.Editor
{
        public static class ModuleViewDrawer
        {
            public class SOViewState
            {
                public bool foldout;
                public UnityEditor.Editor editor;
                public string prefsKey;
                private Object lastTargetReference; 

                public SOViewState(string prefsKey)
                {
                    this.prefsKey = prefsKey;
                    this.foldout = EditorPrefs.GetBool(prefsKey, false);
                }

                public void Save() => EditorPrefs.SetBool(prefsKey, foldout);

                public void Dispose()
                {
                    Save();

                    // Always destroy the editor regardless of the state target
                    if (editor != null)
                    {
                        try
                        {
                            if (!editor.Equals(null)) // Unity's null check
                            {
                                UnityEngine.Object.DestroyImmediate(editor);
                            }
                        }
                        catch (System.Exception)
                        { }
                        finally
                        {
                            editor = null;
                        }
                    }

                    lastTargetReference = null;
                }

                // Method to check if the editor is still valid
                public bool IsEditorValid()
                {
                    if (editor == null)
                        return false;

                    try
                    {
                        // Check if the target still exists
                        return editor.target != null && !editor.target.Equals(null);
                    }
                    catch (System.Exception)
                    {
                        return false;
                    }
                }

                // Secure editor creation
                public void CreateEditorSafely(Object targetObject)
                {
                    if (targetObject == null || targetObject.Equals(null))
                        return;

                    // If editor exists but target has changed, destroy the old one
                    if (editor != null && (editor.target != targetObject || !IsEditorValid()))
                    {
                        try
                        {
                            UnityEngine.Object.DestroyImmediate(editor);
                        }
                        catch (System.Exception)
                        {
                            // Ignore mistakes
                        }
                        finally
                        {
                            editor = null;
                        }
                    }

                    // Create a new editor only if required
                    if (editor == null)
                    {
                        try
                        {
                            UnityEditor.Editor.CreateCachedEditor(targetObject, null, ref editor);
                            lastTargetReference = targetObject;
                        }
                        catch (System.Exception ex)
                        {
                            Debug.LogWarning($"Failed to create cached editor: {ex.Message}");
                            editor = null;
                        }
                    }
                }
            }

            // Global tidbit on game mode changes
            private static Dictionary<string, SOViewState> _globalViewStates = new Dictionary<string, SOViewState>();

            [InitializeOnLoadMethod]
            static void Initialize()
            {
                EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
                EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            }

            private static void OnPlayModeStateChanged(PlayModeStateChange state)
            {
                if (state == PlayModeStateChange.ExitingPlayMode ||
                    state == PlayModeStateChange.ExitingEditMode)
                {
                    // CRITICAL: Clear all cache editors
                    CleanupAllViewStates();
                }
            }

            public static void CleanupAllViewStates()
            {
                foreach (var kvp in _globalViewStates.ToArray())
                {
                    try
                    {
                        kvp.Value?.Dispose();
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"Error cleaning up view state {kvp.Key}: {ex.Message}");
                    }
                }

                _globalViewStates.Clear();
            }

            public static void DrawModuleViewList(Object target, FieldInfo field, SerializedProperty property, Dictionary<string, SOViewState> viewStates,
                CoreProGUIFoldout foldoutCorePro, ModuleViewAttribute moduleAttr)
            {
                if (property == null || !property.isArray)
                    return;

                // Checking whether the target is valid
                if (target == null || target.Equals(null))
                {
                    EditorGUILayout.HelpBox("Target object is null", MessageType.Warning);
                    return;
                }

                for (int i = 0; i < property.arraySize; i++)
                {
                    var elementProp = property.GetArrayElementAtIndex(i);

                    Object objRef = null;
                    if (elementProp.propertyType == SerializedPropertyType.ObjectReference)
                        objRef = elementProp.objectReferenceValue;
                    else
                        continue;

                    if (objRef == null)
                        continue;

                    int uniqueId = objRef.GetInstanceID();
                    string viewKey = $"{field.Name}_Module_{uniqueId}";
                    string prefsKey = $"Foldout_{target.GetType().Name}_{field.Name}_Module_{uniqueId}_{target.GetInstanceID()}";

                    if (!viewStates.TryGetValue(viewKey, out var viewState))
                    {
                        viewState = new SOViewState(prefsKey);
                        viewStates.Add(viewKey, viewState);

                        // Global tracking
                        _globalViewStates[viewKey] = viewState;
                    }

                    string objName = objRef != null ? objRef.name : $"Element {i}";

                    // Save the status IMMEDIATELY after a change
                    bool previousFoldoutState = viewState.foldout;
                    
                    viewState.foldout = foldoutCorePro.DrawFoldoutModule(ref viewState.foldout, null, objName, viewState.prefsKey);
        
                    // Save immediately if the status has changed
                    if (viewState.foldout != previousFoldoutState)
                    {
                        viewState.Save();
                    }

                    if (viewState.foldout)
                    {
                        // EditorGUILayout.BeginHorizontal(GUIStyle.none);
                        // GUILayout.Space(CoreProEditorStyle.Asset.moduleView_InspectorRenderMarginLeft);
                        // EditorGUILayout.BeginVertical(GUIStyle.none);

                        try
                        {
                            if (objRef == null || objRef.Equals(null))
                            {
                                EditorGUILayout.HelpBox("Object has been deleted", MessageType.Warning);

                                // CLEANUP: Remove invalid view state
                                viewState.Dispose();
                                viewStates.Remove(viewKey);
                                _globalViewStates.Remove(viewKey);
                            }
                            else
                            {
                                // SAFE editor creation
                                viewState.CreateEditorSafely(objRef);

                                // Render only if the editor is OK
                                if (viewState.IsEditorValid())
                                {
                                    try
                                    {
                                        viewState.editor.OnInspectorGUI();
                                    }
                                    catch (System.Exception ex)
                                    {
                                        EditorGUILayout.HelpBox($"Editor error: {ex.Message}", MessageType.Error);

                                        // CLEANUP
                                        viewState.Dispose();
                                    }
                                }
                                else
                                {
                                    EditorGUILayout.HelpBox("Cannot display the editor", MessageType.Info);
                                }
                            }
                        }
                        catch (System.Exception ex)
                        {
                            EditorGUILayout.HelpBox($"Module view error: {ex.Message}", MessageType.Error);
                        }

                        // GUILayout.Space(1);
                        // EditorGUILayout.EndVertical();
                        // GUILayout.Space(CoreProEditorStyle.Asset.moduleView_InspectorRenderMarginRight);
                        // EditorGUILayout.EndHorizontal();
                    }

                    GUILayout.Space(CoreProEditorStyle.Asset.spaceBetweenModulesFoldouts);
                }

                // Cleanup of unused view states
                CleanupUnusedViewStates(viewStates, property);
            }

            // Clearing unused view states
            private static void CleanupUnusedViewStates(Dictionary<string, SOViewState> viewStates, SerializedProperty property)
            {
                var keysToRemove = new List<string>();

                foreach (var kvp in viewStates)
                {
                    bool found = false;
                    for (int i = 0; i < property.arraySize; i++)
                    {
                        var elementProp = property.GetArrayElementAtIndex(i);
                        if (elementProp.propertyType == SerializedPropertyType.ObjectReference)
                        {
                            var objRef = elementProp.objectReferenceValue;
                            if (objRef != null)
                            {
                                string checkKey = kvp.Key;
                                if (checkKey.Contains($"_{objRef.GetInstanceID()}"))
                                {
                                    found = true;
                                    break;
                                }
                            }
                        }
                    }

                    if (!found)
                    {
                        keysToRemove.Add(kvp.Key);
                    }
                }

                // Delete unused
                foreach (var key in keysToRemove)
                {
                    if (viewStates.TryGetValue(key, out var viewState))
                    {
                        viewState.Dispose();
                        viewStates.Remove(key);
                        _globalViewStates.Remove(key);
                    }
                }
            }
        }
}