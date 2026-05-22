// AudioBus - routing group that controls polyphony, voice-stealing and
//            optional AudioMixerGroup assignment for a set of SFX entries.
//
// MusicTrack  - a single clip inside a playlist with a per-track volume.
// MusicEntry  - a named music player: owns a playlist and crossfade settings.

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace CorePro.AudioSystem
{
    // =========================================================================
    // AudioBus
    // =========================================================================

    /// <summary>
    /// Groups a set of <see cref="SFXEntry"/> objects and governs how many of
    /// them can play simultaneously (polyphony), what happens when that limit
    /// is exceeded (voice stealing), and which <see cref="AudioMixerGroup"/>
    /// they route to.
    /// </summary>
    [Serializable]
    public sealed class AudioBus
    {
        [Tooltip("Unique name used by SFXEntry.busName and at runtime lookup.")]
        public string name = "Master";

        [Tooltip("AudioMixer routing target. Leave null to use the default output.")]
        public AudioMixerGroup mixerGroup;

        [Tooltip("Volume multiplier for all sounds routed through this bus (0–1).")]
        [Range(0f, 1f)]
        public float volume = 1f;

        [Tooltip("Silence all sounds on this bus without changing individual volumes.")]
        public bool muted;

        [Tooltip("Solo this bus - all other buses are silenced.")]
        public bool soloed;

        [Tooltip("Maximum simultaneous voices. Increasing this allocates more AudioSources at startup.")]
        [Range(1, 64)]
        public int polyphony = 8;

        [Tooltip("When all voices are busy, steal the oldest playing voice instead of skipping the new sound.")]
        public bool allowVoiceStealing = true;

        [Tooltip("Minimum seconds between consecutive plays on this bus (0 = no limit).")]
        [Min(0f)]
        public float playLimitInterval;

        // Editor fold state - not serialized to build
#if UNITY_EDITOR
        [NonSerialized] public bool editorUnfolded;
#endif
    }

    // =========================================================================
    // MusicTrack
    // =========================================================================

    /// <summary>
    /// One audio clip inside a <see cref="MusicEntry"/> playlist,
    /// with an independent per-track volume multiplier.
    /// </summary>
    [Serializable]
    public sealed class MusicTrack
    {
        [Tooltip("The music audio clip.")]
        public AudioClip clip;

        [Tooltip("Per-track volume multiplier applied on top of the MusicEntry volume.")]
        [Range(0f, 1f)]
        public float volume = 1f;

        /// <summary>
        /// Display name derived from the clip asset name, or <c>"(empty)"</c>
        /// when no clip is assigned.
        /// </summary>
        public string DisplayName => clip != null ? clip.name : "(empty)";
    }

    // =========================================================================
    // MusicPlaybackMode
    // =========================================================================

    /// <summary>Controls how a <see cref="MusicEntry"/> advances through its playlist.</summary>
    public enum MusicPlaybackMode
    {
        /// <summary>Plays a single track and stops.</summary>
        Single,
        /// <summary>Automatically advances to the next track with a crossfade.</summary>
        AutoAdvance,
    }

    // =========================================================================
    // MusicEntry
    // =========================================================================

    /// <summary>
    /// A named music player that owns a playlist and a full set of playback
    /// rules (crossfade, loop, shuffle, playback mode, mixer routing).
    /// Multiple entries can play simultaneously and are controlled independently.
    /// Name format: <c>"Category/Name"</c> (slash creates folder hierarchy in the dropdown).
    /// </summary>
    [Serializable]
    public sealed class MusicEntry
    {
        [Tooltip("Hierarchical name. Use '/' to create categories, e.g. 'Combat/MainTheme'.")]
        public string name = "New Music";

        [Tooltip("AudioMixer routing target. Leave null for default output.")]
        public AudioMixerGroup mixerGroup;

        [Tooltip("Master volume for this player (0–1).")]
        [Range(0f, 1f)]
        public float volume = 1f;

        [Tooltip("Silence this player without changing its volume.")]
        public bool muted;

        [Tooltip("Crossfade duration in seconds for Play, Stop, Next and Prev.")]
        [Min(0f)]
        public float fadeDuration = 0.5f;

        [Tooltip("Loop the current track (Single mode) or the entire playlist (AutoAdvance).")]
        public bool loop = true;

        [Tooltip("Randomize playlist order. The order is reshuffled after every full cycle.")]
        public bool shuffle;

        [Tooltip("Start playing automatically when the library registers at runtime.")]
        public bool playOnStart;

        public MusicPlaybackMode playbackMode = MusicPlaybackMode.AutoAdvance;

        [Tooltip("Ordered list of tracks in the playlist.")]
        public List<MusicTrack> tracks = new List<MusicTrack>();
        
    }
}
