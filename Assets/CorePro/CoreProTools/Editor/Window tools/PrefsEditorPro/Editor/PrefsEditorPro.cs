using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;

namespace CorePro.Editor
{
    /// <summary>
    /// Professional PlayerPrefs and EditorPrefs Editor for Unity.
    /// Features: Virtualized list, Multi-selection (Shift), Custom Tabs, and User-defined Prefix Filtering.
    /// </summary>
    public class PrefsEditorPro : EditorWindow
    {
        #region Data Structures

        private const float ROW_HEIGHT = 22f;

        [System.Serializable]
        public class PrefEntry
        {
            public enum PrefType { String = 0, Int = 1, Float = 2 }
            public PrefType typeSelection;
            public string key;
            public string stringValue;
            public int intValue;
            public float floatValue;
            public bool isSelected;
            public bool isEditorPref;

            public string GetDisplayValue() => typeSelection switch
            {
                PrefType.String => stringValue,
                PrefType.Int => intValue.ToString(),
                PrefType.Float => floatValue.ToString(),
                _ => ""
            };
        }

        #endregion

        #region Fields

        private List<PrefEntry> playerPrefsList = new List<PrefEntry>();
        private List<PrefEntry> editorPrefsList = new List<PrefEntry>();
        private List<PrefEntry> coreProPrefsList = new List<PrefEntry>();

        private Vector2 scrollPosition;
        private string searchQuery = "";
        private int selectedTab = 0; // 0: PlayerPrefs, 1: EditorPrefs, 2: CorePro
        private float relativeSplitterPos = 0.4f;
        private int lastSelectedIndex = -1;

        // Exclusion filter settings
        private bool hideExcluded = true;
        private string excludedPrefixesStr = "unity, com.unity"; // User can edit this in the window

        #endregion

        [MenuItem("Window/CorePro/Prefs Editor Pro")]
        public static void ShowWindow() => GetWindow<PrefsEditorPro>("Prefs Editor Pro");

        private void OnEnable() => Refresh();

        private void OnGUI()
        {
            // --- TOP INTERFACE SECTION ---
            DrawTabs();
            DrawActionToolbar();
            DrawFilterSettings(); // New section for defining exclusion prefixes
            DrawInfoToolbar();
            
            // --- SCROLLABLE LIST SECTION ---
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            switch (selectedTab)
            {
                case 0: DrawVirtualizedList(playerPrefsList, "Player Preferences"); break;
                case 1: DrawVirtualizedList(editorPrefsList, "Unity Editor Preferences"); break;
                case 2: DrawVirtualizedList(coreProPrefsList, "CorePro Editor Preferences"); break;
            }

            EditorGUILayout.EndScrollView();
        }

        #region Drawing Methods

        private void DrawTabs()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            string[] tabs = { "PlayerPrefs", "EditorPrefs", "EditorCoreProPrefs" };
            int newTab = GUILayout.Toolbar(selectedTab, tabs, EditorStyles.toolbarButton);
            if (newTab != selectedTab)
            {
                selectedTab = newTab;
                lastSelectedIndex = -1;
                GUI.FocusControl(null);
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawActionToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            // Search field
            GUILayout.Label("Search:", EditorStyles.miniLabel);
            searchQuery = EditorGUILayout.TextField(searchQuery, EditorStyles.toolbarSearchField);
            
            GUILayout.Space(5);

            // Buttons group
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60))) Refresh();
            
            if (GUILayout.Button("Add New", EditorStyles.toolbarButton, GUILayout.Width(70)))
                AddEntryWindow.ShowWindow(selectedTab == 0, Refresh);

            // Restore Delete Selected
            GUI.enabled = GetActiveList().Any(x => x.isSelected);
            if (GUILayout.Button("Delete Selected", EditorStyles.toolbarButton, GUILayout.Width(100)))
                DeleteSelected();
            
            // Restore Delete All (Visible)
            GUI.enabled = GetFilteredList(GetActiveList()).Count > 0;
            if (GUILayout.Button("Delete All", EditorStyles.toolbarButton, GUILayout.Width(70)))
                DeleteAllVisible();
            
            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();
        }

        private void DrawFilterSettings()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            // Toggle for the exclusion logic
            hideExcluded = EditorGUILayout.ToggleLeft("Hide System Prefs", hideExcluded, GUILayout.Width(125));
            
            // Input field for prefixes to hide
            GUILayout.Label("Hide if starts with (comma separated):", EditorStyles.miniLabel);
            EditorGUI.BeginChangeCheck();
            excludedPrefixesStr = EditorGUILayout.TextField(excludedPrefixesStr, EditorStyles.toolbarTextField);
            if (EditorGUI.EndChangeCheck())
            {
                // Repaint to show changes immediately
                Repaint();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawInfoToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            
            var list = GetActiveList();
            var filtered = GetFilteredList(list);

            GUILayout.Label($"Total: {list.Count} | Shown (Filtered): {filtered.Count} | Selected: {list.Count(x => x.isSelected)}", EditorStyles.miniLabel);
            
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private List<PrefEntry> GetFilteredList(List<PrefEntry> source)
        {
            IEnumerable<PrefEntry> result = source;

            // Apply prefix exclusion (HideIfContain logic from the UI text field)
            if (hideExcluded && !string.IsNullOrEmpty(excludedPrefixesStr))
            {
                string[] prefixes = excludedPrefixesStr.Split(',')
                                    .Select(p => p.Trim())
                                    .Where(p => !string.IsNullOrEmpty(p))
                                    .ToArray();

                result = result.Where(x => !prefixes.Any(p => x.key.StartsWith(p, StringComparison.OrdinalIgnoreCase)));
            }

            // Apply search query
            if (!string.IsNullOrEmpty(searchQuery))
            {
                string query = searchQuery.ToLower();
                result = result.Where(x => x.key.ToLower().Contains(query));
            }

            return result.ToList();
        }

        private void DrawVirtualizedList(List<PrefEntry> list, string title)
        {
            var filtered = GetFilteredList(list);

            if (filtered.Count == 0)
            {
                EditorGUILayout.HelpBox("No records found with current filters.", MessageType.Info);
                return;
            }

            float totalHeight = filtered.Count * ROW_HEIGHT;
            Rect containerRect = GUILayoutUtility.GetRect(0, totalHeight, GUILayout.ExpandWidth(true));
            
            int firstVisible = Mathf.Max(0, Mathf.FloorToInt(scrollPosition.y / ROW_HEIGHT));
            int visibleCount = Mathf.CeilToInt(position.height / ROW_HEIGHT) + 2;
            int lastVisible = Mathf.Min(filtered.Count, firstVisible + visibleCount);

            for (int i = firstVisible; i < lastVisible; i++)
            {
                Rect rowRect = new Rect(containerRect.x, containerRect.y + (i * ROW_HEIGHT), containerRect.width, ROW_HEIGHT);
                HandleInput(rowRect, filtered, i);
                DrawPrefRow(rowRect, filtered[i]);
            }
        }

        private void DrawPrefRow(Rect rect, PrefEntry entry)
        {
            if (entry.isSelected)
                EditorGUI.DrawRect(rect, new Color(0.24f, 0.45f, 0.8f, 0.35f));

            float splitter = rect.width * relativeSplitterPos;
            Rect keyRect = new Rect(rect.x + 5, rect.y + 2, splitter - 10, ROW_HEIGHT - 4);
            Rect typeRect = new Rect(rect.x + splitter, rect.y + 2, 55, ROW_HEIGHT - 4);
            Rect valRect = new Rect(rect.x + splitter + 60, rect.y + 2, rect.width - splitter - 65, ROW_HEIGHT - 4);

            EditorGUI.LabelField(keyRect, new GUIContent(entry.key, entry.key));

            GUI.enabled = false;
            EditorGUI.EnumPopup(typeRect, entry.typeSelection);
            GUI.enabled = true;

            EditorGUI.BeginChangeCheck();
            switch (entry.typeSelection)
            {
                case PrefEntry.PrefType.String: entry.stringValue = EditorGUI.TextField(valRect, entry.stringValue); break;
                case PrefEntry.PrefType.Int: entry.intValue = EditorGUI.IntField(valRect, entry.intValue); break;
                case PrefEntry.PrefType.Float: entry.floatValue = EditorGUI.FloatField(valRect, entry.floatValue); break;
            }
            if (EditorGUI.EndChangeCheck()) SaveEntry(entry);
        }

        #endregion

        #region Logic & Operations

        private void HandleInput(Rect rect, List<PrefEntry> list, int index)
        {
            Event e = Event.current;
            if (e.type == EventType.MouseDown && rect.Contains(e.mousePosition))
            {
                if (e.shift && lastSelectedIndex != -1)
                {
                    int start = Mathf.Min(lastSelectedIndex, index);
                    int end = Mathf.Max(lastSelectedIndex, index);
                    for (int i = 0; i < list.Count; i++) list[i].isSelected = (i >= start && i <= end);
                }
                else if (e.control || e.command)
                {
                    list[index].isSelected = !list[index].isSelected;
                }
                else
                {
                    foreach (var item in GetActiveList()) item.isSelected = false;
                    list[index].isSelected = true;
                }
                lastSelectedIndex = index;
                e.Use();
                Repaint();
            }
        }

        private List<PrefEntry> GetActiveList() => selectedTab switch { 0 => playerPrefsList, 1 => editorPrefsList, _ => coreProPrefsList };

        private void SaveEntry(PrefEntry entry)
        {
            if (entry.isEditorPref)
            {
                if (entry.typeSelection == PrefEntry.PrefType.String) EditorPrefs.SetString(entry.key, entry.stringValue);
                else if (entry.typeSelection == PrefEntry.PrefType.Int) EditorPrefs.SetInt(entry.key, entry.intValue);
                else EditorPrefs.SetFloat(entry.key, entry.floatValue);
            }
            else
            {
                if (entry.typeSelection == PrefEntry.PrefType.String) PlayerPrefs.SetString(entry.key, entry.stringValue);
                else if (entry.typeSelection == PrefEntry.PrefType.Int) PlayerPrefs.SetInt(entry.key, entry.intValue);
                else PlayerPrefs.SetFloat(entry.key, entry.floatValue);
                PlayerPrefs.Save();
            }
        }

        private void DeleteSelected()
        {
            var active = GetActiveList();
            int count = active.Count(x => x.isSelected);
            if (!EditorUtility.DisplayDialog("Confirm Delete", $"Remove {count} selected records?", "Delete", "Cancel")) return;

            foreach (var entry in active.Where(x => x.isSelected).ToList())
            {
                if (entry.isEditorPref) EditorPrefs.DeleteKey(entry.key);
                else PlayerPrefs.DeleteKey(entry.key);
            }
            Refresh();
        }

        private void DeleteAllVisible()
        {
            var visible = GetFilteredList(GetActiveList());
            if (!EditorUtility.DisplayDialog("Confirm Delete All Visible", $"Remove ALL {visible.Count} currently visible records?", "Delete All", "Cancel")) return;

            foreach (var entry in visible)
            {
                if (entry.isEditorPref) EditorPrefs.DeleteKey(entry.key);
                else PlayerPrefs.DeleteKey(entry.key);
            }
            Refresh();
        }

        private void Refresh()
        {
            playerPrefsList.Clear();
            editorPrefsList.Clear();
            coreProPrefsList.Clear();

            foreach (var key in GetWindowsRegistryKeys(@"Software\Unity Technologies\Unity Editor 5.x"))
            {
                var entry = CreateFetchedEntry(key, true);
                editorPrefsList.Add(entry);
                if (key.StartsWith("CorePro")) coreProPrefsList.Add(entry);
            }

            string keyPath = $@"Software\Unity\UnityEditor\{PlayerSettings.companyName}\{PlayerSettings.productName}";
            foreach (var key in GetWindowsRegistryKeys(keyPath))
            {
                playerPrefsList.Add(CreateFetchedEntry(key, false));
            }
            Repaint();
        }

        private PrefEntry CreateFetchedEntry(string key, bool isEditor)
        {
            var entry = new PrefEntry { key = key, isEditorPref = isEditor };
            entry.typeSelection = PrefEntry.PrefType.String; 
            if (isEditor)
            {
                entry.stringValue = EditorPrefs.GetString(key, "");
                entry.intValue = EditorPrefs.GetInt(key, 0);
                entry.floatValue = EditorPrefs.GetFloat(key, 0f);
            }
            else
            {
                entry.stringValue = PlayerPrefs.GetString(key, "");
                entry.intValue = PlayerPrefs.GetInt(key, 0);
                entry.floatValue = PlayerPrefs.GetFloat(key, 0f);
            }
            return entry;
        }

        private string[] GetWindowsRegistryKeys(string path)
        {
            List<string> keys = new List<string>();
#if UNITY_EDITOR_WIN
            try {
                var regType = Type.GetType("Microsoft.Win32.Registry, Microsoft.Win32.Registry") ?? Type.GetType("Microsoft.Win32.Registry, mscorlib");
                object currentUser = regType.GetField("CurrentUser").GetValue(null);
                var openSubKey = currentUser.GetType().GetMethod("OpenSubKey", new[] { typeof(string) });
                var subKey = openSubKey.Invoke(currentUser, new object[] { path });
                if (subKey != null) keys.AddRange((string[])subKey.GetType().GetMethod("GetValueNames").Invoke(subKey, null));
            } catch { }
#endif
            return keys.Distinct().ToArray();
        }

        #endregion

        #region Add Entry Popup

        public class AddEntryWindow : EditorWindow
        {
            private string key = "";
            private PrefEntry.PrefType type = PrefEntry.PrefType.String;
            private bool isPlayerPref;
            private Action onComplete;

            public static void ShowWindow(bool isPlayerPref, Action callback)
            {
                var win = GetWindow<AddEntryWindow>(true, "Add New Preference");
                win.isPlayerPref = isPlayerPref;
                win.onComplete = callback;
                win.minSize = win.maxSize = new Vector2(350, 130);
                win.CenterOnMainWin();
            }

            private void OnGUI()
            {
                EditorGUILayout.Space(10);
                key = EditorGUILayout.TextField("Key Name", key);
                type = (PrefEntry.PrefType)EditorGUILayout.EnumPopup("Value Type", type);
                GUILayout.FlexibleSpace();
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Cancel")) Close();
                if (GUILayout.Button("Create Key", GUILayout.Height(30)))
                {
                    if (string.IsNullOrEmpty(key)) return;
                    if (isPlayerPref) PlayerPrefs.SetString(key, ""); else EditorPrefs.SetString(key, "");
                    onComplete?.Invoke();
                    Close();
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(5);
            }
        }

        #endregion
    }

    public static class EditorWindowExtensions
    {
        public static void CenterOnMainWin(this EditorWindow window)
        {
            var main = EditorGUIUtility.GetMainWindowPosition();
            var pos = window.position;
            window.position = new Rect(main.x + (main.width - pos.width) * 0.5f, main.y + (main.height - pos.height) * 0.5f, pos.width, pos.height);
        }
    }
}