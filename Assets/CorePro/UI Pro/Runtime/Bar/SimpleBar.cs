using UnityEngine;

namespace CorePro.UI
{
    /// <summary>
    /// Simple implementation of UIBar for progress display, resource meters, etc.
    /// Provides inspector controls for testing and configuration.
    /// Zero GC allocation during runtime updates.
    /// </summary>
    [AddComponentMenu("CorePro/UI Tools/Simple Bar")]
    public class SimpleBar : UIBar
    {
        [Header("Initial Values")]
        [Tooltip("Starting value for the bar (clamped to valid range)")]
        [SerializeField] private float initialValue = 100f;
        
        [Tooltip("Maximum capacity (minimum 1)")]
        [SerializeField] [Min(1f)] private float initialMaxValue = 100f;
        
        [Tooltip("Initial overcharge capacity (minimum 0)")]
        [SerializeField] [Min(0f)] private float initialOvercharge = 0f;

        /// <summary>
        /// Gets the current value. Zero GC property access.
        /// </summary>
        public float CurrentValue => currentValue;
        
        /// <summary>
        /// Gets the maximum value. Zero GC property access.
        /// </summary>
        public float MaxValue => maxValue;
        
        /// <summary>
        /// Gets the overcharge value. Zero GC property access.
        /// </summary>
        public float OverchargeValue => overchargeValue;
        
        /// <summary>
        /// Gets the normalized value (0-1 range). Zero GC property access.
        /// </summary>
        public float NormalizedValue => maxValue > 0f ? currentValue / maxValue : 0f;

        /// <summary>
        /// Sets bar value directly. Use for inventory, resources, progress bars.
        /// Zero GC allocation method.
        /// </summary>
        /// <param name="value">Current value (clamped to valid range)</param>
        /// <param name="max">Maximum value (minimum 1)</param>
        /// <param name="overcharge">Optional overcharge capacity (minimum 0)</param>
        public void SetValue(float value, float max, float overcharge = 0f)
        {
            float safeMax = Mathf.Max(1f, max);
            float safeOvercharge = Mathf.Max(0f, overcharge);
            float safeValue = Mathf.Clamp(value, 0f, safeMax + safeOvercharge);
            
            UpdateValue(safeValue, safeMax, safeOvercharge);
        }

        /// <summary>
        /// Sets bar value using existing max and overcharge values.
        /// Zero GC allocation method.
        /// </summary>
        /// <param name="value">Current value (clamped to valid range)</param>
        public void SetValue(float value)
        {
            float safeValue = Mathf.Clamp(value, 0f, maxValue + overchargeValue);
            UpdateValue(safeValue, maxValue, overchargeValue);
        }

        /// <summary>
        /// Sets bar value as normalized (0-1 range).
        /// Zero GC allocation method.
        /// </summary>
        /// <param name="normalizedValue">Value in 0-1 range (clamped automatically)</param>
        public void SetNormalizedValue(float normalizedValue)
        {
            float clampedValue = Mathf.Clamp01(normalizedValue);
            float value = clampedValue * maxValue;
            
            UpdateValue(value, maxValue, overchargeValue);
        }

        /// <summary>
        /// Adds or subtracts value from current amount.
        /// Zero GC allocation method.
        /// </summary>
        /// <param name="amount">Amount to add (negative to subtract)</param>
        public void ModifyValue(float amount)
        {
            float newValue = Mathf.Max(0f, currentValue + amount);
            float cappedValue = Mathf.Min(newValue, maxValue + overchargeValue);
            
            UpdateValue(cappedValue, maxValue, overchargeValue);
        }

        /// <summary>
        /// Changes max capacity (e.g., upgraded storage, increased inventory).
        /// Preserves current value ratio when possible.
        /// Zero GC allocation method.
        /// </summary>
        /// <param name="newMax">New maximum value (minimum 1)</param>
        /// <param name="preserveRatio">If true, maintains current fill percentage</param>
        public void SetMaxValue(float newMax, bool preserveRatio = false)
        {
            float safeMax = Mathf.Max(1f, newMax);
            float newCurrent;
            
            if (preserveRatio == true && maxValue > 0f)
            {
                float ratio = currentValue / maxValue;
                newCurrent = ratio * safeMax;
            }
            else
            {
                newCurrent = Mathf.Min(currentValue, safeMax);
            }
            
            UpdateValue(newCurrent, safeMax, overchargeValue);
        }

        /// <summary>
        /// Resets bar to initial configured values.
        /// Useful for object pooling reset.
        /// </summary>
        public void ResetToInitial()
        {
            SetValue(initialValue, initialMaxValue, initialOvercharge);
        }

        /// <summary>
        /// Sets bar to full (100% of max, no overcharge).
        /// </summary>
        public void SetFull()
        {
            UpdateValue(maxValue, maxValue, overchargeValue);
        }

        /// <summary>
        /// Sets bar to empty (0%).
        /// </summary>
        public void SetEmpty()
        {
            UpdateValue(0f, maxValue, overchargeValue);
        }

        protected override void Awake()
        {
            base.Awake();
            
            // Initialize with configured values
            SetValue(initialValue, initialMaxValue, initialOvercharge);
        }
    }
}
