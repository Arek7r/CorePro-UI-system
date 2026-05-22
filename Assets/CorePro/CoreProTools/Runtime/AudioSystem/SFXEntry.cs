// Persistent data for a single sound effect and all its playback variations.
// The naming convention "Category/SubCategory/Name" drives the searchable
// dropdown hierarchy in the Editor (e.g. "Weapons/Pistol/Shoot").

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace CorePro.AudioSystem
{
    //  Variation retrigger strategy 

    /// <summary>Defines how an <see cref="SFXEntry"/> cycles through its variations on repeated plays.</summary>
    public enum RetriggerMode
    {
        /// <summary>Plays variations in order, wrapping back to the first.</summary>
        Sequential,
        /// <summary>Picks a random variation; the same one may repeat.</summary>
        Random,
        /// <summary>Picks a random variation, never the same one twice in a row.</summary>
        RandomNoRepeat,
        /// <summary>Plays forward through variations then backward, ping-pong style.</summary>
        PingPong,
    }

    //  Spatial attenuation model 

    /// <summary>Maps to <see cref="AudioRolloffMode"/> for 3-D attenuation.</summary>
    public enum AttenuationMode
    {
        /// <summary>Real-world logarithmic rolloff.</summary>
        Logarithmic,
        /// <summary>Linear rolloff - easier to tune by hand.</summary>
        Linear,
        /// <summary>Custom rolloff curve set on the AudioSource.</summary>
        Custom,
    }

    //  Single variation 

    /// <summary>
    /// One clip variant within an <see cref="SFXEntry"/>.
    /// Volume, pitch and delay can each be a fixed value or a random range.
    /// </summary>
    [Serializable]
    public sealed class SFXVariation
    {
        [Tooltip("The audio clip to play for this variation.")]
        public AudioClip clip;

        // Volume
        [Tooltip("Use a random volume range instead of a fixed value.")]
        public bool randomizeVolume;
        [Range(0f, 1f), Tooltip("Fixed playback volume.")]
        public float fixedVolume = 1f;
        [Tooltip("Random volume range (x = min, y = max).")]
        public Vector2 randomVolumeRange = Vector2.one;

        // Pitch
        [Tooltip("Use a random pitch range instead of a fixed value.")]
        public bool randomizePitch;
        [Range(-3f, 3f), Tooltip("Fixed playback pitch.")]
        public float fixedPitch = 1f;
        [Tooltip("Random pitch range (x = min, y = max).")]
        public Vector2 randomPitchRange = Vector2.one;

        // Delay
        [Tooltip("Use a random delay range instead of a fixed value.")]
        public bool randomizeDelay;
        [Min(0f), Tooltip("Fixed playback delay in seconds.")]
        public float fixedDelay;
        [Tooltip("Random delay range in seconds (x = min, y = max).")]
        public Vector2 randomDelayRange = Vector2.zero;

        //  Evaluated properties (called once per Play - no alloc) 

        /// <summary>Returns the volume to use this play, respecting the randomize flag.</summary>
        public float Volume  => randomizeVolume ? UnityEngine.Random.Range(randomVolumeRange.x, randomVolumeRange.y) : fixedVolume;

        /// <summary>Returns the pitch to use this play, respecting the randomize flag.</summary>
        public float Pitch   => randomizePitch  ? UnityEngine.Random.Range(randomPitchRange.x,  randomPitchRange.y)  : fixedPitch;

        /// <summary>Returns the delay to use this play, respecting the randomize flag.</summary>
        public float Delay   => randomizeDelay  ? UnityEngine.Random.Range(randomDelayRange.x,  randomDelayRange.y)  : fixedDelay;
    }

    //  Sound effect entry 

    /// <summary>
    /// A named sound effect with one or more <see cref="SFXVariation"/>s and
    /// shared 3-D spatialization settings.
    /// Name format: <c>"Category/SubCategory/Name"</c> (slash creates folder hierarchy
    /// in the searchable dropdown and in the auto-generated <c>SFX</c> constants file).
    /// </summary>
    [Serializable]
    public sealed class SFXEntry
    {
        [Tooltip("Hierarchical name. Use '/' to create categories, e.g. 'Weapons/Pistol/Shoot'.")]
        public string name = "New SFX";

        [Tooltip("Sound bus this entry routes through.")]
        public string busName = "Master";

        [Tooltip("Master volume multiplier applied on top of variation volume.")]
        [Range(0f, 1f)]
        public float masterVolume = 1f;

        [Tooltip("Whether the sound is currently silent regardless of volume.")]
        public bool muted;

        [Tooltip("Whether only this sound plays within its bus (others are silenced).")]
        public bool soloed;

        // 3-D settings
        [Tooltip("0 = fully 2-D, 1 = fully 3-D.")]
        [Range(0f, 1f)]
        public float spatialBlend;

        [Tooltip("Stereo pan [-1 left … 1 right]. Only effective in 2-D mode.")]
        [Range(-1f, 1f)]
        public float stereoPan;

        [Tooltip("Doppler effect scale.")]
        [Range(0f, 5f)]
        public float dopplerLevel = 1f;

        [Tooltip("Spread angle in degrees for 3-D multichannel audio.")]
        [Range(0f, 360f)]
        public float spread;

        public AttenuationMode attenuationMode = AttenuationMode.Logarithmic;

        [Min(0f)]
        public float minDistance = 1f;

        [Min(0f)]
        public float maxDistance = 500f;

        [Tooltip("How consecutive plays cycle through variations.")]
        public RetriggerMode retriggerMode = RetriggerMode.RandomNoRepeat;

        [Tooltip("One or more audio clip variants. The retrigger mode decides which one plays.")]
        public List<SFXVariation> variations = new List<SFXVariation>();

        //  Runtime variation selection 
        // These are non-serialized runtime counters - reset on deserialization.
        [NonSerialized] private int  _currentIndex;
        [NonSerialized] private int  _direction = 1;
        [NonSerialized] private bool _runtimeInitialized;

        /// <summary>
        /// Returns the next variation to play according to the <see cref="retriggerMode"/>.
        /// Never allocates. Returns <c>null</c> if the variations list is empty.
        /// </summary>
        public SFXVariation FetchVariation()
        {
            int count = variations.Count;
            if (count == 0) return null;
            if (count == 1) return variations[0];

            if (!_runtimeInitialized)
            {
                _currentIndex      = 0;
                _direction         = 1;
                _runtimeInitialized = true;
            }

            SFXVariation result = variations[_currentIndex];

            switch (retriggerMode)
            {
                case RetriggerMode.Sequential:
                    _currentIndex = (_currentIndex + 1) % count;
                    break;

                case RetriggerMode.Random:
                    _currentIndex = UnityEngine.Random.Range(0, count);
                    break;

                case RetriggerMode.RandomNoRepeat:
                {
                    int next = _currentIndex;
                    // With 2+ entries this always terminates in 1-2 iterations
                    while (next == _currentIndex && count > 1)
                        next = UnityEngine.Random.Range(0, count);
                    _currentIndex = next;
                    break;
                }

                case RetriggerMode.PingPong:
                {
                    _currentIndex = Mathf.Clamp(_currentIndex, 0, count - 1);
                    if (_currentIndex == count - 1) _direction = -1;
                    else if (_currentIndex == 0)    _direction =  1;
                    _currentIndex += _direction;
                    break;
                }
            }

            return result;
        }

        /// <summary>Resets the variation counter. Called after deserialization.</summary>
        internal void ResetRuntime()
        {
            _currentIndex       = 0;
            _direction          = 1;
            _runtimeInitialized = false;
        }
    }
}
