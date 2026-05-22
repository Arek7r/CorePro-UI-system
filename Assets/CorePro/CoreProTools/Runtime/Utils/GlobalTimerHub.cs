using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using CorePro.Utils; 

namespace CorePro.Timing
{
    
    #region Interfaces
    public interface ITimerCompleted
    {
        // Called when timer completes.
        void OnTimerComplete(GlobalTimerHub.TimerHandle handle, int token);
    }

    public interface ITimerUpdateListener
    {
        // Called periodically during timer lifetime (based on updateInterval)
        void OnTimerUpdate(GlobalTimerHub.TimerHandle handle, float remaining);
    }

    public interface ITimerCancelListener
    {
        // Called periodically during timer lifetime (based on updateInterval)
        void OnTimerCancel(GlobalTimerHub.TimerHandle handle, float remaining);
    }
    
    public interface ITimerListener
    {
        // Called periodically during timer lifetime (based on updateInterval)
        void TimerUpdated(GlobalTimerHub.TimerHandle handle, float remaining);
        void TimerCompleted(GlobalTimerHub.TimerHandle handle, int token);
    }
    
    #endregion
    
    /// <summary>
    /// Centralized global timer manager.
    /// - Zero-GC architecture (no delegates, no LINQ)
    /// - Supports OnTimerUpdate and OnTimerComplete
    /// - Allows per-timer update interval
    /// - Auto-suspends when owner is inactive or destroyed
    /// - Persisted across scenes (DontDestroyOnLoad)
    /// </summary>
    public sealed class GlobalTimerHub : Singleton<GlobalTimerHub>
    {
        #region Singleton

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Clear() => ResetSingleton();
#endif

        #endregion
        
        #region Structs

        [Serializable]
        public struct TimerHandle
        {
            public int Id;
            public int Version;

            public bool IsValid => Id >= 0;
            public static TimerHandle Invalid => new TimerHandle { Id = -1, Version = -1 };
        }

        private struct Slot
        {
            public float Duration;
            public float TimeLeft;
            public bool Running;
            public bool UseUnscaledTime;
            public bool Paused;

            public int Version;
            public int Token;

            public TimerChannel Channel;
            public MonoBehaviour Owner;

            public ITimerCancelListener canceled;
            public ITimerCompleted completed;
            public ITimerUpdateListener UpdateListener;

            public float UpdateInterval;
            public float TimeSinceLastUpdate;
        }

        public enum TimerChannel : byte
        {
            Gameplay = 0,
            UI = 1,
            Background = 2
        }

        private struct CompletionEvent
        {
            public int Id;
            public int Version;
            public ITimerCompleted completed;
            public int Token;
            public TimerHandle Handle;
        }

        #endregion

        #region Fields

        private Slot[] _slots;
        private int _count;

        private int[] _free;
        private int _freeCount;

        private bool[] _channelPaused;
        private readonly List<CompletionEvent> _completionQueue = new List<CompletionEvent>(256);

        private float _dtScaled;
        private float _dtUnscaled;

        #endregion

        #region Unity Lifecycle

        protected override void Awake()
        {
            doNotDestroyOnLoad = true;
            base.Awake();
            if (this != Instance) return;

            const int initialCapacity = 256;
            _slots = new Slot[initialCapacity];
            _free = new int[initialCapacity];
            _channelPaused = new bool[Enum.GetValues(typeof(TimerChannel)).Length];

            SceneManager.activeSceneChanged += OnSceneChanged;
        }

        protected override void OnDestroy()
        {
            if (this == Instance)
                SceneManager.activeSceneChanged -= OnSceneChanged;

            base.OnDestroy();
        }

        private void OnSceneChanged(Scene from, Scene to)
        {
            // You can add, for example, auto-pausing of the Gameplay channel when changing scenes.
        }

        private void Update()
        {
            _dtScaled = Time.deltaTime;
            _dtUnscaled = Time.unscaledDeltaTime;

            for (int i = 0; i < _count; i++)
            {
                ref var s = ref _slots[i];
                if (!s.Running || s.Paused)
                    continue;

                if (_channelPaused[(int)s.Channel])
                    continue;

                if (s.Owner != null)
                {
                    if (ReferenceEquals(s.Owner, null) || IsDisabled(s.Owner))
                    {
                        CancelInternal(i);
                        continue;
                    }

                    if (!s.Owner.isActiveAndEnabled)
                        continue;
                }

                float dt = s.UseUnscaledTime ? _dtUnscaled : _dtScaled;
                if (dt <= 0f)
                    continue;

                s.TimeLeft -= dt;
                s.TimeSinceLastUpdate += dt;

                
                if (s.UpdateListener != null && s.UpdateInterval > 0f && s.TimeSinceLastUpdate >= s.UpdateInterval)
                {
                    s.TimeSinceLastUpdate = 0f;
                    float remaining = Mathf.Max(0f, s.TimeLeft);
                    s.UpdateListener.OnTimerUpdate(new TimerHandle { Id = i, Version = s.Version }, remaining);
                }

                // Timer finished
                if (s.TimeLeft <= 0f)
                {
                 
                    if (s.UpdateListener != null && s.UpdateInterval > 0f)
                    {
                        s.TimeSinceLastUpdate = 0f;
                        s.UpdateListener.OnTimerUpdate(new TimerHandle { Id = i, Version = s.Version }, 0);
                    }
                    
                    QueueCompletion(i);
                    s.Running = false;
                }
            }

            // Deliver completion events
            for (int c = 0; c < _completionQueue.Count; c++)
            {
                var ev = _completionQueue[c];
                if (ev.Id < 0 || ev.Id >= _count)
                    continue;
                if (_slots[ev.Id].Version != ev.Version)
                    continue;

                ev.completed?.OnTimerComplete(ev.Handle, ev.Token);

                // Auto-cleanup update-listenera
                _slots[ev.Id].UpdateListener = null;
            }

            _completionQueue.Clear();
        }

        #endregion

        #region Public API

        public TimerHandle Create(
            float duration,
            bool useUnscaledTime = false,
            int token = 0,
            TimerChannel channel = TimerChannel.Gameplay,
            MonoBehaviour owner = null,
            ITimerCancelListener cancel = null,
            ITimerCompleted complete = null,
            ITimerUpdateListener updateListener = null,
            float updateInterval = 1f)
        {
            int id = AllocateSlot();
            ref var s = ref _slots[id];

            s.Duration = Mathf.Max(0f, duration);
            s.TimeLeft = s.Duration;
            s.Running = s.Duration > 0f;
            s.UseUnscaledTime = useUnscaledTime;
            s.Channel = channel;
            s.completed = complete;
            s.canceled = cancel;
            s.UpdateListener = updateListener;
            s.UpdateInterval = updateInterval;
            s.TimeSinceLastUpdate = 100f;
            s.Token = token;
            s.Owner = owner;
            s.Version++;

            TimerHandle h;
            h.Id = id;
            h.Version = s.Version;
            return h;
        }

        public void Cancel(TimerHandle handle)
        {
            if (!Validate(handle))
                return;
            
            CancelInternal(handle.Id);
        }
        
        /// <summary>
        /// Cancels all active timers associated with a specific owner.
        /// Useful for stopping multiple related processes (e.g., logic, VFX, and UI) simultaneously.
        /// </summary>
        /// <param name="owner">The MonoBehaviour owner whose timers should be canceled.</param>
        public void CancelAllByOwner(MonoBehaviour owner)
        {
            if (owner == null) return;

            // Iterate only through allocated slots
            for (int i = 0; i < _count; i++)
            {
                // Check if the slot is currently active and belongs to the specified owner
                if (_slots[i].Running && _slots[i].Owner == owner)
                {
                    CancelInternal(i);
                }
            }
        }

        public bool TryGetRemaining(TimerHandle handle, out float remaining)
        {
            if (!Validate(handle))
            {
                remaining = 0f;
                return false;
            }
            remaining = Mathf.Max(0f, _slots[handle.Id].TimeLeft);
            return true;
        }

        public bool TryGetNormalized(TimerHandle handle, out float progress)
        {
            if (!Validate(handle))
            {
                progress = 0f;
                return false;
            }

            ref var s = ref _slots[handle.Id];
            progress = s.Duration > 0f ? Mathf.Clamp01(1f - s.TimeLeft / s.Duration) : 1f;
            return true;
        }

        public void SetChannelPaused(TimerChannel channel, bool paused)
        {
            _channelPaused[(int)channel] = paused;
        }

        #endregion

        #region Internal

        private int AllocateSlot()
        {
            if (_freeCount > 0)
            {
                _freeCount--;
                return _free[_freeCount];
            }

            if (_count >= _slots.Length)
            {
                int newCap = _slots.Length << 1;
                Array.Resize(ref _slots, newCap);
                Array.Resize(ref _free, newCap);
            }

            return _count++;
        }
        
        private bool IsDisabled(MonoBehaviour sOwner)   
        {   
            //return sOwner.gameObject.activeSelf == false;
            return sOwner.gameObject.activeInHierarchy == false;
        } 

        private void CancelInternal(int id)
        {
            ref var s = ref _slots[id];
    
            // Notify if anyone is listening for cancellation
            s.canceled?.OnTimerCancel(new TimerHandle { Id = id, Version = s.Version }, s.TimeLeft);
            
            //Clean everything
            s.Running = false;
            s.completed = null;
            s.UpdateListener = null;
            s.canceled = null;
            s.Owner = null;
            s.TimeLeft = 0f;
            s.Version++; // Version increment invalidates old Handle
    
            _free[_freeCount++] = id; // Return the slot to the pool
        }

        private bool Validate(TimerHandle handle)
        {
            int id = handle.Id;
            return id >= 0 && id < _count && _slots[id].Version == handle.Version;
        }

        private void QueueCompletion(int id)
        {
            ref var s = ref _slots[id];

            CompletionEvent ev;
            ev.Id = id;
            ev.Version = s.Version;
            ev.completed = s.completed;
            ev.Token = s.Token;
            ev.Handle = new TimerHandle { Id = id, Version = s.Version };
            _completionQueue.Add(ev);
        }

        #endregion
    }
}
