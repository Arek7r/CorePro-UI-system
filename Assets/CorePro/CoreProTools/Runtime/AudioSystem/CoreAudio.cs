// The single public entry point for all audio operations.
// Designed for maximum ergonomics - programmers call this directly in gameplay code.
//
// Usage examples:
//
//   // SFX
//   CoreAudio.Play(SFX.Weapons_Pistol_Shoot);
//   CoreAudio.Play(SFX.Weapons_Pistol_Shoot, transform.position);
//   CoreAudio.Play(SFX.Weapons_Pistol_Shoot, volume: 0.8f, pitch: 1.1f);
//
//   // Music
//   CoreAudio.Music.Play(Music.Combat_MainTheme);
//   CoreAudio.Music.Play(Music.Combat_MainTheme, fadeDuration: 1.5f);
//   CoreAudio.Music.Next(Music.Combat_MainTheme);
//   CoreAudio.Music.Stop(Music.Combat_MainTheme, fadeDuration: 0.5f);
//   CoreAudio.Music.PauseAll();
//
//   // Volume (auto-saved to PlayerPrefs)
//   CoreAudio.MasterVolume = 0.8f;
//   CoreAudio.MusicVolume  = 0.5f;
//   float current = CoreAudio.MasterVolume;

using UnityEngine;

namespace CorePro.AudioSystem
{
    /// <summary>
    /// Static facade for the CorePro Audio system.
    /// All methods are safe to call before any library is registered - they are
    /// silently ignored when the runtime has not yet been created.
    /// </summary>
    public static class CoreAudio
    {
        //  Runtime singleton 

        private static AudioRuntime _runtime;

        private static AudioRuntime Runtime
        {
            get
            {
                if (_runtime != null) return _runtime;

                // DontDestroyOnLoad and AddComponent are only legal in Play Mode.
                if (!Application.isPlaying) return null;

                // After a domain reload _runtime is null but the GameObject created
                // in the previous Play session may still exist (DontDestroyOnLoad).
                // Find it before creating a new one to avoid duplicates.
                _runtime = Object.FindFirstObjectByType<AudioRuntime>();
                if (_runtime != null) return _runtime;

                var go = new GameObject("[CorePro Audio]");
                Object.DontDestroyOnLoad(go);
                _runtime = go.AddComponent<AudioRuntime>();

                // Restore volume from PlayerPrefs using the first registered library's keys,
                // or sensible defaults if nothing is registered yet.
                LoadVolumes();

                return _runtime;
            }
        }

        //  Library registration (called by AudioLibrary.OnEnable/OnDisable) 

        /// <summary>
        /// Registers an <see cref="AudioLibrary"/> with the runtime.
        /// Called automatically by <see cref="AudioLibrary.OnEnable"/>.
        /// </summary>
        public static void RegisterLibrary(AudioLibrary library)
        {
            if (library == null) return;
            var rt = Runtime;
            if (rt == null) return;   // Edit Mode - no runtime exists
            rt.Register(library);

            // First library registered drives PlayerPrefs key names
            if (!_volumeLoaded)
                LoadVolumesFromLibrary(library);
        }

        /// <summary>
        /// Unregisters an <see cref="AudioLibrary"/> from the runtime.
        /// Called automatically by <see cref="AudioLibrary.OnDisable"/>.
        /// </summary>
        public static void UnregisterLibrary(AudioLibrary library)
        {
            if (library == null || _runtime == null) return;
            _runtime.Unregister(library);
        }

        //  Volume - PlayerPrefs-backed properties 

        private static bool _volumeLoaded;

        /// <summary>Master volume (0–1). Saved to PlayerPrefs immediately.</summary>
        public static float MasterVolume
        {
            get => _runtime != null ? _runtime.MasterVolume : 1f;
            set
            {
                float v = Mathf.Clamp01(value);
                Runtime.MasterVolume = v;
                PlayerPrefs.SetFloat(_masterKey, v);
                PlayerPrefs.Save();
            }
        }

        /// <summary>Music volume (0–1). Saved to PlayerPrefs immediately.</summary>
        public static float MusicVolume
        {
            get => _runtime != null ? _runtime.MusicVolume : 1f;
            set
            {
                float v = Mathf.Clamp01(value);
                Runtime.MusicVolume = v;
                PlayerPrefs.SetFloat(_musicKey, v);
                PlayerPrefs.Save();
            }
        }

        /// <summary>SFX volume (0–1). Saved to PlayerPrefs immediately.</summary>
        public static float SFXVolume
        {
            get => _runtime != null ? _runtime.SFXVolume : 1f;
            set
            {
                float v = Mathf.Clamp01(value);
                Runtime.SFXVolume = v;
                PlayerPrefs.SetFloat(_sfxKey, v);
                PlayerPrefs.Save();
            }
        }

        /// <summary>Voice/dialogue volume (0–1). Saved to PlayerPrefs immediately.</summary>
        public static float VoiceVolume
        {
            get => _runtime != null ? _runtime.VoiceVolume : 1f;
            set
            {
                float v = Mathf.Clamp01(value);
                Runtime.VoiceVolume = v;
                PlayerPrefs.SetFloat(_voiceKey, v);
                PlayerPrefs.Save();
            }
        }

        // PlayerPrefs keys - defaults used before any library is registered
        private static string _masterKey = "CoreAudio_Master";
        private static string _musicKey  = "CoreAudio_Music";
        private static string _sfxKey    = "CoreAudio_SFX";
        private static string _voiceKey  = "CoreAudio_Voice";

        private static void LoadVolumes()
        {
            if (_runtime == null) return;
            _runtime.MasterVolume = PlayerPrefs.GetFloat(_masterKey, 1f);
            _runtime.MusicVolume  = PlayerPrefs.GetFloat(_musicKey,  1f);
            _runtime.SFXVolume    = PlayerPrefs.GetFloat(_sfxKey,    1f);
            _runtime.VoiceVolume  = PlayerPrefs.GetFloat(_voiceKey,  1f);
        }

        private static void LoadVolumesFromLibrary(AudioLibrary lib)
        {
            _masterKey    = lib.masterVolumeKey;
            _musicKey     = lib.musicVolumeKey;
            _sfxKey       = lib.sfxVolumeKey;
            _voiceKey     = lib.voiceVolumeKey;
            _volumeLoaded = true;
            LoadVolumes();
        }

        //  SFX - Play overloads 

        /// <summary>
        /// Plays a one-shot 2D sound effect.
        /// Does nothing if <paramref name="handle"/> is <see cref="AudioHandle.None"/>.
        /// </summary>
        public static void Play(
            AudioHandle handle,
            float?      volume = null,
            float?      pitch  = null,
            float?      delay  = null)
        {
            if (!handle) return;
            Runtime.PlaySFX(handle, Vector3.zero, is3D: false,
                            loop: false, volume, pitch, delay);
        }

        /// <summary>
        /// Plays a one-shot 3D sound effect at the given world-space position.
        /// </summary>
        public static void Play(
            AudioHandle handle,
            Vector3     position,
            float?      volume = null,
            float?      pitch  = null,
            float?      delay  = null)
        {
            if (!handle) return;
            Runtime.PlaySFX(handle, position, is3D: true,
                            loop: false, volume, pitch, delay);
        }

        /// <summary>
        /// Starts a looping 2D sound effect.
        /// Call <see cref="StopLooped"/> with the same handle to stop it.
        /// </summary>
        public static void PlayLooped(
            AudioHandle handle,
            float?      volume = null,
            float?      pitch  = null)
        {
            if (!handle) return;
            Runtime.PlaySFX(handle, Vector3.zero, is3D: false,
                            loop: true, volume, pitch, null);
        }

        /// <summary>
        /// Starts a looping 3D sound effect at the given world-space position.
        /// </summary>
        public static void PlayLooped(
            AudioHandle handle,
            Vector3     position,
            float?      volume = null,
            float?      pitch  = null)
        {
            if (!handle) return;
            Runtime.PlaySFX(handle, position, is3D: true,
                            loop: true, volume, pitch, null);
        }

        /// <summary>
        /// Stops all looping voices for the given sound effect handle.
        /// </summary>
        public static void StopLooped(AudioHandle handle)
        {
            if (!handle || _runtime == null) return;
            _runtime.StopLoopedSFX(handle);
        }

        //  SFX - Global control 

        /// <summary>Immediately stops all playing sound effects across all libraries.</summary>
        public static void StopAllSFX()  => _runtime?.StopAllSFX();

        /// <summary>Pauses all playing sound effects across all libraries.</summary>
        public static void PauseAllSFX() => _runtime?.PauseAllSFX();

        /// <summary>Resumes all paused sound effects across all libraries.</summary>
        public static void ResumeAllSFX()=> _runtime?.ResumeAllSFX();

        //  Music - nested static class 

        /// <summary>
        /// Music playback control. Access via <c>CoreAudio.Music.Play(handle)</c> etc.
        /// </summary>
        public static class Music
        {
            /// <summary>
            /// Starts playback of the music player identified by <paramref name="handle"/>.
            /// </summary>
            /// <param name="handle">Handle pointing to a <see cref="MusicEntry"/>.</param>
            /// <param name="fadeDuration">
            /// Override the entry's fade duration for this play call.
            /// Pass <c>null</c> to use the value configured in the library.
            /// </param>
            public static void Play(AudioHandle handle, float? fadeDuration = null)
            {
                if (!handle) return;
                Runtime.MusicPlay(handle, fadeDuration);
            }

            /// <summary>Stops the music player, fading out over <paramref name="fadeDuration"/> seconds.</summary>
            public static void Stop(AudioHandle handle, float? fadeDuration = null)
            {
                if (!handle || _runtime == null) return;
                _runtime.MusicStop(handle, fadeDuration);
            }

            /// <summary>Pauses the music player (crossfades to silence, preserves position).</summary>
            public static void Pause(AudioHandle handle, float? fadeDuration = null)
            {
                if (!handle || _runtime == null) return;
                _runtime.MusicPause(handle, fadeDuration);
            }

            /// <summary>Resumes a paused music player.</summary>
            public static void Resume(AudioHandle handle, float? fadeDuration = null)
            {
                if (!handle || _runtime == null) return;
                _runtime.MusicResume(handle, fadeDuration);
            }

            /// <summary>Crossfades to the next track in the playlist.</summary>
            public static void Next(AudioHandle handle, float? fadeDuration = null)
            {
                if (!handle || _runtime == null) return;
                _runtime.MusicNext(handle, fadeDuration);
            }

            /// <summary>Crossfades to the previous track in the playlist.</summary>
            public static void Previous(AudioHandle handle, float? fadeDuration = null)
            {
                if (!handle || _runtime == null) return;
                _runtime.MusicPrevious(handle, fadeDuration);
            }

            /// <summary>Crossfades to a specific track by its zero-based index in the playlist.</summary>
            public static void Seek(AudioHandle handle, int trackIndex, float? fadeDuration = null)
            {
                if (!handle || _runtime == null) return;
                _runtime.MusicSeek(handle, trackIndex, fadeDuration);
            }

            /// <summary>Pauses all music players across all registered libraries.</summary>
            public static void PauseAll(float? fadeDuration = null)  => _runtime?.MusicPauseAll(fadeDuration);

            /// <summary>Resumes all paused music players across all registered libraries.</summary>
            public static void ResumeAll(float? fadeDuration = null) => _runtime?.MusicResumeAll(fadeDuration);

            /// <summary>Stops all music players across all registered libraries.</summary>
            public static void StopAll(float? fadeDuration = null)   => _runtime?.MusicStopAll(fadeDuration);
        }
    }
}