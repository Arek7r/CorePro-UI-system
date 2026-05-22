using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace InspectorPro
{
    public static class MultiTagField
    {
        private static MultiTagPopup _popup;
        private static int _controlId = -1;

        // Max how many tags to show without scroll (rest scrollable)
        private const int MaxTagsWithoutScroll = 12; 
        
        public static List<string> Draw(Rect position, List<string> currentTags, out bool changed)
        {
            changed = false;
            int id = GUIUtility.GetControlID(FocusType.Passive);

            string[] allTags = UnityEditorInternal.InternalEditorUtility.tags;
            string display;
            
            // Display logic
            if (currentTags == null || currentTags.Count == 0)
                display = "None";
            else if (currentTags.Count == allTags.Length)
                display = "All";
            else
                display = string.Join(", ", currentTags);

            if (EditorGUI.DropdownButton(position, new GUIContent(display), FocusType.Keyboard))
            {
                _controlId = id;
                _popup = new MultiTagPopup(currentTags, allTags, id);
                PopupWindow.Show(position, _popup);
            }

            // Reading changes after closure
            if (_popup != null && _popup.DoneId == id)
            {
                changed = _popup.HasChanged;
                var newList = _popup.ResultList;
                _popup = null;
                return newList;
            }

            return currentTags;
        }

        private class MultiTagPopup : PopupWindowContent
        {
            public List<string> ResultList { get; private set; }
            public bool HasChanged { get; private set; }
            public int DoneId { get; private set; }

            private string[] _allTags;
            private List<bool> _tagChecked;
            private Vector2 _scroll;
            private int _controlId;

            public MultiTagPopup(List<string> selected, string[] allTags, int controlId)
            {
                _allTags = allTags;
                _controlId = controlId;
                
                ResultList = new List<string>(selected ?? new List<string>());
                _tagChecked = new List<bool>(_allTags.Length);
                
                // Initialize checkboxes based on selected tags
                for (int i = 0; i < _allTags.Length; i++)
                {
                    _tagChecked.Add(ResultList.Contains(_allTags[i]));
                }
            }

            public override Vector2 GetWindowSize()
            {
                float line = EditorGUIUtility.singleLineHeight + 2;
                int showTags = Mathf.Min(_allTags.Length, MaxTagsWithoutScroll);
                // header + All/None buttons + tags + apply + margins
                float h = line * (2 + showTags) + 40;
                if (_allTags.Length > MaxTagsWithoutScroll)
                    h += 4; // scroll
                h += 10; // space after
                return new Vector2(240, h);
            }

            public override void OnGUI(Rect rect)
            {
                float line = EditorGUIUtility.singleLineHeight + 2;
                EditorGUILayout.LabelField("Select Tags", EditorStyles.boldLabel);

                // "All" and "None" buttons side by side
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button("All", GUILayout.Height(20)))
                {
                    // Check all checkboxes
                    for (int i = 0; i < _tagChecked.Count; i++)
                        _tagChecked[i] = true;
                }
                
                if (GUILayout.Button("None", GUILayout.Height(20)))
                {
                    // Uncheck all checkboxes
                    for (int i = 0; i < _tagChecked.Count; i++)
                        _tagChecked[i] = false;
                }
                
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(2);

                int tagsToShow = _allTags.Length;
                bool useScroll = tagsToShow > MaxTagsWithoutScroll;
                int lines = useScroll ? MaxTagsWithoutScroll : tagsToShow;
                float scrollHeight = line * lines;

                if (useScroll)
                    _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.Height(scrollHeight));

                for (int i = 0; i < _allTags.Length; i++)
                {
                    bool prev = _tagChecked[i];
                    bool now = EditorGUILayout.ToggleLeft(_allTags[i], prev);
                    
                    if (now != prev)
                    {
                        _tagChecked[i] = now;
                    }
                }

                if (useScroll)
                    EditorGUILayout.EndScrollView();

                GUILayout.Space(6);
                if (GUILayout.Button("Apply", GUILayout.Height(26)))
                {
                    // Build result list based on checked tags
                    ResultList.Clear();
                    
                    for (int i = 0; i < _allTags.Length; i++)
                    {
                        if (_tagChecked[i])
                        {
                            ResultList.Add(_allTags[i]);
                        }
                    }
                    
                    HasChanged = true;
                    DoneId = _controlId;
                    editorWindow.Close();
                }
            }
        }
    }
}