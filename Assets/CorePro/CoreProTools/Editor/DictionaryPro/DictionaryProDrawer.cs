using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;
using System;
using InspectorPro.Editor;

namespace CorePro.DictionaryPro
{
    [CanEditMultipleObjects]
    [CustomPropertyDrawer(typeof(DictionaryPro<,>), true)]
    public class DictionaryProDrawer : PropertyDrawer
    {
        #region Variables

        private const int ItemsPerPage = 10; // Number of elements per page
        private const float ColumnWidthMin = 0.1f;
        private const float ColumnWidthMax = 0.9f;
        private const float DefaultColumnWidth = 0.3f; // Default column width for key (30%)
        private const float HandleWidth = 14.5f; // Width of ReorderableList handle
        private const float SplitterWidth = 1f;
        private const float Spacing = 10f;
        private static readonly Color SplitterColor = new Color(0.4f, 0.4f, 0.4f, 1);
        private static readonly Color DuplicateKeyColor = new Color(1f, 0.4f, 0.4f, 0.3f);

        private static float columnWidth = DefaultColumnWidth;
        private static bool isResizing = false;

        private static readonly GUIContent addButtonContent = new GUIContent("+", "Add an element");
        private static readonly GUIContent removeButtonContent = new GUIContent("-", "Delete the selected item");
        private static readonly GUIContent duplicateKeyContent = new GUIContent("Duplicate key detected!", "The key already exists in the dictionary.");
        private const string KeysPropertyName = "keys";
        private const string KeyLabel = "Key";
        private const string ValuesPropertyName = "values";

        private static readonly GUIStyle foldoutStyle = new GUIStyle(EditorStyles.foldout)
        {
            fontStyle = FontStyle.Normal,
            alignment = TextAnchor.MiddleLeft,
        };

        private static readonly GUIStyle countStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleRight,
            fontStyle = FontStyle.Normal
        };

        private static readonly GUIStyle boxStyle = new GUIStyle("RL Header")
        {
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            fixedHeight = 30f // Increased foldout height
        };

        // Static caches shared across all instances to minimize GC pressure
        private static readonly Dictionary<string, int> PageCache = new Dictionary<string, int>();
        private static readonly Dictionary<string, ReorderableList> ListCache = new Dictionary<string, ReorderableList>();
        private static readonly Dictionary<string, bool> FoldoutCache = new Dictionary<string, bool>();
        private static readonly Dictionary<string, string> ValueTypeNameCache = new Dictionary<string, string>();
        private static readonly Dictionary<string, Type> ValueTypeCache = new Dictionary<string, Type>();

        // Reusable GUIContent to avoid allocations
        private static readonly GUIContent TempContent = new GUIContent();

        // Static constructor to register play mode state change handler
        static DictionaryProDrawer()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode || state == PlayModeStateChange.ExitingPlayMode)
            {
                ClearAllCaches();
            }
        }

        private static void ClearAllCaches()
        {
            PageCache.Clear();
            ListCache.Clear();
            FoldoutCache.Clear();
            ValueTypeNameCache.Clear();
            ValueTypeCache.Clear();
        }

        // DrawHeader Cache
        private Rect cachedKeyRectHeader;
        private Rect cachedSplitterRectHeader;
        private Rect cachedValueRectHeader;
        private float cachedLineHeightHeader;
        private float availableWidthHeader;
        private float keyWidthHeader;
        private float valueWidthHeader;
        // ----------------------------

        // DrawPropertyFoldout Cache
        private Rect cachedBoxRect;
        private Rect cachedFoldoutRect;
        private Rect cachedCountRect;
        private float cachedLineHeight;
        private float cachedStandardSpacing;
        private float cachedFoldoutXOffset;
        // ----------------------------

        // DrawElement Cache
        private Rect cachedKeyRectElement;
        private Rect cachedSplitterRectElement;
        private Rect cachedValueRectElement;

        // ----------------------------

        #endregion

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            string propertyPath = property.propertyPath;
            string key = GetPropertyKey(property);

            // Get foldout state
            if (!FoldoutCache.TryGetValue(key, out bool isExpanded))
            {
                isExpanded = true; // Default expanded
                FoldoutCache[key] = isExpanded;
            }

            // Draw foldout with frame and element counter
            position = DrawPropertyFoldout(position, property, label, ref isExpanded);

            // Update foldout state
            FoldoutCache[key] = isExpanded;

            if (isExpanded)
            {
                ReorderableList list = GetList(property, label);

                // Draw list
                list.DoList(position);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            string key = GetPropertyKey(property);

            // Get foldout state
            if (!FoldoutCache.TryGetValue(key, out bool isExpanded))
            {
                isExpanded = true; // Default expanded
                FoldoutCache[key] = isExpanded;
            }

            float height = EditorGUIUtility.singleLineHeight + 15;

            if (isExpanded)
            {
                try
                {
                    ReorderableList list = GetList(property, label);
                    if (list.serializedProperty != null && list.serializedProperty.serializedObject != null)
                    {
                        height += list.GetHeight();
                    }
                }
                catch (NullReferenceException)
                {
                    // If the list is invalid, remove it from cache and return default height
                    ListCache.Remove(key);
                }
            }

            return height;
        }


        private void CacheFoldoutValues()
        {
            // Cache frequently used values only once
            if (cachedLineHeight == 0f)
            {
                cachedLineHeight = 30;
                cachedStandardSpacing = EditorGUIUtility.standardVerticalSpacing;
                cachedFoldoutXOffset = 30f;
            }
        }

        private Rect DrawPropertyFoldout(Rect position, SerializedProperty property, GUIContent label, ref bool isExpanded)
        {
            // Load cached values
            CacheFoldoutValues();

            // Set height
            float boxHeight = cachedLineHeight;

            // Instead of creating new Rect, edit cached objects
            cachedBoxRect.Set(position.x, position.y, position.width, boxHeight);
            GUI.Box(cachedBoxRect, GUIContent.none, boxStyle);

            int originalIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0; // Reset Unity indent to allow absolute positioning

            // Apply offset to X and adjust width accordingly
            cachedFoldoutRect.Set(position.x + cachedFoldoutXOffset, position.y, position.width - cachedFoldoutXOffset, boxHeight);
            isExpanded = EditorGUI.Foldout(cachedFoldoutRect, isExpanded, label, true, foldoutStyle);

            EditorGUI.indentLevel = originalIndent; // Restore original indent

            // Get element count
            int elementCount = property.FindPropertyRelative(KeysPropertyName).arraySize;

            // Update cached Rect for displaying element count
            cachedCountRect.Set(position.x, position.y, position.width - 16f, boxHeight);
            string countText = $"Elements: {elementCount}";
            GUI.Label(cachedCountRect, countText, countStyle);

            // Update position to next line
            position.y += boxHeight - cachedStandardSpacing;
            position.height = cachedLineHeight;

            return position;
        }

        private ReorderableList GetList(SerializedProperty property, GUIContent label)
        {
            string key = GetPropertyKey(property);
            if (ListCache.TryGetValue(key, out ReorderableList list))
            {
                // Check if the serialized property is still valid
                if (list.serializedProperty != null && list.serializedProperty.serializedObject != null)
                {
                    // === AUTOCORRECT FOR CACHE ===
                    // Protection in case desynchronisation occurs after the list has been created
                    SerializedProperty k = property.FindPropertyRelative(KeysPropertyName);
                    SerializedProperty v = property.FindPropertyRelative(ValuesPropertyName);
                    if (k != null && v != null && k.arraySize != v.arraySize)
                    {
                        int safeSize = Mathf.Min(k.arraySize, v.arraySize);
                        k.arraySize = safeSize;
                        v.arraySize = safeSize;
                        property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
                    }
                    // ==============================

                    return list;
                }

                // Remove invalid entry from cache
                ListCache.Remove(key);
            }

            SerializedProperty keys = property.FindPropertyRelative(KeysPropertyName);
            SerializedProperty values = property.FindPropertyRelative(ValuesPropertyName);

            // === AUTOCORRECTION WHEN CREATING A NEW LIST ===
            // Protection against creating a list with corrupted data (fixes OutOfRange error)
            if (keys != null && values != null && keys.arraySize != values.arraySize)
            {
                int safeSize = Mathf.Min(keys.arraySize, values.arraySize);
                keys.arraySize = safeSize;
                values.arraySize = safeSize;
                property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }

            list = new ReorderableList(property.serializedObject, keys, true, true, true, true);

            if (!ValueTypeCache.TryGetValue(key, out Type valueType))
            {
                Type currentType = fieldInfo.FieldType;

                // Traverse the inheritance hierarchy to find the generic base class containing type arguments.
                // Avoid allocations in loops for performance safety.
                while (currentType != null && (!currentType.IsGenericType || currentType.GetGenericArguments().Length < 2))
                {
                    currentType = currentType.BaseType;
                }

                if (currentType != null)
                {
                    // Successfully found the base DictionaryPro<TKey, TValue> definition.
                    valueType = currentType.GetGenericArguments()[1];
                }
                else
                {
                    // Fallback safety to prevent further exceptions if type parsing fails (e.g., nested lists).
                    valueType = typeof(object);
                }

                ValueTypeCache[key] = valueType;
                ValueTypeNameCache[key] = valueType.Name;
            }

            // Configure list
            ConfigureReorderableList(list, property, label, keys, values, valueType);

            ListCache[key] = list;
            return list;
        }

        private void ConfigureReorderableList(ReorderableList list, SerializedProperty property, GUIContent label, SerializedProperty keys,
            SerializedProperty values, Type valueType)
        {
            list.drawHeaderCallback = (Rect rect) => { DrawHeader(rect, property); };

            // NOTE: All callbacks use FindPropertyRelative() instead of captured keys/values.
            // SerializedProperty references are invalidated by Unity after ApplyModifiedProperties()
            // + Update() - closures that capture the original refs would operate on stale data.
            list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                var k = property.FindPropertyRelative(KeysPropertyName);
                var v = property.FindPropertyRelative(ValuesPropertyName);
                DrawElement(rect, index, property, k, v);
            };

            list.elementHeightCallback = (int index) =>
            {
                var k = property.FindPropertyRelative(KeysPropertyName);
                var v = property.FindPropertyRelative(ValuesPropertyName);
                return GetElementHeight(index, property, k, v);
            };

            list.onAddCallback = (ReorderableList l) =>
            {
                var k = property.FindPropertyRelative(KeysPropertyName);
                var v = property.FindPropertyRelative(ValuesPropertyName);
                OnAddElement(l, property, k, v);
            };

            list.onRemoveCallback = (ReorderableList l) =>
            {
                var k = property.FindPropertyRelative(KeysPropertyName);
                var v = property.FindPropertyRelative(ValuesPropertyName);
                OnRemoveElement(l, property, k, v);
            };

            list.onReorderCallbackWithDetails = (ReorderableList l, int oldIndex, int newIndex) =>
            {
                var v = property.FindPropertyRelative(ValuesPropertyName);
                OnReorderElements(l, oldIndex, newIndex, property, v);
            };

            list.drawFooterCallback = (Rect rect) =>
            {
                var k = property.FindPropertyRelative(KeysPropertyName);
                DrawFooter(rect, list, property, k);
            };
        }

        private void CacheValuesHeader()
        {
            // Cache values only once
            if (cachedLineHeightHeader == 0f)
            {
                cachedLineHeightHeader = EditorGUIUtility.singleLineHeight;
            }
        }

        private void DrawHeader(Rect rect, SerializedProperty property)
        {
            // Cache line height, width and other parameters
            CacheValuesHeader();

            // Account for handle width
            rect.x += HandleWidth;
            rect.width -= HandleWidth;

            // Calculate column widths using common method
            CalculateColumnWidths(rect.width, out keyWidthHeader, out valueWidthHeader);

            // Edit existing rectangles instead of creating new ones
            cachedKeyRectHeader.Set(rect.x, rect.y, keyWidthHeader, cachedLineHeightHeader);
            cachedSplitterRectHeader.Set(cachedKeyRectHeader.xMax + Spacing / 2f, rect.y, SplitterWidth, cachedLineHeightHeader);
            cachedValueRectHeader.Set(cachedSplitterRectHeader.xMax + Spacing / 2f, rect.y, valueWidthHeader, cachedLineHeightHeader);

            // Draw labels
            EditorGUI.LabelField(cachedKeyRectHeader, KeyLabel);

            // Get value type name from cache
            string key = GetPropertyKey(property);
            string valueTypeName = ValueTypeNameCache.TryGetValue(key, out string typeName) ? typeName : "Value";
            EditorGUI.LabelField(cachedValueRectHeader, valueTypeName);

            // Draw splitter line
            EditorGUI.DrawRect(new Rect(cachedSplitterRectHeader.x, rect.y, SplitterWidth, rect.height), SplitterColor);

            // Handle splitter events
            HandleSplitterEvents(rect, cachedSplitterRectHeader);
        }


        private void DrawElement(Rect rect, int index, SerializedProperty property, SerializedProperty keys, SerializedProperty values)
        {
            // Get current page and element indexes
            int currentPageIndex = GetCurrentPage(property);
            int startIndex = currentPageIndex * ItemsPerPage;
            int endIndex = Mathf.Min(startIndex + ItemsPerPage, keys.arraySize);

            if (index < startIndex || index >= endIndex)
                return;

            // Update rect position
            rect.y += 2;

            // Get appropriate elements
            SerializedProperty keyProp = keys.GetArrayElementAtIndex(index);
            SerializedProperty valueProp = values.GetArrayElementAtIndex(index);

            // Check if key is duplicate
            bool isDuplicate = IsDuplicateKey(keys, index);

            // Apply colored background for duplicates
            if (isDuplicate)
            {
                EditorGUI.DrawRect(rect, DuplicateKeyColor);
            }

            // Calculate heights for key and value
            float keyHeightElement = EditorGUI.GetPropertyHeight(keyProp, true);
            float valueHeightElement = EditorGUI.GetPropertyHeight(valueProp, true);
            float elementHeightElement = Mathf.Max(keyHeightElement, valueHeightElement);

            // Calculate column widths using common method
            CalculateColumnWidths(rect.width, out float keyWidthElement, out float valueWidthElement);

            // Detection of structures/tables (with foldouts)
            float valueOffset = valueProp.hasVisibleChildren ? 12f : 0f;

            // Edit existing rectangles instead of creating new ones
            cachedKeyRectElement.Set(rect.x, rect.y, keyWidthElement, keyHeightElement);
            cachedSplitterRectElement.Set(cachedKeyRectElement.xMax + Spacing / 2f, rect.y, SplitterWidth, elementHeightElement);
            cachedValueRectElement.Set(cachedSplitterRectElement.xMax + Spacing / 2f + valueOffset, rect.y, valueWidthElement - valueOffset, valueHeightElement);
            
            // Save original GUI settings
            int originalIndentLevel = EditorGUI.indentLevel;
            float originalLabelWidth = EditorGUIUtility.labelWidth;

            // Draw key and value fields
            EditorGUI.indentLevel = 0;
            EditorGUIUtility.labelWidth = keyWidthElement * 0.4f;
            EditorGUI.PropertyField(cachedKeyRectElement, keyProp, GUIContent.none, true);

            // EditorGUIUtility.labelWidth = valueWidthElement * 0.4f;
            // EditorGUI.PropertyField(cachedValueRectElement, valueProp, GUIContent.none, true);

            EditorGUIUtility.labelWidth = valueWidthElement * 0.4f;

            // Creating label for expandable structures (e.g. type name from cache)
            GUIContent valueLabel = GUIContent.none;
            if (valueProp.hasVisibleChildren)
            {
                // string propKey = GetPropertyKey(property);
                // string typeName = ValueTypeNameCache.TryGetValue(propKey, out string tName) ? tName : "Data";
                string typeName = "Values:";
                valueLabel = new GUIContent(typeName);
            }

            // Passing valueLabel instead of GUIContent.none
            EditorGUI.PropertyField(cachedValueRectElement, valueProp, valueLabel, true);
            
            // Restore original settings
            EditorGUI.indentLevel = originalIndentLevel;
            EditorGUIUtility.labelWidth = originalLabelWidth;

            // Draw splitter line
            EditorGUI.DrawRect(new Rect(cachedSplitterRectElement.x, rect.y, SplitterWidth, elementHeightElement), SplitterColor);

            // Draw warning icon for duplicates
            if (isDuplicate)
            {
                Rect warningRect = new Rect(cachedKeyRectElement.xMax - 18f, cachedKeyRectElement.y, 16f, 16f);
                TempContent.image = EditorGUIUtility.IconContent("console.warnicon.sml").image;
                TempContent.tooltip = "Duplicate key!";
                GUI.Label(warningRect, TempContent);
                TempContent.image = null;
                TempContent.tooltip = null;
            }

            // Add spacing after drawing element to prevent drawers from sticking together
            rect.y += elementHeightElement + 4f;
        }

        private void CalculateColumnWidths(float totalWidth, out float keyWidth, out float valueWidth)
        {
            float availableWidth = totalWidth - Spacing - SplitterWidth;
            keyWidth = availableWidth * columnWidth;
            valueWidth = availableWidth * (1f - columnWidth);
        }

        private float GetElementHeight(int index, SerializedProperty property, SerializedProperty keys, SerializedProperty values)
        {
            int currentPageIndex = GetCurrentPage(property);
            int startIndex = currentPageIndex * ItemsPerPage;
            int endIndex = Mathf.Min(startIndex + ItemsPerPage, keys.arraySize);

            if (index < startIndex || index >= endIndex)
                return 0;

            SerializedProperty keyProp = keys.GetArrayElementAtIndex(index);
            SerializedProperty valueProp = values.GetArrayElementAtIndex(index);

            float keyHeight = EditorGUI.GetPropertyHeight(keyProp, true);
            float valueHeight = EditorGUI.GetPropertyHeight(valueProp, true);

            float height = Mathf.Max(keyHeight, valueHeight) + 6;

            if (IsDuplicateKey(keys, index))
            {
                height += EditorGUIUtility.singleLineHeight + 4;
            }

            return height;
        }

        private void OnAddElement(ReorderableList list, SerializedProperty property, SerializedProperty keys, SerializedProperty values)
        {
            Undo.RecordObject(property.serializedObject.targetObject, "Add Dictionary Element");

            int index = keys.arraySize;
            keys.arraySize++;
            values.arraySize++;
            list.index = index;

            SerializedProperty keyProperty   = keys.GetArrayElementAtIndex(index);
            SerializedProperty valueProperty = values.GetArrayElementAtIndex(index);

            ResetPropertyToDefault(keyProperty);
            ResetPropertyToDefault(valueProperty);

            // For non-primitive keys (ObjectReference etc.) duplicate check is skipped -
            // null key cannot be a duplicate and GenerateUniqueKey ignores ObjectReference anyway.
            if (IsDuplicateKey(keys, index))
                GenerateUniqueKey(keyProperty, keys, index);

            // Apply changes to the backing serialized lists.
            // OnBeforeSerialize will fire here, but DictionaryPro.OnBeforeSerialize now
            // preserves null-key rows so the newly added empty row remains visible.
            property.serializedObject.ApplyModifiedProperties();

            // NOTE: Do NOT call serializedObject.Update() here.
            // Update() triggers OnBeforeSerialize again which, even with the null-key fix,
            // causes an unnecessary round-trip. The Inspector will refresh on the next repaint.
            ListCache.Remove(GetPropertyKey(property));

            // Automatically go to last page
            int totalItems = keys.arraySize;
            int totalPages = Mathf.CeilToInt((float)totalItems / ItemsPerPage);
            SetCurrentPage(property, totalPages - 1);

            // Force Inspector repaint so the new row appears immediately
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }

        private void GenerateUniqueKey(SerializedProperty keyProperty, SerializedProperty keys, int currentIndex)
        {
            object keyValue = GetPropertyValue(keyProperty);
            if (keyValue == null)
                return;

            // For types that are not easy to modify (ObjectReference, Vector, etc.), don't generate unique keys
            if (keyProperty.propertyType != SerializedPropertyType.String &&
                keyProperty.propertyType != SerializedPropertyType.Integer &&
                keyProperty.propertyType != SerializedPropertyType.Float)
            {
                return;
            }

            int suffix = 1;
            object originalValue = keyValue;
            int maxAttempts = 1000; // Protection against infinite loop

            while (IsKeyDuplicate(keyValue, keys, currentIndex) && suffix < maxAttempts)
            {
                // Generate new key value with suffix
                switch (keyProperty.propertyType)
                {
                    case SerializedPropertyType.String:
                        keyValue = originalValue.ToString() + "_" + suffix++;
                        break;
                    case SerializedPropertyType.Integer:
                        keyValue = Convert.ToInt32(originalValue) + suffix++;
                        break;
                    case SerializedPropertyType.Float:
                        keyValue = Convert.ToSingle(originalValue) + suffix++;
                        break;
                    default:
                        // For other types just increment suffix and break
                        suffix++;
                        break;
                }
            }

            // Set new key value
            switch (keyProperty.propertyType)
            {
                case SerializedPropertyType.String:
                    keyProperty.stringValue = keyValue.ToString();
                    break;
                case SerializedPropertyType.Integer:
                    keyProperty.intValue = Convert.ToInt32(keyValue);
                    break;
                case SerializedPropertyType.Float:
                    keyProperty.floatValue = Convert.ToSingle(keyValue);
                    break;
            }
        }

        private bool IsKeyDuplicate(object keyValue, SerializedProperty keys, int currentIndex)
        {
            for (int i = 0; i < keys.arraySize; i++)
            {
                if (i == currentIndex)
                    continue;

                SerializedProperty otherKey = keys.GetArrayElementAtIndex(i);
                object otherValue = GetPropertyValue(otherKey);

                if (keyValue != null && keyValue.Equals(otherValue))
                    return true;
            }

            return false;
        }

        private void OnRemoveElement(ReorderableList list, SerializedProperty property, SerializedProperty keys, SerializedProperty values)
        {
            int index = list.index;
            if (index >= 0 && index < keys.arraySize)
            {
                Undo.RecordObject(property.serializedObject.targetObject, "Remove Dictionary Element");

                keys.DeleteArrayElementAtIndex(index);
                values.DeleteArrayElementAtIndex(index);

                property.serializedObject.ApplyModifiedProperties();
                ListCache.Remove(GetPropertyKey(property));
                property.serializedObject.Update();

                list.index = index - 1;

                int totalItems = keys.arraySize;
                int totalPages = Mathf.CeilToInt((float)totalItems / ItemsPerPage);
                int currentPageIndex = GetCurrentPage(property);

                int startIndex = currentPageIndex * ItemsPerPage;
                if (startIndex >= totalItems && currentPageIndex > 0)
                    SetCurrentPage(property, currentPageIndex - 1);
            }
        }

        private void OnReorderElements(ReorderableList list, int oldIndex, int newIndex, SerializedProperty property, SerializedProperty values)
        {
            Undo.RecordObject(property.serializedObject.targetObject, "Reorder Dictionary Elements");

            values.MoveArrayElement(oldIndex, newIndex);
            property.serializedObject.ApplyModifiedProperties();
            ListCache.Remove(GetPropertyKey(property));
            property.serializedObject.Update();
        }

        private void DrawFooter(Rect rect, ReorderableList list, SerializedProperty property, SerializedProperty keys)
        {
            int totalItems = keys.arraySize;
            int totalPages = Mathf.CeilToInt((float)totalItems / ItemsPerPage);
            int currentPageIndex = GetCurrentPage(property);

            float buttonWidth = 22;
            float spacing = 5f;
            float offset = 5f;

            // Store original GUI.enabled state
            bool originalEnabled = GUI.enabled;

            // Draw add and remove buttons on the right
            Rect removeButtonRect = new Rect(rect.xMax - buttonWidth - 5 - offset, rect.y, buttonWidth, buttonWidth);
            GUI.enabled = originalEnabled && (list.index >= 0 && list.index < totalItems);
            if (GUI.Button(removeButtonRect, removeButtonContent))
            {
                list.onRemoveCallback(list);
            }

            GUI.enabled = originalEnabled;

            Rect addButtonRect = new Rect(rect.xMax - (buttonWidth + spacing) * 2 - offset, rect.y, buttonWidth, buttonWidth);
            if (GUI.Button(addButtonRect, addButtonContent))
            {
                list.onAddCallback(list);
            }

            // Only show page navigation when there are multiple pages
            if (totalPages > 1)
            {
                Rect prevButtonRect = new Rect(rect.x, rect.y, buttonWidth, rect.height);
                GUI.enabled = originalEnabled && (currentPageIndex > 0);
                if (GUI.Button(prevButtonRect, "<"))
                {
                    SetCurrentPage(property, currentPageIndex - 1);
                }

                GUI.enabled = originalEnabled;

                Rect pageLabelRect = new Rect(rect.x + (buttonWidth + spacing), rect.y, rect.width - (buttonWidth + spacing) * 4, rect.height);
                GUI.Label(pageLabelRect, $"Page {currentPageIndex + 1}/{totalPages}", EditorStyles.centeredGreyMiniLabel);

                Rect nextButtonRect = new Rect(rect.x + (buttonWidth + spacing) * 2, rect.y, buttonWidth, rect.height);
                GUI.enabled = originalEnabled && (currentPageIndex < totalPages - 1);
                if (GUI.Button(nextButtonRect, ">"))
                {
                    SetCurrentPage(property, currentPageIndex + 1);
                }

                GUI.enabled = originalEnabled;
            }
        }

        private void HandleSplitterEvents(Rect rect, Rect splitterRect)
        {
            EditorGUIUtility.AddCursorRect(splitterRect, MouseCursor.ResizeHorizontal);
            if (Event.current.type == UnityEngine.EventType.MouseDown && splitterRect.Contains(Event.current.mousePosition))
            {
                isResizing = true;
                Event.current.Use();
            }

            if (isResizing)
            {
                columnWidth = Mathf.Clamp((Event.current.mousePosition.x - rect.x - Spacing / 2f) / (rect.width - Spacing - SplitterWidth), ColumnWidthMin,
                    ColumnWidthMax);
                GUI.changed = true;
            }

            if (Event.current.type == UnityEngine.EventType.MouseUp)
            {
                isResizing = false;
            }
        }

        private void ResetPropertyToDefault(SerializedProperty property)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                    property.intValue = 0;
                    break;
                case SerializedPropertyType.Boolean:
                    property.boolValue = false;
                    break;
                case SerializedPropertyType.Float:
                    property.floatValue = 0f;
                    break;
                case SerializedPropertyType.String:
                    property.stringValue = "";
                    break;
                case SerializedPropertyType.Color:
                    property.colorValue = Color.white;
                    break;
                case SerializedPropertyType.ObjectReference:
                    property.objectReferenceValue = null;
                    break;
                case SerializedPropertyType.LayerMask:
                    property.intValue = 0;
                    break;
                case SerializedPropertyType.Enum:
                    property.enumValueIndex = 0;
                    break;
                case SerializedPropertyType.Vector2:
                    property.vector2Value = Vector2.zero;
                    break;
                case SerializedPropertyType.Vector3:
                    property.vector3Value = Vector3.zero;
                    break;
                case SerializedPropertyType.Vector4:
                    property.vector4Value = Vector4.zero;
                    break;
                case SerializedPropertyType.Rect:
                    property.rectValue = Rect.zero;
                    break;
                case SerializedPropertyType.ArraySize:
                    property.intValue = 0;
                    break;
                case SerializedPropertyType.Character:
                    property.intValue = 0;
                    break;
                case SerializedPropertyType.AnimationCurve:
                    property.animationCurveValue = AnimationCurve.Linear(0f, 0f, 1f, 1f);
                    break;
                case SerializedPropertyType.Bounds:
                    property.boundsValue = new Bounds(Vector3.zero, Vector3.one);
                    break;
                case SerializedPropertyType.Quaternion:
                    property.quaternionValue = Quaternion.identity;
                    break;
                case SerializedPropertyType.ExposedReference:
                    property.exposedReferenceValue = null;
                    break;
                case SerializedPropertyType.Vector2Int:
                    property.vector2IntValue = Vector2Int.zero;
                    break;
                case SerializedPropertyType.Vector3Int:
                    property.vector3IntValue = Vector3Int.zero;
                    break;
                case SerializedPropertyType.RectInt:
                    property.rectIntValue = new RectInt(0, 0, 0, 0);
                    break;
                case SerializedPropertyType.BoundsInt:
                    property.boundsIntValue = new BoundsInt(Vector3Int.zero, Vector3Int.one);
                    break;
                case SerializedPropertyType.ManagedReference:
                    property.managedReferenceValue = null;
                    break;
                case SerializedPropertyType.Hash128:
                    property.hash128Value = new Hash128();
                    break;
                default:
                    // Handle other types if necessary
                    break;
            }
        }

        private bool IsDuplicateKey(SerializedProperty keys, int index)
        {
            if (index >= keys.arraySize)
                return false;

            SerializedProperty keyProp = keys.GetArrayElementAtIndex(index);
            object keyValue = GetPropertyValue(keyProp);

            if (keyValue == null)
                return false;

            for (int i = 0; i < keys.arraySize; i++)
            {
                if (i == index)
                    continue;

                SerializedProperty otherKey = keys.GetArrayElementAtIndex(i);
                object otherValue = GetPropertyValue(otherKey);

                if (keyValue.Equals(otherValue))
                {
                    return true;
                }
            }

            return false;
        }

        private object GetPropertyValue(SerializedProperty property)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                    return property.intValue;
                case SerializedPropertyType.Boolean:
                    return property.boolValue;
                case SerializedPropertyType.Float:
                    return property.floatValue;
                case SerializedPropertyType.String:
                    return property.stringValue;
                case SerializedPropertyType.Color:
                    return property.colorValue;
                case SerializedPropertyType.ObjectReference:
                    return property.objectReferenceValue;
                case SerializedPropertyType.LayerMask:
                    return property.intValue;
                case SerializedPropertyType.Enum:
                    return property.enumValueIndex;
                case SerializedPropertyType.Vector2:
                    return property.vector2Value;
                case SerializedPropertyType.Vector3:
                    return property.vector3Value;
                case SerializedPropertyType.Vector4:
                    return property.vector4Value;
                case SerializedPropertyType.Rect:
                    return property.rectValue;
                case SerializedPropertyType.ArraySize:
                    return property.intValue;
                case SerializedPropertyType.Character:
                    return property.intValue;
                case SerializedPropertyType.AnimationCurve:
                    return property.animationCurveValue;
                case SerializedPropertyType.Bounds:
                    return property.boundsValue;
                case SerializedPropertyType.Quaternion:
                    return property.quaternionValue;
                case SerializedPropertyType.ExposedReference:
                    return property.exposedReferenceValue;
                case SerializedPropertyType.Vector2Int:
                    return property.vector2IntValue;
                case SerializedPropertyType.Vector3Int:
                    return property.vector3IntValue;
                case SerializedPropertyType.RectInt:
                    return property.rectIntValue;
                case SerializedPropertyType.BoundsInt:
                    return property.boundsIntValue;
                case SerializedPropertyType.ManagedReference:
                    return property.managedReferenceValue;
                case SerializedPropertyType.Hash128:
                    return property.hash128Value;
                default:
                    return null;
            }
        }

        private int GetCurrentPage(SerializedProperty property)
        {
            string key = GetPropertyKey(property);
            if (!PageCache.TryGetValue(key, out int page))
            {
                page = 0;
                PageCache[key] = page;
            }

            return page;
        }

        private void SetCurrentPage(SerializedProperty property, int page)
        {
            string key = GetPropertyKey(property);
            PageCache[key] = page;
        }

        private string GetPropertyKey(SerializedProperty property)
        {
            return property.serializedObject.targetObject.GetInstanceID() + "/" + property.propertyPath;
        }
    }
}