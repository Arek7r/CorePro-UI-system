using System;
using System.Collections;
using System.Collections.Generic;
using InspectorPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Serialization;

namespace CorePro.UI
{
    /// <summary>
    /// Standalone progress bar - no inheritance, all-in-one.
    /// Designed for loading screens, crafting timers, quest progress, tutorials, and XP bars.
    /// Supports checkpoints with indicators, indeterminate mode, ETA display, and auto-complete.
    /// Zero GC allocation during runtime updates.
    /// </summary>
    [AddComponentMenu("CorePro/UI Tools/UI Progress Bar")]
    public class UIProgressBar : MonoBehaviourCorePro
    {
        #region Visual References

        [Group("Visual References")]
        [SerializeField] private Image mainFill;
        [SerializeField] private Image background;
        [SerializeField] private TextMeshProUGUI valueText;

        [Tooltip("Optional second text label - receives the same value as Value Text. " +
                 "Useful when a duplicate indicator sits on a contrasting background.")]
        [SerializeField] private TextMeshProUGUI valueText2;

        #endregion

        #region Initial Values

        [Header("Initial Values")]
        [Tooltip("Starting progress value (clamped to 0-max)")]
        [SerializeField] private float initialValue = 0f;

        [Tooltip("Maximum value (minimum 1)")]
        [SerializeField] [Min(1f)] private float initialMaxValue = 100f;

        [SerializeField]
        [DynamicRange("initialValue", "initialMaxValue")]
        private float currentValue;
        
        #endregion

        #region Display Options

        [Group("Display Options")]
        [SerializeField] private bool showText = true;
        [SerializeField] private bool hideWhenFull = false;
        [SerializeField] private TextDisplayMode textMode = TextDisplayMode.Percentage;
        [SerializeField] private string customPrefix = "";
        [SerializeField] private string customSuffix = "";

        #endregion

        #region Visual Styling

        [Group("Visual Styling")]
        [SerializeField] private bool useGradient = false;
        [SerializeField] private Gradient colorGradient;
        [SerializeField] private Color solidColor = new Color(0.2f, 0.7f, 0.2f, 1f);

        #endregion

        #region Smooth Transition

        [Group("Smooth Transition")]
        [SerializeField] private bool smoothTransition = false;
        [SerializeField] private float transitionSpeed = 5f;

        #endregion

        #region Checkpoints

        [Group("Checkpoints")]
        [Tooltip("Enable checkpoint markers along the bar")]
        [SerializeField] private bool enableCheckpoints = false;

        [SerializeField] private List<ProgressCheckpoint> checkpoints = new List<ProgressCheckpoint>();

        [Tooltip("Prefab for the indicator marker placed on the bar at each checkpoint position")]
        [SerializeField] private RectTransform indicatorPrefab;

        [Tooltip("Parent container for spawned indicators (defaults to mainFill parent if null)")]
        [SerializeField] private RectTransform indicatorContainer;

        [Tooltip("Size of each indicator marker")]
        [SerializeField] private Vector2 indicatorSize = new Vector2(16f, 16f);

        #endregion

        #region Indeterminate Mode

        [Group("Indeterminate Mode")]
        [Tooltip("When true, shows an animated bouncing strip - use when progress amount is unknown")]
        [SerializeField] private bool indeterminateMode = false;

        [Tooltip("Image used as the bouncing strip (separate from mainFill)")]
        [SerializeField] private Image indeterminateStrip;

        [Tooltip("Width of the strip as a fraction of the bar width (0-1)")]
        [SerializeField] [Range(0.05f, 0.5f)] private float stripWidth = 0.2f;

        [Tooltip("How fast the strip bounces back and forth")]
        [SerializeField] private float indeterminateSpeed = 1.5f;

        #endregion

        #region ETA Display

        [Group("ETA Display")]
        [Tooltip("Show estimated time remaining based on recent fill rate")]
        [SerializeField] private bool showEta = false;

        [Tooltip("Text field for the ETA label")]
        [SerializeField] private TextMeshProUGUI etaText;

        [Tooltip("Format string - use {0} for seconds. E.g. '~{0}s'")]
        [SerializeField] private string etaFormat = "~{0}s";

        [Tooltip("How many seconds of history to use when estimating fill rate")]
        [SerializeField] [Min(0.5f)] private float etaSampleWindow = 3f;

        #endregion

        #region Auto Complete

        [Group("Auto Complete")]
        [Tooltip("When enabled, the bar will automatically fill to 100% after a delay")]
        [SerializeField] private bool autoComplete = false;

        [Tooltip("Seconds to wait before starting the auto-fill")]
        [SerializeField] [Min(0f)] private float autoCompleteDelay = 0f;

        [Tooltip("Seconds to spend filling from current value to 100%")]
        [SerializeField] [Min(0.01f)] private float autoCompleteDuration = 1f;

        #endregion

        #region Time Settings

        [Group("Time Settings")]
        [SerializeField] private TimeMode timeMode = TimeMode.Scaled;

        #endregion

        #region Events

        [Group("Events",true)]
        public UnityEvent onComplete;
        public UnityEvent onReset;
        public UnityEvent<float> onValueChanged;
        public UnityEvent<ProgressCheckpoint> onCheckpointReached;

        #endregion

        #region Enums & Serializable Types

        public enum TextDisplayMode
        {
            None,
            CurrentOnly,
            CurrentAndMax,
            Percentage
        }

        public enum TimeMode
        {
            Scaled,
            Unscaled
        }

        [Serializable]
        public struct ProgressCheckpoint
        {
            [Tooltip("Progress percentage to place this checkpoint at (0-1)")]
            [Range(0f, 1f)] public float atPercent;

            [Tooltip("Optional label shown on or near the indicator")]
            public string label;

            [Tooltip("Fired once when progress crosses this checkpoint")]
            public UnityEvent onReached;
        }

        #endregion

        #region Public Properties

        /// <summary>Gets current progress value. Zero GC.</summary>
        public float CurrentValue => currentValue;

        /// <summary>Gets max value. Zero GC.</summary>
        public float MaxValue => _maxValue;

        /// <summary>Gets normalized progress (0-1). Zero GC.</summary>
        public float CurrentPercent => _maxValue > 0f ? currentValue / _maxValue : 0f;

        /// <summary>True when progress has reached 100%.</summary>
        public bool IsComplete => currentValue >= _maxValue;

        /// <summary>True when indeterminate mode is active.</summary>
        public bool IsIndeterminate => indeterminateMode;

        #endregion

        #region Private State

        private float _maxValue = 100f;

        // Reused for text formatting - avoids intermediate string allocations per update
        private readonly System.Text.StringBuilder _textBuilder = new System.Text.StringBuilder(32);

        private float _targetFillAmount;
        private float _displayedFillAmount;

        private Color _cachedMainColor;
        private bool _isInitialized = false;

        // Checkpoints
        private List<RectTransform> _spawnedIndicators = new List<RectTransform>();
        private int _lastReachedCheckpointIndex = -1;

        // Indeterminate
        private float _stripPhase = 0f;

        // ETA
        private float _etaSampleTime = 0f;
        private float _etaSampleValueStart = 0f;
        private float _etaEstimatedSeconds = -1f;

        // Auto complete
        private Coroutine _autoCompleteCoroutine;

        private float DeltaTime => timeMode == TimeMode.Scaled ? Time.deltaTime : Time.unscaledDeltaTime;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            Initialize();

            if (Application.isPlaying)
            {
                SetValue(initialValue, initialMaxValue);
                _etaSampleValueStart = initialValue;

                if (enableCheckpoints)
                    SpawnIndicators();

                if (autoComplete)
                    _autoCompleteCoroutine = StartCoroutine(AutoCompleteCoroutine());
            }
        }

        private void Initialize()
        {
            if (_isInitialized) return;

            _maxValue = Mathf.Max(1f, initialMaxValue);

            if (mainFill != null)
            {
                _cachedMainColor = useGradient ? colorGradient.Evaluate(1f) : solidColor;
                mainFill.color = _cachedMainColor;
                mainFill.enabled = !indeterminateMode;
            }

            if (indeterminateStrip != null)
                indeterminateStrip.enabled = indeterminateMode;

            UpdateTextVisibility();

            _isInitialized = true;
        }

        /// <summary>Forces re-initialization. Use after programmatic setup before first SetValue.</summary>
        public void ForceInitialize()
        {
            _isInitialized = false;
            Initialize();
        }

        private void Update()
        {
            UpdateSmoothTransition();

            if (indeterminateMode)
                UpdateIndeterminate();

            if (showEta && Application.isPlaying)
                UpdateEta();
        }

        private void OnDestroy()
        {
            if (_autoCompleteCoroutine != null)
                StopCoroutine(_autoCompleteCoroutine);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Sets progress using current and max values.
        /// Zero GC allocation.
        /// </summary>
        public void SetValue(float value, float max)
        {
            float safeMax = Mathf.Max(1f, max);
            float safeValue = Mathf.Clamp(value, 0f, safeMax);
            UpdateValue(safeValue, safeMax);
        }

        /// <summary>
        /// Sets progress using current value only, keeping existing max.
        /// Zero GC allocation.
        /// </summary>
        public void SetValue(float value)
        {
            float safeValue = Mathf.Clamp(value, 0f, _maxValue);
            UpdateValue(safeValue, _maxValue);
        }

        /// <summary>
        /// Sets progress as a normalized value (0-1).
        /// Zero GC allocation.
        /// </summary>
        public void SetNormalizedValue(float normalized)
        {
            float clamped = Mathf.Clamp01(normalized);
            UpdateValue(clamped * _maxValue, _maxValue);
        }

        /// <summary>
        /// Adds or subtracts from current progress.
        /// Zero GC allocation.
        /// </summary>
        public void ModifyValue(float amount)
        {
            float newValue = Mathf.Clamp(currentValue + amount, 0f, _maxValue);
            UpdateValue(newValue, _maxValue);
        }

        /// <summary>
        /// Changes the max value. Optionally preserves current fill ratio.
        /// Zero GC allocation.
        /// </summary>
        public void SetMaxValue(float newMax, bool preserveRatio = false)
        {
            float safeMax = Mathf.Max(1f, newMax);
            float newCurrent;

            if (preserveRatio && _maxValue > 0f)
                newCurrent = (currentValue / _maxValue) * safeMax;
            else
                newCurrent = Mathf.Min(currentValue, safeMax);

            UpdateValue(newCurrent, safeMax);
        }

        /// <summary>
        /// Resets progress to 0 and fires onReset.
        /// </summary>
        public void ResetProgress()
        {
            if (_autoCompleteCoroutine != null)
            {
                StopCoroutine(_autoCompleteCoroutine);
                _autoCompleteCoroutine = null;
            }

            _lastReachedCheckpointIndex = -1;
            _etaSampleValueStart = 0f;
            _etaSampleTime = 0f;
            _etaEstimatedSeconds = -1f;

            UpdateValue(0f, _maxValue);
            onReset?.Invoke();
        }

        /// <summary>
        /// Resets to initial configured values. Useful for object pooling.
        /// </summary>
        public void ResetToInitial()
        {
            SetValue(initialValue, initialMaxValue);
        }

        /// <summary>
        /// Immediately fills bar to 100%.
        /// </summary>
        public void SetComplete()
        {
            UpdateValue(_maxValue, _maxValue);
        }

        /// <summary>
        /// Waits delay seconds then fills to 100% over fillDuration seconds.
        /// Fires onComplete when done.
        /// </summary>
        public void CompleteWithDelay(float delay, float fillDuration)
        {
            if (_autoCompleteCoroutine != null)
                StopCoroutine(_autoCompleteCoroutine);

            _autoCompleteCoroutine = StartCoroutine(FillToCompleteCoroutine(delay, fillDuration));
        }

        /// <summary>
        /// Enables or disables indeterminate (bouncing strip) mode at runtime.
        /// </summary>
        public void SetIndeterminate(bool enabled)
        {
            indeterminateMode = enabled;

            if (mainFill != null)
                mainFill.enabled = !enabled;

            if (indeterminateStrip != null)
                indeterminateStrip.enabled = enabled;

            if (!enabled)
                _stripPhase = 0f;
        }

        public float GetCurrentValue()   => currentValue;
        public float GetMaxValue()       => _maxValue;
        public float GetNormalizedValue() => _maxValue > 0f ? currentValue / _maxValue : 0f;

        #endregion

        #region Core Update

        // private void UpdateCurrentValue()
        // {
        //     UpdateValue(currentValue,_maxValue);
        // } 
        
        private void UpdateValue(float current, float max)
        {
            if (!_isInitialized)
                Initialize();

            bool wasComplete = currentValue >= _maxValue;

            currentValue = Mathf.Clamp(current, 0f, max);
            _maxValue = Mathf.Max(1f, max);

            float normalized = currentValue / _maxValue;
            _targetFillAmount = Mathf.Clamp01(normalized);

            if (!smoothTransition)
            {
                _displayedFillAmount = _targetFillAmount;
                if (mainFill != null)
                    mainFill.fillAmount = _displayedFillAmount;
            }

            UpdateMainFillColor(normalized);
            UpdateTextDisplay();
            UpdateVisibility(normalized);

            onValueChanged?.Invoke(currentValue);

            if (enableCheckpoints)
                CheckCheckpoints(normalized);

            if (!wasComplete && IsComplete)
                onComplete?.Invoke();
        }

        private void UpdateSmoothTransition()
        {
            if (!smoothTransition) return;

            if (Mathf.Abs(_displayedFillAmount - _targetFillAmount) > 0.001f)
            {
                _displayedFillAmount = Mathf.Lerp(_displayedFillAmount, _targetFillAmount, DeltaTime * transitionSpeed);
                if (mainFill != null)
                    mainFill.fillAmount = _displayedFillAmount;
            }
        }

        private void UpdateMainFillColor(float normalizedValue)
        {
            if (mainFill == null) return;

            Color targetColor = useGradient && colorGradient != null
                ? colorGradient.Evaluate(normalizedValue)
                : solidColor;

            if (_cachedMainColor != targetColor)
            {
                _cachedMainColor = targetColor;
                mainFill.color = _cachedMainColor;
            }
        }

        // Parameterless wrapper used at runtime (currentValue / _maxValue already up to date)
        private void UpdateTextDisplay() => UpdateTextDisplay(currentValue, _maxValue);

        private void UpdateTextDisplay(float current, float max)
        {
            if (!showText) return;
            if (valueText == null && valueText2 == null) return;

            switch (textMode)
            {
                case TextDisplayMode.None:
                    if (valueText  != null) valueText.text  = string.Empty;
                    if (valueText2 != null) valueText2.text = string.Empty;
                    return;

                case TextDisplayMode.CurrentOnly:
                    _textBuilder.Clear();
                    _textBuilder.Append(customPrefix);
                    _textBuilder.Append(Mathf.CeilToInt(current));
                    _textBuilder.Append(customSuffix);
                    break;

                case TextDisplayMode.CurrentAndMax:
                    _textBuilder.Clear();
                    _textBuilder.Append(customPrefix);
                    _textBuilder.Append(Mathf.CeilToInt(current));
                    _textBuilder.Append('/');
                    _textBuilder.Append(Mathf.CeilToInt(max));
                    _textBuilder.Append(customSuffix);
                    break;

                case TextDisplayMode.Percentage:
                    _textBuilder.Clear();
                    _textBuilder.Append(customPrefix);
                    _textBuilder.Append(Mathf.RoundToInt(max > 0f ? current / max * 100f : 0f));
                    _textBuilder.Append('%');
                    _textBuilder.Append(customSuffix);
                    break;
            }

            string result = _textBuilder.ToString();
            if (valueText  != null) valueText.text  = result;
            if (valueText2 != null) valueText2.text = result;
        }

        private void UpdateVisibility(float normalizedValue)
        {
            if (!hideWhenFull) return;

            bool shouldBeVisible = normalizedValue < 1f;
            if (gameObject.activeSelf != shouldBeVisible)
                gameObject.SetActive(shouldBeVisible);
        }

        private void UpdateTextVisibility()
        {
            if (valueText  != null) valueText.gameObject.SetActive(showText);
            if (valueText2 != null) valueText2.gameObject.SetActive(showText);
        }

        #endregion

        #region Checkpoints & Indicators

        private void CheckCheckpoints(float normalizedValue)
        {
            if (checkpoints == null || checkpoints.Count == 0) return;

            for (int i = 0; i < checkpoints.Count; i++)
            {
                if (i <= _lastReachedCheckpointIndex) continue;

                if (normalizedValue >= checkpoints[i].atPercent)
                {
                    _lastReachedCheckpointIndex = i;
                    checkpoints[i].onReached?.Invoke();
                    onCheckpointReached?.Invoke(checkpoints[i]);

                    if (i < _spawnedIndicators.Count && _spawnedIndicators[i] != null)
                        MarkIndicatorReached(_spawnedIndicators[i]);
                }
            }
        }

        private void SpawnIndicators()
        {
            if (indicatorPrefab == null) return;

            ClearIndicators();

            RectTransform container = indicatorContainer;
            if (container == null && mainFill != null)
                container = mainFill.rectTransform.parent as RectTransform;
            if (container == null) return;

            float containerWidth = container.rect.width;

            for (int i = 0; i < checkpoints.Count; i++)
            {
                RectTransform indicator = Instantiate(indicatorPrefab, container);
                indicator.sizeDelta = indicatorSize;
                indicator.anchorMin = new Vector2(0f, 0.5f);
                indicator.anchorMax = new Vector2(0f, 0.5f);
                indicator.anchoredPosition = new Vector2(checkpoints[i].atPercent * containerWidth, 0f);

                _spawnedIndicators.Add(indicator);
            }
        }

        private void ClearIndicators()
        {
            for (int i = _spawnedIndicators.Count - 1; i >= 0; i--)
            {
                if (_spawnedIndicators[i] == null) continue;

                if (Application.isPlaying)
                    Destroy(_spawnedIndicators[i].gameObject);
                else
                    DestroyImmediate(_spawnedIndicators[i].gameObject);
            }
            _spawnedIndicators.Clear();
        }

        /// <summary>
        /// Called when a checkpoint indicator is visually marked as reached.
        /// Override in a subclass to customize the indicator reaction.
        /// Default scales it up slightly.
        /// </summary>
        protected virtual void MarkIndicatorReached(RectTransform indicator)
        {
            indicator.localScale = Vector3.one * 1.2f;
        }

        #endregion

        #region Indeterminate

        private void UpdateIndeterminate()
        {
            if (indeterminateStrip == null) return;

            _stripPhase += DeltaTime * indeterminateSpeed;

            float t = Mathf.PingPong(_stripPhase, 1f);

            RectTransform stripRect = indeterminateStrip.rectTransform;
            RectTransform parentRect = stripRect.parent as RectTransform;
            if (parentRect == null) return;

            float barWidth = parentRect.rect.width;
            float usableWidth = barWidth * (1f - stripWidth);

            stripRect.anchorMin = new Vector2(0f, 0f);
            stripRect.anchorMax = new Vector2(stripWidth, 1f);
            stripRect.offsetMin = new Vector2(t * usableWidth, 0f);
            stripRect.offsetMax = new Vector2(t * usableWidth, 0f);
        }

        #endregion

        #region ETA

        private void UpdateEta()
        {
            if (etaText == null || IsComplete) return;

            _etaSampleTime += DeltaTime;

            if (_etaSampleTime >= etaSampleWindow)
            {
                float valueDelta = currentValue - _etaSampleValueStart;

                if (valueDelta > 0f)
                {
                    float ratePerSecond = valueDelta / _etaSampleTime;
                    float remaining = _maxValue - currentValue;
                    _etaEstimatedSeconds = remaining / ratePerSecond;
                }
                else
                {
                    _etaEstimatedSeconds = -1f;
                }

                _etaSampleValueStart = currentValue;
                _etaSampleTime = 0f;
            }

            if (_etaEstimatedSeconds >= 0f)
            {
                etaText.text = string.Format(etaFormat, Mathf.CeilToInt(_etaEstimatedSeconds));
                etaText.gameObject.SetActive(true);
            }
            else
            {
                etaText.gameObject.SetActive(false);
            }
        }

        #endregion

        #region Auto Complete Coroutines

        private IEnumerator AutoCompleteCoroutine()
        {
            yield return FillToCompleteCoroutine(autoCompleteDelay, autoCompleteDuration);
        }

        private IEnumerator FillToCompleteCoroutine(float delay, float duration)
        {
            if (delay > 0f)
            {
                float waited = 0f;
                while (waited < delay)
                {
                    waited += DeltaTime;
                    yield return null;
                }
            }

            float startValue = currentValue;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += DeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                UpdateValue(Mathf.Lerp(startValue, _maxValue, t), _maxValue);
                yield return null;
            }

            UpdateValue(_maxValue, _maxValue);
            _autoCompleteCoroutine = null;
        }

        #endregion

#if UNITY_EDITOR
        /// <summary>
        /// Called by UIProgressBarEditor after any inspector change in Edit Mode.
        /// Applies all visual state to the scene objects without relying on OnValidate.
        /// Must be called after serializedObject.ApplyModifiedProperties() so all fields are current.
        /// </summary>
        public void Editor_Refresh()
        {
            // Clamp checkpoint percentages
            if (checkpoints != null)
            {
                for (int i = 0; i < checkpoints.Count; i++)
                {
                    var cp = checkpoints[i];
                    cp.atPercent = Mathf.Clamp01(cp.atPercent);
                    checkpoints[i] = cp;
                }
            }

            // Indeterminate strip visibility
            if (indeterminateStrip != null)
                indeterminateStrip.enabled = indeterminateMode;

            // Main fill - amount + color
            if (mainFill != null)
            {
                mainFill.enabled = !indeterminateMode;
                float normalized = Mathf.Clamp01(currentValue / Mathf.Max(1f, initialMaxValue));
                mainFill.fillAmount = normalized;
                mainFill.color = useGradient && colorGradient != null
                    ? colorGradient.Evaluate(normalized)
                    : solidColor;
                UnityEditor.EditorUtility.SetDirty(mainFill);
            }

            // Value text - content + visibility
            UpdateTextVisibility();
            if (showText)
            {
                UpdateTextDisplay(currentValue, Mathf.Max(1f, initialMaxValue));
                if (valueText  != null) UnityEditor.EditorUtility.SetDirty(valueText);
                if (valueText2 != null) UnityEditor.EditorUtility.SetDirty(valueText2);
            }

            // ETA text visibility
            if (etaText != null && !showEta)
                etaText.gameObject.SetActive(false);
        }
#endif
    }
}