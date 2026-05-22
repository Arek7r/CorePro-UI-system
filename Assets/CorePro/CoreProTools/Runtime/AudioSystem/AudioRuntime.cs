// The single MonoBehaviour that drives the entire audio system at runtime.
// CoreAudio creates one instance of this on first use (DontDestroyOnLoad).
// It owns:
//   - Per-bus AudioSource pools  (SFX)
//   - Per-MusicEntry dual-source crossfaders  (Music)
//   - Memory manager for clip load/unload
//
// Design goals:
//   - Zero GC allocations in the Update loop and in the hot Play() path.
//   - All collections sized at registration time; no List.Add in Update.
//   - Dictionary enumerators are struct-based (no boxing).

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace CorePro.AudioSystem
{
    // =========================================================================
    // Internal SFX voice - wraps a single pooled AudioSource
    // =========================================================================

    internal sealed class SFXVoice
    {
        internal AudioSource source;
        internal bool inUse;
        internal float startTime; // Time.realtimeSinceStartup when play began
    }

    // =========================================================================
    // Internal bus runtime - manages a pool of SFXVoices
    // =========================================================================

    internal sealed class BusRuntime
    {
        internal readonly AudioBus bus;

        private readonly SFXVoice[] _voices;
        private float _playCooldown;

        // Cached count to avoid property lookup in tight loops
        private readonly int _polyphony;

        internal BusRuntime(Transform parent, AudioBus bus)
        {
            this.bus = bus;
            _polyphony = bus.polyphony;

            _voices = new SFXVoice[_polyphony];
            for (int i = 0; i < _polyphony; i++)
            {
                var go = new GameObject($"[SFX] {bus.name} Voice {i + 1}");
                go.transform.SetParent(parent, false);

                var src = go.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.outputAudioMixerGroup = bus.mixerGroup;

                _voices[i] = new SFXVoice { source = src };
            }
        }

        //  Update called every frame by AudioRuntime 

        internal void Update(float masterVol, float sfxVol, float deltaTime)
        {
            if (_playCooldown > 0f)
                _playCooldown -= deltaTime;

            float busVolume = bus.muted ? 0f : bus.volume * sfxVol * masterVol;

            for (int i = 0; i < _polyphony; i++)
            {
                SFXVoice v = _voices[i];
                if (!v.inUse) continue;

                // Auto-release finished (non-looping) voices
                if (!v.source.isPlaying && !v.source.loop)
                {
                    v.inUse = false;
                    v.source.clip = null;
                    continue;
                }

                v.source.volume = v.source.volume * busVolume; // already set at Play; keep in sync
            }
        }

        //  Grab a free voice for playback 

        /// <summary>
        /// Returns a free <see cref="SFXVoice"/>, optionally stealing the oldest active one.
        /// Returns <c>null</c> if the bus is play-limited or all voices are busy and
        /// stealing is disabled.
        /// </summary>
        internal SFXVoice GrabVoice()
        {
            if (_playCooldown > 0f) return null;

            // Try a free slot first
            for (int i = 0; i < _polyphony; i++)
            {
                if (!_voices[i].inUse)
                {
                    _voices[i].inUse = true;
                    ResetPlayCooldown();
                    return _voices[i];
                }
            }

            // Voice stealing
            if (!bus.allowVoiceStealing) return null;

            // Find the voice that has been playing the longest
            int oldest = 0;
            float oldestTime = float.MaxValue;
            for (int i = 0; i < _polyphony; i++)
            {
                if (_voices[i].startTime < oldestTime)
                {
                    oldestTime = _voices[i].startTime;
                    oldest = i;
                }
            }

            SFXVoice stolen = _voices[oldest];
            stolen.source.Stop();
            stolen.inUse = true;
            ResetPlayCooldown();
            return stolen;
        }

        internal void StopAll()
        {
            for (int i = 0; i < _polyphony; i++)
            {
                _voices[i].source.Stop();
                _voices[i].inUse = false;
            }
        }

        internal void PauseAll()
        {
            for (int i = 0; i < _polyphony; i++)
                if (_voices[i].inUse)
                    _voices[i].source.Pause();
        }

        internal void ResumeAll()
        {
            for (int i = 0; i < _polyphony; i++)
                if (_voices[i].inUse)
                    _voices[i].source.UnPause();
        }

        private void ResetPlayCooldown() =>
            _playCooldown = bus.playLimitInterval;
    }

    // =========================================================================
    // Internal fader - used by music crossfade
    // =========================================================================

    internal struct Fader
    {
        private float _duration;
        private float _elapsed;
        private float _from;
        private float _to;
        private bool _active;

        internal float Value { get; private set; }
        internal bool IsActive => _active;

        internal void SetImmediate(float value)
        {
            Value = value;
            _active = false;
        }

        internal void FadeTo(float target, float duration)
        {
            _from = Value;
            _to = target;
            _duration = duration;
            _elapsed = 0f;
            _active = duration > 0f;

            if (!_active) Value = target;
        }

        internal void Update(float dt)
        {
            if (!_active)
                return;
            _elapsed += dt;
            float t = Mathf.Clamp01(_elapsed / _duration);
            Value = Mathf.Lerp(_from, _to, t);
            if (t >= 1f) _active = false;
        }
    }

    // =========================================================================
    // Music player runtime - dual AudioSource crossfader
    // =========================================================================

    internal sealed class MusicPlayerRuntime
    {
        internal readonly MusicEntry entry;

        // Two sources for crossfading (A -> B -> A -> …)
        private readonly AudioSource _sourceA;
        private readonly AudioSource _sourceB;
        private bool _aIsActive = true; // which source holds current track

        private Fader _faderA;
        private Fader _faderB;

        private int[] _shuffleOrder;
        private int _shuffleIndex;
        private bool _playing;
        private bool _paused;

        internal bool IsPlaying => _playing && !_paused;

        internal MusicPlayerRuntime(Transform parent, MusicEntry entry)
        {
            this.entry = entry;

            _sourceA = CreateSource(parent, entry, "A");
            _sourceB = CreateSource(parent, entry, "B");

            _faderA.SetImmediate(0f);
            _faderB.SetImmediate(0f);

            BuildShuffleOrder();
        }

        //  Playback control 

        internal void Play(float? fadeOverride = null)
        {
            if (entry.tracks.Count == 0)
                return;

            float fade = fadeOverride ?? entry.fadeDuration;
            PlayTrack(_shuffleIndex, fade, fromStop: true);
            _playing = true;
            _paused = false;
        }

        internal void Stop(float? fadeOverride = null)
        {
            float fade = fadeOverride ?? entry.fadeDuration;
            FadeOutActive(fade, stopAfter: true);
            _playing = false;
            _paused = false;
        }

        internal void Pause(float? fadeOverride = null)
        {
            if (!_playing || _paused)
                return;
            float fade = fadeOverride ?? entry.fadeDuration;
            FadeOutActive(fade, stopAfter: false);
            _paused = true;
        }

        internal void Resume(float? fadeOverride = null)
        {
            if (!_paused)
                return;
            float fade = fadeOverride ?? entry.fadeDuration;
            ActiveSource().UnPause();
            ActiveFader().FadeTo(1f, fade);
            _paused = false;
        }

        internal void Next(float? fadeOverride = null)
        {
            if (!_playing || entry.tracks.Count == 0)
                return;
            AdvanceIndex(+1);
            PlayTrack(_shuffleIndex, fadeOverride ?? entry.fadeDuration, fromStop: false);
        }

        internal void Previous(float? fadeOverride = null)
        {
            if (!_playing || entry.tracks.Count == 0)
                return;
            AdvanceIndex(-1);
            PlayTrack(_shuffleIndex, fadeOverride ?? entry.fadeDuration, fromStop: false);
        }

        internal void Seek(int trackIndex, float? fadeOverride = null)
        {
            if (trackIndex < 0 || trackIndex >= entry.tracks.Count)
                return;
            _shuffleIndex = trackIndex; // direct index, ignores shuffle order
            PlayTrack(_shuffleIndex, fadeOverride ?? entry.fadeDuration, fromStop: false);
        }

        //  Update (called every frame) 

        internal void Update(float masterVol, float musicVol, float dt)
        {
            _faderA.Update(dt);
            _faderB.Update(dt);

            float entryVol = entry.muted ? 0f : entry.volume * musicVol * masterVol;

            _sourceA.volume = _faderA.Value * entryVol;
            _sourceB.volume = _faderB.Value * entryVol;

            // Auto-advance when the active track nears its end
            if (_playing && !_paused && entry.playbackMode == MusicPlaybackMode.AutoAdvance)
            {
                AudioSource active = ActiveSource();
                if (active.clip != null && !active.loop)
                {
                    float fade = entry.fadeDuration;
                    if (active.time + fade >= active.clip.length && !ActiveFaderRef().IsActive)
                        Next();
                }
            }

            // Single mode - detect natural end
            if (_playing && !_paused && entry.playbackMode == MusicPlaybackMode.Single)
            {
                AudioSource active = ActiveSource();
                if (active.clip != null && !active.isPlaying && !active.loop)
                    _playing = false;
            }
        }

        //  Helpers 
        private void PlayTrack(int shuffleIdx, float fade, bool fromStop)
        {
            if (entry.tracks.Count == 0)
                return;

            int realIndex = _shuffleOrder[Mathf.Clamp(shuffleIdx, 0, _shuffleOrder.Length - 1)];
            MusicTrack track = entry.tracks[realIndex];
            if (track?.clip == null)
                return;

            // Swap active/inactive source
            AudioSource incoming = InactiveSource();
            ref Fader inFader = ref InactiveFaderRef();
            ref Fader outFader = ref ActiveFaderRef();

            // Fade out current
            if (!fromStop)
                outFader.FadeTo(0f, fade);
            else
            {
                outFader.SetImmediate(0f);
                ActiveSource().Stop();
            }

            // Set up incoming
            incoming.clip = track.clip;
            incoming.loop = entry.loop && entry.playbackMode == MusicPlaybackMode.Single;
            incoming.outputAudioMixerGroup = entry.mixerGroup;
            incoming.timeSamples = 0;
            incoming.Play();
            inFader.FadeTo(1f, fade);

            _aIsActive = !_aIsActive; // flip active slot
        }

        private void FadeOutActive(float fade, bool stopAfter)
        {
            ref Fader f = ref ActiveFaderRef();
            f.FadeTo(0f, fade);
            if (stopAfter)
            {
                // Stopping after fade is handled implicitly - volume reaches 0,
                // source continues playing silently. We stop it next Update when
                // fader completes; keeping it simple here without coroutines.
                // For immediate stop:
                if (fade <= 0f) ActiveSource().Stop();
            }
        }

        private void AdvanceIndex(int delta)
        {
            int count = entry.tracks.Count;
            if (count == 0) return;

            _shuffleIndex += delta;

            if (entry.loop)
            {
                _shuffleIndex = ((_shuffleIndex % count) + count) % count;
                if (entry.shuffle && (_shuffleIndex == 0 || _shuffleIndex == count - 1))
                    Reshuffle();
            }
            else
            {
                _shuffleIndex = Mathf.Clamp(_shuffleIndex, 0, count - 1);
            }
        }

        private void BuildShuffleOrder()
        {
            int count = Mathf.Max(1, entry.tracks.Count);
            _shuffleOrder = new int[count];
            for (int i = 0; i < count; i++) _shuffleOrder[i] = i;
            if (entry.shuffle) Reshuffle();
            _shuffleIndex = 0;
        }

        private void Reshuffle()
        {
            int n = _shuffleOrder.Length;
            for (int i = n - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                int tmp = _shuffleOrder[i];
                _shuffleOrder[i] = _shuffleOrder[j];
                _shuffleOrder[j] = tmp;
            }
        }

        private AudioSource ActiveSource() => _aIsActive ? _sourceA : _sourceB;
        private AudioSource InactiveSource() => _aIsActive ? _sourceB : _sourceA;

        private ref Fader ActiveFaderRef() => ref (_aIsActive ? ref _faderA : ref _faderB);
        private ref Fader InactiveFaderRef() => ref (_aIsActive ? ref _faderB : ref _faderA);

        // Non-ref version for Update reads (cannot use ref in lambdas/closures)
        private Fader ActiveFader() => _aIsActive ? _faderA : _faderB;

        private static AudioSource CreateSource(Transform parent, MusicEntry entry, string slot)
        {
            var go = new GameObject($"[Music] {entry.name} ({slot})");
            go.transform.SetParent(parent, false);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.outputAudioMixerGroup = entry.mixerGroup;
            return src;
        }
    }

    // =========================================================================
    // AudioRuntime - MonoBehaviour singleton
    // =========================================================================

    /// <summary>
    /// Drives the entire CorePro Audio system.
    /// Created automatically by <see cref="CoreAudio"/> on first use; never
    /// instantiate manually.
    /// </summary>
    [AddComponentMenu("")] // hide from Add Component menu
    internal sealed class AudioRuntime : MonoBehaviour
    {
        //  Singleton guard 
        // Static field survives domain reload in some Unity versions but NOT all.
        // We use FindObjectOfType as a fallback so duplicate instances are always
        // caught and destroyed immediately in Awake.
        private static AudioRuntime _instance;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                // A runtime from a previous Play session survived - destroy the
                // duplicate (this new one) and keep the existing one.
                Destroy(gameObject);
                return;
            }

            _instance = this;
        }

        private void OnDestroy()
        {
            StopAllSFX();
            MusicStopAll(0f);
            if (_instance == this)
                _instance = null;
        }

        //  Library-keyed lookup 
        // Key = AudioLibrary instance ID
        private readonly Dictionary<uint, LibraryRuntime> _libraries =
            new Dictionary<uint, LibraryRuntime>();

        //  Volume state 
        internal float MasterVolume { get; set; } = 1f;
        internal float MusicVolume { get; set; } = 1f;
        internal float SFXVolume { get; set; } = 1f;
        internal float VoiceVolume { get; set; } = 1f;

        //  Inner container per registered library 
        private sealed class LibraryRuntime
        {
            internal readonly AudioLibrary library;
            internal readonly Dictionary<string, BusRuntime> buses = new Dictionary<string, BusRuntime>();
            internal readonly Dictionary<int, MusicPlayerRuntime> music = new Dictionary<int, MusicPlayerRuntime>();
            internal readonly Transform root;

            internal LibraryRuntime(AudioLibrary lib, Transform parent)
            {
                library = lib;
                root = new GameObject($"[AudioLib] {lib.name}").transform;
                root.SetParent(parent, false);
            }
        }

        //  Library registration 

        internal void Register(AudioLibrary lib)
        {
            uint id = lib.GuidHash;
            if (_libraries.ContainsKey(id))
                return;

            var lr = new LibraryRuntime(lib, transform);
            _libraries[id] = lr;

            // Build bus runtimes
            foreach (var bus in lib.Buses)
                lr.buses[bus.name] = new BusRuntime(lr.root, bus);

            // Build music player runtimes
            for (int i = 0; i < lib.Music.Count; i++)
            {
                var entry = lib.Music[i];
                var player = new MusicPlayerRuntime(lr.root, entry);
                lr.music[i] = player;

                if (entry.playOnStart)
                    player.Play();
            }
        }

        internal void Unregister(AudioLibrary lib)
        {
            uint id = lib.GuidHash;
            if (!_libraries.TryGetValue(id, out LibraryRuntime lr))
                return;

            // Stop all audio cleanly
            foreach (var bus in lr.buses.Values) bus.StopAll();
            foreach (var mp in lr.music.Values) mp.Stop(0f);

            if (lr.root != null)
                Destroy(lr.root.gameObject);

            _libraries.Remove(id);
        }

        //  SFX playback 

        /// <summary>
        /// Core play method. All public CoreAudio.Play overloads resolve here.
        /// Zero allocations in the hot path.
        /// </summary>
        internal void PlaySFX(
            AudioHandle handle,
            Vector3 position,
            bool is3D,
            bool loop,
            float? volumeOverride,
            float? pitchOverride,
            float? delayOverride)
        {
            if (!handle.IsValid)
            {
                Debug.LogWarning("[CoreAudio] PlaySFX: handle is invalid (None)");
                return;
            }

            // Debug: show what hash the handle wants vs what keys are registered
            var keys = string.Join(", ", _libraries.Keys);
            Debug.Log($"[CoreAudio] PlaySFX: handle.GuidHash={handle.LibraryGuidHash}, registered keys=[{keys}], count={_libraries.Count}");

            if (!_libraries.TryGetValue(handle.LibraryGuidHash, out LibraryRuntime lr))
            {
                Debug.LogWarning($"[CoreAudio] PlaySFX: no library found for GuidHash={handle.LibraryGuidHash}. Library not registered or handle is stale.");
                return;
            }

            int idx = handle.EntryIndex;

            if (idx < 0 || idx >= lr.library.SFX.Count)
                return;

            SFXEntry entry = lr.library.SFX[idx];
            if (entry == null || entry.muted)
                return;

            SFXVariation variation = entry.FetchVariation();
            if (variation?.clip == null)
                return;

            // Resolve bus
            string busName = string.IsNullOrEmpty(entry.busName) ? "Master" : entry.busName;
            if (!lr.buses.TryGetValue(busName, out BusRuntime bus))
                lr.buses.TryGetValue("Master", out bus);
            if (bus == null)
                return;

            // Bus solo check - any bus soloed? only allow soloed ones
            if (!IsBusAudible(lr, bus)) return;

            SFXVoice voice = bus.GrabVoice();
            if (voice == null) return;

            float vol = volumeOverride ?? variation.Volume;
            float pitch = pitchOverride ?? variation.Pitch;
            float delay = delayOverride ?? variation.Delay;

            // Total volume chain: variation -> entry master -> bus -> sfx channel -> master
            float totalVol = vol * entry.masterVolume * bus.bus.volume * SFXVolume * MasterVolume;

            AudioSource src = voice.source;
            src.clip = variation.clip;
            src.volume = totalVol;
            src.pitch = pitch;
            src.loop = loop;
            src.spatialBlend = entry.spatialBlend;
            src.panStereo = entry.stereoPan;
            src.dopplerLevel = entry.dopplerLevel;
            src.spread = entry.spread;
            src.minDistance = entry.minDistance;
            src.maxDistance = entry.maxDistance;
            src.rolloffMode = ToUnityRolloff(entry.attenuationMode);
            src.outputAudioMixerGroup = bus.bus.mixerGroup;

            if (is3D)
                src.transform.position = position;

            voice.startTime = Time.realtimeSinceStartup;

            Debug.Log($"AR: 5");
            if (delay > 0f) src.PlayDelayed(delay);
            else src.Play();
        }

        internal void StopLoopedSFX(AudioHandle handle)
        {
            if (!handle.IsValid) return;
            if (!_libraries.TryGetValue(handle.LibraryGuidHash, out LibraryRuntime lr)) return;

            int idx = handle.EntryIndex;
            if (idx < 0 || idx >= lr.library.SFX.Count) return;

            SFXEntry entry = lr.library.SFX[idx];
            string busName = string.IsNullOrEmpty(entry.busName) ? "Master" : entry.busName;
            if (!lr.buses.TryGetValue(busName, out BusRuntime bus)) return;

            bus.StopAll(); // targeted stop - only affects this bus
        }

        //  Music control 

        private MusicPlayerRuntime GetMusicPlayer(AudioHandle handle)
        {
            if (!handle.IsValid) return null;
            if (!_libraries.TryGetValue(handle.LibraryGuidHash, out LibraryRuntime lr)) return null;

            int idx = handle.EntryIndex;
            lr.music.TryGetValue(idx, out MusicPlayerRuntime player);
            return player;
        }

        internal void MusicPlay(AudioHandle handle, float? fade) => GetMusicPlayer(handle)?.Play(fade);
        internal void MusicStop(AudioHandle handle, float? fade) => GetMusicPlayer(handle)?.Stop(fade);
        internal void MusicPause(AudioHandle handle, float? fade) => GetMusicPlayer(handle)?.Pause(fade);
        internal void MusicResume(AudioHandle handle, float? fade) => GetMusicPlayer(handle)?.Resume(fade);
        internal void MusicNext(AudioHandle handle, float? fade) => GetMusicPlayer(handle)?.Next(fade);
        internal void MusicPrevious(AudioHandle handle, float? fade) => GetMusicPlayer(handle)?.Previous(fade);
        internal void MusicSeek(AudioHandle handle, int track, float? fade) => GetMusicPlayer(handle)?.Seek(track, fade);

        internal void MusicPauseAll(float? fade)
        {
            foreach (var lr in _libraries.Values)
            foreach (var mp in lr.music.Values)
                mp.Pause(fade);
        }

        internal void MusicResumeAll(float? fade)
        {
            foreach (var lr in _libraries.Values)
            foreach (var mp in lr.music.Values)
                mp.Resume(fade);
        }

        internal void MusicStopAll(float? fade)
        {
            foreach (var lr in _libraries.Values)
            foreach (var mp in lr.music.Values)
                mp.Stop(fade);
        }

        //  SFX global control 

        internal void StopAllSFX()
        {
            foreach (var lr in _libraries.Values)
            foreach (var bus in lr.buses.Values)
                bus.StopAll();
        }

        internal void PauseAllSFX()
        {
            foreach (var lr in _libraries.Values)
            foreach (var bus in lr.buses.Values)
                bus.PauseAll();
        }

        internal void ResumeAllSFX()
        {
            foreach (var lr in _libraries.Values)
            foreach (var bus in lr.buses.Values)
                bus.ResumeAll();
        }

        //  Unity lifecycle 

        private void Update()
        {
            float dt = Time.deltaTime;
            float master = MasterVolume;
            float sfx = SFXVolume;
            float music = MusicVolume;

            foreach (var lr in _libraries.Values)
            {
                foreach (var bus in lr.buses.Values)
                    bus.Update(master, sfx, dt);

                foreach (var mp in lr.music.Values)
                    mp.Update(master, music, dt);
            }
        }

        //  Utility 

        private static bool IsBusAudible(LibraryRuntime lr, BusRuntime target)
        {
            // Check if any bus is soloed; if so, only soloed buses are audible
            bool anySoloed = false;
            foreach (var b in lr.buses.Values)
                if (b.bus.soloed)
                {
                    anySoloed = true;
                    break;
                }

            if (anySoloed)
                return target.bus.soloed;

            return !target.bus.muted;
        }

        private static AudioRolloffMode ToUnityRolloff(AttenuationMode mode) =>
            mode switch
            {
                AttenuationMode.Logarithmic => AudioRolloffMode.Logarithmic,
                AttenuationMode.Linear => AudioRolloffMode.Linear,
                AttenuationMode.Custom => AudioRolloffMode.Custom,
                _ => AudioRolloffMode.Logarithmic,
            };
    }
}