#if UNITY_EDITOR
using CorePro.Editor.Framework;
using UnityEditor;
using UnityEngine;

namespace CorePro.UI.Editor
{
    [CustomEditor(typeof(UIPanelManager))]
    public class UIPanelManagerEditor : CoreProEditor
    {
        // == SerializedProperties ==

        // Panels
        SerializedProperty _panels;

        // Settings
        SerializedProperty _defaultPanelIndex;
        SerializedProperty _cullInactivePanels;

        // Back Button
        SerializedProperty _globalBackButton;

        // Back Stack
        SerializedProperty _enableBackStack;
        SerializedProperty _maxHistoryDepth;

        // Events
        SerializedProperty _onPanelOpened;
        SerializedProperty _onPanelClosed;
        SerializedProperty _onPanelOpenedByName;

        // == Foldouts (only Back Stack and Events) ==

        bool _foldBackStack;
        bool _foldEvents;

        // == Lifecycle ==

        protected override void OnEnable()
        {
            base.OnEnable();

            _panels             = serializedObject.FindProperty("panels");
            _defaultPanelIndex  = serializedObject.FindProperty("defaultPanelIndex");
            _cullInactivePanels = serializedObject.FindProperty("cullInactivePanels");
            _globalBackButton   = serializedObject.FindProperty("globalBackButton");
            _enableBackStack    = serializedObject.FindProperty("enableBackStack");
            _maxHistoryDepth    = serializedObject.FindProperty("maxHistoryDepth");
            _onPanelOpened      = serializedObject.FindProperty("onPanelOpened");
            _onPanelClosed      = serializedObject.FindProperty("onPanelClosed");
            _onPanelOpenedByName = serializedObject.FindProperty("onPanelOpenedByName");

            _foldBackStack = LoadFoldout("BackStack", true);
            _foldEvents    = LoadFoldout("Events",    false);
        }

        // == Main Draw ==

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            UIPanelManager manager = (UIPanelManager)target;

            DrawHeader("Panels",      DrawPanels);
            DrawHeader("Settings",    DrawSettings);
            DrawHeader("Back Button", DrawBackButton);

            DrawSection("Back Stack", ref _foldBackStack, "BackStack", DrawBackStack);
            DrawSection("Events",     ref _foldEvents,    "Events",    DrawEvents);

            if (Application.isPlaying)
                DrawRuntimeControls(manager);

            serializedObject.ApplyModifiedProperties();
        }

        // == Section Bodies ==

        void DrawPanels()
        {
            EditorGUILayout.PropertyField(_panels, new GUIContent("Panel List"), true);

            int count = _panels.arraySize;
            if (count == 0)
                EditorGUILayout.HelpBox("No panels assigned. Add at least one PanelEntry.", MessageType.Warning);
        }

        void DrawSettings()
        {
            int count = _panels.arraySize;

            EditorGUI.BeginChangeCheck();
            int newDefault = EditorGUILayout.IntSlider(
                new GUIContent("Default Panel Index", "Index of the panel shown on Awake. -1 = none."),
                _defaultPanelIndex.intValue, -1, Mathf.Max(0, count - 1));
            if (EditorGUI.EndChangeCheck())
                _defaultPanelIndex.intValue = newDefault;

            // Show name of the default panel as a hint
            if (newDefault >= 0 && newDefault < count)
            {
                var entry = _panels.GetArrayElementAtIndex(newDefault);
                var nameP = entry.FindPropertyRelative("panelName");
                if (nameP != null)
                    EditorGUILayout.HelpBox($"Default panel: \"{nameP.stringValue}\"", MessageType.None);
            }
            else if (newDefault == -1)
            {
                EditorGUILayout.HelpBox("No panel will open on Awake.", MessageType.None);
            }

            EditorGUILayout.PropertyField(_cullInactivePanels, new GUIContent("Cull Inactive Panels",
                "Calls HideImmediate on all panels except the active one at startup."));
        }

        void DrawBackButton()
        {
            EditorGUILayout.PropertyField(_globalBackButton, new GUIContent("Global Back Button",
                "ButtonPro shown on panels where EnableBackButton is true. Hidden automatically when back stack is empty."));

            if (_globalBackButton.objectReferenceValue == null)
                EditorGUILayout.HelpBox("No Global Back Button assigned. Back button functionality will be unavailable.", MessageType.Info);
        }

        void DrawBackStack()
        {
            EditorGUILayout.PropertyField(_enableBackStack, new GUIContent("Enable Back Stack",
                "Keeps a history of opened panels so the Back() method can return to the previous one."));

            if (_enableBackStack.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_maxHistoryDepth, new GUIContent("Max History Depth",
                    "Maximum number of entries in the back stack. Oldest entries are discarded when the limit is reached."));
                EditorGUI.indentLevel--;
            }
        }

        void DrawEvents()
        {
            EditorGUILayout.PropertyField(_onPanelOpened,       new GUIContent("On Panel Opened (int)"));
            EditorGUILayout.PropertyField(_onPanelClosed,       new GUIContent("On Panel Closed (int)"));
            EditorGUILayout.PropertyField(_onPanelOpenedByName, new GUIContent("On Panel Opened By Name (string)"));
        }

        // == Runtime Controls ==

        void DrawRuntimeControls(UIPanelManager manager)
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Runtime Controls", EditorStyles.boldLabel);

            // State readout
            string currentName = manager.CurrentIndex >= 0 && manager.CurrentIndex < _panels.arraySize
                ? _panels.GetArrayElementAtIndex(manager.CurrentIndex)
                         .FindPropertyRelative("panelName")?.stringValue ?? manager.CurrentIndex.ToString()
                : "none";

            EditorGUILayout.LabelField(
                $"Current: [{manager.CurrentIndex}] \"{currentName}\"   Can Go Back: {manager.CanGoBack}",
                EditorStyles.centeredGreyMiniLabel);

            EditorGUILayout.Space(2);

            // Navigation buttons
            EditorGUILayout.BeginHorizontal();

            using (new EditorGUI.DisabledScope(!manager.CanGoBack))
                if (GUILayout.Button("Back"))
                    manager.Back();

            using (new EditorGUI.DisabledScope(manager.CurrentIndex <= 0))
                if (GUILayout.Button("Previous"))
                    manager.Previous();

            using (new EditorGUI.DisabledScope(manager.CurrentIndex >= _panels.arraySize - 1))
                if (GUILayout.Button("Next"))
                    manager.Next();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Close Current"))
                manager.CloseCurrentPanel();

            if (GUILayout.Button("Clear History"))
                manager.ClearHistory();

            EditorGUILayout.EndHorizontal();

            // Open panel by index buttons
            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("Open Panel by Index", EditorStyles.miniLabel);

            int cols     = 5;
            int panelCount = _panels.arraySize;

            for (int row = 0; row * cols < panelCount; row++)
            {
                EditorGUILayout.BeginHorizontal();
                for (int col = 0; col < cols; col++)
                {
                    int idx = row * cols + col;
                    if (idx >= panelCount) break;

                    bool isCurrent = idx == manager.CurrentIndex;
                    var  nameP     = _panels.GetArrayElementAtIndex(idx).FindPropertyRelative("panelName");
                    string btnLabel = nameP != null ? nameP.stringValue : idx.ToString();

                    using (new EditorGUI.DisabledScope(isCurrent))
                        if (GUILayout.Button(btnLabel, GUILayout.MinWidth(40)))
                            manager.OpenPanel(idx);
                }
                EditorGUILayout.EndHorizontal();
            }
        }
    }
}
#endif
