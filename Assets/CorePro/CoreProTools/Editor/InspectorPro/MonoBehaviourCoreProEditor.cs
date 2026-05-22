using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace InspectorPro.Editor
{
    [CustomEditor(typeof(MonoBehaviourCorePro), true)]
    public partial class MonoBehaviourCoreProEditor : UnityEditor.Editor
    {
        #region  Variables
        private bool _initialized;
        private CoreProGUIFoldout guiBasicFoldoutCorePro;
        private Type _lastInspectedType;

        // ---- Caches ----
        private List<SerializedProperty> _defaultProps; // Fields not in any foldout
        private Dictionary<string, FieldInfo> _fieldInfoCache;
        private Dictionary<string, HideIfAttribute> _hideIfAttrCache;
        private Dictionary<string, List<ShowIfAttribute>> _showIfAttrCache;
        private Dictionary<string, ShowIfAllAttribute> _showIfAllAttrCache;
        private Dictionary<string, DisableIfAttribute> _disableIfAttrCache;
        private Dictionary<string, SectionHeaderAttribute> _sectionHeaderAttrCache;
        private Dictionary<string, TitleAttribute> _titleAttrCache;
        private Dictionary<string, LineAttribute> _lineAttrCache;
        private Dictionary<string, HeaderProAttribute> _headerProAttrCache;
        private Dictionary<string, ClassViewFlat> _flatDisplayAttrCache;
        private Dictionary<string, FieldInfo> _runtimeAttrCache;
        
        private Dictionary<Type, FieldInfo[]> _allFieldsCache = new Dictionary<Type, FieldInfo[]>();
        private FieldInfo[] _showInInspectorFieldsCache;
        private MethodInfo[] _buttonMethodsCache;

        // ---- FoldoutH1 & Grouping ----
        private Dictionary<string, FoldoutCache> _foldouts;
        private Dictionary<string, bool> _groupFoldoutStates = new Dictionary<string, bool>();
        private Dictionary<string, GroupAttribute> _groupAttrCache;
        private Dictionary<string, EndGroupAttribute> _endGroupAttrCache;
      
        // ---- Group stacks  ----
        private Stack<string> groupStack = new Stack<string>();
        const string NoGroupKey = "__NO_GROUP__";
        private GUIStyle _sectionHeaderStyle;
        private GUIStyle _titleStyle;
        
        private struct GroupInfo
        {
            public string groupName;
            public bool isFoldout;
            public bool isOpen;
        }
        
        // ---- Drawn tracker  ----
        private HashSet<string> _drawnPropertyPaths = new HashSet<string>();
        private bool _isInitializing = false;

        // Weapon System Pro Hooks 
        partial void ModuleView_OnInitializeCaches();
        partial void ModuleView_OnCleanup();
        partial void ModuleView_Draw(SerializedProperty property, FieldInfo fieldInfo, ref bool handled);
        #endregion

        private class FoldoutCache
        {
            public FoldoutAttribute attr;
            public List<SerializedProperty> props = new();
            public bool expanded;
            public string prefsKey;
        }
        
        // Cached sorted foldouts list to prevent GC allocations in OnInspectorGUI
        private List<FoldoutCache> _sortedFoldoutsCache;

        private void OnEnable()
        {
            if (target == null || target.Equals(null))
            {
                _initialized = false;
                return;
            }

            try
            {
                var testAccess = target.name;
            }
            catch (MissingReferenceException)
            {
                _initialized = false;
                return;
            }
            catch (System.Exception)
            {
                _initialized = false;
                return;
            }

            // Check that we are already initialising
            if (_isInitializing)
                return;

            _isInitializing = true;

            try
            {
                if (target == null || target.Equals(null) || targets == null || targets.Length == 0)
                {
                    _initialized = false;
                    return;
                }

                // Check serializedObject
                try
                {
                    if (serializedObject == null || serializedObject.Equals(null))
                    {
                        _initialized = false;
                        return;
                    }
                }
                catch (System.Exception)
                {
                    _initialized = false;
                    return;
                }

                // Check that the object has not been destroyed
                try
                {
                    var testType = target.GetType();
                    var testName = target.name; // Test access to Unity object
                }
                catch (System.Exception)
                {
                    _initialized = false;
                    return;
                }

                InitializeCaches();
                CacheFieldsAndAttributes();
            }
            finally
            {
                _isInitializing = false;
            }
        }


        private void InitializeCaches()
        {
            if (target == null || target.Equals(null))
            {
                _initialized = false;
                return;
            }
            
            ModuleView_OnInitializeCaches();
            
            _drawnPropertyPaths = new HashSet<string>();
            groupStack = new Stack<string>();
             
            // --- Initialize caches ---
            _initialized = false;
            guiBasicFoldoutCorePro = new CoreProGUIFoldout();

            _fieldInfoCache = new Dictionary<string, FieldInfo>();
            _hideIfAttrCache = new Dictionary<string, HideIfAttribute>();
            _showIfAttrCache = new Dictionary<string, List<ShowIfAttribute>>();
            _showIfAllAttrCache = new Dictionary<string, ShowIfAllAttribute>();
            _disableIfAttrCache = new Dictionary<string, DisableIfAttribute>();
            _sectionHeaderAttrCache = new Dictionary<string, SectionHeaderAttribute>();
            _titleAttrCache = new Dictionary<string, TitleAttribute>();
            _lineAttrCache = new Dictionary<string, LineAttribute>();
            _headerProAttrCache = new Dictionary<string, HeaderProAttribute>();
            _groupAttrCache = new  Dictionary<string, GroupAttribute>();
            _endGroupAttrCache = new Dictionary<string, EndGroupAttribute>();
            _flatDisplayAttrCache = new Dictionary<string, ClassViewFlat>();
            _groupFoldoutStates = new Dictionary<string, bool>();
            _runtimeAttrCache = new Dictionary<string, FieldInfo>();
        }

        private void CacheFieldsAndAttributes()
        {
            if (target == null || target.Equals(null))
            {
                _initialized = false;
                return;
            }

            var fields = GetAllFields(target.GetType());

            foreach (var field in fields)
            {
                _fieldInfoCache[field.Name] = field;

                foreach (var attr in field.GetCustomAttributes(false))
                {
                    if (attr == null) continue; 
        
                    switch (attr) 
                    {
                        case HideIfAttribute hideIf:
                            _hideIfAttrCache[field.Name] = hideIf;
                            break;
                        case ShowIfAttribute showIf:
                            if (!_showIfAttrCache.ContainsKey(field.Name))
                                _showIfAttrCache[field.Name] = new List<ShowIfAttribute>();
                            _showIfAttrCache[field.Name].Add(showIf);
                            break;
                        case ShowIfAllAttribute showIfAll:
                            _showIfAllAttrCache[field.Name] = showIfAll;
                            break;
                        case DisableIfAttribute disableIf:
                            _disableIfAttrCache[field.Name] = disableIf;
                            break;
                        case SectionHeaderAttribute sectionHeader:
                            _sectionHeaderAttrCache[field.Name] = sectionHeader;
                            break;
                        case TitleAttribute title:
                            _titleAttrCache[field.Name] = title;
                            break;
                        case LineAttribute line:
                            _lineAttrCache[field.Name] = line;
                            break;
                        case HeaderProAttribute headerPro:
                            _headerProAttrCache[field.Name] = headerPro;
                            break;
                        case GroupAttribute group:
                            _groupAttrCache[field.Name] = group;
                            break;
                        case EndGroupAttribute endGroup:
                            _endGroupAttrCache[field.Name] = endGroup;
                            break;
                        case ClassViewFlat flatDisplay:
                            _flatDisplayAttrCache[field.Name] = flatDisplay;
                            break;
                        case RuntimeAttribute runtime:
                            _runtimeAttrCache[field.Name] = field;
                            break;
                    }
                }
            }
        }
        
        /// <summary>
        /// Checks if a given type has InspectorPro attributes.
        /// Uses caching for better performance.
        /// </summary>
        public static bool HasInspectorProAttributes(Type type)
        {
            if (typesWithInspectorProAttributes != null) 
                return typesWithInspectorProAttributes.Contains(type);

            // Initialize cache
            typesWithInspectorProAttributes = new HashSet<Type>();

            // Collect all types that use InspectorPro attributes
            typesWithInspectorProAttributes.UnionWith(TypeCache.GetFieldsWithAttribute<FoldoutAttribute>().Select(r => r.DeclaringType));
            typesWithInspectorProAttributes.UnionWith(TypeCache.GetFieldsWithAttribute<GroupAttribute>().Select(r => r.DeclaringType));
            typesWithInspectorProAttributes.UnionWith(TypeCache.GetFieldsWithAttribute<EndGroupAttribute>().Select(r => r.DeclaringType));
            
            typesWithInspectorProAttributes.UnionWith(TypeCache.GetFieldsWithAttribute<HideIfAttribute>().Select(r => r.DeclaringType));
            typesWithInspectorProAttributes.UnionWith(TypeCache.GetFieldsWithAttribute<ShowIfAttribute>().Select(r => r.DeclaringType));
            typesWithInspectorProAttributes.UnionWith(TypeCache.GetFieldsWithAttribute<DisableIfAttribute>().Select(r => r.DeclaringType));
            typesWithInspectorProAttributes.UnionWith(TypeCache.GetFieldsWithAttribute<ReadOnlyAttribute>().Select(r => r.DeclaringType));
            typesWithInspectorProAttributes.UnionWith(TypeCache.GetFieldsWithAttribute<ShowInInspectorAttribute>().Select(r => r.DeclaringType));
            
            typesWithInspectorProAttributes.UnionWith(TypeCache.GetFieldsWithAttribute<SectionHeaderAttribute>().Select(r => r.DeclaringType));
            typesWithInspectorProAttributes.UnionWith(TypeCache.GetFieldsWithAttribute<TitleAttribute>().Select(r => r.DeclaringType));
            typesWithInspectorProAttributes.UnionWith(TypeCache.GetFieldsWithAttribute<LineAttribute>().Select(r => r.DeclaringType));
            typesWithInspectorProAttributes.UnionWith(TypeCache.GetFieldsWithAttribute<HeaderProAttribute>().Select(r => r.DeclaringType));
            typesWithInspectorProAttributes.UnionWith(TypeCache.GetFieldsWithAttribute<ClassViewFlat>().Select(r => r.DeclaringType));
            typesWithInspectorProAttributes.UnionWith(TypeCache.GetFieldsWithAttribute<InlineSOAttribute>().Select(r => r.DeclaringType));
            
            typesWithInspectorProAttributes.UnionWith(TypeCache.GetMethodsWithAttribute<ButtonAttribute>().Select(r => r.DeclaringType));
            typesWithInspectorProAttributes.UnionWith(TypeCache.GetFieldsWithAttribute<ButtonAttribute>().Select(r => r.DeclaringType));

            // Add all derived types (important for inheritance)
            var baseTypes = new List<Type>(typesWithInspectorProAttributes); 
            foreach (var baseType in baseTypes)
            {
                typesWithInspectorProAttributes.UnionWith(TypeCache.GetTypesDerivedFrom(baseType));
            }

            return typesWithInspectorProAttributes.Contains(type);
        }

        private static HashSet<Type> typesWithInspectorProAttributes = null;
        
        // Cached button styles to prevent GC allocations in DrawButtons
        private static GUIStyle _buttonStyleSmall;
        private static GUIStyle _buttonStyleMedium;
        private static GUIStyle _buttonStyleBig;
        
        /// <summary>
        /// Resets static caches when entering play mode.
        /// </summary>
        [UnityEditor.InitializeOnEnterPlayMode]
        private static void ResetStaticCachesOnEnterPlayMode()
        {
            typesWithInspectorProAttributes = null;
            _buttonStyleSmall = null;
            _buttonStyleMedium = null;
            _buttonStyleBig = null;
        }

        private void OnDisable()
        {
            try
            {
                CleanupCaches();
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Warning during OnDisable cleanup: {ex.Message}");
                CleanupCaches();
            }
        }

        private void CleanupCaches()
        {
            ModuleView_OnCleanup();
            
            _fieldInfoCache?.Clear();
            _hideIfAttrCache?.Clear();
            _showIfAttrCache?.Clear();
            _showIfAllAttrCache?.Clear(); 
            _disableIfAttrCache?.Clear();
            _sectionHeaderAttrCache?.Clear();
            _titleAttrCache?.Clear();
            _lineAttrCache?.Clear();
            _headerProAttrCache?.Clear();
            _groupAttrCache?.Clear();
            _endGroupAttrCache?.Clear();
            _flatDisplayAttrCache?.Clear();
            _drawnPropertyPaths?.Clear();
            _groupFoldoutStates?.Clear();
            _allFieldsCache?.Clear();
            _runtimeAttrCache?.Clear();
            _showInInspectorFieldsCache = null;
            _buttonMethodsCache = null;
            _sortedFoldoutsCache = null;
            
            if (_foldouts != null)
            {
                _foldouts.Clear();
                _foldouts = null;
            }

            _initialized = false;
        }

        private bool ValidateTargetForInspector(out Type targetType)
        {
            targetType = target?.GetType();
    
            if (targetType == null || target == null || targets == null || serializedObject == null)
            {
                EditorGUILayout.HelpBox("Target object is null", MessageType.Warning);
                return false;
            }
    
            return true;
        }
        
        public override void OnInspectorGUI()
        {
            if (target == null || target.Equals(null))
            {
                _initialized = false;
                return;
            }
            
            if (!ValidateTargetForInspector(out Type targetType))
                return;

            if (HasInspectorProAttributes(targetType) == false)
            {
                DrawDefaultInspector();
                return;
            }
            
            try
            {
                if (targetType != _lastInspectedType)
                {
                    _lastInspectedType = targetType;
                    _initialized = false; 

                    if (!_isInitializing)
                    {
                        CacheFieldsAndAttributes();
                    }
                }
                
                DrawBanner(ref targetType);
                serializedObject.Update();

                if (!_initialized)
                {
                    AnalyzeLayout();
                    _initialized = true;
                }

                _drawnPropertyPaths.Clear();

                // ---- Render foldouts (using cached sorted list to prevent GC allocations) ----
                if (_sortedFoldoutsCache != null)
                {
                    for (int i = 0; i < _sortedFoldoutsCache.Count; i++)
                    {
                        DrawStyledFoldout(_sortedFoldoutsCache[i], targetType);
                    }
                }

                // ---- Render properties not in any foldout ----
                if (_defaultProps != null)
                {
                    DrawPropertiesList(_defaultProps, targetType);
                }

                serializedObject.ApplyModifiedProperties();
        
                // ---- Draw ShowInInspector fields (non-serialized) ----
                DrawShowInInspectorFields();
                DrawButtons(target);
            }
            catch (UnityEngine.ExitGUIException)
            {
                // ignore this exception - it is required by Unity for the GUI to function correctly.
                throw;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Inspector error: {ex.Message}"); 
            }
        }
        
        private void DrawBanner(ref Type targetType)   
        {   
            if (target == null || target.Equals(null))
            {
                _initialized = false;
                return;
            }
            
            if (targetType.Name == "GridManager")
            {
                CoreProEditorStyle.DrawBanner("Grid Manager");
            }
            else if (targetType.Name == "BuildingManager")
            {
                CoreProEditorStyle.DrawBanner("Building Manager");
            }
            else if (targetType.Name == "ContextMenuManager")
            {
                CoreProEditorStyle.DrawBanner("Context Menu Manager");
            }
        } 
        
        /// <summary>
        /// Analysis phase: separate fields into foldouts and defaults (with propertyPath uniqueness).
        /// </summary>
        private void AnalyzeLayout()
        {
            if (target == null || target.Equals(null))
            {
                _initialized = false;
                return;
            }
            
            if (!IsTargetValid())
                return;

            _foldouts = new Dictionary<string, FoldoutCache>();
            _defaultProps = new List<SerializedProperty>();
            var assignedPaths = new HashSet<string>();

            var cacheType = target.GetType();
            var fields = GetAllFields(cacheType);

            FoldoutAttribute activeFoldoutAttr = null;
            FoldoutCache activeFoldoutCache = null;

            // FIRST PHASE: Operation of FoldoutAttribute (unchanged)
            foreach (var field in fields)
            {
                var prop = serializedObject.FindProperty(field.Name);
                if (prop == null) continue;

                var foldoutAttr = field.GetCustomAttribute<FoldoutAttribute>();
                if (foldoutAttr != null)
                {
                    // New foldout - set as active
                    activeFoldoutAttr = foldoutAttr;
                    if (!_foldouts.ContainsKey(foldoutAttr.name))
                    {
                        activeFoldoutCache = new FoldoutCache
                        {
                            attr = foldoutAttr,
                            prefsKey = $"{foldoutAttr.name}_{cacheType.Name}_{target.GetInstanceID()}",
                        };
                        _foldouts.Add(foldoutAttr.name, activeFoldoutCache);
                    }
                    else
                    {
                        activeFoldoutCache = _foldouts[foldoutAttr.name];
                    }

                    activeFoldoutCache.props.Add(prop);
                    assignedPaths.Add(prop.propertyPath);
                }
                else if (activeFoldoutAttr != null && activeFoldoutAttr.foldEverything)
                {
                    // Continue adding to current folderout
                    activeFoldoutCache.props.Add(prop);
                    assignedPaths.Add(prop.propertyPath);
                }
            }

            // SECOND PHASE: GroupAttribute handling with reorganization
            AnalyzeLayoutGroups(fields, assignedPaths);

            // THIRD PHASE: Fallback for the remaining fields - ONLY for fields without Group and without FoldoutH1
            var propIter = serializedObject.GetIterator();
            bool enter = true;
            while (propIter.NextVisible(enter))
            {
                enter = false;
                if (propIter.name == "m_Script") continue;
                var foundProp = serializedObject.FindProperty(propIter.propertyPath);
                if (foundProp != null && !assignedPaths.Contains(foundProp.propertyPath))
                {
                    _defaultProps.Add(foundProp);
                    assignedPaths.Add(foundProp.propertyPath);
                }
            }
            
            // FOURTH PHASE: Cache sorted foldouts to prevent GC allocations in OnInspectorGUI
            if (_foldouts != null && _foldouts.Count > 0)
            {
                _sortedFoldoutsCache = new List<FoldoutCache>(_foldouts.Values);
                _sortedFoldoutsCache.Sort((a, b) => a.attr.order.CompareTo(b.attr.order));
            }
            else
            {
                _sortedFoldoutsCache = null;
            }
        }


        private void AnalyzeLayoutGroups(FieldInfo[] fields, HashSet<string> assignedPaths)
        {
            if (target == null || target.Equals(null))
            {
                _initialized = false;
                return;
            }
            
            string currentGroup = NoGroupKey;
            groupStack.Clear();

            // First phase: assign all fields to groups (sequentially)
            var fieldsWithGroups = new List<(string group, FieldInfo field, int originalOrder)>();
            int order = 0;

            foreach (var field in fields)
            {
                var prop = serializedObject.FindProperty(field.Name);
                if (prop == null) continue;

                // We check whether the field already has a FoldoutAttribute - if so, we omit it
                var foldoutAttr = field.GetCustomAttribute<FoldoutAttribute>();
                if (foldoutAttr != null)
                {
                    order++;
                    continue;
                }

                // Check if the field is already in assignedPaths (added by foldout)
                if (assignedPaths.Contains(prop.propertyPath))
                {
                    order++;
                    continue;
                }

                var groupAttr = field.GetCustomAttribute<InspectorPro.GroupAttribute>();
                var endGroupAttr = field.GetCustomAttribute<InspectorPro.EndGroupAttribute>();

                // Handling a new group
                if (groupAttr != null)
                {
                    groupStack.Push(currentGroup);
                    currentGroup = groupAttr.GroupName;
                }

                // Add field to current group
                fieldsWithGroups.Add((currentGroup, field, order));
                order++;

                // EndGroup handling
                if (endGroupAttr != null)
                {
                    if (string.IsNullOrEmpty(endGroupAttr.GroupName))
                    {
                        if (groupStack.Count > 0)
                        {
                            currentGroup = groupStack.Pop();
                        }
                        else
                        {
                            currentGroup = NoGroupKey;
                        }
                    }
                    else
                    {
                        if (currentGroup == endGroupAttr.GroupName)
                        {
                            if (groupStack.Count > 0)
                            {
                                currentGroup = groupStack.Pop();
                            }
                            else
                            {
                                currentGroup = NoGroupKey;
                            }
                        }
                    }
                }
            }

            var groupedFields = new Dictionary<string, List<(FieldInfo field, int originalOrder)>>();
            var groupFirstOccurrence = new Dictionary<string, int>();

            foreach (var (group, field, originalOrder) in fieldsWithGroups)
            {
                if (!groupedFields.ContainsKey(group))
                {
                    groupedFields[group] = new List<(FieldInfo, int)>();
                    groupFirstOccurrence[group] = originalOrder;
                }

                groupedFields[group].Add((field, originalOrder));
            }

            var finalOrdered = new List<(string group, FieldInfo field)>();
            var sortedGroups = groupedFields.Keys.OrderBy(g => groupFirstOccurrence[g]).ToList();

            foreach (var groupName in sortedGroups)
            {
                var fieldsInGroup = groupedFields[groupName];
                fieldsInGroup.Sort((a, b) => a.originalOrder.CompareTo(b.originalOrder));

                foreach (var (field, _) in fieldsInGroup)
                {
                    finalOrdered.Add((groupName, field));
                }
            }

            // Add fields to _defaultProps in new order
            foreach (var (group, field) in finalOrdered)
            {
                var prop = serializedObject.FindProperty(field.Name);
                if (prop != null)
                {
                    _defaultProps.Add(prop);
                    assignedPaths.Add(prop.propertyPath);
                }
            }
        }

        private bool IsTargetValid()
        { 
            try
            {
                return target != null &&
                       targets != null &&
                       targets.Length > 0 &&
                       serializedObject != null &&
                       target.GetType() != null;
            }
            catch (System.Exception)
            {
                return false;
            }
        }

        private FieldInfo[] GetAllFields(Type type)
        {
            if (_allFieldsCache.TryGetValue(type, out var cachedFields))
                return cachedFields;

            var fields = new List<FieldInfo>();
            var currentType = type;
            var types = new Stack<Type>();

            // First we collect all types in the hierarchy
            while (currentType != null && currentType != typeof(object))
            {
                types.Push(currentType);
                currentType = currentType.BaseType;
            }

            // We then add fields in reverse order (from base class to derived class)
            while (types.Count > 0)
            {
                var typeToProcess = types.Pop();
                var typeFields = typeToProcess.GetFields(
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.DeclaredOnly
                );

                fields.AddRange(typeFields);
            }

            var result = fields.ToArray();
            _allFieldsCache[type] = result;
            return result;
        }


        /// <summary>
        /// Renders a foldout with a list of properties.
        /// </summary>
        private void DrawStyledFoldout(FoldoutCache cache, Type targetType)
        {
            if (cache.props.Count == 0)
                return;
            
            if (cache.attr.name == "Basic" || cache.attr.name == "Fire modes"|| cache.attr.name == "Magazines")
            {
                if (guiBasicFoldoutCorePro.DrawFoldoutH1Wide(ref cache.expanded, null, cache.attr.name, cache.prefsKey))
                {
                    //EditorGUILayout.BeginVertical();
                    DrawPropertiesList(cache.props, targetType);
                  //  EditorGUILayout.EndVertical();
                }
            }
            else if (guiBasicFoldoutCorePro.DrawFoldoutH1(ref cache.expanded, null, cache.attr.name, cache.prefsKey))
            {
                //EditorGUILayout.BeginVertical();
                EditorGUI.indentLevel++;
                DrawPropertiesList(cache.props, targetType);
                EditorGUI.indentLevel--;
                //EditorGUILayout.EndVertical();
                //guiBasicFoldoutCorePro.EndFoldoutH1(cache.expanded);
            }

            //EditorGUILayout.Space(1);
        }
        
        private Stack<GroupInfo> activeGroups = new Stack<GroupInfo>();

        /// <summary>
        /// Draws a list of property (any source) - without duplication.
        /// </summary>
        private void DrawPropertiesList(List<SerializedProperty> props, Type targetType)
        {
            // Stack to track open groups
            activeGroups.Clear();
            
            foreach (var prop in props)
            {
                _groupAttrCache.TryGetValue(prop.name, out var groupAttr);
                _endGroupAttrCache.TryGetValue(prop.name, out var endGroupAttr);

                // EndGroup handling
                if (endGroupAttr != null)
                {
                    ProcessEndGroup(endGroupAttr);
                }

                // Group handling
                if (groupAttr != null)
                {
                    ProcessGroupStart(groupAttr);
                }

                // Draw property ONLY when there is no active foldout group or the group is open
                bool shouldDrawProperty = true;

                if (activeGroups.Count > 0)
                {
                    var currentGroup = activeGroups.Peek();
                    if (currentGroup.isFoldout && !currentGroup.isOpen)
                    {
                        shouldDrawProperty = false;
                        // Group is foldout but collapsed - do not draw
                    }
                }

                if (shouldDrawProperty && _drawnPropertyPaths.Add(prop.propertyPath))
                {
                    DrawPropertyWithAttributes(prop, targetType);
                }
            }

            // Close all open groups
            while (activeGroups.Count > 0)
            {
                var group = activeGroups.Pop();
                CloseGroup(group);
            }
            
            activeGroups.Clear();
        }

        private void ProcessGroupStart(GroupAttribute groupAttr)
        {
            // Close the previous group if it exists
            if (activeGroups.Count > 0)
            {
                var prevGroup = activeGroups.Peek();
                if (prevGroup.groupName == groupAttr.GroupName)
                    return;

                prevGroup = activeGroups.Pop();
                CloseGroup(prevGroup);
            }

            var groupInfo = new GroupInfo
            {
                groupName = groupAttr.GroupName,
                isFoldout = groupAttr.Foldout,
                isOpen = false
            };

            // Open a new group
            if (groupInfo.isFoldout)
            {
                string uniqueKey = $"GroupFoldout_{groupAttr.GroupName}_{target.GetType().Name}_{target.GetInstanceID()}";
                // Get the state from the local dictionary
                if (!_groupFoldoutStates.TryGetValue(uniqueKey, out bool currentState))
                {
                    // Default: download from EditorPrefs or set to false
                    currentState = EditorPrefs.GetBool(uniqueKey, false);
                    _groupFoldoutStates[uniqueKey] = currentState;
                }

                groupInfo.isOpen = CoreProGUIGroup.BeginGroupFoldout(groupAttr.GroupName, currentState, out bool newState);

                // Save new status
                if (newState != currentState)
                {
                    _groupFoldoutStates[uniqueKey] = newState;
                    EditorPrefs.SetBool(uniqueKey, newState);
                }
            }
            else
            {
                CoreProGUIGroup.BeginGroup(groupAttr.GroupName);
                groupInfo.isOpen = true;
            }

            activeGroups.Push(groupInfo);
        }


        private void CloseGroup(GroupInfo groupInfo, bool drawBottomBorder = false)
        {
            if (groupInfo.isFoldout)
            {
                CoreProGUIGroup.EndGroupFoldout(groupInfo.isOpen);
            }
            else
            {
                CoreProGUIGroup.EndGroup();
            }
        }


        private void ProcessEndGroup(EndGroupAttribute endGroupAttr)
        {
            if (activeGroups.Count > 0)
            {
                // Case 1: EndGroup without name (GroupName == null) - closes the last opened group
                if (string.IsNullOrEmpty(endGroupAttr.GroupName))
                {
                    var group = activeGroups.Pop();
                    CloseGroup(group);
                    return;
                }

                // Case 2: EndGroup with name - looking for a specific group
                var currentGroup = activeGroups.Peek();
                if (currentGroup.groupName == endGroupAttr.GroupName)
                {
                    var group = activeGroups.Pop();
                    CloseGroup(group);
                }
            }
        }

        private void DrawPropertyWithAttributes(SerializedProperty prop, Type targetType)
        {
            if (prop == null)
                return;

            var fieldName = prop.name;

            // Cache instead of reflection
            _fieldInfoCache.TryGetValue(fieldName, out var fieldInfo);

            if (ShouldDrawSectionHeader(fieldName, targetType))
                DrawSectionHeader(_sectionHeaderAttrCache[fieldName].Text);

            DrawTitle(fieldName);
            DrawLine(fieldName);
            DrawHeaderPro(fieldName);
                    
            if (DrawHideIfAttribute(fieldName, targetType))
                return;

            if (DrawShowIfAttribute(fieldName, targetType) == false)
                return;

            if (DrawModuleView(prop, fieldInfo))
                return;

            if (DrawRuntimeAttribute(prop.name))
                return;
            
            bool prevGuiState = GUI.enabled;
            if (DrawDisableIfAttribute(fieldName, targetType))
                GUI.enabled = false;

            if (_flatDisplayAttrCache.TryGetValue(fieldName, out var flatDisplayAttr) && flatDisplayAttr != null)
            {
                DrawFlatDisplayNoFrame(prop);
                GUI.enabled = prevGuiState;
                return;
            }

            EditorGUILayout.PropertyField(prop, true);
            GUI.enabled = prevGuiState;
        }

        private bool DrawModuleView(SerializedProperty property, FieldInfo fieldInfo)
        {
            bool handled = false;
            ModuleView_Draw(property, fieldInfo, ref handled); 
            return handled;
        }

        private void DrawFlatDisplay(SerializedProperty property)
        {
            var prop = property.Copy();
            var end = prop.GetEndProperty();
            prop.NextVisible(true); // skip self

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            while (!SerializedProperty.EqualContents(prop, end))
            {
                EditorGUILayout.PropertyField(prop, true);
                prop.NextVisible(false);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawFlatDisplayNoFrame(SerializedProperty property)
        {
            var prop = property.Copy();
            var end = prop.GetEndProperty();
            prop.NextVisible(true);

            while (!SerializedProperty.EqualContents(prop, end))
            {
                EditorGUILayout.PropertyField(prop, true);
                prop.NextVisible(false);
            }
        }


        private FieldInfo GetFieldInfo(Type type, string name)
        {
            // First try the cache, if it is
            if (_fieldInfoCache != null && _fieldInfoCache.TryGetValue(name, out var fi))
                return fi;

            // Fallback on reflection (for children/list elements, if needed)
            return type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }


        private bool ShouldDrawSectionHeader(string propName, Type targetType)
        {
            if (!_sectionHeaderAttrCache.TryGetValue(propName, out var sectionAttr) || sectionAttr == null)
                return false;

            if (!sectionAttr.HasCondition)
                return true;

            var comparedField = GetFieldInfo(targetType, sectionAttr.ComparedField);
            if (comparedField == null)
                return false;

            var comparedValue = comparedField.GetValue(target);
            bool equals = Equals(comparedValue, sectionAttr.ComparedValue);
            return sectionAttr.Invert ? !equals : equals;
        }

        private bool DrawShowIfAttribute(string propName, Type targetType)
        {
            // ShowIfAll - AND logic
            if (_showIfAllAttrCache.TryGetValue(propName, out var showIfAll) && showIfAll != null)
            {
                for (int i = 0; i < showIfAll.fieldNames.Length; i++)
                {
                    var fi = GetFieldInfo(targetType, showIfAll.fieldNames[i]);
                    if (fi == null) return false;
                    if (!Equals(fi.GetValue(target), showIfAll.values[i])) return false;
                }
                return true;
            }

            // ShowIf (multiple) - AND logic
            if (!_showIfAttrCache.TryGetValue(propName, out var showIfList) || showIfList == null)
                return true;

            foreach (var showIf in showIfList)
            {
                var comparedField = GetFieldInfo(targetType, showIf.fieldName);
                if (comparedField == null) return false;

                var comparedValue = comparedField.GetValue(target);
                bool equals;

                if (showIf.values != null && showIf.values.Length > 0)
                    equals = showIf.values.Any(v => Equals(comparedValue, v));
                else
                    equals = Equals(comparedValue, showIf.value);

                if (showIf.invert ? equals : !equals) return false;
            }

            return true;
        }
        
        
        
        private bool DrawHideIfAttribute(string propName, Type targetType)
        {
            if (!_hideIfAttrCache.TryGetValue(propName, out var hideIfAttr) || hideIfAttr == null)
                return false;

            var comparedField = GetFieldInfo(targetType, hideIfAttr.fieldName);
            if (comparedField == null)
                return false;

            var comparedValue = comparedField.GetValue(target);
            bool equals = Equals(comparedValue, hideIfAttr.value);
            return hideIfAttr.invert ? !equals : equals;
        }


        private bool DrawDisableIfAttribute(string propName, Type targetType)
        {
            if (!_disableIfAttrCache.TryGetValue(propName, out var disableIfAttr) || disableIfAttr == null)
                return false;

            var comparedField = GetFieldInfo(targetType, disableIfAttr.FieldName);
            if (comparedField == null)
                return false;

            var comparedValue = comparedField.GetValue(target);
            bool equals = Equals(comparedValue, disableIfAttr.Value);
            return disableIfAttr.Invert ? !equals : equals;
        }


   
        /// <summary>
        /// Draws a section header with custom style.
        /// </summary>
        private void DrawSectionHeader(string text)
        {
            if (_sectionHeaderStyle == null)
            {
                _sectionHeaderStyle = new GUIStyle(EditorStyles.helpBox)
                {
                    alignment = TextAnchor.MiddleLeft,
                    fontStyle = FontStyle.Bold,
                    fontSize = 12,
                    padding = new RectOffset(10, 0, 6, 6),
                    margin = new RectOffset(0, 2, 6, 6)
                };
            }

            EditorGUILayout.LabelField(text, _sectionHeaderStyle, GUILayout.ExpandWidth(true), GUILayout.Height(24));
        }
        

        private void DrawTitle(string propName)   
        {   
            if (!_titleAttrCache.TryGetValue(propName, out var title) || title == null)
                return ;
            
            if (_titleStyle == null)
            {
                _titleStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = CoreProEditorStyle.Asset.titleUnderlineFontSize,
                    fontStyle = CoreProEditorStyle.Asset.titleUnderlineFont,
                    alignment = TextAnchor.MiddleLeft
                };
            }

            EditorGUILayout.Space(CoreProEditorStyle.Asset.titleSpaceBefore);
            EditorGUILayout.LabelField(title.Title, _titleStyle, GUILayout.ExpandWidth(true), GUILayout.Height(24));
            EditorGUILayout.Space(1);
            
            Rect titleRect = GUILayoutUtility.GetLastRect();
            float lineY = titleRect.yMax - 2 ; 
            Rect lineRect = new Rect(titleRect.x, lineY, titleRect.width, CoreProEditorStyle.Asset.titleUnderlineHeight);
            EditorGUI.DrawRect(lineRect, CoreProEditorStyle.Asset.TitleUnderline);
            EditorGUILayout.Space(2);
        } 
        

/// <summary>
/// Draws non-serialized fields marked with [ShowInInspector]
/// </summary>
private void DrawShowInInspectorFields()
{
    if (_showInInspectorFieldsCache == null)
    {
        _showInInspectorFieldsCache = target.GetType().GetFields(
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
    }
    
    bool hasAnyFields = false;
    
    foreach (var field in _showInInspectorFieldsCache)
    {
        // Skip serialized fields (they're already drawn)
        if (field.IsPublic && !Attribute.IsDefined(field, typeof(System.NonSerializedAttribute)))
            continue;
        
        if (Attribute.IsDefined(field, typeof(SerializeField)))
            continue;
            
        // Check for ShowInInspector attribute
        var attr = Attribute.GetCustomAttribute(field, typeof(ShowInInspectorAttribute), true);
        if (attr == null) continue;
        if (field.IsStatic) continue;

        if (!hasAnyFields)
        {
            EditorGUILayout.Space(8);
            hasAnyFields = true;
        }

        object value = field.GetValue(target);
        DrawReadonlyField(field, value);
    }
}

/// <summary>
/// Draws a single readonly field for ShowInInspector
/// </summary>
private void DrawReadonlyField(FieldInfo field, object value)
{
    EditorGUI.BeginDisabledGroup(true);

    string displayName = ObjectNames.NicifyVariableName(field.Name);

    if (value == null)
    {
        EditorGUILayout.LabelField(displayName, "null");
    }
    else if (value is IList list && !(value is string))
    {
        DrawReadonlyList(displayName, list);
    }
    else if (value is int intValue)
    {
        EditorGUILayout.IntField(displayName, intValue);
    }
    else if (value is float floatValue)
    {
        EditorGUILayout.FloatField(displayName, floatValue);
    }
    else if (value is bool boolValue)
    {
        EditorGUILayout.Toggle(displayName, boolValue);
    }
    else if (value is string stringValue)
    {
        EditorGUILayout.TextField(displayName, stringValue);
    }
    else if (value is Vector2 vec2Value)
    {
        EditorGUILayout.Vector2Field(displayName, vec2Value);
    }
    else if (value is Vector3 vec3Value)
    {
        EditorGUILayout.Vector3Field(displayName, vec3Value);
    }
    else if (value is Color colorValue)
    {
        EditorGUILayout.ColorField(displayName, colorValue);
    }
    else if (value is UnityEngine.Object unityObj)
    {
        EditorGUILayout.ObjectField(displayName, unityObj, field.FieldType, true);
    }
    else
    {
        EditorGUILayout.LabelField(displayName, value.ToString());
    }

    EditorGUI.EndDisabledGroup();
}

/// <summary>
/// Draws a readonly list/array for ShowInInspector
/// </summary>
private void DrawReadonlyList(string label, IList list)
{
    string uniqueKey = $"ShowInInspector_{target.GetType().Name}_{label}_{target.GetInstanceID()}";
    bool expanded = EditorPrefs.GetBool(uniqueKey, false);
    
    expanded = EditorGUILayout.Foldout(expanded, $"{label} ({list.Count})", true);
    EditorPrefs.SetBool(uniqueKey, expanded);

    if (expanded)
    {
        EditorGUI.indentLevel++;
        for (int i = 0; i < list.Count; i++)
        {
            object el = list[i];
            string elLabel = $"Element {i}";
            
            if (el == null)
            {
                EditorGUILayout.LabelField(elLabel, "null");
            }
            else if (el is UnityEngine.Object unityObj)
            {
                EditorGUILayout.ObjectField(elLabel, unityObj, el.GetType(), true);
            }
            else
            {
                EditorGUILayout.LabelField(elLabel, el.ToString());
            }
        }
        EditorGUI.indentLevel--;
    }
}

        private void DrawLine(string propName)   
        {   
            if (!_lineAttrCache.TryGetValue(propName, out var line) || line == null)
                return;

            DrawLine(4, 4);
        } 
        private void DrawLine (float spaceBefore, float spaceAfter)   
        {   
            EditorGUILayout.Space(spaceBefore);

            Rect titleRect = GUILayoutUtility.GetLastRect();
            float lineY = titleRect.yMax ; 
            Rect lineRect = new Rect(titleRect.x, lineY, titleRect.width, 2f);
            
            EditorGUI.DrawRect(lineRect, CoreProEditorStyle.Asset.TitleUnderline);
            EditorGUILayout.Space(spaceAfter);
        } 
        
        private void DrawHeaderPro(string propName)   
        {   
            if (!_headerProAttrCache.TryGetValue(propName, out var headerPro) || headerPro == null)
                return;
            
            //EditorGUILayout.Space(10);
            CoreProEditorStyle.DrawGroupHeaderPro(headerPro.Title);
        } 
        
        public void DrawAllProperties()
        {
            try
            {
                if (target == null || targets == null || serializedObject == null)
                {
                    EditorGUILayout.HelpBox("Target object is null", MessageType.Warning);
                    return;
                }

                if (!_initialized)
                {
                    try
                    {
                        AnalyzeLayout();
                        _initialized = true;
                    }
                    catch (System.Exception ex)
                    {
                        EditorGUILayout.HelpBox($"Error analyzing layout: {ex.Message}", MessageType.Error);
                        return;
                    }
                }

                _drawnPropertyPaths?.Clear();

                // Render foldouts (using cached sorted list to prevent GC allocations)
                try
                {
                    if (_sortedFoldoutsCache != null)
                    {
                        for (int i = 0; i < _sortedFoldoutsCache.Count; i++)
                        {
                            try
                            {
                                DrawStyledFoldout(_sortedFoldoutsCache[i], target.GetType());
                            }
                            catch (System.Exception ex)
                            {
                                EditorGUILayout.HelpBox($"Error drawing foldout '{_sortedFoldoutsCache[i].attr.name}': {ex.Message}", MessageType.Warning);
                            }
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    EditorGUILayout.HelpBox($"Error rendering foldouts: {ex.Message}", MessageType.Error);
                }

                // Render properties not in any foldout
                try
                {
                    if (_defaultProps != null)
                    {
                        DrawPropertiesList(_defaultProps, target.GetType());
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"Error drawing properties: {ex.Message}"); 
                    EditorGUILayout.HelpBox($"Error drawing properties: {ex.Message}", MessageType.Error);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error drawing properties {ex.Message}"); 
                EditorGUILayout.HelpBox($"Critical error in DrawAllProperties: {ex.Message}", MessageType.Error);
                
                // Reset initialization to try again on next refresh
                _initialized = false;
            }
        }
        
    private void DrawButtons(object targetObject)
        {
            // Lazy initialization of method cache if it does not yet exist
            if (_buttonMethodsCache == null)
            {
                var type = targetObject.GetType();
                // We retrieve all methods (public and private) with the Button attribute.
                var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                var buttonMethods = new List<MethodInfo>();
                
                for (int i = 0; i < methods.Length; i++)
                {
                    if (methods[i].GetCustomAttributes(typeof(ButtonAttribute), true).Length > 0)
                    {
                        buttonMethods.Add(methods[i]);
                    }
                }
                _buttonMethodsCache = buttonMethods.ToArray();
            }

            if (_buttonMethodsCache.Length == 0)
                return;

            GUILayout.Space(2);
            
            foreach (var method in _buttonMethodsCache)
            {
                var attributes = method.GetCustomAttributes(typeof(ButtonAttribute), true);
                if (attributes.Length > 0)
                {
                    // Handling multiple Button attributes in a single method
                    foreach (var attr in attributes)
                    {
                        if (attr is ButtonAttribute buttonAttr)
                        {
                            string label = string.IsNullOrEmpty(buttonAttr.Label) ? method.Name : buttonAttr.Label;

                            // Determining style and height based on size (using cached styles)
                            float height = 20f; // Standard Unity height
                            GUIStyle style;

                            switch (buttonAttr.Size)
                            {
                                case ButtonSize.Medium:
                                    height = 30f;
                                    if (_buttonStyleMedium == null)
                                    {
                                        _buttonStyleMedium = new GUIStyle(GUI.skin.button)
                                        {
                                            fontSize = 14,
                                            alignment = TextAnchor.MiddleCenter
                                        };
                                    }
                                    style = _buttonStyleMedium;
                                    break;
                                case ButtonSize.Big:
                                    height = 40f;
                                    if (_buttonStyleBig == null)
                                    {
                                        _buttonStyleBig = new GUIStyle(GUI.skin.button)
                                        {
                                            fontSize = 14,
                                            alignment = TextAnchor.MiddleCenter
                                        };
                                    }
                                    style = _buttonStyleBig;
                                    break;
                                default:
                                    if (_buttonStyleSmall == null)
                                    {
                                        _buttonStyleSmall = new GUIStyle(GUI.skin.button)
                                        {
                                            fontSize = 12,
                                            alignment = TextAnchor.MiddleCenter
                                        };
                                    }
                                    style = _buttonStyleSmall;
                                    break;
                            }

                            if (GUILayout.Button(label, style, GUILayout.Height(height)))
                            {
                                method.Invoke(targetObject, null);
                            }
                        }
                    }
                }
            }
        }
        
        private bool DrawRuntimeAttribute(string propName)
        {
            if (_runtimeAttrCache.TryGetValue(propName, out var field) == false)
                return false; // show by default

            if (typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType))
            {
                var currentValue = field.GetValue(target) as UnityEngine.Object;

                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(ObjectNames.NicifyVariableName(field.Name), GUILayout.Width(EditorGUIUtility.labelWidth));

                var newValue = EditorGUILayout.ObjectField(currentValue, field.FieldType, true);

                if (newValue != currentValue)
                {
                    field.SetValue(target, newValue);
                }

                EditorGUILayout.EndHorizontal();
                EditorGUI.EndDisabledGroup();
                return true;
            }

            return false;
        }
    }
}