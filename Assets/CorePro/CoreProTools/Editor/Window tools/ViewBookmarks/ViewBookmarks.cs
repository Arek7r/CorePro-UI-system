using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

namespace CorePro.Editor
{
    public class ViewBookmarksWindow : EditorWindow
    {
        private ViewBookmarksData _data;
        private Vector2 _scrollPos;
        private string _newPointName = "New Bookmark";

        // Separate folder and file name for clarity and logic
        private const string FolderPath = "Assets/CorePro/CoreTools/Editor/ViewBookmarks";
        private const string AssetName = "ViewBookmarksData.asset";
        private string FullAssetPath => $"{FolderPath}/{AssetName}";

        [MenuItem("Window/CorePro/View Bookmarks")]
        public static void ShowWindow()
        {
            ViewBookmarksWindow window = GetWindow<ViewBookmarksWindow>("View Bookmarks");
            window.minSize = new Vector2(250, 300);
        }

        private void OnEnable()
        {
            LoadOrCreateData();
        }

        private void LoadOrCreateData()
        {
            // 1. Try to load the actual file, not the folder
            _data = AssetDatabase.LoadAssetAtPath<ViewBookmarksData>(FullAssetPath);

            if (_data == null)
            {
                // 2. Ensure folder structure exists
                EnsureFoldersExist();

                // 3. Create instance and save it
                _data = CreateInstance<ViewBookmarksData>();
                AssetDatabase.CreateAsset(_data, FullAssetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log($"<color=green>Success:</color> Data asset created at {FullAssetPath}");
            }
        }

        private void EnsureFoldersExist()
        {
            string[] folders = FolderPath.Split('/');
            string currentPath = folders[0]; // "Assets"

            for (int i = 1; i < folders.Length; i++)
            {
                string nextPath = $"{currentPath}/{folders[i]}";
                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    AssetDatabase.CreateFolder(currentPath, folders[i]);
                }
                currentPath = nextPath;
            }
        }

        private void OnGUI()
        {
            if (_data == null)
            {
                if (GUILayout.Button("Retry Load Data")) LoadOrCreateData();
                return;
            }

            DrawHeader();
            EditorGUILayout.Space(5);

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            DrawListView();
            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Slot Name");
            _newPointName = EditorGUILayout.TextField( _newPointName);

            GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button("Save view state", GUILayout.Height(30)))
            {
                CaptureCurrentView();
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndVertical();
        }

        private void DrawListView()
        {
            if (_data.Views == null) return;

            for (int i = 0; i < _data.Views.Count; i++)
            {
                ViewPoint vp = _data.Views[i];
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

                if (GUILayout.Button(vp.Name, GUILayout.ExpandWidth(true)))
                {
                    ApplyView(vp);
                }

                GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    DeleteView(i);
                }
                GUI.backgroundColor = Color.white;

                EditorGUILayout.EndHorizontal();
            }
        }

        private void CaptureCurrentView()
        {
            SceneView view = SceneView.lastActiveSceneView;
            if (view == null) return;

            Undo.RecordObject(_data, "Capture View");

            _data.Views.Add(new ViewPoint
            {
                Name = string.IsNullOrEmpty(_newPointName) ? $"View {_data.Views.Count}" : _newPointName,
                Pivot = view.pivot,
                Rotation = view.rotation,
                Size = view.size
            });

            EditorUtility.SetDirty(_data);
            AssetDatabase.SaveAssets();
        }

        private void ApplyView(ViewPoint vp)
        {
            SceneView view = SceneView.lastActiveSceneView;
            if (view == null) return;

            Undo.RecordObject(view, "Apply View Bookmark");

            view.pivot = vp.Pivot;
            view.size = vp.Size;

            // Fix for 2D mode error: SceneView rotation is identity-only in 2D
            if (!view.in2DMode)
            {
                view.rotation = vp.Rotation;
            }
            else
            {
                Debug.LogWarning("View Bookmarks: Cannot apply rotation while SceneView is in 2D Mode.");
            }

            view.Repaint();
        }

        private void DeleteView(int index)
        {
            Undo.RecordObject(_data, "Delete View View");
            _data.Views.RemoveAt(index);
            EditorUtility.SetDirty(_data);
            AssetDatabase.SaveAssets();
        }
    }
}