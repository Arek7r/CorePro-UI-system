using InspectorPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using TMPro;

namespace CorePro.UI
{
    /// <summary>
    /// Generic checkbox component for CorePro.
    /// Supports two animation modes: Animator (state machine) or Generic (GO swap + CanvasGroup fade).
    /// Can operate standalone or as part of UICheckboxGroup (radio behavior).
    /// Supports TextMeshPro text groups - SetText() / SetActiveTextGroup().
    /// </summary>
    public class UICheckboxPro : MonoBehaviourCorePro, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler, ISubmitHandler
    {
        #region Variables
        // =============================================
        // Animator hashes
        // =============================================

        private static readonly int StateOn         = Animator.StringToHash("On");
        private static readonly int StateOff        = Animator.StringToHash("Off");
        private static readonly int StateOnInstant  = Animator.StringToHash("On Instant");
        private static readonly int StateOffInstant = Animator.StringToHash("Off Instant");

        private const float  AnimatorDisableDelayFallback = 0.5f;
        private const string PrefsPrefix                  = "Checkbox_";

        // =============================================
        // Inspector - References
        // =============================================

        [Group("References")]
        [Tooltip("Root GameObject shown when checked. Optional.")]
        [SerializeField] private CanvasGroup  stateOnGO;

        [Tooltip("Root GameObject shown when unchecked. Optional.")]
        [SerializeField] private CanvasGroup  stateOffGO;

        [Tooltip("CanvasGroup faded on transition (Generic mode). Optional.")]
        [SerializeField] private CanvasGroup checkmarkCG;

        [Tooltip("CanvasGroup for hover highlight. Optional.")]
        [SerializeField] private CanvasGroup highlightCG;

        // =============================================
        // Inspector - Text Groups (TMP)
        // =============================================

        [Group("Text Groups")]
        [Tooltip("Named groups of TextMeshPro objects. Use SetText() / SetActiveTextGroup() to control them.")]
        [SerializeField] private TextGroup[] textGroups = new TextGroup[0];

        // =============================================
        // Inspector - Animation
        // =============================================

        [SerializeField] private AnimationMode animationMode = AnimationMode.Generic;

        // == Animator mode ==
        [ShowIf(nameof(animationMode), AnimationMode.Animator)]
        [Tooltip("Animator with states: On / Off / On Instant / Off Instant.")]
        [SerializeField] private Animator checkboxAnimator;

        // == Generic mode ==
        [ShowIf(nameof(animationMode), AnimationMode.Generic)]
        [Tooltip("Fade checkmarkCG in/out. If false - GO swap is instant.")]
        [SerializeField] private bool useFade = true;

        [ShowIfAll(nameof(useFade), true, nameof(animationMode), AnimationMode.Generic)]
        [SerializeField, Min(0.001f)] private float fadeInDuration  = 0.15f;

        [ShowIfAll(nameof(useFade), true, nameof(animationMode), AnimationMode.Generic)]
        [SerializeField, Min(0.001f)] private float fadeOutDuration = 0.1f;

        [ShowIfAll(nameof(useFade), true, nameof(animationMode), AnimationMode.Generic)]
        [SerializeField] private AnimationCurve fadeInCurve  = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [ShowIfAll(nameof(useFade), true, nameof(animationMode), AnimationMode.Generic)]
        [SerializeField] private AnimationCurve fadeOutCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [ShowIf(nameof(animationMode), AnimationMode.Generic)]
        [Tooltip("How stateOnGO and stateOffGO transition. Instant = SetActive. Fade = CanvasGroup alpha crossfade.")]
        [SerializeField] private StateGoMode stateGoMode = StateGoMode.Instant;

        [ShowIfAll(nameof(stateGoMode), StateGoMode.Fade, nameof(animationMode), AnimationMode.Generic)]
        [SerializeField, Min(0.001f)] private float stateGoFadeInDuration  = 0.15f;

        [ShowIfAll(nameof(stateGoMode), StateGoMode.Fade, nameof(animationMode), AnimationMode.Generic)]
        [SerializeField, Min(0.001f)] private float stateGoFadeOutDuration = 0.1f;

        // =============================================
        // Inspector - Save
        // =============================================

        [Group("Save")]
        [SerializeField] private bool   saveValue = false;
        [SerializeField] private string saveKey   = "Checkbox";

        // =============================================
        // Inspector - Settings
        // =============================================

        [Group("Settings")]
        [SerializeField] private bool  isOn           = false;
        [SerializeField] private bool  isInteractable = true;
        [SerializeField] private bool  invokeOnAwake  = false;
        [SerializeField] private bool  useSounds      = false;
        [SerializeField, Range(1f, 20f)] private float highlightSpeed = 8f;

        // =============================================
        // Inspector - Events
        // =============================================

        [Group("Events", true)]
        [SerializeField] public CheckboxEvent onValueChanged = new CheckboxEvent();
        [SerializeField] public UnityEvent    onChecked      = new UnityEvent();
        [SerializeField] public UnityEvent    onUnchecked    = new UnityEvent();

        [System.Serializable]
        public class CheckboxEvent : UnityEvent<bool> { }

        public enum AnimationMode { None, Generic, Animator }

        /// <summary>Controls how stateOnGO / stateOffGO transition in Generic mode.</summary>
        public enum StateGoMode { Instant, Fade }

        // =============================================
        // Nested types
        // =============================================

        /// <summary>
        /// Named group of TMP labels. Assign any number of TextMeshProUGUI objects.
        /// Control via <see cref="SetText"/> and <see cref="SetActiveTextGroup"/>.
        /// </summary>
        [System.Serializable]
        public class TextGroup
        {
            [Tooltip("Unique name used to identify this group in code.")]
            public string groupName = "Group";

            [Tooltip("All TMP labels belonging to this group.")]
            public TextMeshProUGUI[] labels = new TextMeshProUGUI[0];
        }

        // =============================================
        // State
        // =============================================

        public bool IsOn           => isOn;
        public bool IsInteractable => isInteractable;

        private string _prefsKey;

        // Generic fade - checkmarkCG
        private float          _fadeElapsed  = 0f;
        private float          _fadeDuration = 0f;
        private float          _fadeFrom     = 0f;
        private float          _fadeTo       = 0f;
        private AnimationCurve _fadeCurve;
        private bool           _fadePending  = false;

        // StateGO fade - stateOnGO / stateOffGO
        private float _stateGoFadeElapsed  = 0f;
        private float _stateGoFadeDuration = 0f;
        private float _stateGoFromOn       = 0f;
        private float _stateGoToOn         = 0f;
        private float _stateGoFromOff      = 0f;
        private float _stateGoToOff        = 0f;
        private bool  _stateGoFadePending  = false;

        // Animator disable timer
        private float _animatorTimer   = 0f;
        private bool  _animatorPending = false;

        // Highlight
        private float _highlightAlpha  = 0f;
        private float _highlightTarget = 0f;

        private bool _isInitialized = false;

        #endregion


        private void Awake()
        {
#if UNITY_EDITOR
            if (animationMode == AnimationMode.Animator && checkboxAnimator == null)
                Debug.LogError($"[UICheckboxPro] Animator mode selected but checkboxAnimator is not assigned on '{name}'.", this);

            if (animationMode == AnimationMode.Generic && stateOnGO == null)
                Debug.LogError($"[UICheckboxPro] stateOnGO is not assigned on '{name}'. Checked state will have no visual.", this);
#endif
            _prefsKey = PrefsPrefix + saveKey;

            if (saveValue)
                LoadSavedState();
            else
                ApplyInstant(isOn);

            if (invokeOnAwake)
                FireEvents(isOn);

            _isInitialized = true;
        }

        private void OnEnable()
        {
            if (!_isInitialized) return;

            // Don't interrupt an in-progress animation
            if (!_fadePending && !_stateGoFadePending && !_animatorPending)
                ApplyInstant(isOn);
        }

        private void Update()
        {
            if (!_fadePending && !_stateGoFadePending && !_animatorPending && Mathf.Approximately(_highlightAlpha, _highlightTarget))
                return;

            TickFade();
            TickStateGoFade();
            TickAnimatorDisable();
            TickHighlight();
        }

        // =============================================
        // Public API - Checkbox state
        // =============================================

        /// <summary>Toggle checked state with animation.</summary>
        public void Toggle() => SetValue(!isOn, animated: true);

        /// <summary>Set checked with animation.</summary>
        public void SetChecked() => SetValue(true, animated: true);

        /// <summary>Set unchecked with animation.</summary>
        public void SetUnchecked() => SetValue(false, animated: true);

        /// <summary>Set state. Skips if value hasn't changed.</summary>
        public void SetValue(bool value, bool animated = true)
        {
            if (isOn == value) return;
            ApplyValue(value, animated, fireEvents: true);
        }

        /// <summary>Force set state even if value hasn't changed. Useful for refresh after scene load.</summary>
        public void ForceSetValue(bool value, bool animated = false)
        {
            ApplyValue(value, animated, fireEvents: true);
        }

        /// <summary>Set state silently - no events fired. Used internally by UICheckboxGroup.</summary>
        public void SetValueSilent(bool value, bool animated = true)
        {
            if (isOn == value) return;
            ApplyValue(value, animated, fireEvents: false);
        }

        /// <summary>Set interactable state. Clears highlight when disabled.</summary>
        public void SetInteractable(bool value)
        {
            isInteractable = value;
            if (!isInteractable)
                SetHighlightTarget(0f);
        }

        // =============================================
        // Public API - Text Groups
        // =============================================

        /// <summary>
        /// Sets the text of every label inside the named group.
        /// Does nothing (with a warning) when the group is not found.
        /// </summary>
        /// <param name="groupName">Name of the <see cref="TextGroup"/> to target.</param>
        /// <param name="text">String to assign to all labels in the group.</param>
        public void SetText(string groupName, string text)
        {
            TextGroup group = FindTextGroup(groupName);
            if (group == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"[UICheckboxPro] SetText - group '{groupName}' not found on '{name}'.", this);
#endif
                return;
            }

            foreach (TextMeshProUGUI label in group.labels)
            {
                if (label != null)
                    label.text = text;
            }
        }

        /// <summary>
        /// Sets the text of every label across ALL groups at once.
        /// Useful when all groups always display the same content.
        /// </summary>
        public void SetText(string text)
        {
            foreach (TextGroup group in textGroups)
            {
                foreach (TextMeshProUGUI label in group.labels)
                {
                    if (label != null)
                        label.text = text;
                }
            }
        }

        /// <summary>
        /// Activates or deactivates every label inside the named group.
        /// Does nothing (with a warning) when the group is not found.
        /// </summary>
        /// <param name="groupName">Name of the <see cref="TextGroup"/> to target.</param>
        /// <param name="active">True = show labels, false = hide labels.</param>
        public void SetActiveTextGroup(string groupName, bool active)
        {
            TextGroup group = FindTextGroup(groupName);
            if (group == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"[UICheckboxPro] SetActiveTextGroup - group '{groupName}' not found on '{name}'.", this);
#endif
                return;
            }

            foreach (TextMeshProUGUI label in group.labels)
            {
                if (label != null)
                    label.gameObject.SetActive(active);
            }
        }

        /// <summary>
        /// Activates one group by name and deactivates all others.
        /// Convenient for mutually exclusive label states (e.g. "Checked" vs "Unchecked" copy).
        /// </summary>
        public void SetActiveTextGroupExclusive(string groupName)
        {
            foreach (TextGroup group in textGroups)
            {
                bool shouldBeActive = group.groupName == groupName;
                foreach (TextMeshProUGUI label in group.labels)
                {
                    if (label != null)
                        label.gameObject.SetActive(shouldBeActive);
                }
            }
        }

        // =============================================
        // Pointer / Input
        // =============================================

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!isInteractable || eventData.button != PointerEventData.InputButton.Left) return;
            PlayClickSound();
            Toggle();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!isInteractable) return;
            PlayHoverSound();
            SetHighlightTarget(1f);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!isInteractable) return;
            SetHighlightTarget(0f);
        }

        public void OnSelect(BaseEventData eventData)
        {
            if (!isInteractable) return;
            SetHighlightTarget(1f);
        }

        public void OnDeselect(BaseEventData eventData)
        {
            if (!isInteractable) return;
            SetHighlightTarget(0f);
        }

        public void OnSubmit(BaseEventData eventData)
        {
            if (!isInteractable) return;
            Toggle();
            SetHighlightTarget(0f);
        }

        // =============================================
        // Core
        // =============================================

        private void ApplyValue(bool value, bool animated, bool fireEvents)
        {
            isOn = value;

            if (animated) ApplyAnimated(isOn);
            else          ApplyInstant(isOn);

            if (saveValue)  SaveState(isOn);
            if (fireEvents) FireEvents(isOn);
        }

        private void ApplyAnimated(bool value)
        {
            if (animationMode == AnimationMode.Animator)
            {
                PlayAnimatorState(value ? StateOn : StateOff);

                if (Application.isPlaying == false)
                    SwapStateGOs(value);
            }
            else if (animationMode == AnimationMode.Generic)
            {
                // Checkmark CanvasGroup fade
                if (useFade && checkmarkCG != null)
                    StartFade(value);

                // StateGO transition - Instant or Fade
                if (stateGoMode == StateGoMode.Fade)
                    StartStateGoFade(value);
                else
                    SwapStateGOs(value);
            }
            else if (animationMode == AnimationMode.None)
            {
                SwapStateGOs(value);
            }
        }

        private void ApplyInstant(bool value)
        {
            if (animationMode == AnimationMode.Animator)
            {
                PlayAnimatorState(value ? StateOnInstant : StateOffInstant);

                if (Application.isPlaying ==false)
                    SwapStateGOs(value);
            }
            else if (animationMode == AnimationMode.Generic)
            {
                // Cancel any in-progress stateGO fade and snap to final state
                _stateGoFadePending = false;
                SwapStateGOs(value);

                // When Fade mode is active we also need to reset the CanvasGroup alphas
                // (the GO may have been mid-fade when instant was requested)
                if (stateGoMode == StateGoMode.Fade)
                {
                    if (stateOnGO  != null) stateOnGO.alpha  =  value ? 1f : 0f;
                    if (stateOffGO != null) stateOffGO.alpha = !value ? 1f : 0f;
                }

                if (checkmarkCG != null)
                {
                    checkmarkCG.alpha = value ? 1f : 0f;
                    _fadePending      = false;
                }
            }
            else if (animationMode == AnimationMode.None)
            {
                SwapStateGOs(value);
            }
        }

        private void SwapStateGOs(bool value)
        {
            if (stateOnGO  != null)
                stateOnGO.gameObject.SetActive(value);
            
            if (stateOffGO != null) 
                stateOffGO.gameObject.SetActive(!value);

            if (Application.isPlaying == false)
            {
                if (stateOnGO  != null)
                    stateOnGO.alpha =  value ? 1f : 0f;
            
                if (stateOffGO != null) 
                    stateOffGO.alpha =  !value ? 1f : 0f;
            }

        }

        // =============================================
        // Animator
        // =============================================

        private void PlayAnimatorState(int stateHash)
        {
            if (checkboxAnimator == null)
            {
                Debug.LogWarning($"[UICheckboxPro] PlayAnimatorState called but checkboxAnimator is NULL on '{name}'.", this);
                return;
            }

            checkboxAnimator.enabled = true;
            checkboxAnimator.Rebind();
            checkboxAnimator.Play(stateHash);
            checkboxAnimator.Update(0f);

            float length = checkboxAnimator.GetCurrentAnimatorStateInfo(0).length;
            ScheduleAnimatorDisable(length);
        }

        private void ScheduleAnimatorDisable(float duration)
        {
            _animatorTimer   = duration + 0.05f;
            _animatorPending = true;
        }

        private void TickAnimatorDisable()
        {
            if (!_animatorPending) return;

            _animatorTimer -= Time.unscaledDeltaTime;
            if (_animatorTimer > 0f) return;

            if (checkboxAnimator != null)
                checkboxAnimator.enabled = false;
            _animatorPending = false;
        }

        // =============================================
        // Generic fade (Update-driven)
        // =============================================

        private void StartFade(bool value)
        {
            _fadeFrom     = checkmarkCG.alpha;
            _fadeTo       = value ? 1f : 0f;
            _fadeElapsed  = 0f;
            _fadeDuration = value ? fadeInDuration  : fadeOutDuration;
            _fadeCurve    = value ? fadeInCurve     : fadeOutCurve;
            _fadePending  = true;
        }

        private void TickFade()
        {
            if (!_fadePending) return;

            _fadeElapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(_fadeElapsed / _fadeDuration);
            checkmarkCG.alpha = Mathf.Lerp(_fadeFrom, _fadeTo, _fadeCurve.Evaluate(t));

            if (t >= 1f)
                _fadePending = false;
        }

        // =============================================
        // StateGO fade (Update-driven)
        // =============================================

        private void StartStateGoFade(bool value)
        {
            // Both GOs must be active so they are visible during the crossfade
            if (stateOnGO  != null) { stateOnGO.gameObject.SetActive(true);  _stateGoFromOn  = stateOnGO.alpha; }
            if (stateOffGO != null) { stateOffGO.gameObject.SetActive(true); _stateGoFromOff = stateOffGO.alpha; }

            _stateGoToOn         = value  ? 1f : 0f;
            _stateGoToOff        = !value ? 1f : 0f;
            _stateGoFadeDuration = value  ? stateGoFadeInDuration : stateGoFadeOutDuration;
            _stateGoFadeElapsed  = 0f;
            _stateGoFadePending  = true;
        }

        private void TickStateGoFade()
        {
            if (!_stateGoFadePending) return;

            _stateGoFadeElapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(_stateGoFadeElapsed / _stateGoFadeDuration);

            if (stateOnGO  != null) stateOnGO.alpha  = Mathf.Lerp(_stateGoFromOn,  _stateGoToOn,  t);
            if (stateOffGO != null) stateOffGO.alpha = Mathf.Lerp(_stateGoFromOff, _stateGoToOff, t);

            if (t >= 1f)
            {
                _stateGoFadePending = false;
                // Deactivate the GO that has faded to invisible
                if (stateOnGO  != null && _stateGoToOn  == 0f) stateOnGO.gameObject.SetActive(false);
                if (stateOffGO != null && _stateGoToOff == 0f) stateOffGO.gameObject.SetActive(false);
            }
        }

        // =============================================
        // Highlight (Update-driven)
        // =============================================

        private void SetHighlightTarget(float target)
        {
            if (highlightCG == null)
                return;
            _highlightTarget = target;
        }

        private void TickHighlight()
        {
            if (highlightCG == null)
                return;
            if (Mathf.Approximately(_highlightAlpha, _highlightTarget)) return;

            _highlightAlpha = Mathf.MoveTowards(
                _highlightAlpha,
                _highlightTarget,
                Time.unscaledDeltaTime * highlightSpeed);

            highlightCG.alpha = _highlightAlpha;
        }

        // =============================================
        // Save / Load
        // =============================================

        private void LoadSavedState()
        {
            isOn = PlayerPrefs.GetInt(_prefsKey, isOn ? 1 : 0) == 1;
            ApplyInstant(isOn);
        }

        private void SaveState(bool value)
        {
            PlayerPrefs.SetInt(_prefsKey, value ? 1 : 0);
        }

        // =============================================
        // Events
        // =============================================

        private void FireEvents(bool value)
        {
            if (value) onChecked.Invoke();
            else       onUnchecked.Invoke();

            onValueChanged.Invoke(value);
        }

        // =============================================
        // Audio
        // =============================================

        private void PlayClickSound()
        {
            if (!useSounds || UIAudio.Instance == null)
                return;

            // if (UIAudio.Instance)
            //     UIAudio.Instance.audioSource.PlayOneShot(UIAudio.Instance.UIManagerAsset.clickSound);
        }

        private void PlayHoverSound()
        {
            if (!useSounds || UIAudio.Instance == null)
                return;

            // if (UIAudio.Instance)
            //     UIAudio.Instance.audioSource.PlayOneShot(UIAudio.Instance.UIManagerAsset.hoverSound);
        }

        // =============================================
        // Helpers
        // =============================================

        private TextGroup FindTextGroup(string groupName)
        {
            foreach (TextGroup g in textGroups)
                if (g.groupName == groupName) return g;
            return null;
        }

        // =============================================
        // ContextMenu - Debug / Preview
        // =============================================

#if UNITY_EDITOR
        [Button("Toggle")]
        [ContextMenu("Debug / Toggle")]
        private void Debug_Toggle()
        {
            Debug.Log($"[UICheckboxPro] Debug_Toggle - isPlaying:{Application.isPlaying} isOn:{isOn} animMode:{animationMode} animator:{checkboxAnimator} initialized:{_isInitialized}", this);
            if (Application.isPlaying) Toggle();
            else { isOn = !isOn; ApplyInstant(isOn); }
            UnityEditor.EditorUtility.SetDirty(this);
        }

        [ContextMenu("Debug / Set Checked")]
        private void Debug_SetChecked()
        {
            if (Application.isPlaying) SetChecked();
            else { isOn = true; ApplyInstant(true); }
            UnityEditor.EditorUtility.SetDirty(this);
        }

        [ContextMenu("Debug / Set Unchecked")]
        private void Debug_SetUnchecked()
        {
            if (Application.isPlaying)
                SetUnchecked();
            else
            {
                isOn = false; ApplyInstant(false);
            }

            UnityEditor.EditorUtility.SetDirty(this);
        }

        [ContextMenu("Debug / Force Refresh UI")]
        private void Debug_ForceRefresh()
        {
            if (Application.isPlaying) ForceSetValue(isOn, animated: false);
            else                       ApplyInstant(isOn);
            UnityEditor.EditorUtility.SetDirty(this);
        }

        [ContextMenu("Debug / Print State")]
        private void Debug_PrintState()
        {
            Debug.Log(
                $"[UICheckboxPro] '{name}'\n" +
                $"  isOn:             {isOn}\n" +
                $"  isInteractable:   {isInteractable}\n" +
                $"  animationMode:    {animationMode}\n" +
                $"  fadePending:      {_fadePending} ({_fadeElapsed:F3}/{_fadeDuration:F3}s)\n" +
                $"  animatorPending:  {_animatorPending} (timer: {_animatorTimer:F3}s)\n" +
                $"  highlightAlpha:   {_highlightAlpha:F2} -> {_highlightTarget:F2}\n" +
                $"  prefsKey:         {_prefsKey}\n" +
                $"  textGroups:       {textGroups.Length}",
                this);
        }
#endif
    }
}