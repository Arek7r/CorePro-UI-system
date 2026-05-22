using System;
using System.Collections.Generic;
using UnityEngine;

namespace CorePro.AudioSystem
{
    /// <summary>
    /// Defines how audio clips belonging to this library are managed in memory.
    /// </summary>
    public enum AudioClipLoadMode
    {
        /// <summary>All clips are loaded at library registration time and kept until the library unregisters.</summary>
        PreloadAndKeep,
        /// <summary>Clips are loaded on first play and unloaded after a configurable idle period.</summary>
        LoadOnDemand,
        /// <summary>
        /// Clip loading is left entirely to the developer.
        /// Use <c>AudioClip.LoadAudioData()</c> / <c>UnloadAudioData()</c> manually.
        /// </summary>
        Manual,
    }

    /// <summary>
    /// A CorePro Audio Library asset - the single source of truth for SFX entries,
    /// music entries, buses and global settings within one audio "domain".
    /// </summary>
    [CreateAssetMenu(
        fileName = "NewAudioLibrary",
        menuName  = "CorePro/Audio Library",
        order     = 200)]
    public sealed class AudioLibrary : ScriptableObject, ISerializationCallbackReceiver
    {
        //  Data 

        [Tooltip("Sound buses (routing groups). 'Master' is always present at index 0.")]
        [SerializeField] internal List<AudioBus>   buses   = new List<AudioBus>();

        [Tooltip("Sound effect entries. Name supports '/' hierarchy.")]
        [SerializeField] internal List<SFXEntry>   sfx     = new List<SFXEntry>();

        [Tooltip("Music player entries. Name supports '/' hierarchy.")]
        [SerializeField] internal List<MusicEntry> music   = new List<MusicEntry>();

        //  Memory management 

        [Header("Memory Management")]
        [Tooltip("Default clip load policy. Individual entries can override this.")]
        [SerializeField] public AudioClipLoadMode defaultLoadMode = AudioClipLoadMode.LoadOnDemand;

        [Tooltip("Default idle seconds before an on-demand clip is unloaded. Individual entries can override.")]
        [Min(0f)]
        [SerializeField] public float defaultUnloadDelay = 60f;

        //  PlayerPrefs keys 

        [Header("Volume Settings - PlayerPrefs Keys")]
        [Tooltip("PlayerPrefs key for Master volume.")]
        [SerializeField] public string masterVolumeKey = "CoreAudio_Master";

        [Tooltip("PlayerPrefs key for Music volume.")]
        [SerializeField] public string musicVolumeKey  = "CoreAudio_Music";

        [Tooltip("PlayerPrefs key for SFX volume.")]
        [SerializeField] public string sfxVolumeKey    = "CoreAudio_SFX";

        [Tooltip("PlayerPrefs key for Voice/Dialogue volume.")]
        [SerializeField] public string voiceVolumeKey  = "CoreAudio_Voice";

        //  Pool warm-up 

        [Header("Pool Settings")]
        [Tooltip("Number of AudioSources pre-created in the manual SoundInstance pool at startup.")]
        [SerializeField] public int manualPoolSize = 32;

        //  Code generation 

        [Header("Code Generation")]
        [Tooltip("Folder (relative to Assets/) where the auto-generated constants file is written.")]
        [SerializeField] public string generatedCodeFolder = "CorePro/Generated";

        [Tooltip("Namespace for the generated SFX / Music static classes.")]
        [SerializeField] public string generatedNamespace = "CorePro.Audio.Generated";

        //  Public read-only accessors 

        /// <summary>Read-only view of all buses in this library.</summary>
        public IReadOnlyList<AudioBus>   Buses => buses;

        /// <summary>Read-only view of all SFX entries in this library.</summary>
        public IReadOnlyList<SFXEntry>   SFX   => sfx;

        /// <summary>Read-only view of all Music entries in this library.</summary>
        public IReadOnlyList<MusicEntry> Music => music;

        //  Stable GUID hash 
        // GetInstanceID() changes every Play Mode entry after domain reload, so
        // handles baked in the Inspector would silently fail at runtime.
        // We use a hash of the AssetDatabase GUID instead - it is permanent.

        [SerializeField] private uint _guidHash;

        /// <summary>
        /// Stable 32-bit hash of this library's AssetDatabase GUID.
        /// Survives domain reload; used as the key inside <see cref="AudioHandle"/>.
        /// </summary>
        public uint GuidHash
        {
            get
            {
                if (_guidHash == 0) RebuildGuidHash();
                return _guidHash;
            }
        }

        private void RebuildGuidHash()
        {
#if UNITY_EDITOR
            string guid = UnityEditor.AssetDatabase.AssetPathToGUID(
                UnityEditor.AssetDatabase.GetAssetPath(this));
            if (!string.IsNullOrEmpty(guid))
            {
                _guidHash = FnvHash(guid);
                return;
            }
#endif
            // Fallback for builds: hash the asset name (deterministic if names are unique).
            _guidHash = FnvHash(name);
        }

        private static uint FnvHash(string s)
        {
            uint h = 2166136261u;
            for (int i = 0; i < s.Length; i++) { h ^= (byte)s[i]; h *= 16777619u; }
            return h == 0u ? 1u : h;   // 0 reserved for "invalid"
        }

        //  Handle factory 

        /// <summary>Creates an <see cref="AudioHandle"/> for the SFX entry at <paramref name="index"/>.</summary>
        public AudioHandle MakeSFXHandle(int index) => new AudioHandle(GuidHash, index);

        /// <summary>Creates an <see cref="AudioHandle"/> for the Music entry at <paramref name="index"/>.</summary>
        public AudioHandle MakeMusicHandle(int index) => new AudioHandle(GuidHash, index);

        //  Self-registration 

        private void OnEnable()
        {
            EnsureMasterBus();
            ResetRuntimeState();
            RebuildGuidHash();   // must run before Register so handles are stable

            // Registration creates a runtime GameObject - only valid in Play Mode.
            // In Edit Mode the library is fully usable as a data asset; the Editor
            // tools read it directly without going through CoreAudio.
            if (Application.isPlaying)
                CoreAudio.RegisterLibrary(this);
        }

        private void OnDisable()
        {
            if (Application.isPlaying)
                CoreAudio.UnregisterLibrary(this);
        }

        //  ISerializationCallbackReceiver 

        public void OnBeforeSerialize()
        {
            EnsureMasterBus();
        }

        public void OnAfterDeserialize()
        {
            ResetRuntimeState();
        }

        //  Helpers 

        /// <summary>Guarantees a bus named "Master" exists at index 0.</summary>
        private void EnsureMasterBus()
        {
            if (buses.Count > 0 && buses[0].name == "Master")
                return;

            // Remove any orphan Master that ended up at a wrong index
            for (int i = buses.Count - 1; i >= 0; i--)
                if (buses[i].name == "Master")
                    buses.RemoveAt(i);

            buses.Insert(0, new AudioBus { name = "Master" });
        }

        /// <summary>Resets non-serialized runtime state on all entries after deserialization.</summary>
        private void ResetRuntimeState()
        {
            for (int i = 0; i < sfx.Count; i++)
                sfx[i]?.ResetRuntime();
        }

        //  Editor helpers 

#if UNITY_EDITOR
        /// <summary>
        /// Returns the index of the SFX entry with the given name, or -1 if not found.
        /// Used by the PropertyDrawer to restore the dropdown selection after recompile.
        /// </summary>
        internal int FindSFXIndex(string entryName)
        {
            for (int i = 0; i < sfx.Count; i++)
                if (sfx[i] != null && sfx[i].name == entryName)
                    return i;
            return -1;
        }

        /// <summary>
        /// Returns the index of the Music entry with the given name, or -1 if not found.
        /// </summary>
        internal int FindMusicIndex(string entryName)
        {
            for (int i = 0; i < music.Count; i++)
                if (music[i] != null && music[i].name == entryName)
                    return i;
            return -1;
        }
#endif
    }
}