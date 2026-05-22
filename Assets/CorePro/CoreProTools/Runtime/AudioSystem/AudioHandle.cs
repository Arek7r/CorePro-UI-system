// A lightweight, serializable value-type identifier for audio entries.
// Packs a library instance ID (upper 32 bits) and an entry index (lower 32 bits)
// into a single long - zero heap allocation, safe as a field on any MonoBehaviour.
//
// Designers assign handles via the [AudioSFX] / [AudioMusic] PropertyDrawer (dropdown).
// Programmers use the auto-generated SFX / Music static classes for IntelliSense access.

using System;
using UnityEngine;

namespace CorePro.AudioSystem
{
    /// <summary>
    /// Identifies a single SFX or Music entry inside an <see cref="AudioLibrary"/>.
    /// Assign in the Inspector via <see cref="AudioSFXAttribute"/> or <see cref="AudioMusicAttribute"/>.
    /// Use <c>AudioHandle.None</c> to represent "no sound".
    /// </summary>
    [Serializable]
    public struct AudioHandle : IEquatable<AudioHandle>
    {
        // Packing layout:
        //   bits 63-32  →  library instance ID  (UnityEngine.Object.GetInstanceID)
        //   bits 31-0   →  entry index inside that library  (int, -1 = None)
        [SerializeField] private long _packed;

        /// <summary>Represents "no sound assigned". Evaluates to <c>false</c> in a boolean context.</summary>
        public static readonly AudioHandle None = default;

        // Internal construction 

        /// <param name="libraryGuidHash">
        /// Stable 32-bit hash of the library's AssetDatabase GUID.
        /// Use <see cref="AudioLibrary.GuidHash"/> - never pass GetInstanceID() here.
        /// </param>
        internal AudioHandle(uint libraryGuidHash, int entryIndex)
        {
            _packed = ((long)libraryGuidHash << 32) | (uint)entryIndex;
        }

        // Properties 

        /// <summary>Returns <c>true</c> when this handle points to a real entry.</summary>
        public bool IsValid => _packed != 0L;

        /// <summary>Stable GUID hash of the owning <see cref="AudioLibrary"/>.</summary>
        internal uint LibraryGuidHash => (uint)(_packed >> 32);

        /// <summary>Zero-based index of the entry within its library list.</summary>
        internal int EntryIndex => (int)(_packed & 0xFFFFFFFFL);

        // Equality 

        public bool Equals(AudioHandle other) => _packed == other._packed;
        public override bool Equals(object obj) => obj is AudioHandle h && Equals(h);
        public override int GetHashCode() => _packed.GetHashCode();
        public static bool operator ==(AudioHandle a, AudioHandle b) => a._packed == b._packed;
        public static bool operator !=(AudioHandle a, AudioHandle b) => a._packed != b._packed;

        /// <summary>Implicit bool - <c>false</c> when the handle is <see cref="None"/>.</summary>
        public static implicit operator bool(AudioHandle h) => h.IsValid;

        public override string ToString() =>
            IsValid ? $"AudioHandle(lib=, idx={EntryIndex})" : "AudioHandle.None";
    }

    // Attributes used by the PropertyDrawer 

    /// <summary>
    /// Marks an <see cref="AudioHandle"/> field as an SFX selector.
    /// The Inspector will display a searchable dropdown of all SFX entries
    /// from every registered <see cref="AudioLibrary"/> in the project.
    /// </summary>
    public sealed class AudioSFXAttribute : PropertyAttribute { }

    /// <summary>
    /// Marks an <see cref="AudioHandle"/> field as a Music selector.
    /// The Inspector will display a searchable dropdown of all Music entries
    /// from every registered <see cref="AudioLibrary"/> in the project.
    /// </summary>
    public sealed class AudioMusicAttribute : PropertyAttribute { }
}