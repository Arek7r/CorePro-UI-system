using System;
using System.Collections.Generic;
using InspectorPro;
using InspectorPro.Editor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InspectorPro.Editor

{
    [CustomPropertyDrawer(typeof(SingleModuleAttribute))]
    public class SingleModuleDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            DrawSingleModule(position, property, label, null);
        }

        private void DrawSingleModule(Rect position, SerializedProperty property, GUIContent label, SerializedProperty propertyList)
        {
            float btnWidth = 22f;
            float btnBigWidth = 48;
            float spacing = 2f;
            int btnCount = 2; // How many buttons (Select + Remove)

            Rect bgRect = new Rect(
                position.x,
                position.y - 2f,
                position.width,
                position.height + 4f
            );

            EditorGUI.DrawRect(bgRect, CoreProEditorStyle.Asset.SingleModuleHeaderColor);

            // Width for ObjectField
            float fieldWidth = position.width - (btnWidth + spacing) * btnCount;

            if (property.objectReferenceValue == null)
                fieldWidth = position.width - (btnBigWidth + spacing) * btnCount;

            Rect fieldRect = new Rect(position.x, position.y, fieldWidth, position.height);

            // Button: Select (S)
            Rect selectBtnRect = new Rect(fieldRect.xMax + spacing, position.y, btnWidth, position.height);

            // Button: Remove (R)
            Rect removeBtnRect = new Rect(selectBtnRect.xMax + spacing, position.y, btnWidth, position.height);

            // Button: Find (F)
            Rect findBtnRect = new Rect(fieldRect.xMax, position.y, btnBigWidth, position.height);

            // --- Draw ObjectField ---
            EditorGUI.PropertyField(fieldRect, property, label);

            // --- Buttons ---
            // Select = ping script
            if (property.objectReferenceValue != null)
            {
                GUIContent selectIcon = EditorGUIUtility.IconContent("d_scenepicking_pickable_hover@2x");
                selectIcon.tooltip = "Ping Script";
                if (GUI.Button(selectBtnRect, selectIcon))
                {
                    MonoScript script = null;
                    if (property.objectReferenceValue is MonoBehaviour mono)
                        script = MonoScript.FromMonoBehaviour(mono);
                    else if (property.objectReferenceValue is ScriptableObject so)
                        script = MonoScript.FromScriptableObject(so);

                    if (script != null)
                        EditorGUIUtility.PingObject(script);
                }

                // Remove
                GUIContent removeIcon = EditorGUIUtility.IconContent("d_Toolbar Minus@2x");
                removeIcon.tooltip = "Remove Reference";
                if (GUI.Button(removeBtnRect, removeIcon))
                {
                    property.objectReferenceValue = null;
                    property.serializedObject.ApplyModifiedProperties();
                }
            }
            else
            {
                // GUIContent findIcon = EditorGUIUtility.IconContent("d_Search Icon");
                // findIcon.tooltip = "Find in children";
                if (GUI.Button(findBtnRect, "Find"))
                {
                    var comp = property.serializedObject.targetObject as Component;
                    if (comp != null)
                    {
                        System.Type moduleType = fieldInfo.FieldType;
                        var found = comp.GetComponentInChildren(moduleType, true);
                        if (found != null)
                        {
                            property.objectReferenceValue = found as Object;
                            property.serializedObject.ApplyModifiedProperties();
                        }
                    }
                }

                float createBtnWidth = 48f;
                Rect createBtnRect = new Rect(fieldRect.xMax + spacing + btnBigWidth + spacing, position.y, createBtnWidth, position.height);

                if (GUI.Button(createBtnRect, "Create"))
                {
                    Type moduleType = fieldInfo.FieldType;
                    if (moduleType.IsGenericType && moduleType.GetGenericTypeDefinition() == typeof(List<>))
                        moduleType = moduleType.GetGenericArguments()[0];

                    // Cache the persistent Unity Object reference and property path.
                    // DO NOT cache property.serializedObject directly - Unity may release
                    // the underlying native object by the time the popup callback fires.
                    Object  cachedTarget  = property.serializedObject.targetObject;
                    string  propertyPath  = property.propertyPath;
                    Component targetComp = cachedTarget as Component;

                    // Open popup
                    PopupWindow.Show(createBtnRect, new ModuleCreatePopup(baseType: moduleType,
                        onTypeSelected: (selectedType) =>
                        {
                            if (targetComp == null)
                            {
                                Debug.LogWarning("Target component is null. Cannot create module.");
                                return;
                            }

                            // Find or create child "Modules"
                            Transform parentGO = targetComp.transform.Find("Modules");
                            if (parentGO == null)
                            {
                                parentGO = new GameObject("Modules").transform;
                                parentGO.parent = targetComp.transform;
                            }

                            // Remove previous modules of the required type to avoid duplicates.
                            var oldModules = parentGO.GetComponents(moduleType);
                            for (int i = 0; i < oldModules.Length; i++)
                            {
                                GameObject.DestroyImmediate(oldModules[i], true);
                            }

                            // Add new component
                            var newModule = parentGO.gameObject.AddComponent(selectedType);

                            // Create a fresh SerializedObject from the persistent Unity Object -
                            // the one cached before the popup cannot be reused after GUI frame end.
                            var serializedObj = new SerializedObject(cachedTarget);
                            serializedObj.Update();
                            SerializedProperty freshProp = serializedObj.FindProperty(propertyPath);

                            if (freshProp != null)
                            {
                                freshProp.objectReferenceValue = newModule;
                                serializedObj.ApplyModifiedProperties();
                            }
                            else
                            {
                                Debug.LogError($"Failed to find serialized property at path: {propertyPath}");
                            }
                        }));
                }
            }

            // --- Render inspector below, as orginal ---
            if (property.objectReferenceValue != null)
            {
                if (property.objectReferenceValue is MonoBehaviourCorePro mbCorePro)
                {
                    MonoBehaviourCoreProEditor editorPro = null;

                    try
                    {
                        editorPro = (MonoBehaviourCoreProEditor)UnityEditor.Editor.CreateEditor(mbCorePro);
                    }
                    catch (MissingReferenceException ex)
                    {
                        Debug.LogWarning("MissingReferenceException in CreateEditor: " + ex.Message);
                        property.objectReferenceValue = null;
                        return;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning("Editor error: " + ex.Message);
                        return;
                    }

                    if (editorPro != null)
                    {
                        editorPro.serializedObject.Update();
                        editorPro.DrawAllProperties();
                        editorPro.serializedObject.ApplyModifiedProperties();
                    }
                }
                else
                {
                    UnityEditor.Editor editor = null;
                    try
                    {
                        editor = UnityEditor.Editor.CreateEditor(property.objectReferenceValue);
                    }
                    catch (MissingReferenceException ex)
                    {
                        Debug.LogWarning("MissingReferenceException in CreateEditor: " + ex.Message);
                        property.objectReferenceValue = null;
                        return;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning("Editor error: " + ex.Message);
                        return;
                    }

                    if (editor != null)
                    {
                        EditorGUILayout.BeginVertical();
                        var so = editor.serializedObject;
                        so.Update();
                        var prop = so.GetIterator();
                        bool expanded = true;
                        while (prop.NextVisible(expanded))
                        {
                            if (prop.name == "m_Script")
                                continue;
                            EditorGUILayout.PropertyField(prop, true);
                            expanded = false;
                        }

                        so.ApplyModifiedProperties();
                        EditorGUILayout.EndVertical();
                    }
                }
            }
        }


        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float defaultHeight = EditorGUI.GetPropertyHeight(property, label, true);
            float extraHeight = 0f;
            return defaultHeight + extraHeight;
        }
    }
}