#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace CorePro.UI.Editor
{
    [CustomEditor(typeof(UIStyleSheet))]
    public class UIStyleSheetEditor : UnityEditor.Editor
    {
        // Constants 
        private const string Namespace       = "CorePro.UI";
        private const string GeneratedFolder = "Generated";
        private const int    NoThemeIndex    = -1;

        // Foldout state 
        private bool _colorsFoldout     = true;
        private bool _fontColorsFoldout = true;
        private bool _fontsFoldout      = true;
        private bool _audioFoldout      = true;
        private bool _themesFoldout     = true;

        // Reorderable lists 
        private ReorderableList _colorsList;
        private ReorderableList _fontColorsList;
        private ReorderableList _fontsList;

        // Lifecycle 

        private void OnEnable()
        {
            MigrateStableIds();
            _colorsList     = MakeColorList(serializedObject.FindProperty("colors"),     "colors");
            _fontColorsList = MakeColorList(serializedObject.FindProperty("fontColors"), "fontColors");
            _fontsList      = MakeFontList (serializedObject.FindProperty("fonts"),      "fonts");
        }

        // Inspector 
        public override void OnInspectorGUI()
        {
            var sheet = (UIStyleSheet)target;
            serializedObject.Update();

            EditorGUI.BeginChangeCheck();

            DrawColorSlots();
            EditorGUILayout.Space(4);
            DrawFontColorSlots();
            EditorGUILayout.Space(4);
            DrawFontSlots();
            EditorGUILayout.Space(4);
            DrawAudio();
            EditorGUILayout.Space(4);
            DrawThemes();

            serializedObject.ApplyModifiedProperties();

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(sheet);
                sheet.NotifyChangedFromEditor();
                BroadcastToScene(sheet);
            }

            EditorGUILayout.Space(8);
            DrawGenerateButton(sheet);
        }

        // Slot sections (ReorderableList) 
        private void DrawColorSlots()
        {
            _colorsFoldout = EditorGUILayout.Foldout(_colorsFoldout, "Color Slots (defaults)", true, EditorStyles.foldoutHeader);
            if (_colorsFoldout) _colorsList.DoLayoutList();
        }

        private void DrawFontColorSlots()
        {
            _fontColorsFoldout = EditorGUILayout.Foldout(_fontColorsFoldout, "Font Color Slots (defaults)", true, EditorStyles.foldoutHeader);
            if (_fontColorsFoldout) _fontColorsList.DoLayoutList();
        }

        private void DrawFontSlots()
        {
            _fontsFoldout = EditorGUILayout.Foldout(_fontsFoldout, "Font Slots (defaults)", true, EditorStyles.foldoutHeader);
            if (_fontsFoldout) _fontsList.DoLayoutList();
        }

        // ReorderableList factories 
        private ReorderableList MakeColorList(SerializedProperty prop, string themeArrayName)
        {
            var list = new ReorderableList(serializedObject, prop,
                draggable: true, displayHeader: false,
                displayAddButton: true, displayRemoveButton: true);

            list.elementHeight = EditorGUIUtility.singleLineHeight + 4f;

            list.drawElementCallback = (rect, index, _, __) =>
            {
                if (index >= prop.arraySize) return;
                var entry = prop.GetArrayElementAtIndex(index);
                rect.y += 2f; rect.height = EditorGUIUtility.singleLineHeight;

                float x = rect.x;

                //Stable ID – read-only label showing the enum value
                int id = entry.FindPropertyRelative("stableId").intValue;
                EditorGUI.LabelField(new Rect(x, rect.y, 24f, rect.height), id.ToString());
                x += 24f;

                EditorGUI.PropertyField(new Rect(x, rect.y, 120f, rect.height),
                    entry.FindPropertyRelative("name"), GUIContent.none);
                x += 124f;

                EditorGUI.PropertyField(new Rect(x, rect.y, rect.xMax - x, rect.height),
                    entry.FindPropertyRelative("color"), GUIContent.none);
            };

            list.onAddCallback = rl =>
            {
                int nextId = ComputeNextId(prop);
                prop.InsertArrayElementAtIndex(prop.arraySize);
                var e = prop.GetArrayElementAtIndex(prop.arraySize - 1);
                e.FindPropertyRelative("name").stringValue     = string.Empty;
                e.FindPropertyRelative("color").colorValue     = Color.white;
                e.FindPropertyRelative("stableId").intValue    = nextId;
                OnListChanged();
            };

            list.onRemoveCallback = rl =>
            {
                SyncThemeArrayOnRemove(themeArrayName, rl.index);
                prop.DeleteArrayElementAtIndex(rl.index);
                OnListChanged();
            };

            list.onReorderCallbackWithDetails = (rl, oldIdx, newIdx) =>
            {
                SyncThemeArrayOnReorder(themeArrayName, oldIdx, newIdx);
                OnListChanged();
            };

            return list;
        }

        private ReorderableList MakeFontList(SerializedProperty prop, string themeArrayName)
        {
            var list = new ReorderableList(serializedObject, prop,
                draggable: true, displayHeader: false,
                displayAddButton: true, displayRemoveButton: true);

            list.elementHeight = EditorGUIUtility.singleLineHeight + 4f;

            list.drawElementCallback = (rect, index, _, __) =>
            {
                if (index >= prop.arraySize) return;
                var entry = prop.GetArrayElementAtIndex(index);
                rect.y += 2f; rect.height = EditorGUIUtility.singleLineHeight;

                float x = rect.x;

                int id = entry.FindPropertyRelative("stableId").intValue;
                EditorGUI.LabelField(new Rect(x, rect.y, 24f, rect.height), id.ToString());
                x += 24f;

                EditorGUI.PropertyField(new Rect(x, rect.y, 120f, rect.height),
                    entry.FindPropertyRelative("name"), GUIContent.none);
                x += 124f;

                EditorGUI.PropertyField(new Rect(x, rect.y, 55f, rect.height),
                    entry.FindPropertyRelative("size"), GUIContent.none);
                x += 58f;

                EditorGUI.PropertyField(new Rect(x, rect.y, rect.xMax - x, rect.height),
                    entry.FindPropertyRelative("font"), GUIContent.none);
            };

            list.onAddCallback = rl =>
            {
                int nextId = ComputeNextId(prop);
                prop.InsertArrayElementAtIndex(prop.arraySize);
                var e = prop.GetArrayElementAtIndex(prop.arraySize - 1);
                e.FindPropertyRelative("name").stringValue              = string.Empty;
                e.FindPropertyRelative("font").objectReferenceValue     = null;
                e.FindPropertyRelative("size").floatValue               = 14f;
                e.FindPropertyRelative("stableId").intValue             = nextId;
                OnListChanged();
            };

            list.onRemoveCallback = rl =>
            {
                SyncThemeArrayOnRemove(themeArrayName, rl.index);
                prop.DeleteArrayElementAtIndex(rl.index);
                OnListChanged();
            };

            list.onReorderCallbackWithDetails = (rl, oldIdx, newIdx) =>
            {
                SyncThemeArrayOnReorder(themeArrayName, oldIdx, newIdx);
                OnListChanged();
            };

            return list;
        }

        // Theme parallel-array sync 

        private void SyncThemeArrayOnReorder(string themeRelProp, int oldIdx, int newIdx)
        {
            var themesProp = serializedObject.FindProperty("themes");
            for (int t = 0; t < themesProp.arraySize; t++)
            {
                var arr = themesProp.GetArrayElementAtIndex(t).FindPropertyRelative(themeRelProp);
                if (arr.arraySize > Mathf.Max(oldIdx, newIdx))
                    arr.MoveArrayElement(oldIdx, newIdx);
            }
        }

        private void SyncThemeArrayOnRemove(string themeRelProp, int idx)
        {
            var themesProp = serializedObject.FindProperty("themes");
            for (int t = 0; t < themesProp.arraySize; t++)
            {
                var arr = themesProp.GetArrayElementAtIndex(t).FindPropertyRelative(themeRelProp);
                if (idx < arr.arraySize)
                    arr.DeleteArrayElementAtIndex(idx);
            }
        }

        private void OnListChanged()
        {
            serializedObject.ApplyModifiedProperties();
            var sheet = (UIStyleSheet)target;
            EditorUtility.SetDirty(sheet);
            sheet.NotifyChangedFromEditor();
            BroadcastToScene(sheet);
            serializedObject.Update();
        }

        // Stable-ID helpers 
        private static int ComputeNextId(SerializedProperty list)
        {
            int max = -1;
            for (int i = 0; i < list.arraySize; i++)
            {
                int id = list.GetArrayElementAtIndex(i).FindPropertyRelative("stableId").intValue;
                if (id > max) max = id;
            }
            return max + 1;
        }

        ///<summary>
        ///First-time migration: if all stableIds are 0 (default), assign array-position-based
        ///IDs so existing serialized slot references (colorSlot=N) remain valid after the lookup
        ///changes from positional to ID-based.
        ///</summary>
        private void MigrateStableIds()
        {
            serializedObject.Update();
            bool changed = false;
            changed |= MigrateList(serializedObject.FindProperty("colors"));
            changed |= MigrateList(serializedObject.FindProperty("fontColors"));
            changed |= MigrateList(serializedObject.FindProperty("fonts"));
            if (changed)
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
            }
        }

        private static bool MigrateList(SerializedProperty list)
        {
            int count = list.arraySize;
            if (count == 0) return false;

            //Only migrate when every entry has stableId == 0 (never assigned before).
            for (int i = 0; i < count; i++)
                if (list.GetArrayElementAtIndex(i).FindPropertyRelative("stableId").intValue != 0)
                    return false;

            //Assign stableId = array index to preserve backward compatibility:
            //existing scene components store slot as int equal to old enum value (= old array index).
            for (int i = 0; i < count; i++)
                list.GetArrayElementAtIndex(i).FindPropertyRelative("stableId").intValue = i;

            return true;
        }

        // Audio 

        private void DrawAudio()
        {
            _audioFoldout = EditorGUILayout.Foldout(_audioFoldout, "Audio", true, EditorStyles.foldoutHeader);
            if (!_audioFoldout) return;

            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("clickSound"),        new GUIContent("Click Sound"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("hoverSound"),        new GUIContent("Hover Sound"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("notificationSound"), new GUIContent("Notification Sound"));
            EditorGUI.indentLevel--;
        }

        // Themes 
        private void DrawThemes()
        {
            _themesFoldout = EditorGUILayout.Foldout(_themesFoldout, "Themes", true, EditorStyles.foldoutHeader);
            if (!_themesFoldout) return;

            EditorGUI.indentLevel++;

            var themesProp       = serializedObject.FindProperty("themes");
            var currentIndexProp = serializedObject.FindProperty("currentThemeIndex");

            //Read slot data from serialized properties (not from target.Colors etc.) so that
            //in-progress unsaved changes (e.g. reorders) are reflected immediately.
            var colorsProp     = serializedObject.FindProperty("colors");
            var fontColorsProp = serializedObject.FindProperty("fontColors");
            var fontsPropRef   = serializedObject.FindProperty("fonts");
            int colorCount     = colorsProp.arraySize;
            int fontColorCount = fontColorsProp.arraySize;
            int fontCount      = fontsPropRef.arraySize;

            //Active theme dropdown – index 0 = None (-1), index N+1 = themes[N]
            {
                int      popupCount = themesProp.arraySize + 1;
                string[] popupNames = new string[popupCount];
                popupNames[0] = "None (defaults only)";
                for (int i = 0; i < themesProp.arraySize; i++)
                {
                    string n = themesProp.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue;
                    popupNames[i + 1] = string.IsNullOrWhiteSpace(n) ? $"Theme {i}" : n;
                }

                int storedIndex = currentIndexProp.intValue;
                int popupIndex  = storedIndex < 0 ? 0 : Mathf.Clamp(storedIndex + 1, 1, popupCount - 1);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Active Theme", GUILayout.Width(EditorGUIUtility.labelWidth));
                int nextPopup = EditorGUILayout.Popup(popupIndex, popupNames);
                EditorGUILayout.EndHorizontal();

                int nextStored = nextPopup == 0 ? NoThemeIndex : nextPopup - 1;
                if (nextStored != storedIndex)
                    currentIndexProp.intValue = nextStored;
            }

            EditorGUILayout.Space(6);

            //Per-theme foldouts
            for (int t = 0; t < themesProp.arraySize; t++)
            {
                var    themeProp = themesProp.GetArrayElementAtIndex(t);
                var    nameProp  = themeProp.FindPropertyRelative("name");
                string label     = string.IsNullOrWhiteSpace(nameProp.stringValue)
                    ? $"Theme {t}" : nameProp.stringValue;

                EditorGUILayout.BeginHorizontal();
                themeProp.isExpanded = EditorGUILayout.Foldout(themeProp.isExpanded, label, true);
                if (GUILayout.Button("✕", GUILayout.Width(22)))
                {
                    themesProp.DeleteArrayElementAtIndex(t);
                    break;
                }
                EditorGUILayout.EndHorizontal();

                if (!themeProp.isExpanded) continue;

                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(nameProp);

                if (GUILayout.Button($"↓  Copy default values into \"{label}\""))
                    CopyDefaultsIntoTheme(serializedObject, t, colorCount, fontColorCount, fontCount);

                EditorGUILayout.Space(2);

                SyncListLength(themeProp.FindPropertyRelative("colors"),     colorCount);
                SyncListLength(themeProp.FindPropertyRelative("fontColors"), fontColorCount);
                SyncListLength(themeProp.FindPropertyRelative("fonts"),      fontCount);

                // UI Colors 
                EditorGUI.indentLevel = 0;
                EditorGUILayout.LabelField("Colors", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                var themeColorsProp = themeProp.FindPropertyRelative("colors");
                for (int i = 0; i < colorCount; i++)
                {
                    string slotName = colorsProp.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue;
                    if (string.IsNullOrWhiteSpace(slotName)) slotName = $"Color {i}";
                    EditorGUILayout.PropertyField(themeColorsProp.GetArrayElementAtIndex(i), new GUIContent(slotName));
                }

                EditorGUILayout.Space(2);

                // Font Colors 
                EditorGUI.indentLevel = 0;
                EditorGUILayout.LabelField("Font Colors", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                var themeFontColorsProp = themeProp.FindPropertyRelative("fontColors");
                for (int i = 0; i < fontColorCount; i++)
                {
                    string slotName = fontColorsProp.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue;
                    if (string.IsNullOrWhiteSpace(slotName)) slotName = $"FontColor {i}";
                    EditorGUILayout.PropertyField(themeFontColorsProp.GetArrayElementAtIndex(i), new GUIContent(slotName));
                }

                EditorGUILayout.Space(2);

                // Fonts 
                EditorGUI.indentLevel = 0;
                EditorGUILayout.LabelField("Fonts", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                var themeFontsProp = themeProp.FindPropertyRelative("fonts");
                for (int i = 0; i < fontCount; i++)
                {
                    string slotName = fontsPropRef.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue;
                    if (string.IsNullOrWhiteSpace(slotName)) slotName = $"Font {i}";

                    var fontOverrideProp = themeFontsProp.GetArrayElementAtIndex(i);
                    var fontProp         = fontOverrideProp.FindPropertyRelative("font");
                    var sizeProp         = fontOverrideProp.FindPropertyRelative("size");

                    EditorGUILayout.BeginHorizontal();
                    int savedIndent = EditorGUI.indentLevel;
                    EditorGUI.indentLevel = 0;
                    EditorGUILayout.LabelField(slotName, GUILayout.Width(100));
                    EditorGUILayout.PropertyField(sizeProp, GUIContent.none, GUILayout.Width(55));
                    EditorGUILayout.PropertyField(fontProp, GUIContent.none);
                    EditorGUI.indentLevel = savedIndent;
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUI.indentLevel--;
                EditorGUILayout.Space(6);
            }

            if (GUILayout.Button("+ Add Theme"))
            {
                themesProp.InsertArrayElementAtIndex(themesProp.arraySize);
                var newTheme = themesProp.GetArrayElementAtIndex(themesProp.arraySize - 1);
                newTheme.FindPropertyRelative("name").stringValue = $"Theme {themesProp.arraySize - 1}";
            }

            EditorGUI.indentLevel--;
        }

        // Copy defaults into theme 
        private static void CopyDefaultsIntoTheme(
            SerializedObject so,
            int themeIndex,
            int colorCount, int fontColorCount, int fontCount)
        {
            var themesProp = so.FindProperty("themes");
            var themeProp  = themesProp.GetArrayElementAtIndex(themeIndex);

            CopyColorList(so.FindProperty("colors"),     themeProp.FindPropertyRelative("colors"),     colorCount);
            CopyColorList(so.FindProperty("fontColors"), themeProp.FindPropertyRelative("fontColors"), fontColorCount);

            var fontsSrc = so.FindProperty("fonts");
            var fontsDst = themeProp.FindPropertyRelative("fonts");
            SyncListLength(fontsDst, fontCount);

            for (int i = 0; i < fontCount; i++)
            {
                var src = fontsSrc.GetArrayElementAtIndex(i);
                var dst = fontsDst.GetArrayElementAtIndex(i);
                dst.FindPropertyRelative("font").objectReferenceValue = src.FindPropertyRelative("font").objectReferenceValue;
                dst.FindPropertyRelative("size").floatValue           = src.FindPropertyRelative("size").floatValue;
            }

            so.ApplyModifiedProperties();
            Debug.Log($"[UIStyleSheet] Copied default values into theme [{themeIndex}].");
        }

        private static void CopyColorList(SerializedProperty srcList, SerializedProperty dstList, int count)
        {
            SyncListLength(dstList, count);
            for (int i = 0; i < count; i++)
                dstList.GetArrayElementAtIndex(i).colorValue =
                    srcList.GetArrayElementAtIndex(i).FindPropertyRelative("color").colorValue;
        }

        // Utility 
        private static void SyncListLength(SerializedProperty list, int targetCount)
        {
            while (list.arraySize < targetCount) list.InsertArrayElementAtIndex(list.arraySize);
            while (list.arraySize > targetCount) list.DeleteArrayElementAtIndex(list.arraySize - 1);
        }

        // Code generation 

        private void DrawGenerateButton(UIStyleSheet sheet)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                var style = new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold };
                if (GUILayout.Button("Generate ColorSlot & FontSlot enums", style, GUILayout.Height(28)))
                    GenerateEnums(sheet);
                GUILayout.FlexibleSpace();
            }

            EditorGUILayout.HelpBox(
                "Click Generate after adding, removing, or renaming a slot.\n" +
                "Output: ColorSlot.cs, FontColorSlot.cs, FontSlot.cs → " + GeneratedFolder,
                MessageType.Info);
        }

        private static void GenerateEnums(UIStyleSheet sheet)
        {
            string assetPath = AssetDatabase.GetAssetPath(sheet);
            string assetDir  = Path.GetDirectoryName(assetPath)!;
            string outputDir = Path.Combine(assetDir, GeneratedFolder);
            Directory.CreateDirectory(outputDir);

            WriteEnum(outputDir, "ColorSlot",    sheet.Colors,     e => e.name, e => e.stableId);
            WriteEnum(outputDir, "FontColorSlot", sheet.FontColors, e => e.name, e => e.stableId);
            WriteEnum(outputDir, "FontSlot",      sheet.Fonts,      e => e.name, e => e.stableId);

            AssetDatabase.Refresh();
            Debug.Log($"[UIStyleSheet] Generated ColorSlot.cs, FontColorSlot.cs, FontSlot.cs → {outputDir}");
        }

        private static void WriteEnum<T>(
            string dir,
            string enumName,
            System.Collections.Generic.IReadOnlyList<T> entries,
            System.Func<T, string> getName,
            System.Func<T, int>    getId)
        {
            var sb = new StringBuilder();
            sb.AppendLine("//AUTO-GENERATED – do not edit manually.");
            sb.AppendLine("//Add or rename slots in UIStyleSheet, then click Generate.");
            sb.AppendLine("//Enum values are stable IDs – they do NOT change when slots are reordered.");
            sb.AppendLine();
            sb.AppendLine($"namespace {Namespace}");
            sb.AppendLine("{");
            sb.AppendLine($"    public enum {enumName}");
            sb.AppendLine("    {");

            for (int i = 0; i < entries.Count; i++)
            {
                int    id   = getId(entries[i]);
                string safe = SanitizeIdentifier(getName(entries[i]));
                if (string.IsNullOrEmpty(safe)) safe = $"Slot{id}";
                sb.AppendLine($"        {safe} = {id},");
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            File.WriteAllText(Path.Combine(dir, $"{enumName}.cs"), sb.ToString(), Encoding.UTF8);
        }

        private static string SanitizeIdentifier(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
            var sb = new StringBuilder(raw.Length);
            foreach (char c in raw) sb.Append(char.IsLetterOrDigit(c) ? c : '_');
            string result = sb.ToString();
            return char.IsDigit(result[0]) ? "_" + result : result;
        }

        // Scene broadcast 

        private static void BroadcastToScene(UIStyleSheet sheet)
        {
            var styles = FindObjectsByType<UIStyle>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            foreach (var style in styles)
                style.ForceApplyInEditor(sheet);
        }
    }
}
#endif
