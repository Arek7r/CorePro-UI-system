using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CorePro.UI
{
    /// <summary>
    /// Advanced UI Bar with effects, animations, segmentation, and events.
    /// Optimized for zero GC allocation and proper pause handling.
    /// </summary>
    [AddComponentMenu("CorePro/UI Tools/Advanced Bar")]
    public class AdvancedBar : UIBar
    {
        [Header("Initial Values")]
        [Tooltip("Starting value for the bar (clamped to valid range)")]
        [SerializeField] private float initialValue = 100f;
        
        [Tooltip("Maximum capacity (minimum 1)")]
        [SerializeField] [Min(1f)] private float initialMaxValue = 100f;
        
        [Tooltip("Initial overcharge capacity (minimum 0)")]
        [SerializeField] [Min(0f)] private float initialOvercharge = 0f;
        
        [Header("Ghost/Delayed Fill")]
        [SerializeField] private bool enableGhostBar = false;
        [SerializeField] private Image ghostFill;
        [SerializeField] private float ghostDelay = 0.5f;
        [SerializeField] private float ghostSpeed = 2f;
        [SerializeField] private Color ghostColor = new Color(1f, 0f, 0f, 0.5f);
        
        [Header("Segmentation")]
        [SerializeField] private bool enableSegments = false;
        [SerializeField] private int segmentCount = 10;
        [SerializeField] private RectTransform segmentContainer;
        [SerializeField] private Image segmentPrefab;
        [SerializeField] private float segmentSpacing = 2f;
        
        [Header("Fill Direction")]
        [SerializeField] private FillMode fillMode = FillMode.HorizontalLeftToRight;
        
        [Header("Thresholds")]
        [SerializeField] private bool enableThresholds = false;
        [SerializeField] private List<ThresholdData> thresholds = new List<ThresholdData>();
        
        [Header("Text Formatting")]
        [SerializeField] private NumberFormat numberFormat = NumberFormat.Standard;
        [SerializeField] private bool animateNumbers = false;
        [SerializeField] private float numberAnimationSpeed = 5f;
        [SerializeField] private string customPrefix = "";
        [SerializeField] private string customSuffix = "";
        
        [Header("Visual Effects")]
        [SerializeField] private bool enableFlashOnDamage = false;
        [SerializeField] private Color flashColor = Color.red;
        [SerializeField] private float flashDuration = 0.2f;
        
        [SerializeField] private bool enablePulseOnLow = false;
        [SerializeField] private float pulseBelowPercent = 0.25f;
        [SerializeField] private Color pulseColor = Color.red;
        [SerializeField] private float pulseSpeed = 3f;
        
        [SerializeField] private bool enableShakeOnHit = false;
        [SerializeField] private float shakeIntensity = 5f;
        [SerializeField] private float shakeDuration = 0.2f;
        
        [Header("Update Mode")]
        [SerializeField] private UpdateMode updateMode = UpdateMode.Continuous;
        [SerializeField] private float throttleRate = 0.1f;
        
        [Header("Time Settings")]
        [SerializeField] private TimeMode timeMode = TimeMode.Scaled;
        
        [Header("Events")]
        public UnityEvent<float> onValueChanged;
        public UnityEvent onEmpty;
        public UnityEvent onFull;
        public UnityEvent<float> onThresholdCrossed;

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
        
        private float ghostTargetFill;
        private float ghostCurrentFill;
        private float ghostTimer;
        
        private float displayedNumber;
        private float previousValue;
        
        private EffectState flashState;
        private EffectState pulseState;
        private EffectState shakeState;
        
        private Vector2 originalBarPosition;
        private float throttleTimer;
        
        private List<Image> segmentImages = new List<Image>();
        private int lastTriggeredThresholdIndex = -1;
        
        private struct EffectState
        {
            public bool isActive;
            public float timer;
            public float duration;
            public Color startColor;
            public Color targetColor;
        }

        public enum FillMode
        {
            HorizontalLeftToRight,
            HorizontalRightToLeft,
            VerticalBottomToTop,
            VerticalTopToBottom,
            Radial
        }

        public enum NumberFormat
        {
            Standard,           // 1234567
            Abbreviated,        // 1.2M
            Percentage,         // 45%
            PercentageDecimal   // 45.5%
        }

        public enum UpdateMode
        {
            Continuous,
            OnValueChange,
            Throttled
        }

        public enum TimeMode
        {
            Scaled,      // Uses Time.deltaTime (affected by pause)
            Unscaled     // Uses Time.unscaledDeltaTime (ignores pause)
        }

        [System.Serializable]
        public struct ThresholdData
        {
            public float percentage;
            public Color color;
            public bool triggerEvent;
        }

        protected override void Awake()
        {
            base.Awake();
            
            if (mainFill != null && mainFill.rectTransform != null)
            {
                originalBarPosition = mainFill.rectTransform.anchoredPosition;
            }

            if (enableSegments == true)
            {
                GenerateSegments();
            }

            if (Application.isPlaying == true)
            {
                SetValue(initialValue, initialMaxValue, initialOvercharge);
                displayedNumber = initialValue;
            }
        }

        protected override void Update()
        {
            base.Update();

            float deltaTime = GetDeltaTime();

            if (updateMode == UpdateMode.Throttled)
            {
                throttleTimer += deltaTime;
                if (throttleTimer < throttleRate)
                {
                    return;
                }
                throttleTimer = 0f;
            }

            UpdateGhostBar(deltaTime);
            UpdateFlashEffect(deltaTime);
            UpdatePulseEffect(deltaTime);
            UpdateShakeEffect(deltaTime);
            UpdateNumberAnimation(deltaTime);
        }

        public override void UpdateValue(float current, float max, float overcharge = 0f)
        {
            if (updateMode == UpdateMode.OnValueChange)
            {
                if (Mathf.Approximately(current, currentValue) == true && 
                    Mathf.Approximately(max, maxValue) == true && 
                    Mathf.Approximately(overcharge, overchargeValue) == true)
                {
                    return;
                }
            }

            float oldValue = currentValue;
            bool wasEmpty = currentValue <= 0f;
            bool wasFull = currentValue >= maxValue && overchargeValue <= 0f;

            base.UpdateValue(current, max, overcharge);

            if (enableGhostBar == true && ghostFill != null)
            {
                if (current < oldValue)
                {
                    ghostTargetFill = targetFillAmount;
                    ghostTimer = 0f;
                }
                else
                {
                    ghostCurrentFill = targetFillAmount;
                }
            }

            if (enableFlashOnDamage == true && current < oldValue)
            {
                TriggerFlashEffect();
            }

            if (enableShakeOnHit == true && current < oldValue)
            {
                TriggerShakeEffect();
            }

            CheckThresholds(currentValue / maxValue);

            onValueChanged?.Invoke(currentValue);

            if (wasEmpty == false && currentValue <= 0f)
            {
                onEmpty?.Invoke();
            }

            if (wasFull == false && currentValue >= maxValue && overchargeValue <= 0f)
            {
                onFull?.Invoke();
            }

            UpdateSegmentDisplay();
            ApplyFillMode();
        }

        protected override void UpdateTextDisplay()
        {
            if (valueText == null || showText == false) return;

            float displayValue = animateNumbers == true ? displayedNumber : currentValue;

            string formattedValue = FormatNumber(displayValue);
            string formattedMax = FormatNumber(maxValue);

            switch (textMode)
            {
                case TextDisplayMode.None:
                    valueText.text = string.Empty;
                    break;
                    
                case TextDisplayMode.CurrentOnly:
                    valueText.text = $"{customPrefix}{formattedValue}{customSuffix}";
                    break;
                    
                case TextDisplayMode.CurrentAndMax:
                    valueText.text = $"{customPrefix}{formattedValue}/{formattedMax}{customSuffix}";
                    break;
                    
                case TextDisplayMode.Percentage:
                    int percentage = Mathf.RoundToInt((displayValue / maxValue) * 100f);
                    valueText.text = $"{customPrefix}{percentage}%{customSuffix}";
                    break;
            }
        }

        private string FormatNumber(float value)
        {
            switch (numberFormat)
            {
                case NumberFormat.Standard:
                    return Mathf.CeilToInt(value).ToString();
                    
                case NumberFormat.Abbreviated:
                    return AbbreviateNumber(value);
                    
                case NumberFormat.Percentage:
                    return Mathf.RoundToInt((value / maxValue) * 100f).ToString();
                    
                case NumberFormat.PercentageDecimal:
                    return ((value / maxValue) * 100f).ToString("F1");
                    
                default:
                    return Mathf.CeilToInt(value).ToString();
            }
        }

        private string AbbreviateNumber(float value)
        {
            if (value >= 1000000000f)
                return (value / 1000000000f).ToString("F1") + "B";
            if (value >= 1000000f)
                return (value / 1000000f).ToString("F1") + "M";
            if (value >= 1000f)
                return (value / 1000f).ToString("F1") + "K";
            return Mathf.CeilToInt(value).ToString();
        }

        private void UpdateGhostBar(float deltaTime)
        {
            if (enableGhostBar == false || ghostFill == null) return;

            ghostTimer += deltaTime;

            if (ghostTimer >= ghostDelay)
            {
                ghostCurrentFill = Mathf.Lerp(ghostCurrentFill, ghostTargetFill, deltaTime * ghostSpeed);
                ghostFill.fillAmount = ghostCurrentFill;
            }
        }

        private void UpdateNumberAnimation(float deltaTime)
        {
            if (animateNumbers == false) return;

            if (Mathf.Abs(displayedNumber - currentValue) > 0.1f)
            {
                displayedNumber = Mathf.Lerp(displayedNumber, currentValue, deltaTime * numberAnimationSpeed);
                UpdateTextDisplay();
            }
            else if (Mathf.Approximately(displayedNumber, currentValue) == false)
            {
                displayedNumber = currentValue;
                UpdateTextDisplay();
            }
        }

        private void TriggerFlashEffect()
        {
            if (mainFill == null) return;

            flashState.isActive = true;
            flashState.timer = 0f;
            flashState.duration = flashDuration;
            flashState.startColor = mainFill.color;
            flashState.targetColor = flashColor;
        }

        private void UpdateFlashEffect(float deltaTime)
        {
            if (flashState.isActive == false || mainFill == null) return;

            flashState.timer += deltaTime;
            float progress = flashState.timer / flashState.duration;

            if (progress >= 1f)
            {
                flashState.isActive = false;
                mainFill.color = flashState.startColor;
            }
            else
            {
                mainFill.color = Color.Lerp(flashState.targetColor, flashState.startColor, progress);
            }
        }

        private void UpdatePulseEffect(float deltaTime)
        {
            if (enablePulseOnLow == false || mainFill == null) return;

            float normalizedValue = currentValue / maxValue;

            if (normalizedValue <= pulseBelowPercent)
            {
                if (pulseState.isActive == false)
                {
                    pulseState.isActive = true;
                    pulseState.timer = 0f;
                    pulseState.startColor = mainFill.color;
                }

                pulseState.timer += deltaTime * pulseSpeed;
                float pingPong = (Mathf.Sin(pulseState.timer) + 1f) * 0.5f;
                mainFill.color = Color.Lerp(pulseState.startColor, pulseColor, pingPong);
            }
            else if (pulseState.isActive == true)
            {
                pulseState.isActive = false;
                UpdateMainFillColor(normalizedValue);
            }
        }

        private void TriggerShakeEffect()
        {
            shakeState.isActive = true;
            shakeState.timer = 0f;
            shakeState.duration = shakeDuration;
        }

        private void UpdateShakeEffect(float deltaTime)
        {
            if (shakeState.isActive == false || mainFill == null) return;

            shakeState.timer += deltaTime;
            float progress = shakeState.timer / shakeState.duration;

            if (progress >= 1f)
            {
                shakeState.isActive = false;
                mainFill.rectTransform.anchoredPosition = originalBarPosition;
            }
            else
            {
                float intensity = shakeIntensity * (1f - progress);
                Vector2 randomOffset = Random.insideUnitCircle * intensity;
                mainFill.rectTransform.anchoredPosition = originalBarPosition + randomOffset;
            }
        }

        private void CheckThresholds(float normalizedValue)
        {
            if (enableThresholds == false || thresholds == null || thresholds.Count == 0) return;

            for (int i = 0; i < thresholds.Count; i++)
            {
                ThresholdData threshold = thresholds[i];
                
                if (normalizedValue <= threshold.percentage && i != lastTriggeredThresholdIndex)
                {
                    if (mainFill != null)
                    {
                        mainFill.color = threshold.color;
                    }

                    if (threshold.triggerEvent == true)
                    {
                        onThresholdCrossed?.Invoke(threshold.percentage);
                    }

                    lastTriggeredThresholdIndex = i;
                    break;
                }
            }

            if (normalizedValue > thresholds[0].percentage)
            {
                lastTriggeredThresholdIndex = -1;
            }
        }

        private void GenerateSegments()
        {
            if (segmentContainer == null || segmentPrefab == null) return;

            ClearSegments();

            float segmentWidth = (segmentContainer.rect.width - (segmentSpacing * (segmentCount - 1))) / segmentCount;

            for (int i = 0; i < segmentCount; i++)
            {
                Image segment = Instantiate(segmentPrefab, segmentContainer);
                RectTransform segmentRect = segment.rectTransform;
                
                segmentRect.anchorMin = new Vector2(0f, 0f);
                segmentRect.anchorMax = new Vector2(0f, 1f);
                segmentRect.sizeDelta = new Vector2(segmentWidth, 0f);
                segmentRect.anchoredPosition = new Vector2(i * (segmentWidth + segmentSpacing), 0f);

                segmentImages.Add(segment);
            }
        }

        private void ClearSegments()
        {
            for (int i = segmentImages.Count - 1; i >= 0; i--)
            {
                if (segmentImages[i] != null)
                {
                    if (Application.isPlaying == true)
                    {
                        Destroy(segmentImages[i].gameObject);
                    }
                    else
                    {
                        DestroyImmediate(segmentImages[i].gameObject);
                    }
                }
            }
            segmentImages.Clear();
        }

        private void UpdateSegmentDisplay()
        {
            if (enableSegments == false || segmentImages.Count == 0) return;

            float normalizedValue = currentValue / maxValue;
            int filledSegments = Mathf.CeilToInt(normalizedValue * segmentCount);

            for (int i = 0; i < segmentImages.Count; i++)
            {
                if (segmentImages[i] != null)
                {
                    segmentImages[i].fillAmount = i < filledSegments ? 1f : 0f;
                }
            }
        }

        private void ApplyFillMode()
        {
            if (mainFill == null) return;

            switch (fillMode)
            {
                case FillMode.HorizontalLeftToRight:
                    mainFill.type = Image.Type.Filled;
                    mainFill.fillMethod = Image.FillMethod.Horizontal;
                    mainFill.fillOrigin = (int)Image.OriginHorizontal.Left;
                    break;

                case FillMode.HorizontalRightToLeft:
                    mainFill.type = Image.Type.Filled;
                    mainFill.fillMethod = Image.FillMethod.Horizontal;
                    mainFill.fillOrigin = (int)Image.OriginHorizontal.Right;
                    break;

                case FillMode.VerticalBottomToTop:
                    mainFill.type = Image.Type.Filled;
                    mainFill.fillMethod = Image.FillMethod.Vertical;
                    mainFill.fillOrigin = (int)Image.OriginVertical.Bottom;
                    break;

                case FillMode.VerticalTopToBottom:
                    mainFill.type = Image.Type.Filled;
                    mainFill.fillMethod = Image.FillMethod.Vertical;
                    mainFill.fillOrigin = (int)Image.OriginVertical.Top;
                    break;

                case FillMode.Radial:
                    mainFill.type = Image.Type.Filled;
                    mainFill.fillMethod = Image.FillMethod.Radial360;
                    mainFill.fillOrigin = (int)Image.Origin360.Top;
                    break;
            }

            if (overchargeFill != null && fillMode != FillMode.Radial)
            {
                overchargeFill.type = mainFill.type;
                overchargeFill.fillMethod = mainFill.fillMethod;
                overchargeFill.fillOrigin = mainFill.fillOrigin;
            }

            if (ghostFill != null && fillMode != FillMode.Radial)
            {
                ghostFill.type = mainFill.type;
                ghostFill.fillMethod = mainFill.fillMethod;
                ghostFill.fillOrigin = mainFill.fillOrigin;
            }
        }

        private float GetDeltaTime()
        {
            return timeMode == TimeMode.Scaled ? Time.deltaTime : Time.unscaledDeltaTime;
        }

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
        /// Sets the overcharge capacity.
        /// Zero GC allocation method.
        /// </summary>
        /// <param name="newOvercharge">New overcharge value (minimum 0)</param>
        public void SetOvercharge(float newOvercharge)
        {
            float safeOvercharge = Mathf.Max(0f, newOvercharge);
            UpdateValue(currentValue, maxValue, safeOvercharge);
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
    }
}
