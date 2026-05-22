using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CorePro.UI
{
    /// <summary>
    /// Professional base class for UI bar system.
    /// Provides foundation for health bars, progress bars, resource bars, etc.
    /// Optimized for zero GC allocation during runtime updates.
    /// </summary>
    public abstract class UIBar : MonoBehaviour
    {
        [Header("Visual References")]
        [SerializeField] protected Image mainFill;
        [SerializeField] protected Image overchargeFill;
        [SerializeField] protected Image background;
        [SerializeField] protected TextMeshProUGUI valueText;

        [Header("Display Options")]
        [SerializeField] protected bool showText = true;
        [SerializeField] protected bool hideWhenFull = false;
        [SerializeField] protected TextDisplayMode textMode = TextDisplayMode.CurrentAndMax;
        
        [Header("Visual Styling")]
        [SerializeField] protected bool useGradient = false;
        [SerializeField] protected Gradient colorGradient;
        [SerializeField] protected Color solidColor = Color.green;
        [SerializeField] protected Color overchargeColor = new Color(1f, 0.84f, 0f, 0.6f);

        [Header("Animation")]
        [SerializeField] protected bool smoothTransition = false;
        [SerializeField] protected float transitionSpeed = 5f;

        protected float currentValue;
        protected float maxValue = 100f;
        protected float overchargeValue;

        protected float targetFillAmount;
        private float displayedFillAmount;
        private float targetOverchargeFill;
        private float displayedOverchargeFill;
        
        private Color cachedMainColor;
        private bool isInitialized = false;

        protected enum TextDisplayMode
        {
            None,
            CurrentOnly,
            CurrentAndMax,
            Percentage
        }

        protected virtual void Awake()
        {
            Initialize();
        }

        protected void Initialize()
        {
            if (isInitialized == true)
                return;
            
            if (mainFill != null)
            {
                cachedMainColor = useGradient == false ? solidColor : colorGradient.Evaluate(1f);
                mainFill.color = cachedMainColor;
            }

            if (overchargeFill != null)
            {
                overchargeFill.color = overchargeColor;
                overchargeFill.fillAmount = 0f;
            }

            UpdateTextVisibility();
            isInitialized = true;
        }
        
        
        public void ForceInitialize()
        {
            isInitialized = false;
            Initialize();
        }

        protected virtual void Update()
        {
            if (smoothTransition == false) 
                return;

            if (Mathf.Abs(displayedFillAmount - targetFillAmount) > 0.001f)
            {
                displayedFillAmount = Mathf.Lerp(displayedFillAmount, targetFillAmount, Time.deltaTime * transitionSpeed);
                if (mainFill != null)
                {
                    mainFill.fillAmount = displayedFillAmount;
                }
            }

            if (Mathf.Abs(displayedOverchargeFill - targetOverchargeFill) > 0.001f)
            {
                displayedOverchargeFill = Mathf.Lerp(displayedOverchargeFill, targetOverchargeFill, Time.deltaTime * transitionSpeed);
                if (overchargeFill != null)
                {
                    overchargeFill.fillAmount = displayedOverchargeFill;
                }
            }
        }

        /// <summary>
        /// Updates bar display with current, max and optional overcharge values.
        /// Zero GC allocation method for runtime performance.
        /// </summary>
        public virtual void UpdateValue(float current, float max, float overcharge = 0f)
        {
            if (isInitialized == false)
            {
                Initialize();
            }

            currentValue = Mathf.Max(0f, current);
            maxValue = Mathf.Max(1f, max);
            overchargeValue = Mathf.Max(0f, overcharge);

            float normalizedValue = currentValue / maxValue;
            targetFillAmount = Mathf.Clamp01(normalizedValue);

            if (smoothTransition == false)
            {
                displayedFillAmount = targetFillAmount;
                if (mainFill != null)
                {
                    mainFill.fillAmount = displayedFillAmount;
                }
            }

            UpdateMainFillColor(normalizedValue);
            UpdateOverchargeFill(normalizedValue);
            UpdateTextDisplay();
            UpdateVisibility(normalizedValue);
        }

        protected virtual void UpdateMainFillColor(float normalizedValue)
        {
            if (mainFill == null) return;

            Color targetColor;
            if (useGradient == true && colorGradient != null)
            {
                targetColor = colorGradient.Evaluate(normalizedValue);
            }
            else
            {
                targetColor = solidColor;
            }

            if (cachedMainColor != targetColor)
            {
                cachedMainColor = targetColor;
                mainFill.color = cachedMainColor;
            }
        }

        protected virtual void UpdateOverchargeFill(float normalizedValue)
        {
            if (overchargeFill == null) return;

            float totalValue = currentValue + overchargeValue;
            float totalNormalized = totalValue / maxValue;
            targetOverchargeFill = Mathf.Clamp01(totalNormalized);

            if (smoothTransition == false)
            {
                displayedOverchargeFill = targetOverchargeFill;
                overchargeFill.fillAmount = displayedOverchargeFill;
            }
        }

        protected virtual void UpdateTextDisplay()
        {
            if (valueText == null || showText == false) return;

            switch (textMode)
            {
                case TextDisplayMode.None:
                    valueText.text = string.Empty;
                    break;
                    
                case TextDisplayMode.CurrentOnly:
                    valueText.text = Mathf.CeilToInt(currentValue).ToString();
                    break;
                    
                case TextDisplayMode.CurrentAndMax:
                    valueText.text = $"{Mathf.CeilToInt(currentValue)}/{Mathf.CeilToInt(maxValue)}";
                    break;
                    
                case TextDisplayMode.Percentage:
                    int percentage = Mathf.RoundToInt((currentValue / maxValue) * 100f);
                    valueText.text = $"{percentage}%";
                    break;
            }
        }

        protected virtual void UpdateVisibility(float normalizedValue)
        {
            if (hideWhenFull == false) return;

            bool shouldBeVisible = normalizedValue < 1f || overchargeValue > 0f;
            
            if (gameObject.activeSelf != shouldBeVisible)
            {
                gameObject.SetActive(shouldBeVisible);
            }
        }

        protected void UpdateTextVisibility()
        {
            if (valueText == null) return;
            valueText.gameObject.SetActive(showText);
        }

        public float GetCurrentValue() => currentValue;
        public float GetMaxValue() => maxValue;
        public float GetOverchargeValue() => overchargeValue;
        public float GetNormalizedValue() => currentValue / maxValue;

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            if (Application.isPlaying == false)
            {
                ValidateReferences();
                ValidateColors();
                UpdateTextVisibility();
            }
        }

        private void ValidateReferences()
        {
            if (mainFill == null)
            {
                mainFill = transform.Find("MainFill")?.GetComponent<Image>();
            }

            if (overchargeFill == null)
            {
                overchargeFill = transform.Find("OverchargeFill")?.GetComponent<Image>();
            }

            if (background == null)
            {
                background = transform.Find("Background")?.GetComponent<Image>();
            }

            if (valueText == null)
            {
                valueText = GetComponentInChildren<TextMeshProUGUI>();
            }
        }

        private void ValidateColors()
        {
            if (mainFill != null && useGradient == false)
            {
                mainFill.color = solidColor;
            }

            if (overchargeFill != null)
            {
                overchargeFill.color = overchargeColor;
            }
        }
#endif
    }
}
