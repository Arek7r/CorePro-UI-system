// Component that fires audio actions in response to Unity physics/lifecycle events.
// Drag onto any GameObject. Configure events + conditions + actions in the Inspector
// via AudioTriggerEditor.
//
// Renamed from STEM's "AudioEvents" to "AudioTrigger" - clearer intent.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace CorePro.AudioSystem
{
    // =========================================================================
    // Enums
    // =========================================================================

    /// <summary>The Unity event that activates an <see cref="AudioTriggerEvent"/>.</summary>
    public enum AudioTriggerType
    {
        OnStart,
        OnEnable,
        OnDisable,
        OnDestroy,
        OnMouseDown,
        OnMouseEnter,
        OnMouseExit,
        OnMouseUp,
        OnTriggerEnter,
        OnTriggerExit,
        OnTriggerEnter2D,
        OnTriggerExit2D,
        OnCollisionEnter,
        OnCollisionExit,
        OnCollisionEnter2D,
        OnCollisionExit2D,
    }

    /// <summary>Filter type for physics/collision events.</summary>
    public enum AudioTriggerConditionType
    {
        Tag,
        Layer,
        Name,
        GameObject,
    }

    /// <summary>The type of audio action to perform when the trigger fires.</summary>
    public enum AudioTriggerActionType
    {
        PlaySFX,
        PlaySFX3DAtTrigger,
        PlayMusic,
        StopMusic,
        PauseMusic,
        ResumeMusic,
        NextMusicTrack,
        PreviousMusicTrack,
        StopAllSFX,
        PauseAllSFX,
        ResumeAllSFX,
        SetMasterVolume,
        SetMusicVolume,
        SetSFXVolume,
    }

    // =========================================================================
    // Condition
    // =========================================================================

    /// <summary>
    /// Filter applied to physics/collision events.
    /// All conditions on an event must match for its actions to fire.
    /// </summary>
    [Serializable]
    public sealed class AudioTriggerCondition
    {
        public AudioTriggerConditionType type = AudioTriggerConditionType.Tag;
        public string                    nameOrTag;
        public LayerMask                 layerMask;
        public GameObject                targetGameObject;

        internal bool Matches(GameObject go)
        {
            if (go == null) return true;
            return type switch
            {
                AudioTriggerConditionType.Tag        => go.CompareTag(nameOrTag),
                AudioTriggerConditionType.Layer      => ((1 << go.layer) & layerMask.value) != 0,
                AudioTriggerConditionType.Name       => go.name == nameOrTag,
                AudioTriggerConditionType.GameObject => go == targetGameObject,
                _                                    => false,
            };
        }
    }

    // =========================================================================
    // Action
    // =========================================================================

    /// <summary>
    /// A single audio command executed when a trigger fires.
    /// Serialized as a union - only the fields relevant to the active
    /// <see cref="AudioTriggerActionType"/> are used.
    /// </summary>
    [Serializable]
    public sealed class AudioTriggerAction
    {
        public AudioTriggerActionType type = AudioTriggerActionType.PlaySFX;

        // SFX
        [AudioSFX]   public AudioHandle sfxHandle;
        [AudioMusic] public AudioHandle musicHandle;

        // Volume override for this action (null = use configured value)
        public bool  overrideVolume;
        [Range(0f, 1f)]
        public float volume = 1f;

        // Pitch override for SFX
        public bool  overridePitch;
        [Range(-3f, 3f)]
        public float pitch = 1f;

        // Target value for SetXxxVolume actions
        [Range(0f, 1f)]
        public float targetVolume = 1f;

        internal void Execute(Transform triggerTransform)
        {
            float? vol   = overrideVolume ? volume : (float?)null;
            float? pit   = overridePitch  ? pitch  : (float?)null;

            switch (type)
            {
                case AudioTriggerActionType.PlaySFX:
                    CoreAudio.Play(sfxHandle, vol, pit);
                    break;

                case AudioTriggerActionType.PlaySFX3DAtTrigger:
                    CoreAudio.Play(sfxHandle,
                                   triggerTransform ? triggerTransform.position : Vector3.zero,
                                   vol, pit);
                    break;

                case AudioTriggerActionType.PlayMusic:
                    CoreAudio.Music.Play(musicHandle);
                    break;

                case AudioTriggerActionType.StopMusic:
                    CoreAudio.Music.Stop(musicHandle);
                    break;

                case AudioTriggerActionType.PauseMusic:
                    CoreAudio.Music.Pause(musicHandle);
                    break;

                case AudioTriggerActionType.ResumeMusic:
                    CoreAudio.Music.Resume(musicHandle);
                    break;

                case AudioTriggerActionType.NextMusicTrack:
                    CoreAudio.Music.Next(musicHandle);
                    break;

                case AudioTriggerActionType.PreviousMusicTrack:
                    CoreAudio.Music.Previous(musicHandle);
                    break;

                case AudioTriggerActionType.StopAllSFX:
                    CoreAudio.StopAllSFX();
                    break;

                case AudioTriggerActionType.PauseAllSFX:
                    CoreAudio.PauseAllSFX();
                    break;

                case AudioTriggerActionType.ResumeAllSFX:
                    CoreAudio.ResumeAllSFX();
                    break;

                case AudioTriggerActionType.SetMasterVolume:
                    CoreAudio.MasterVolume = targetVolume;
                    break;

                case AudioTriggerActionType.SetMusicVolume:
                    CoreAudio.MusicVolume = targetVolume;
                    break;

                case AudioTriggerActionType.SetSFXVolume:
                    CoreAudio.SFXVolume = targetVolume;
                    break;
            }
        }
    }

    // =========================================================================
    // Event
    // =========================================================================

    /// <summary>
    /// One trigger event: a type (which Unity callback), zero or more conditions
    /// (filter for physics events), and one or more actions to execute.
    /// </summary>
    [Serializable]
    public sealed class AudioTriggerEvent
    {
        [Tooltip("Human-readable label shown in the Inspector.")]
        public string name = "New Event";

        public AudioTriggerType triggerType = AudioTriggerType.OnStart;

        [Tooltip("All conditions must match for actions to fire. Leave empty to always fire.")]
        public List<AudioTriggerCondition> conditions = new List<AudioTriggerCondition>();

        [Tooltip("Actions executed when the trigger fires and all conditions match.")]
        public List<AudioTriggerAction> actions = new List<AudioTriggerAction>();

        // Editor fold state
#if UNITY_EDITOR
        [NonSerialized] public bool editorExpanded = true;
#endif

        /// <summary>
        /// Returns true if this event type can have conditions (physics events only).
        /// </summary>
        public bool CanHaveConditions =>
            triggerType == AudioTriggerType.OnTriggerEnter    ||
            triggerType == AudioTriggerType.OnTriggerExit     ||
            triggerType == AudioTriggerType.OnTriggerEnter2D  ||
            triggerType == AudioTriggerType.OnTriggerExit2D   ||
            triggerType == AudioTriggerType.OnCollisionEnter  ||
            triggerType == AudioTriggerType.OnCollisionExit   ||
            triggerType == AudioTriggerType.OnCollisionEnter2D||
            triggerType == AudioTriggerType.OnCollisionExit2D;

        internal void Fire(GameObject collidingObject, Transform self)
        {
            // Check all conditions
            for (int i = 0; i < conditions.Count; i++)
                if (!conditions[i].Matches(collidingObject))
                    return;

            // Execute all actions
            for (int i = 0; i < actions.Count; i++)
                actions[i].Execute(self);
        }
    }

    // =========================================================================
    // AudioTrigger MonoBehaviour
    // =========================================================================

    /// <summary>
    /// Attach to any GameObject to trigger audio actions in response to
    /// Unity lifecycle and physics events.
    /// Configure via the custom Inspector.
    /// </summary>
    [AddComponentMenu("CorePro/Audio Trigger")]
    public sealed class AudioTrigger : MonoBehaviour
    {
        [SerializeField] private List<AudioTriggerEvent> _events = new List<AudioTriggerEvent>();

        /// <summary>Read-only access to the configured events (for Editor use).</summary>
        internal List<AudioTriggerEvent> Events => _events;

        // Lifecycle dispatch 
        private void Start()              => Dispatch(null, AudioTriggerType.OnStart);
        private void OnEnable()           => Dispatch(null, AudioTriggerType.OnEnable);
        private void OnDisable()          => Dispatch(null, AudioTriggerType.OnDisable);
        private void OnDestroy()          => Dispatch(null, AudioTriggerType.OnDestroy);
        private void OnMouseDown()        => Dispatch(null, AudioTriggerType.OnMouseDown);
        private void OnMouseEnter()       => Dispatch(null, AudioTriggerType.OnMouseEnter);
        private void OnMouseExit()        => Dispatch(null, AudioTriggerType.OnMouseExit);
        private void OnMouseUp()          => Dispatch(null, AudioTriggerType.OnMouseUp);

        private void OnTriggerEnter(Collider c)      => Dispatch(c.gameObject, AudioTriggerType.OnTriggerEnter);
        private void OnTriggerExit(Collider c)       => Dispatch(c.gameObject, AudioTriggerType.OnTriggerExit);
        private void OnTriggerEnter2D(Collider2D c)  => Dispatch(c.gameObject, AudioTriggerType.OnTriggerEnter2D);
        private void OnTriggerExit2D(Collider2D c)   => Dispatch(c.gameObject, AudioTriggerType.OnTriggerExit2D);
        private void OnCollisionEnter(Collision c)   => Dispatch(c.gameObject, AudioTriggerType.OnCollisionEnter);
        private void OnCollisionExit(Collision c)    => Dispatch(c.gameObject, AudioTriggerType.OnCollisionExit);

        private void OnCollisionEnter2D(Collision2D c) =>
            Dispatch(c.gameObject, AudioTriggerType.OnCollisionEnter2D);
        private void OnCollisionExit2D(Collision2D c) =>
            Dispatch(c.gameObject, AudioTriggerType.OnCollisionExit2D);

        private void Dispatch(GameObject go, AudioTriggerType type)
        {
            Debug.Log($"AR: 2"); 
            for (int i = 0; i < _events.Count; i++)
                if (_events[i].triggerType == type)
                    _events[i].Fire(go, transform);
        }
    }
}
