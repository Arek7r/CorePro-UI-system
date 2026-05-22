#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace CorePro.AudioSystem.Editor
{
    // =========================================================================
    // Data passed back when the user makes a selection
    // =========================================================================

    internal readonly struct AudioDropdownSelection
    {
        /// <summary>The selected AudioHandle, or AudioHandle.None when "None" was chosen.</summary>
        internal readonly AudioHandle Handle;

        /// <summary>Full display path of the selected item (e.g. "Weapons/Pistol/Shoot").</summary>
        internal readonly string Path;

        internal AudioDropdownSelection(AudioHandle handle, string path)
        {
            Handle = handle;
            Path = path;
        }
    }

    // =========================================================================
    // AdvancedDropdown subclass
    // =========================================================================

    internal sealed class AudioHandleDropdown : AdvancedDropdown
    {
        //  Dropdown item 
        private sealed class AudioDropdownItem : AdvancedDropdownItem
        {
            internal readonly AudioHandle Handle;
            internal readonly string FullPath;
            internal readonly bool IsNone;

            internal AudioDropdownItem(string displayName, AudioHandle handle, string fullPath, bool isNone = false)
                : base(displayName)
            {
                Handle = handle;
                FullPath = fullPath;
                IsNone = isNone;
            }
        }

        //  Fields 
        private readonly List<(AudioHandle handle, string path)> _entries;
        private readonly Action<AudioDropdownSelection> _onSelected;
        private readonly AudioHandle _currentHandle;

        //  Construction 
        internal AudioHandleDropdown(
            AdvancedDropdownState state,
            List<(AudioHandle handle, string path)> entries,
            AudioHandle currentHandle,
            Action<AudioDropdownSelection> onSelected)
            : base(state)
        {
            _entries = entries;
            _currentHandle = currentHandle;
            _onSelected = onSelected;

            minimumSize = new Vector2(260f, 300f);
        }

        //  Build tree 
        protected override AdvancedDropdownItem BuildRoot()
        {
            var root = new AdvancedDropdownItem("Audio");

            //  None item (always first) 
            var noneItem = new AudioDropdownItem("None", AudioHandle.None, "None", isNone: true)
            {
                icon = EditorGUIUtility.IconContent("d_console.warnicon.sml").image as Texture2D,
            };
            root.AddChild(noneItem);
            root.AddSeparator();

            //  Build folder tree from slash-separated paths 
            // Key: folder path (e.g. "Weapons/Pistol"), Value: the dropdown item for that folder
            var folderItems = new Dictionary<string, AdvancedDropdownItem>(StringComparer.Ordinal);

            for (int i = 0; i < _entries.Count; i++)
            {
                (AudioHandle handle, string path) = _entries[i];
                AddEntryToTree(root, folderItems, handle, path);
            }

            return root;
        }

        private static void AddEntryToTree(
            AdvancedDropdownItem root,
            Dictionary<string, AdvancedDropdownItem> folderItems,
            AudioHandle handle,
            string fullPath)
        {
            int lastSlash = fullPath.LastIndexOf('/');

            if (lastSlash < 0)
            {
                // Top-level item - no folder
                root.AddChild(new AudioDropdownItem(fullPath, handle, fullPath));
                return;
            }

            string folderPath = fullPath.Substring(0, lastSlash);
            string itemName = fullPath.Substring(lastSlash + 1);
            var parentFolder = GetOrCreateFolder(root, folderItems, folderPath);
            parentFolder.AddChild(new AudioDropdownItem(itemName, handle, fullPath));
        }

        private static AdvancedDropdownItem GetOrCreateFolder(
            AdvancedDropdownItem root,
            Dictionary<string, AdvancedDropdownItem> folderItems,
            string folderPath)
        {
            if (folderItems.TryGetValue(folderPath, out var existing))
                return existing;

            // Recursively ensure parent folders exist
            int lastSlash = folderPath.LastIndexOf('/');
            string folderName = lastSlash < 0 ? folderPath : folderPath.Substring(lastSlash + 1);
            var parentPath = lastSlash < 0 ? null : folderPath.Substring(0, lastSlash);
            var parent = parentPath != null
                ? GetOrCreateFolder(root, folderItems, parentPath)
                : root;

            var folder = new AdvancedDropdownItem(folderName);
            parent.AddChild(folder);
            folderItems[folderPath] = folder;
            return folder;
        }

        //  Selection callback 

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            if (item is AudioDropdownItem audioItem)
                _onSelected?.Invoke(new AudioDropdownSelection(audioItem.Handle, audioItem.FullPath));
        }
    }

    // =========================================================================
    // PropertyDrawer - shared base for [AudioSFX] and [AudioMusic]
    // =========================================================================

    /// <summary>
    /// Shared implementation for <see cref="AudioSFXAttribute"/> and
    /// <see cref="AudioMusicAttribute"/> drawers.
    /// Renders a single-line field showing the current selection with a
    /// dropdown button that opens the searchable <see cref="AudioHandleDropdown"/>.
    /// </summary>
    internal abstract class AudioHandleDrawerBase : PropertyDrawer
    {
        private AdvancedDropdownState _dropdownState;

        // Cached display name - rebuilt when the serialized value changes
        private string _cachedDisplayName;
        private long _cachedPackedValue = long.MinValue; // impossible default
        private GUIStyle _arrowStyle;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            => EditorGUIUtility.singleLineHeight;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // Decode the current handle from the serialized _packed long
            SerializedProperty packedProp = property.FindPropertyRelative("_packed");
            if (packedProp == null)
            {
                EditorGUI.LabelField(position, label.text, "Missing _packed field on AudioHandle.");
                EditorGUI.EndProperty();
                return;
            }

            long currentPacked = packedProp.longValue;

            // Rebuild cached display name only when the value changes
            if (currentPacked != _cachedPackedValue)
            {
                _cachedPackedValue = currentPacked;
                _cachedDisplayName = BuildDisplayName(currentPacked);
            }

            // Layout: label | [display box] [▼ button]
            Rect labelRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, position.height);
            Rect controlRect = new Rect(position.x + EditorGUIUtility.labelWidth, position.y,
                position.width - EditorGUIUtility.labelWidth, position.height);

            float btnWidth = 20f;
            Rect valueRect = new Rect(controlRect.x + 12, controlRect.y, controlRect.width - btnWidth - 2f, controlRect.height);
            Rect frameRect = new Rect(controlRect.x, controlRect.y, controlRect.width - btnWidth - 2f, controlRect.height);

            EditorGUI.LabelField(labelRect, label);

            // Draw a box border around the value rect
            GUI.Box(frameRect, GUIContent.none, EditorStyles.helpBox);

            // Display box - shows current name, greyed out when None
            bool isNone = currentPacked == 0L;
            EditorGUI.LabelField(valueRect, isNone ? "None" : _cachedDisplayName, EditorStyles.largeLabel);

            if (_arrowStyle == null)
                _arrowStyle = new GUIStyle(EditorStyles.label)
                {
                    alignment = TextAnchor.MiddleLeft,
                    normal    = { textColor = Color.white }
                };

            GUI.Label(new Rect(frameRect.x + 3, frameRect.y, 18f, frameRect.height), "▾", _arrowStyle);

            if (GUI.Button(frameRect, "", GUIStyle.none))
                ShowDropdown(frameRect, property, packedProp);

            EditorGUI.EndProperty();
        }

        private void ShowDropdown(Rect buttonRect, SerializedProperty property, SerializedProperty packedProp)
        {
            if (_dropdownState == null)
                _dropdownState = new AdvancedDropdownState();

            var entries = CollectEntries();
            // Decode current handle (we can't call internal constructor from here, but we can
            // read back via reflection or just pass packed directly - stored in the list by value)
            long currentPacked = packedProp.longValue;

            // Find current handle in the list by matching packed value
            AudioHandle currentHandle = AudioHandle.None;
            for (int i = 0; i < entries.Count; i++)
            {
                // We need to compare the packed bits - use the struct's equality
                // The handle in entries is constructed by AudioLibrary.MakeSFXHandle/MakeMusicHandle
                // which packs (instanceID << 32 | index). We can verify by checking if the handle
                // encoded the same bits. Simplest: encode a temporary handle with same bits.
                // AudioHandle is a struct with only one private long field; we match via Equals
                // which compares _packed directly.
                if (HanldeMatchesPacked(entries[i].handle, currentPacked))
                {
                    currentHandle = entries[i].handle;
                    break;
                }
            }

            var dropdown = new AudioHandleDropdown(
                _dropdownState, entries, currentHandle,
                selection =>
                {
                    packedProp.longValue = GetPackedFromHandle(selection.Handle);
                    packedProp.serializedObject.ApplyModifiedProperties();
                    _cachedPackedValue = long.MinValue; // force display name rebuild
                });

            dropdown.Show(buttonRect);
        }

        //  Subclasses supply the entry list 

        /// <summary>Returns (handle, fullPath) pairs for all entries of the relevant type.</summary>
        protected abstract List<(AudioHandle handle, string path)> CollectEntries();

        //  Helpers 

        private string BuildDisplayName(long packed)
        {
            if (packed == 0L) return "None";

            var entries = CollectEntries();
            for (int i = 0; i < entries.Count; i++)
                if (HanldeMatchesPacked(entries[i].handle, packed))
                    return entries[i].path;

            return "⚠ Missing";
        }

        // Reinterpret an AudioHandle's bits as a long via MemoryMarshal - no unsafe needed.
        // AudioHandle is a single-field struct { long _packed }, so the layout is identical.
        private static long GetPackedFromHandle(AudioHandle handle) =>
            System.Runtime.InteropServices.MemoryMarshal.Read<long>(
                System.Runtime.InteropServices.MemoryMarshal.AsBytes(
                    System.Runtime.InteropServices.MemoryMarshal.CreateReadOnlySpan(ref handle, 1)));

        private static bool HanldeMatchesPacked(AudioHandle handle, long packed) =>
            GetPackedFromHandle(handle) == packed;
    }

    // =========================================================================
    // Entry collection helpers - scan all AudioLibrary assets in project
    // =========================================================================

    internal static class AudioLibraryScanner
    {
        /// <summary>
        /// Returns all SFX entries from all <see cref="AudioLibrary"/> assets in the project.
        /// Result is sorted alphabetically by path.
        /// </summary>
        internal static List<(AudioHandle handle, string path)> CollectSFX()
        {
            var result = new List<(AudioHandle handle, string path)>();
            foreach (var lib in LoadAllLibraries())
                for (int i = 0; i < lib.SFX.Count; i++)
                    if (lib.SFX[i] != null && !string.IsNullOrWhiteSpace(lib.SFX[i].name))
                        result.Add((lib.MakeSFXHandle(i), lib.SFX[i].name));
            result.Sort((a, b) => string.Compare(a.path, b.path, StringComparison.Ordinal));
            return result;
        }

        /// <summary>
        /// Returns all Music entries from all <see cref="AudioLibrary"/> assets in the project.
        /// Result is sorted alphabetically by path.
        /// </summary>
        internal static List<(AudioHandle handle, string path)> CollectMusic()
        {
            var result = new List<(AudioHandle handle, string path)>();
            foreach (var lib in LoadAllLibraries())
                for (int i = 0; i < lib.Music.Count; i++)
                    if (lib.Music[i] != null && !string.IsNullOrWhiteSpace(lib.Music[i].name))
                        result.Add((lib.MakeMusicHandle(i), lib.Music[i].name));
            result.Sort((a, b) => string.Compare(a.path, b.path, StringComparison.Ordinal));
            return result;
        }

        private static IEnumerable<AudioLibrary> LoadAllLibraries()
        {
            string[] guids = AssetDatabase.FindAssets("t:AudioLibrary");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var lib = AssetDatabase.LoadAssetAtPath<AudioLibrary>(path);
                if (lib != null) yield return lib;
            }
        }
    }

    // =========================================================================
    // Concrete drawers
    // =========================================================================

    [CustomPropertyDrawer(typeof(AudioSFXAttribute))]
    internal sealed class AudioSFXDrawer : AudioHandleDrawerBase
    {
        protected override List<(AudioHandle handle, string path)> CollectEntries()
            => AudioLibraryScanner.CollectSFX();
    }

    [CustomPropertyDrawer(typeof(AudioMusicAttribute))]
    internal sealed class AudioMusicDrawer : AudioHandleDrawerBase
    {
        protected override List<(AudioHandle handle, string path)> CollectEntries()
            => AudioLibraryScanner.CollectMusic();
    }
}
#endif