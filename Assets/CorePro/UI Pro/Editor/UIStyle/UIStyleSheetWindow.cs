#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace CorePro.UI.Editor
{
    public class UIStyleSheetWindow : EditorWindow
    {
        [SerializeField] private UIStyleSheet _sheet;
        private UnityEditor.Editor _editor;
        private Vector2 _scroll;

        [MenuItem("Tools/CorePro/UI Style Sheet")]
        public static void Open()
        {
            var win = GetWindow<UIStyleSheetWindow>("UI Style Sheet");
            win.minSize = new Vector2(420, 300);
            win.Show();
        }

        private void OnEnable()
        {
            TryAutoFind();
            RebuildEditor();
            Repaint();
        }

        private void OnDisable() => DestroyEditor();

        private void TryAutoFind()
        {
            if (_sheet != null) return;

            var guids = AssetDatabase.FindAssets("t:UIStyleSheet");
            if (guids.Length == 0) return;

            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            _sheet = AssetDatabase.LoadAssetAtPath<UIStyleSheet>(path);
        }

        private void RebuildEditor()
        {
            DestroyEditor();
            if (_sheet != null)
                _editor = UnityEditor.Editor.CreateEditor(_sheet);
        }

        private void DestroyEditor()
        {
            if (_editor == null) return;
            DestroyImmediate(_editor);
            _editor = null;
        }

        private void OnGUI()
        {
            DrawHeader();

            if (_sheet == null)
            {
                EditorGUILayout.Space(8);
                EditorGUILayout.HelpBox(
                    "No UIStyleSheet found in the project.\nAssign one above or click Find.",
                    MessageType.Info);
                return;
            }

            if (_editor == null) RebuildEditor();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            _editor?.OnInspectorGUI();
            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space(4);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginChangeCheck();
                var next = (UIStyleSheet)EditorGUILayout.ObjectField(
                    "Style Sheet", _sheet, typeof(UIStyleSheet), allowSceneObjects: false);

                if (EditorGUI.EndChangeCheck())
                {
                    _sheet = next;
                    RebuildEditor();
                }

                if (GUILayout.Button("Find", GUILayout.Width(50)))
                {
                    _sheet = null;
                    TryAutoFind();
                    RebuildEditor();
                }
            }

            EditorGUILayout.Space(2);
            EditorGUI.DrawRect(
                EditorGUILayout.GetControlRect(false, 1),
                new Color(0f, 0f, 0f, 0.25f));
            EditorGUILayout.Space(2);
        }
    }
}
#endif