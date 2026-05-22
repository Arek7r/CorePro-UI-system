using UnityEngine;

namespace CorePro.Utils
{
    /// <summary>
    /// Lightweight, allocation-free timer struct for cooldowns, delays, intervals, and effects.
    /// Designed for maximum efficiency: no GC, no references, no delegates.
    /// </summary>

    /// Use as a value type field and call Tick(deltaTime) each frame.
    /// Usage:
    ///     Timer timer = new Timer(2.0f);
    ///     timer.Start();
    ///     timer.Tick(Time.deltaTime);
    ///     if (timer.IsFinished) { ... }
    ///     // OR as property in MonoBehaviour, call Tick(Time.deltaTime) in Update.
    ///     If at some point you need to force the end of cooldown, use:
    ///         cooldownTimer.Reset(); // lub cooldownTimer.ForceFinish();
    ///     If you change the cooldown dynamically and want to keep the progression - you can call:
    ///         cooldownTimer.RestartWithNewDuration(newCooldown);

    [System.Serializable]
    public struct TimerLight
    {
        [SerializeField] private float duration;
        [SerializeField] private float timeLeft;
        [SerializeField] private bool running;
        [SerializeField] private bool paused;

        /// <summary> Total duration of the timer (seconds). </summary>
        public float Duration => duration;

        /// <summary> Time remaining until the timer finishes (seconds). </summary>
        public float TimeLeft => timeLeft;

        /// <summary> Returns true if the timer is running (not finished, not paused). </summary>
        public bool IsRunning => running;

        /// <summary> Remaining time in full seconds (rounded up, no decimals). Example: 1.1 -> 2. "Start in 3 seconds” </summary>
        public int SecondsLeftCeil => Mathf.CeilToInt(timeLeft);

        /// <summary> Remaining time in full seconds (rounded down, no decimals). Example: 1.9 -> 1. "45s remaining" </summary>
        public int SecondsLeftFloor => Mathf.FloorToInt(timeLeft);
        
        /// <summary> Returns true if the timer is paused. </summary>
        public bool IsPaused => paused;

        /// <summary> Returns true if the timer has finished (just finished in this frame). </summary>
        public bool IsFinished => running == false && timeLeft <= 0f;

        /// <summary> Normalized progress [0..1] (0 = just started, 1 = finished). </summary>
        public float Normalized => duration > 0f ? Mathf.Clamp01(1f - (timeLeft / duration)) : 1f;

        /// <summary> Remaining time as [0..1] (1 = full time left, 0 = finished). </summary>
        public float Remaining01 => duration > 0f ? Mathf.Clamp01(timeLeft / duration) : 0f;

        /// <summary>
        /// Create a new TimerLight with the specified duration.
        /// </summary>
        public TimerLight(float duration)
        {
            this.duration = UnityEngine.Mathf.Max(0f, duration);
            this.timeLeft = 0f;
            this.running = false;
            this.paused = false;
        }

        public void Start() => Start(duration);

       
        /// <summary>
        /// Start or restart the timer with a new duration.
        /// </summary>
        public void Start(float newDuration)
        {
            Reset();    
            duration = UnityEngine.Mathf.Max(0f, newDuration);
            timeLeft = duration;
            running = duration > 0f; 
            paused = false;
        }

        /// <summary>
        /// Update the timer. Call this every frame (e.g. in Update).
        /// </summary>
        /// <param name="deltaTime">Elapsed time since last frame (usually Time.deltaTime).</param>
        public void Tick(float deltaTime)
        {
            if (!running || paused) 
                return;
            
            timeLeft -= deltaTime;
            
            if (timeLeft <= 0f)
            {
                timeLeft = 0f;
                running = false;
            }
        }

        /// <summary>
        /// Reset the timer (stops it and sets remaining time to 0).
        /// </summary>
        public void Reset()
        {
            running = false;
            paused = false;
            timeLeft = 0f;
        }


        /// <summary>
        /// Pause the timer (Tick will not decrease time).
        /// </summary>
        public void Pause() => paused = true;

        /// <summary>
        /// Resume the timer from paused state.
        /// </summary>
        public void Resume() => paused = false;

        /// <summary>
        /// Add or subtract time (in seconds) to the timer.
        /// </summary>
        public void AddTime(float seconds)
        {
            timeLeft += seconds;
            if (timeLeft > duration) timeLeft = duration;
            if (timeLeft < 0f) timeLeft = 0f;
        }

        /// <summary>
        /// Restart the timer with a new duration, keeping current progress percentage.
        /// Useful when duration changes due to modifiers.
        /// Application: the player had 50% cooldown, the cooldown changes from 2s to 1s, the timer "jumps" to half of the new time.
        /// </summary>
        /// <param name="newDuration">New total duration (seconds).</param>
        public void RestartWithNewDuration(float newDuration)
        {
            float progress = Normalized;
            duration = newDuration;
            timeLeft = duration * (1f - progress);
            running = true;
            paused = false;
        }

        /// <summary>
        /// Try to "use" the timer (like for cooldowns): 
        /// if timer is not running, start and return true; else return false.
        /// </summary>
        public bool TryUse()
        {
            if (!IsRunning)
            {
                Start();
                return true;
            }
            return false;
        }
    }
}
