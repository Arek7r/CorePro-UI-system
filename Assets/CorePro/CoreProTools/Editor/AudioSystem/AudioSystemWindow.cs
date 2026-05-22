#if UNITY_EDITOR
using System.Collections.Generic;
using CorePro.Editor.Framework;
using UnityEditor;
using UnityEngine;

namespace CorePro.AudioSystem.Editor
{
    public sealed class AudioSystemWindow : EditorWindow
    {
        // Menu item
        [MenuItem("Tools/CorePro/Audio Library Editor", priority = 200)]
        public static void OpenWindow()
        {
            var win = GetWindow<AudioSystemWindow>("Audio Library");
            win.minSize = new Vector2(480f, 400f);
            win.Show();
        }

        /// <summary>Opens the window focused on a specific library.</summary>
        internal static void OpenWithLibrary(AudioLibrary lib)
        {
            var win = GetWindow<AudioSystemWindow>("Audio Library");
            win._selectedLibrary = lib;
            win.minSize = new Vector2(480f, 400f);
            win.Show();
        }

        // State 
        private CoreProEditorTheme _theme;

        // Tabs
        private enum Tab { SFX, Music, Buses, Settings }
        private Tab _currentTab = Tab.SFX;

        // Selected library
        private AudioLibrary   _selectedLibrary;
        private SerializedObject _serializedLibrary;

        // Scroll positions per tab
        private Vector2 _sfxScroll;
        private Vector2 _musicScroll;
        private Vector2 _busScroll;
        private Vector2 _settingsScroll;

        // Expanded entry indices (for inline variation editors)
        private readonly HashSet<int> _expandedSFX   = new HashSet<int>();
        private readonly HashSet<int> _expandedMusic = new HashSet<int>();
        private readonly HashSet<int> _expandedBuses = new HashSet<int>();

        // Preview state 
        // Tracks which SFX index is currently being previewed (Edit Mode = AudioUtil,
        // Play Mode = CoreAudio.Play). -1 means nothing is playing.
        private int  _previewingIndex = -1;
        private bool _previewIsPlayMode;

        // GUIStyles (lazy) 
        private GUIStyle _tabStyle;
        private GUIStyle _tabActiveStyle;
        private GUIStyle _headerLabelStyle;
        private GUIStyle _entryNameStyle;
        private GUIContent    _deleteIcon;
        private bool     _stylesInitialized;
        
        // Lifecycle 
        private void OnEnable()
        {
            _theme = CoreProEditorTheme.Load();
            if (_selectedLibrary == null)
                AutoSelectLibrary();
            RebuildLists();
        }

        private void OnDisable()
        {
            ApplyAndSave();
        }

        private void OnGUI()
        {
            EnsureStyles();

            // Library picker row
            DrawLibraryPicker();
            EditorGUILayout.Space(4);

            if (_selectedLibrary == null)
            {
                EditorGUILayout.HelpBox(
                    "No Audio Library selected. Create one via Assets > Create > CorePro > Audio Library.",
                    MessageType.Info);
                return;
            }

            // Sync serialized object
            if (_serializedLibrary == null || _serializedLibrary.targetObject != _selectedLibrary)
            {
                _serializedLibrary = new SerializedObject(_selectedLibrary);
                RebuildLists();
            }

            _serializedLibrary.Update();

            // Tab bar
            DrawTabBar();
            EditorGUILayout.Space(2);

            // Tab content
            switch (_currentTab)
            {
                case Tab.SFX:      DrawSFXTab();      break;
                case Tab.Music:    DrawMusicTab();     break;
                case Tab.Buses:    DrawBusesTab();     break;
                case Tab.Settings: DrawSettingsTab();  break;
            }

            // Runtime bar - always at bottom, only in Play Mode
            if (Application.isPlaying)
                DrawRuntimeBar();

            if (_serializedLibrary.ApplyModifiedProperties())
                EditorUtility.SetDirty(_selectedLibrary);

            // Keep repainting in Play Mode so the ▶/■ button stays in sync
            if (Application.isPlaying)
                Repaint();
        }

        // Library picker 
        private void DrawLibraryPicker()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField("Library", GUILayout.Width(52));

            var newLib = (AudioLibrary)EditorGUILayout.ObjectField(
                _selectedLibrary, typeof(AudioLibrary), false);

            if (newLib != _selectedLibrary)
            {
                ApplyAndSave();
                _selectedLibrary   = newLib;
                _serializedLibrary = null;
                _expandedSFX.Clear();
                _expandedMusic.Clear();
                _expandedBuses.Clear();
            }

            if (GUILayout.Button("Regenerate Constants", EditorStyles.toolbarButton, GUILayout.Width(160)))
                AudioConstantsGenerator.RegenerateAll();

            EditorGUILayout.EndHorizontal();
        }

        // Tab bar 
        private static readonly string[] TabLabels = { "⬡  SFX", "♪  Music", "⚡  Buses", "⚙  Settings" };

        private void DrawTabBar()
        {
            EditorGUILayout.BeginHorizontal();

            for (int i = 0; i < TabLabels.Length; i++)
            {
                Tab t        = (Tab)i;
                bool active  = _currentTab == t;
                var  style   = active ? _tabActiveStyle : _tabStyle;

                if (GUILayout.Button(TabLabels[i], style, GUILayout.Height(26)))
                    _currentTab = t;
            }

            EditorGUILayout.EndHorizontal();
        }

        // =========================================================================
        // SFX Tab
        // =========================================================================
        private void DrawSFXTab()
        {
            DrawSectionHeader("Sound Effects");

            // Toolbar
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button("Add SFX", EditorStyles.toolbarButton, GUILayout.Width(72)))
                AddSFXEntry();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            // Drag-drop zone
            DrawDragDropZone("Drag AudioClips here to add SFX entries", HandleSFXDrop);

            _sfxScroll = EditorGUILayout.BeginScrollView(_sfxScroll);

            var sfxProp = _serializedLibrary.FindProperty("sfx");
            for (int i = 0; i < sfxProp.arraySize; i++)
                DrawSFXEntryRow(sfxProp, i);

            EditorGUILayout.EndScrollView();
        }

        private void DrawSFXEntryRow(SerializedProperty sfxArray, int index)
        {
            var entryProp = sfxArray.GetArrayElementAtIndex(index);
            var nameProp  = entryProp.FindPropertyRelative("name");
            var busProp   = entryProp.FindPropertyRelative("busName");
            var muteProp  = entryProp.FindPropertyRelative("muted");
            var varsProp  = entryProp.FindPropertyRelative("variations");

            bool expanded = _expandedSFX.Contains(index);

            // Header row 
            Rect headerRect = GUILayoutUtility.GetRect(
                GUIContent.none, GUIStyle.none,
                GUILayout.Height(_theme.headerHeight), GUILayout.ExpandWidth(true));

            float leftPad  = headerRect.x;
            headerRect.x   = 0f;
            headerRect.width = EditorGUIUtility.currentViewWidth;

            bool hover = headerRect.Contains(Event.current.mousePosition);
            EditorGUI.DrawRect(headerRect, _theme.HeaderBg(hover));
            EditorGUI.DrawRect(new Rect(headerRect.x, headerRect.yMax - 1f, headerRect.width, 1f), _theme.HeaderBorder());

            if (expanded)
                EditorGUI.DrawRect(new Rect(headerRect.x, headerRect.y, _theme.accentWidth, headerRect.height), _theme.accentColor);

            // Right-anchored layout - reserve fixed-width controls first,
            // then give the name field whatever space remains. This prevents
            // buttons from being clipped when the window is narrow.

            // Fixed widths (right → left order of reservation)
            const float kRemoveW  = 24f;
            const float kPlayW    = 26f;
            const float kMuteW    = 24f;
            const float kBadgeW   = 44f;
            const float kBusW     = 80f;
            const float kBusLblW  = 28f;
            const float kArrowW   = 18f;
            const float kPad      = 4f;
            const float kBtnH     = 18f;
            float       btnTop    = headerRect.y + (headerRect.height - kBtnH) * 0.5f;

            // Total fixed width needed to the right of the name field
            float fixedRight = kBusLblW + kBusW + kPad + kBadgeW + kPad
                             + kMuteW + kPad + kPlayW + kPad + kRemoveW + kPad;

            // Left: accent + arrow + small gap
            float xLeft  = leftPad + _theme.accentWidth + 4f;
            float xArrow = xLeft;
            float xName  = xArrow + kArrowW + 2f;

            // Name field gets everything between arrow and the right-anchored block
            const float kNameMaxW = 180f;
            float nameW  = Mathf.Min(headerRect.width - xName - fixedRight, kNameMaxW);
            nameW        = Mathf.Max(nameW, 60f);

            // Arrow
            string arrow = expanded ? "▾" : "▸";
            GUI.Label(new Rect(xArrow, headerRect.y, kArrowW, headerRect.height), arrow, _headerLabelStyle);

            // Name
            nameProp.stringValue = EditorGUI.TextField(
                new Rect(xName, headerRect.y + 4f, nameW, headerRect.height - 8f),
                nameProp.stringValue, _entryNameStyle);

            // Right-anchored controls (placed left→right in their reserved block)
            float rx = xName + nameW + kPad;

            // Bus label
            GUI.Label(new Rect(rx, headerRect.y, kBusLblW, headerRect.height), "Bus", _headerLabelStyle);
            rx += kBusLblW;

            // Bus dropdown
            DrawBusDropdown(new Rect(rx, btnTop, kBusW, kBtnH), busProp);
            rx += kBusW + kPad;

            // Var count badge
            string badge = $"{varsProp.arraySize} var";
            GUI.Label(new Rect(rx, headerRect.y, kBadgeW, headerRect.height), badge, _headerLabelStyle);
            rx += kBadgeW + kPad;

            // Mute toggle
            muteProp.boolValue = GUI.Toggle(
                new Rect(rx, btnTop, kMuteW, kBtnH),
                muteProp.boolValue, "M", "button");
            rx += kMuteW + kPad;

            // ▶ / ■ Play-Stop button
            bool thisIsPlaying = _previewingIndex == index;
            string playLabel   = thisIsPlaying ? "■" : "▶";
            if (GUI.Button(new Rect(rx, btnTop, kPlayW, kBtnH), playLabel))
            {
                if (thisIsPlaying) StopPreview();
                else               PreviewSFX(index);
            }
            rx += kPlayW + kPad;

            // Remove button
            // Remove button
            if (GUI.Button(new Rect(rx, btnTop, kRemoveW, kBtnH), _deleteIcon))
            {
                sfxArray.DeleteArrayElementAtIndex(index);
                _expandedSFX.Remove(index);
                return;
            }

            // Toggle expand on row click
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0
                && headerRect.Contains(Event.current.mousePosition))
            {
                if (expanded) _expandedSFX.Remove(index);
                else          _expandedSFX.Add(index);
                Event.current.Use();
                Repaint();
            }

            if (hover && Event.current.type == EventType.MouseMove) Repaint();

            if (!expanded) 
                return;

            // Expanded body 
            EditorGUI.indentLevel++;
            EditorGUILayout.Space(4);

            DrawSFXEntryBody(entryProp);

            EditorGUI.indentLevel--;
            EditorGUILayout.Space(2);
        }

        private void DrawSFXEntryBody(SerializedProperty entry)
        {
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("masterVolume"), new GUIContent("Master Volume"));
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("retriggerMode"), new GUIContent("Retrigger Mode"));

            EditorGUILayout.Space(4);
            DrawSubHeader("3D Settings");
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("spatialBlend"),  new GUIContent("Spatial Blend"));
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("stereoPan"),     new GUIContent("Stereo Pan"));
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("dopplerLevel"),  new GUIContent("Doppler Level"));
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("spread"),        new GUIContent("Spread"));
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("attenuationMode"));
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("minDistance"),   new GUIContent("Min Distance"));
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("maxDistance"),   new GUIContent("Max Distance"));
            EditorGUI.indentLevel--;

            EditorGUILayout.Space(4);
            DrawSubHeader("Variations");
            DrawVariationsList(entry.FindPropertyRelative("variations"));
        }

        private void DrawVariationsList(SerializedProperty varsProp)
{
    for (int i = 0; i < varsProp.arraySize; i++)
    {
        var v = varsProp.GetArrayElementAtIndex(i);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"Variation {i + 1}", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("−", GUILayout.Width(22)))
        {
            varsProp.DeleteArrayElementAtIndex(i);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            return;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.PropertyField(v.FindPropertyRelative("clip"), new GUIContent("Clip"));
        GUILayout.Space(4);

        DrawRandomRangeCheckbox(v, "randomizeVolume", "Volume");
        DrawRandomRangeCheckbox(v, "randomizePitch",  "Pitch");
        DrawRandomRangeCheckbox(v, "randomizeDelay",  "Delay");

        GUILayout.Space(4);

        DrawRandomRangeSlider(v, "fixedVolume", "randomizeVolume", "randomVolumeRange", "Volume", 0f,  1f);
        DrawRandomRangeSlider(v, "fixedPitch",  "randomizePitch",  "randomPitchRange",  "Pitch",  -3f, 3f);
        DrawRandomRangeSlider(v, "fixedDelay",  "randomizeDelay",  "randomDelayRange",  "Delay",  0f,  10f);

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(2);
    }

    if (GUILayout.Button("Add Variation", GUILayout.ExpandWidth(false)))
        varsProp.InsertArrayElementAtIndex(varsProp.arraySize);
}

private static void DrawRandomRangeCheckbox(SerializedProperty parent, string randomizeProp, string label)
{
    var randomize = parent.FindPropertyRelative(randomizeProp);
    EditorGUILayout.PropertyField(randomize, new GUIContent($"Randomize {label}"));
}

private static void DrawRandomRangeSlider(SerializedProperty parent, string fixedProp, string randomizeProp, string rangeProp, string label, float min, float max)
{
    var fixedP    = parent.FindPropertyRelative(fixedProp);
    var randomize = parent.FindPropertyRelative(randomizeProp);
    var range     = parent.FindPropertyRelative(rangeProp);

    if (randomize.boolValue)
    {
        var v = range.vector2Value;

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.MinMaxSlider(label, ref v.x, ref v.y, min, max);
        v.x = EditorGUILayout.FloatField(v.x, GUILayout.Width(60));
        v.y = EditorGUILayout.FloatField(v.y, GUILayout.Width(60));
        EditorGUILayout.EndHorizontal();

        range.vector2Value = v;
    }
    else
    {
        fixedP.floatValue = EditorGUILayout.Slider(label, fixedP.floatValue, min, max);
    }
}

        // =========================================================================
        // Music Tab
        // =========================================================================

        private void DrawMusicTab()
        {
            DrawSectionHeader("Music Players");

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button("Add Music Player", EditorStyles.toolbarButton, GUILayout.Width(130)))
                AddMusicEntry();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            DrawDragDropZone("Drag AudioClips here to add to a new music player", HandleMusicDrop);

            _musicScroll = EditorGUILayout.BeginScrollView(_musicScroll);

            var musicProp = _serializedLibrary.FindProperty("music");
            for (int i = 0; i < musicProp.arraySize; i++)
                DrawMusicEntryRow(musicProp, i);

            EditorGUILayout.EndScrollView();
        }

        private void DrawMusicEntryRow(SerializedProperty musicArray, int index)
        {
            var entry     = musicArray.GetArrayElementAtIndex(index);
            var nameProp  = entry.FindPropertyRelative("name");
            var muteProp  = entry.FindPropertyRelative("muted");
            var tracksProp = entry.FindPropertyRelative("tracks");
            bool expanded = _expandedMusic.Contains(index);

            Rect headerRect = GUILayoutUtility.GetRect(
                GUIContent.none, GUIStyle.none,
                GUILayout.Height(_theme.headerHeight), GUILayout.ExpandWidth(true));

            float leftPad = headerRect.x;
            headerRect.x  = 0f;
            headerRect.width = EditorGUIUtility.currentViewWidth;

            bool hover = headerRect.Contains(Event.current.mousePosition);
            EditorGUI.DrawRect(headerRect, _theme.HeaderBg(hover));
            EditorGUI.DrawRect(new Rect(headerRect.x, headerRect.yMax - 1f, headerRect.width, 1f), _theme.HeaderBorder());
            if (expanded)
                EditorGUI.DrawRect(new Rect(headerRect.x, headerRect.y, _theme.accentWidth, headerRect.height), _theme.accentColor);

            // Right-anchored layout - consistent with SFX and Bus rows
            const float mBtnH     = 18f;
            const float mPad      = 4f;
            const float mArrowW   = 18f;
            const float mNameMaxW = 180f;
            const float mBadgeW   = 56f;
            const float mMuteW    = 24f;
            const float mRemoveW  = 24f;
            float       mBtnTop   = headerRect.y + (headerRect.height - mBtnH) * 0.5f;

            float mFixedRight = mBadgeW + mPad + mMuteW + mPad + mRemoveW + mPad;

            float mxArrow = leftPad + _theme.accentWidth + 4f;
            float mxName  = mxArrow + mArrowW + 2f;
            float mNameW  = Mathf.Min(headerRect.width - mxName - mFixedRight, mNameMaxW);
            mNameW        = Mathf.Max(mNameW, 60f);

            string arrow = expanded ? "▾" : "▸";
            GUI.Label(new Rect(mxArrow, headerRect.y, mArrowW, headerRect.height), arrow, _headerLabelStyle);

            nameProp.stringValue = EditorGUI.TextField(
                new Rect(mxName, headerRect.y + 4f, mNameW, headerRect.height - 8f),
                nameProp.stringValue, _entryNameStyle);

            float mrx = mxName + mNameW + mPad;

            // Track count badge
            string badge = $"{tracksProp.arraySize} track{(tracksProp.arraySize != 1 ? "s" : "")}";
            GUI.Label(new Rect(mrx, headerRect.y, mBadgeW, headerRect.height), badge, _headerLabelStyle);
            mrx += mBadgeW + mPad;

            // Mute toggle
            muteProp.boolValue = GUI.Toggle(
                new Rect(mrx, mBtnTop, mMuteW, mBtnH),
                muteProp.boolValue,
                new GUIContent("M", "Mute - silence this player without changing its volume"), "button");
            mrx += mMuteW + mPad;

            // Remove button
            if (GUI.Button(new Rect(mrx, mBtnTop, mRemoveW, mBtnH), _deleteIcon))
            {
                musicArray.DeleteArrayElementAtIndex(index);
                _expandedMusic.Remove(index);
                return;
            }

            if (Event.current.type == EventType.MouseDown && Event.current.button == 0
                && headerRect.Contains(Event.current.mousePosition))
            {
                if (expanded) _expandedMusic.Remove(index);
                else          _expandedMusic.Add(index);
                Event.current.Use();
                Repaint();
            }

            if (hover && Event.current.type == EventType.MouseMove) Repaint();
            if (!expanded) 
                return;

            EditorGUI.indentLevel++;
            EditorGUILayout.Space(4);

            EditorGUILayout.PropertyField(entry.FindPropertyRelative("mixerGroup"),   new GUIContent("Output Mixer"));
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("volume"),        new GUIContent("Volume"));
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("fadeDuration"),  new GUIContent("Fade Duration (s)"));
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("loop"),          new GUIContent("Loop"));
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("shuffle"),       new GUIContent("Shuffle"));
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("playOnStart"),   new GUIContent("Play On Start"));
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("playbackMode"),  new GUIContent("Playback Mode"));

            EditorGUILayout.Space(4);
            DrawSubHeader("Tracks");
            DrawTracksList(tracksProp);

            EditorGUI.indentLevel--;
            EditorGUILayout.Space(2);
        }

        private void DrawTracksList(SerializedProperty tracksProp)
        {
            for (int i = 0; i < tracksProp.arraySize; i++)
            {
                var t = tracksProp.GetArrayElementAtIndex(i);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(t.FindPropertyRelative("clip"),   GUIContent.none);
                EditorGUILayout.PropertyField(t.FindPropertyRelative("volume"), GUIContent.none, GUILayout.Width(80));
                if (GUILayout.Button(_deleteIcon, GUILayout.Width(22)))
                {
                    tracksProp.DeleteArrayElementAtIndex(i);
                    EditorGUILayout.EndHorizontal();
                    return;
                }
                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button("Add Track", GUILayout.ExpandWidth(false)))
                tracksProp.InsertArrayElementAtIndex(tracksProp.arraySize);
        }

        // =========================================================================
        // Buses Tab
        // =========================================================================

        private void DrawBusesTab()
        {
            DrawSectionHeader("Audio Buses");

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button("Add Bus", EditorStyles.toolbarButton, GUILayout.Width(72)))
                AddBus();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            _busScroll = EditorGUILayout.BeginScrollView(_busScroll);

            var busesProp = _serializedLibrary.FindProperty("buses");
            for (int i = 0; i < busesProp.arraySize; i++)
                DrawBusRow(busesProp, i);

            EditorGUILayout.EndScrollView();
        }

        private void DrawBusRow(SerializedProperty busesArray, int index)
        {
            var bus      = busesArray.GetArrayElementAtIndex(index);
            var nameProp = bus.FindPropertyRelative("name");
            bool isMaster = index == 0;
            bool expanded = _expandedBuses.Contains(index);

            Rect headerRect = GUILayoutUtility.GetRect(
                GUIContent.none, GUIStyle.none,
                GUILayout.Height(_theme.headerHeight), GUILayout.ExpandWidth(true));

            float leftPad = headerRect.x;
            headerRect.x  = 0f;
            headerRect.width = EditorGUIUtility.currentViewWidth;

            bool hover = headerRect.Contains(Event.current.mousePosition);
            EditorGUI.DrawRect(headerRect, _theme.HeaderBg(hover));
            EditorGUI.DrawRect(new Rect(headerRect.x, headerRect.yMax - 1f, headerRect.width, 1f), _theme.HeaderBorder());
            if (expanded)
                EditorGUI.DrawRect(new Rect(headerRect.x, headerRect.y, _theme.accentWidth, headerRect.height), _theme.accentColor);

            float x = leftPad + _theme.accentWidth + 6f;
            string arrow = expanded ? "▾" : "▸";
            GUI.Label(new Rect(x, headerRect.y, 18f, headerRect.height), arrow, _headerLabelStyle);
            x += 20f;

            float nameW = Mathf.Max(headerRect.width - x - 160f, 80f);
            using (new EditorGUI.DisabledScope(isMaster))
                nameProp.stringValue = EditorGUI.TextField(
                    new Rect(x, headerRect.y + 4f, nameW, headerRect.height - 8f),
                    nameProp.stringValue, _entryNameStyle);
            x += nameW + 4f;

            var volProp = bus.FindPropertyRelative("volume");
            volProp.floatValue = EditorGUI.Slider(
                new Rect(x, headerRect.y + 6f, 90f, headerRect.height - 12f),
                volProp.floatValue, 0f, 1f);
            x += 94f;

            var muteProp  = bus.FindPropertyRelative("muted");
            var soloProp  = bus.FindPropertyRelative("soloed");
            muteProp.boolValue = GUI.Toggle(new Rect(x,      headerRect.y + 5f, 22f, 16f), muteProp.boolValue, "M", "button");
            soloProp.boolValue = GUI.Toggle(new Rect(x + 24f, headerRect.y + 5f, 22f, 16f), soloProp.boolValue, "S", "button");
            x += 50f;

            if (!isMaster && GUI.Button(new Rect(x, headerRect.y + 4f, 22f, headerRect.height - 8f), _deleteIcon))
            {
                busesArray.DeleteArrayElementAtIndex(index);
                _expandedBuses.Remove(index);
                return;
            }

            if (Event.current.type == EventType.MouseDown && Event.current.button == 0
                && headerRect.Contains(Event.current.mousePosition))
            {
                if (expanded) _expandedBuses.Remove(index);
                else          _expandedBuses.Add(index);
                Event.current.Use();
                Repaint();
            }

            if (hover && Event.current.type == EventType.MouseMove) Repaint();
            if (!expanded) 
                return;

            EditorGUI.indentLevel++;
            EditorGUILayout.Space(4);

            EditorGUILayout.PropertyField(bus.FindPropertyRelative("mixerGroup"),        new GUIContent("Mixer Group"));
            EditorGUILayout.PropertyField(bus.FindPropertyRelative("polyphony"),          new GUIContent("Polyphony"));
            EditorGUILayout.PropertyField(bus.FindPropertyRelative("allowVoiceStealing"), new GUIContent("Voice Stealing"));
            EditorGUILayout.PropertyField(bus.FindPropertyRelative("playLimitInterval"),  new GUIContent("Play Limit (s)"));

            EditorGUI.indentLevel--;
            EditorGUILayout.Space(2);
        }

        // =========================================================================
        // Settings Tab
        // =========================================================================

        private void DrawSettingsTab()
        {
            DrawSectionHeader("Library Settings");

            _settingsScroll = EditorGUILayout.BeginScrollView(_settingsScroll);

            EditorGUI.indentLevel++;

            EditorGUILayout.Space(6);
            DrawSubHeader("PlayerPrefs Keys");
            EditorGUILayout.PropertyField(_serializedLibrary.FindProperty("masterVolumeKey"),   new GUIContent("Master Volume Key"));
            EditorGUILayout.PropertyField(_serializedLibrary.FindProperty("musicVolumeKey"),    new GUIContent("Music Volume Key"));
            EditorGUILayout.PropertyField(_serializedLibrary.FindProperty("sfxVolumeKey"),      new GUIContent("SFX Volume Key"));
            EditorGUILayout.PropertyField(_serializedLibrary.FindProperty("voiceVolumeKey"),    new GUIContent("Voice Volume Key"));

            EditorGUILayout.Space(6);
            DrawSubHeader("Code Generation");
            EditorGUILayout.PropertyField(_serializedLibrary.FindProperty("generatedCodeFolder"),  new GUIContent("Output Folder"));
            EditorGUILayout.PropertyField(_serializedLibrary.FindProperty("generatedNamespace"),   new GUIContent("Namespace"));

            EditorGUILayout.Space(4);
            if (GUILayout.Button("Regenerate AudioConstants.cs Now"))
                AudioConstantsGenerator.RegenerateAll();

            EditorGUI.indentLevel--;
            EditorGUILayout.EndScrollView();
        }

        // =========================================================================
        // Helpers
        // =========================================================================

        private Rect _dropArea;

        private void DrawDragDropZone(string label, System.Action<Object[]> handler)
        {
            _dropArea = GUILayoutUtility.GetRect(0f, 36f, GUILayout.ExpandWidth(true));

            var style = new GUIStyle(EditorStyles.helpBox) { alignment = TextAnchor.MiddleCenter };
            GUI.Box(_dropArea, label, style);

            Event evt = Event.current;
            if ((evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
                && _dropArea.Contains(evt.mousePosition))
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    handler(DragAndDrop.objectReferences);
                    evt.Use();
                }
            }
        }

        private void HandleSFXDrop(Object[] objects)
        {
            var sfxProp = _serializedLibrary.FindProperty("sfx");
            foreach (var obj in objects)
            {
                if (!(obj is AudioClip clip)) continue;

                sfxProp.InsertArrayElementAtIndex(sfxProp.arraySize);
                var entry = sfxProp.GetArrayElementAtIndex(sfxProp.arraySize - 1);

                // InsertArrayElementAtIndex copies the previous element - reset everything.
                ResetSFXEntryDefaults(entry, clip.name);

                // Add exactly one variation with this clip
                var varsProp = entry.FindPropertyRelative("variations");
                varsProp.InsertArrayElementAtIndex(0);
                ResetVariationDefaults(varsProp.GetArrayElementAtIndex(0));
                varsProp.GetArrayElementAtIndex(0).FindPropertyRelative("clip")
                        .objectReferenceValue = clip;
            }
        }

        private static void ResetVariationDefaults(SerializedProperty v)
        {
            v.FindPropertyRelative("clip").objectReferenceValue      = null;
            v.FindPropertyRelative("randomizeVolume").boolValue      = false;
            v.FindPropertyRelative("fixedVolume").floatValue         = 1f;
            v.FindPropertyRelative("randomVolumeRange").vector2Value = Vector2.one;
            v.FindPropertyRelative("randomizePitch").boolValue       = false;
            v.FindPropertyRelative("fixedPitch").floatValue          = 1f;
            v.FindPropertyRelative("randomPitchRange").vector2Value  = Vector2.one;
            v.FindPropertyRelative("randomizeDelay").boolValue       = false;
            v.FindPropertyRelative("fixedDelay").floatValue          = 0f;
            v.FindPropertyRelative("randomDelayRange").vector2Value  = Vector2.zero;
        }

        private static void ResetSFXEntryDefaults(SerializedProperty entry, string entryName)
        {
            entry.FindPropertyRelative("name").stringValue            = entryName;
            entry.FindPropertyRelative("busName").stringValue         = "Master";
            entry.FindPropertyRelative("masterVolume").floatValue     = 1f;
            entry.FindPropertyRelative("muted").boolValue             = false;
            entry.FindPropertyRelative("soloed").boolValue            = false;
            entry.FindPropertyRelative("spatialBlend").floatValue     = 0f;
            entry.FindPropertyRelative("stereoPan").floatValue        = 0f;
            entry.FindPropertyRelative("dopplerLevel").floatValue     = 1f;
            entry.FindPropertyRelative("spread").floatValue           = 0f;
            entry.FindPropertyRelative("minDistance").floatValue      = 1f;
            entry.FindPropertyRelative("maxDistance").floatValue      = 500f;
            // Clear the variations array completely
            entry.FindPropertyRelative("variations").ClearArray();
        }

        private void HandleMusicDrop(Object[] objects)
        {
            var musicProp = _serializedLibrary.FindProperty("music");
            musicProp.InsertArrayElementAtIndex(musicProp.arraySize);
            var entry      = musicProp.GetArrayElementAtIndex(musicProp.arraySize - 1);
            var tracksProp = entry.FindPropertyRelative("tracks");

            foreach (var obj in objects)
            {
                if (obj is AudioClip clip)
                {
                    tracksProp.InsertArrayElementAtIndex(tracksProp.arraySize);
                    tracksProp.GetArrayElementAtIndex(tracksProp.arraySize - 1)
                              .FindPropertyRelative("clip").objectReferenceValue = clip;
                }
            }

            if (objects.Length > 0 && objects[0] is AudioClip first)
                entry.FindPropertyRelative("name").stringValue = first.name;
        }

        private void DrawBusDropdown(Rect rect, SerializedProperty busProp)
        {
            if (_selectedLibrary == null)
                return;

            var  buses    = _selectedLibrary.Buses;
            var  names    = new string[buses.Count];
            int  current  = 0;

            for (int i = 0; i < buses.Count; i++)
            {
                names[i] = buses[i].name;
                if (buses[i].name == busProp.stringValue) current = i;
            }

            int newIdx = EditorGUI.Popup(rect, current, names);
            if (newIdx >= 0 && newIdx < buses.Count)
                busProp.stringValue = buses[newIdx].name;
        }

        private void DrawSectionHeader(string label)
        {
            EditorGUILayout.Space(_theme.sectionSpacing);

            Rect r = GUILayoutUtility.GetRect(
                GUIContent.none, GUIStyle.none,
                GUILayout.Height(_theme.headerHeight), GUILayout.ExpandWidth(true));

            r.x     = 0f;
            r.width = EditorGUIUtility.currentViewWidth;

            EditorGUI.DrawRect(r, _theme.HeaderBg(false));
            EditorGUI.DrawRect(new Rect(r.x, r.yMax - 1f, r.width, 1f), _theme.HeaderBorder());
            EditorGUI.DrawRect(new Rect(r.x, r.y, _theme.accentWidth, r.height), _theme.accentColor);

            GUI.Label(new Rect(r.x + _theme.accentWidth + 10f, r.y, r.width, r.height),
                label, _headerLabelStyle);
        }

        private static void DrawSubHeader(string label)
        {
            EditorGUILayout.Space(3);
            Rect r = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(r, label, EditorStyles.boldLabel);
            EditorGUI.DrawRect(new Rect(r.x, r.yMax + 1f, r.width, 1f),
                new Color(0.35f, 0.35f, 0.35f, 0.6f));
            EditorGUILayout.Space(2);
        }

        private void AddSFXEntry()
        {
            var p = _serializedLibrary.FindProperty("sfx");
            p.InsertArrayElementAtIndex(p.arraySize);
            var entry = p.GetArrayElementAtIndex(p.arraySize - 1);
            ResetSFXEntryDefaults(entry, "New SFX");
            _expandedSFX.Add(p.arraySize - 1);
        }

        private void AddMusicEntry()
        {
            var p = _serializedLibrary.FindProperty("music");
            p.InsertArrayElementAtIndex(p.arraySize);
            p.GetArrayElementAtIndex(p.arraySize - 1)
             .FindPropertyRelative("name").stringValue = "New Music";
            _expandedMusic.Add(p.arraySize - 1);
        }

        private void AddBus()
        {
            var p = _serializedLibrary.FindProperty("buses");
            p.InsertArrayElementAtIndex(p.arraySize);
            p.GetArrayElementAtIndex(p.arraySize - 1)
             .FindPropertyRelative("name").stringValue = "New Bus";
        }

        private void PreviewSFX(int index)
        {
            if (_selectedLibrary == null || index >= _selectedLibrary.SFX.Count)
                return;

            StopPreview(); // stop whatever was playing before

            var entry     = _selectedLibrary.SFX[index];
            var variation = entry?.FetchVariation();
            if (variation?.clip == null) 
                return;

            if (Application.isPlaying)
            {
                // Play Mode - use the real audio system
                CoreAudio.Play(_selectedLibrary.MakeSFXHandle(index));
                _previewingIndex   = index;
                _previewIsPlayMode = true;
            }
            else
            {
                // Edit Mode - use Unity's internal AudioUtil via reflection
                var audioUtil = System.Type.GetType("UnityEditor.AudioUtil, UnityEditor");
                var playMethod = audioUtil?.GetMethod("PlayPreviewClip",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public,
                    null, new[] { typeof(AudioClip), typeof(int), typeof(bool) }, null);
                if (playMethod != null)
                {
                    playMethod.Invoke(null, new object[] { variation.clip, 0, false });
                    _previewingIndex   = index;
                    _previewIsPlayMode = false;
                }
            }

            Repaint();
        }

        private void StopPreview()
        {
            if (_previewingIndex < 0) 
                return;

            if (_previewIsPlayMode)
            {
                CoreAudio.StopAllSFX();
            }
            else
            {
                // Stop AudioUtil preview clip via reflection
                var audioUtil  = System.Type.GetType("UnityEditor.AudioUtil, UnityEditor");
                var stopMethod = audioUtil?.GetMethod("StopAllPreviewClips",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                stopMethod?.Invoke(null, null);
            }

            _previewingIndex = -1;
            Repaint();
        }

        private void RebuildLists()
        { }

        private void AutoSelectLibrary()
        {
            string[] guids = AssetDatabase.FindAssets("t:AudioLibrary");
            if (guids.Length > 0)
                _selectedLibrary = AssetDatabase.LoadAssetAtPath<AudioLibrary>(
                    AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        // Runtime controls bar 
        // Drawn at the very bottom of OnGUI, visible only in Play Mode.
        private void DrawRuntimeBar()
        {
            // Dark separator line
            Rect sep = GUILayoutUtility.GetRect(0f, 1f, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(sep, _theme.HeaderBorder());

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            // Red dot + label
            var dot = new GUIStyle(EditorStyles.label) { normal = { textColor = new Color(1f, 0.3f, 0.3f) } };
            EditorGUILayout.LabelField("● LIVE", dot, GUILayout.Width(48));

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("■  Stop All SFX",   EditorStyles.toolbarButton, GUILayout.Width(110)))
            {
                CoreAudio.StopAllSFX();
                _previewingIndex = -1;
            }
            if (GUILayout.Button("⏸  Pause All SFX",  EditorStyles.toolbarButton, GUILayout.Width(110)))
                CoreAudio.PauseAllSFX();
            if (GUILayout.Button("▶  Resume All SFX", EditorStyles.toolbarButton, GUILayout.Width(120)))
                CoreAudio.ResumeAllSFX();

            GUILayout.Space(8);

            if (GUILayout.Button("■  Stop All Music",   EditorStyles.toolbarButton, GUILayout.Width(112)))
                CoreAudio.Music.StopAll(0f);
            if (GUILayout.Button("⏸  Pause All Music",  EditorStyles.toolbarButton, GUILayout.Width(112)))
                CoreAudio.Music.PauseAll();
            if (GUILayout.Button("▶  Resume All Music", EditorStyles.toolbarButton, GUILayout.Width(122)))
                CoreAudio.Music.ResumeAll();

            GUILayout.Space(4);
            EditorGUILayout.EndHorizontal();
        }

        private void ApplyAndSave()
        {
            if (_serializedLibrary == null) 
                return;
            _serializedLibrary.ApplyModifiedProperties();
            if (_selectedLibrary != null)
                EditorUtility.SetDirty(_selectedLibrary);
        }

        private void EnsureStyles()
        {
            if (_stylesInitialized) 
                return;
            _stylesInitialized = true;

            _headerLabelStyle = new GUIStyle(EditorStyles.label)
            {
                fontStyle  = FontStyle.Bold,
                fontSize   = _theme.headerFontSize,
                alignment  = TextAnchor.MiddleLeft,
            };
            _headerLabelStyle.normal.textColor = _theme.HeaderText();
            
            // Use FindTexture - returns null when icon doesn't exist, unlike IconContent.
            // winbtn_win_close is the red X on Unity's window tab close button.
            var _closeTexture = EditorGUIUtility.FindTexture("winbtn_win_close")
                                ?? EditorGUIUtility.FindTexture("winbtn_win_close_h");
            _deleteIcon = _closeTexture != null
                ? new GUIContent(_closeTexture)
                : new GUIContent("✕");
            
            _entryNameStyle = new GUIStyle(EditorStyles.textField)
            {
                fontStyle = FontStyle.Normal,
                fontSize  = 12,
                alignment = TextAnchor.MiddleLeft,
            };

            _tabStyle = new GUIStyle(EditorStyles.toolbarButton)
            {
                fontStyle  = FontStyle.Normal,
                fontSize   = 12,
                fixedHeight = 0,
            };

            _tabActiveStyle = new GUIStyle(_tabStyle)
            {
                fontStyle = FontStyle.Bold,
                normal    = { textColor = _theme.accentColor },
            };
        }
    }

    // =========================================================================
    // AudioLibrary double-click => open window
    // =========================================================================
    [CustomEditor(typeof(AudioLibrary))]
    internal sealed class AudioLibraryEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("Open Audio Library Editor", GUILayout.Height(32)))
                AudioSystemWindow.OpenWithLibrary((AudioLibrary)target);

            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox(
                "Edit this library in the Audio Library Editor window.\n" +
                "Double-click this asset or use CorePro/Audio Library Editor.",
                MessageType.Info);
        }
    }
}
#endif