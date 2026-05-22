using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace InspectorPro.Editor
{
    public partial class MonoBehaviourCoreProEditor
    {
        private Dictionary<string, ModuleViewDrawer.SOViewState> _moduleViewStates;
        private Dictionary<string, ModuleViewAttribute> _moduleViewAttrCache;

        partial void ModuleView_OnInitializeCaches()
        {
            _moduleViewStates = new Dictionary<string, ModuleViewDrawer.SOViewState>(16);
            _moduleViewAttrCache = new Dictionary<string, ModuleViewAttribute>(32);
        }

        partial void ModuleView_OnCleanup()
        {
            if (_moduleViewStates != null)
            {
                foreach (var kv in _moduleViewStates)
                {
                    ModuleViewDrawer.SOViewState vs = kv.Value;
                    if (vs == null) continue;
                    try
                    {
                        vs.Dispose();
                    }
                    catch
                    {}
                }

                _moduleViewStates.Clear();
                _moduleViewStates = null;
            }

            if (_moduleViewAttrCache != null)
            {
                _moduleViewAttrCache.Clear();
                _moduleViewAttrCache = null;
            }
        }

        partial void ModuleView_Draw(SerializedProperty property, FieldInfo fieldInfo, ref bool handled)
        {
            if (property == null)
            {
                handled = false;
                return;
            }

            // Get attribute/cached attribute
            var moduleAttr = _moduleViewAttrCache.TryGetValue(property.name, out var cachedAttr)
                ? cachedAttr
                : fieldInfo?.GetCustomAttribute<ModuleViewAttribute>();

            if (moduleAttr != null && _moduleViewAttrCache.ContainsKey(property.name) == false)
            {
                _moduleViewAttrCache.Add(property.name, moduleAttr);
            }

            if (moduleAttr == null)
            {
                handled = false;
                return;
            }


            handled = true;

            GUI.enabled = true;
            GUILayout.Space(6);
            EditorGUILayout.PropertyField(property, includeChildren: true);

            //EditorGUILayout.BeginHorizontal();
           // GUILayout.Space(CoreProEditorStyle.Asset.moduleView_ContentMarginLeft);
           // EditorGUILayout.BeginVertical();

            GUILayout.Space(6);

            // If this is an array/list of modules (ModuleView) - typical in your system
            if (property.isArray && property.propertyType != SerializedPropertyType.String)
            {
                ModuleViewDrawer.DrawModuleViewList(
                    (UnityEngine.Object)target,
                    fieldInfo,
                    property,
                    _moduleViewStates,
                    guiBasicFoldoutCorePro,
                    moduleAttr
                );
            }
            else
            {
                // NON-LIST: regular ModuleView field
                EditorGUILayout.PropertyField(property, true);
            }

            GUILayout.Space(6);

           // EditorGUILayout.EndVertical();
          //  EditorGUILayout.EndHorizontal();
        }
    }
}