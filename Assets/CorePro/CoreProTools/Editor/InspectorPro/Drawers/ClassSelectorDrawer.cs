using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using InspectorPro;
using InspectorPro.ClassSelector;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace InspectorPro
{
   
#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(ClassSelectorAttribute))]
    public sealed class ClassSelectorDrawer : PropertyDrawer
    {
        private static readonly GUIContent s_LabelContent = new GUIContent();
        private static readonly Dictionary<Type, MemberInfo> s_NameMemberCache = new Dictionary<Type, MemberInfo>();

        // Domain-lifetime caches
        private static readonly Dictionary<Type, List<Type>> s_AssignableTypesCache = new Dictionary<Type, List<Type>>();
        private static readonly Dictionary<string, Type> s_TypeByFullName = new Dictionary<string, Type>();
        private static readonly Type s_UnityObjectType = typeof(UnityEngine.Object);

        private static GUIStyle s_DropdownStyle;
        private static readonly GUIContent s_TempContent = new GUIContent();
        private static readonly StringBuilder s_InfoSB = new StringBuilder(512);
        // Drawer static fields:
        private static readonly Dictionary<string, string> s_HumanizeCache = new Dictionary<string, string>();
        private static readonly System.Text.StringBuilder s_HumanizeSB = new System.Text.StringBuilder(128);
        const string MissingTooltip = "Add [SerializeReference] to this field. [ClassSelector] works only with managed references.";
        const string UnityObjectTooltip = "[ClassSelector] does not support UnityEngine.Object-derived types (Component, MonoBehaviour, ScriptableObject).";
        const string PrimitiveOrStructTooltip = "[ClassSelector] does not support primitives or Unity structs (Vector2, Color, etc.).";

        private static void EnsureGuiCache()
        {
            if (s_DropdownStyle == null)
            {
                s_DropdownStyle = new GUIStyle("DropDownButton")
                {
                    padding = { left = 4, right = 16 },
                    fontSize = 12,
                    alignment = TextAnchor.MiddleLeft,
                    fixedHeight = EditorGUIUtility.singleLineHeight - 2f
                };
            }
        }


        // Call this on any label you want to prettify.
        private static string HumanizeText(string text, bool replaceUnderscores)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            if (s_HumanizeCache.TryGetValue(text, out var cached))
            {
                return cached;
            }

            s_HumanizeSB.Length = 0;

            char prev = '\0';
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                char next = (i + 1 < text.Length) ? text[i + 1] : '\0';

                // Treat '_' and '-' as word separators
                if (replaceUnderscores && (c == '_' || c == '-'))
                {
                    if (s_HumanizeSB.Length > 0 && s_HumanizeSB[s_HumanizeSB.Length - 1] != ' ')
                    {
                        s_HumanizeSB.Append(' ');
                    }

                    prev = c;
                    continue;
                }

                bool insertSpace = false;
                if (i > 0)
                {
                    bool prevIsLetter = char.IsLetter(prev);
                    bool currIsLetter = char.IsLetter(c);
                    bool prevIsLower = char.IsLower(prev);
                    bool currIsUpper = char.IsUpper(c);
                    bool prevIsDigit = char.IsDigit(prev);
                    bool currIsDigit = char.IsDigit(c);

                    // lower->UPPER  e.g., "TextAction" => space before 'A'
                    if (prevIsLower && currIsUpper)
                    {
                        insertSpace = true;
                    }
                    // letter<->digit boundaries  e.g., "UI2D" => "UI 2D"
                    else if (prevIsLetter && currIsDigit)
                    {
                        insertSpace = true;
                    }
                    else if (prevIsDigit && currIsLetter)
                    {
                        insertSpace = true;
                    }
                    // ACRONYM + Word  e.g., "UIRenderer" => "UI Renderer" (space before 'R')
                    else if (char.IsUpper(prev) && currIsUpper && (i + 1 < text.Length) && char.IsLower(next))
                    {
                        insertSpace = true;
                    }
                }

                if (insertSpace)
                {
                    if (s_HumanizeSB.Length > 0 && s_HumanizeSB[s_HumanizeSB.Length - 1] != ' ')
                    {
                        s_HumanizeSB.Append(' ');
                    }
                }

                s_HumanizeSB.Append(c);
                prev = c;
            }

            string result = s_HumanizeSB.ToString().Trim();
            s_HumanizeCache[text] = result;
            return result;
        }

        private static GUIContent BuildLabel(SerializedProperty property, GUIContent original, ClassSelectorAttribute opts)
        {
            // Default to original label when not a managed ref or null
            object instance = property.managedReferenceValue;

            string text = original != null ? original.text : null;

            if (instance != null)
            {
                // 1) Interface wins (zero reflection each frame)
                if (instance is IClassSelectorNameProvider provider)
                {
                    text = provider.GetClassSelectorName();
                }
                else
                {
                    // 2) Named member (field/property)
                    if (opts != null && string.IsNullOrEmpty(opts.NameMember) == false)
                    {
                        Type t = instance.GetType();
                        if (s_NameMemberCache.TryGetValue(t, out var mi) == false)
                        {
                            // Try property first, then field (case-insensitive second pass)
                            mi = (MemberInfo)t.GetProperty(opts.NameMember, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                                 ?? t.GetField(opts.NameMember, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                            if (mi == null)
                            {
                                // case-insensitive search (one-time per type; cached)
                                var props = t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                                for (int i = 0; i < props.Length && mi == null; i++)
                                {
                                    if (string.Equals(props[i].Name, opts.NameMember, StringComparison.OrdinalIgnoreCase))
                                        mi = props[i];
                                }

                                if (mi == null)
                                {
                                    var fields = t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                                    for (int i = 0; i < fields.Length && mi == null; i++)
                                    {
                                        if (string.Equals(fields[i].Name, opts.NameMember, StringComparison.OrdinalIgnoreCase))
                                            mi = fields[i];
                                    }
                                }
                            }

                            s_NameMemberCache[t] = mi; // can be null; cached to skip repeats
                        }

                        if (mi != null)
                        {
                            object val = mi is PropertyInfo pi ? pi.GetValue(instance, null)
                                : mi is FieldInfo fi ? fi.GetValue(instance)
                                : null;

                            if (val != null)
                            {
                                text = val.ToString();
                            }
                        }
                    }

                    // 3) Fallback: Type.Name if requested
                    if ((string.IsNullOrEmpty(text) || text == original.text) && (opts == null || opts.UseTypeNameAsLabel))
                    {
                        text = instance.GetType().Name;
                    }
                }
            }
            else
            {
                // instance null -> more readable label
                if (opts == null || opts.UseTypeNameAsLabel)
                {
                    // show the base type for null
                    var baseType = GetFieldTypeOfSerializedReference(property);
                    text = baseType != null ? $"(null) : {baseType.Name}" : "(null)";
                }
                else
                {
                    text = original.text;
                }
            }

            // Prefix/Suffix + truncate
            if (opts != null)
            {
                if (opts.MaxNameLength > 0 && text != null && text.Length > opts.MaxNameLength)
                {
                    text = text.Substring(0, opts.MaxNameLength - 1) + "…";
                }
            }

            text = HumanizeText(text, true);

            s_LabelContent.text = text;
            s_LabelContent.tooltip = original != null ? original.tooltip : null;
            s_LabelContent.image = original != null ? original.image : null;
            return s_LabelContent;
        }


        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType == SerializedPropertyType.ManagedReference)
            {
                EnsureGuiCache();

                // Precompute dropdown rect + content
                Rect dropdownRect = ComputeDropdownRect(position);
                PrepareDropdownContent(property);

                // PRE-HANDLE MouseDown: consume event if it lands on dropdown -> prevents foldout toggle
                Event evt = Event.current;
                if (evt.type == EventType.MouseDown && dropdownRect.Contains(evt.mousePosition))
                {
                    DisplayClassSelectorGenericMenu(property);
                    evt.Use();
                    return;
                }

                // Draw the property (with foldout etc.)
                //EditorGUI.PropertyField(position, property, label, true);

                var opts = attribute as ClassSelectorAttribute;
                var niceLabel = BuildLabel(property, label, opts);
                EditorGUI.PropertyField(position, property, niceLabel, true);

                object parentObj =  InspectorPro.Editor.InspectorProFieldHelper.GetTargetObjectOfProperty(property);
                
                // Draw only the visual of the dropdown (no click handling here)
                GUI.Label(dropdownRect, s_TempContent, s_DropdownStyle);
                EditorGUIUtility.AddCursorRect(dropdownRect, MouseCursor.Arrow);
                return;
            }

            DrawWarning(position, property, label);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.propertyType == SerializedPropertyType.ManagedReference)
            {
                return EditorGUI.GetPropertyHeight(property, label, true);
            }

            return EditorGUIUtility.singleLineHeight;
        }

        private static Rect ComputeDropdownRect(Rect propertyRect)
        {
            // Place the dropdown on the right edge of the whole property rect
            float valueWidth = propertyRect.width - EditorGUIUtility.labelWidth;
            float width = Mathf.Clamp(valueWidth * 0.6f, 80f, Mathf.Max(120f, valueWidth - 4f));
            float x = propertyRect.x + propertyRect.width - width;
            float y = propertyRect.y + 1f;
            return new Rect(x, y, width, EditorGUIUtility.singleLineHeight);
        }

        private void PrepareDropdownContent(SerializedProperty property)
        {
            object value = property.managedReferenceValue;
            string className = value == null ? "(null)" : value.GetType().Name;

            s_TempContent.text = className;
            s_TempContent.tooltip = GetClassInfo(property);
        }

        private static void DrawWarning(Rect position, SerializedProperty property, GUIContent label)
        {
            GUIContent content = property.propertyType switch
            {
                SerializedPropertyType.Generic => new GUIContent("Missing [SerializeReference]", MissingTooltip),
                SerializedPropertyType.ObjectReference => new GUIContent("Unity objects not allowed", UnityObjectTooltip),
                _ => new GUIContent("Primitives/structs not allowed", PrimitiveOrStructTooltip)
            };

            EditorGUI.LabelField(position, label, content);
        }

        private void DisplayClassSelectorGenericMenu(SerializedProperty property)
        {
            Type declaredType = GetFieldTypeOfSerializedReference(property);
            var options = attribute as ClassSelectorAttribute;
            List<Type> assignableTypes = GetAllInstantiableTypesAssignableTo(declaredType, options);
            assignableTypes.Sort((a, b) => string.Compare(a.FullName, b.FullName, StringComparison.Ordinal));

            Type instanceType = GetInstanceTypeOfSerializedReference(property);

            GenericMenu menu = new GenericMenu();
            menu.AddDisabledItem(new GUIContent($"Select {declaredType?.Name ?? "Type"}"));
            menu.AddSeparator("");

            bool isNull = property.managedReferenceValue == null;
            menu.AddItem(new GUIContent("None (null)"), isNull, () => SetInstanceToProperty(property, null));
            menu.AddSeparator("");

            int typesCount = assignableTypes.Count;
            if (typesCount == 0)
            {
                menu.AddDisabledItem(new GUIContent("No valid types found."));
            }
            else
            {
                for (int i = 0; i < typesCount; i++)
                {
                    Type t = assignableTypes[i];
                    string full = t.FullName.Replace('.', '/');
                    bool isCurrent = t == instanceType;

                    if (isCurrent)
                    {
                        menu.AddItem(new GUIContent(full), true, DoNothing);
                    }
                    else
                    {
                        menu.AddItem(new GUIContent(full), false, () => SetInstanceToProperty(property, t));
                    }
                }
            }

            menu.ShowAsContext();
        }

        private static void DoNothing()
        {
            /* no-op */
        }

        private static void SetInstanceToProperty(SerializedProperty property, Type type)
        {
            object instance = null;
            try
            {
                bool isTypeNull = type == null;
                if (isTypeNull == false)
                {
                    instance = Activator.CreateInstance(type);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to create instance of type '{type}': {ex.Message}");
                return;
            }

            SerializedObject so = property.serializedObject;
            Undo.RecordObjects(so.targetObjects, $"Assign {property.displayName} Type");
            so.UpdateIfRequiredOrScript();

            bool wasExpanded = property.isExpanded;
            property.managedReferenceValue = instance;
            property.isExpanded = wasExpanded || (instance != null);

            so.ApplyModifiedProperties();
        }

        // ---- Info helpers ----

        private static string GetClassInfo(SerializedProperty property)
        {
            Type fieldType = GetFieldTypeOfSerializedReference(property);
            Type instanceType = GetInstanceTypeOfSerializedReference(property);

            s_InfoSB.Length = 0;

            if (property.managedReferenceValue != null)
            {
                s_InfoSB.Append("Full Name:\n");
                s_InfoSB.Append(instanceType.FullName);
                s_InfoSB.Append("\n\nInheritance:\n");

                var chain = GetInheritanceChain(instanceType, fieldType);
                int len = chain.Length;
                for (int i = 0; i < len; i++)
                {
                    s_InfoSB.Append(chain[i].Name);
                    bool isLast = i == len - 1;
                    if (isLast == false)
                    {
                        s_InfoSB.Append(" -> ");
                    }
                }

                s_InfoSB.Append("\n\n");
            }

            s_InfoSB.Append("Base Type:\n");
            s_InfoSB.Append(fieldType != null ? fieldType.FullName : "(unknown)");
            return s_InfoSB.ToString();
        }

        private static Type GetFieldTypeOfSerializedReference(SerializedProperty property)
        {
            if (property == null)
            {
                return null;
            }

            if (property.propertyType == SerializedPropertyType.ManagedReference == false)
            {
                return null;
            }

            // "AssemblyName TypeFullName"
            string typeName = property.managedReferenceFieldTypename;
            int space = typeName.IndexOf(' ');
            if (space < 0)
            {
                return null;
            }

            string assemblyName = typeName.Substring(0, space);
            string className = typeName.Substring(space + 1);
            return GetTypeFromCache(className, assemblyName);
        }

        private static Type GetInstanceTypeOfSerializedReference(SerializedProperty property)
        {
            if (property == null)
            {
                return null;
            }

            if (property.propertyType == SerializedPropertyType.ManagedReference == false)
            {
                return null;
            }

            if (property.managedReferenceValue == null)
            {
                return null;
            }

            // "AssemblyName TypeFullName"
            string full = property.managedReferenceFullTypename;
            int space = full.IndexOf(' ');
            if (space < 0)
            {
                return null;
            }

            string assemblyName = full.Substring(0, space);
            string className = full.Substring(space + 1);
            return GetTypeFromCache(className, assemblyName);
        }

        private static Type GetTypeFromCache(string className, string assemblyName)
        {
            string full = $"{className}, {assemblyName}";
            if (s_TypeByFullName.TryGetValue(full, out var cached))
            {
                return cached;
            }

            Type found = Type.GetType(full);
            if (found != null)
            {
                s_TypeByFullName[full] = found;
                return found;
            }

            // Fallback: exact assembly search
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            int ac = assemblies.Length;
            for (int i = 0; i < ac; i++)
            {
                var asm = assemblies[i];
                if (asm == null) continue;

                bool nameMatch = asm.GetName().Name == assemblyName;
                if (nameMatch == false) continue;

                found = asm.GetType(className);
                if (found != null)
                {
                    s_TypeByFullName[full] = found;
                    return found;
                }
            }

            return null;
        }

        private static List<Type> GetAllInstantiableTypesAssignableTo(Type declaredType, ClassSelectorAttribute options)
        {
            if (declaredType == null)
            {
                return new List<Type>(0);
            }

            if (s_AssignableTypesCache.TryGetValue(declaredType, out var cachedList))
            {
                if (options == null)
                {
                    return cachedList;
                }

                var filtered = new List<Type>(cachedList.Count);
                for (int i = 0; i < cachedList.Count; i++)
                {
                    var t = cachedList[i];
                    if (PassesOptionsFilter(t, options))
                    {
                        filtered.Add(t);
                    }
                }

                return filtered;
            }

            // Unity 2022+: fast discovery
            var derived = UnityEditor.TypeCache.GetTypesDerivedFrom(declaredType);
            var found = new List<Type>(derived.Count);

            int count = derived.Count;
            for (int i = 0; i < count; i++)
            {
                var t = derived[i];
                if (t == null) continue;
                if (s_UnityObjectType.IsAssignableFrom(t)) continue; // exclude UnityEngine.Object
                if (t.IsGenericTypeDefinition) continue;

                // Cache superset; abstract filter by options next
                found.Add(t);
            }

            s_AssignableTypesCache[declaredType] = found;

            if (options == null)
            {
                var finalA = new List<Type>(found.Count);
                for (int i = 0; i < found.Count; i++)
                {
                    var t = found[i];
                    if (t.IsAbstract == false)
                    {
                        finalA.Add(t);
                    }
                }

                return finalA;
            }
            else
            {
                var finalB = new List<Type>(found.Count);
                for (int i = 0; i < found.Count; i++)
                {
                    var t = found[i];
                    if (PassesOptionsFilter(t, options))
                    {
                        finalB.Add(t);
                    }
                }

                return finalB;
            }
        }

        private static bool PassesOptionsFilter(Type t, ClassSelectorAttribute options)
        {
            if (options == null)
            {
                return t.IsAbstract == false;
            }

            if (options.IncludeAbstract == false && t.IsAbstract)
            {
                return false;
            }

            var arr = options.AssemblyStartsWith;
            if (arr == null || arr.Length == 0)
            {
                return true;
            }

            string asmName = t.Assembly.GetName().Name;
            for (int i = 0; i < arr.Length; i++)
            {
                string prefix = arr[i];
                if (string.IsNullOrEmpty(prefix))
                {
                    continue;
                }

                if (asmName.StartsWith(prefix, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static Type[] GetInheritanceChain(Type instanceType, Type fieldType)
        {
            if (instanceType == null || fieldType == null)
            {
                return Array.Empty<Type>();
            }

            var list = new List<Type>(8);
            Type current = instanceType;

            if (fieldType.IsInterface == false)
            {
                while (current != null)
                {
                    list.Add(current);
                    if (current == fieldType)
                    {
                        break;
                    }

                    current = current.BaseType;
                }

                list.Reverse();
                return list.ToArray();
            }
            else
            {
                while (current != null)
                {
                    list.Add(current);
                    if (fieldType.IsAssignableFrom(current))
                    {
                        var ifaces = current.GetInterfaces();
                        for (int i = 0; i < ifaces.Length; i++)
                        {
                            if (fieldType.IsAssignableFrom(ifaces[i]))
                            {
                                list.Add(ifaces[i]);
                                break;
                            }
                        }

                        break;
                    }

                    current = current.BaseType;
                }

                list.Reverse();
                return list.ToArray();
            }
        }
    }
#endif
}