using System.Collections.Generic;
using InspectorPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace CorePro.UI
{
    public class UISwitchPro : MonoBehaviourCorePro,
        IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler,
        ISelectHandler, IDeselectHandler, ISubmitHandler
    {
        #region Inspector Fields

        private static readonly int StateOn = Animator.StringToHash("On");
        private static readonly int StateOff = Animator.StringToHash("Off");
        private static readonly int StateOnInstant = Animator.StringToHash("On Instant");
        private static readonly int StateOffInstant = Animator.StringToHash("Off Instant");

        private const string PrefsPrefix = "Switch_";

        #region Inspector

        [Group("References")]
        [SerializeField]
        private Animator switchAnimator;

        [SerializeField]
        private CanvasGroup highlightCG; // optional - if null, highlight is skipped

        [Group("Animation")]
        [SerializeField]
        private AnimationMode animationMode = AnimationMode.Both;

        [Group("Generic Animation")]
        [Tooltip("Assign the handle RectTransform (the circle/knob). If null, generic animation is skipped.")]
        [SerializeField]
        private RectTransform handleRect;

        [Tooltip("anchoredPosition.X when Off (pixels from HandleArea centre, typically negative).")]
        [SerializeField]
        private float handlePosOff = -70f;

        [Tooltip("anchoredPosition.X when On (pixels from HandleArea centre, typically positive).")]
        [SerializeField]
        private float handlePosOn = 70f;

        [SerializeField, Min(0.001f)]
        private float handleDuration = 0.2f;

        [SerializeField]
        private AnimationCurve handleCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Tooltip("Smoothly tint target Images between OFF and ON colors. Only used in GenericHandle mode.")]
        [SerializeField]
        private bool useColorTransition = false;

        [Tooltip("Pick colors from a UIStyleSheet instead of custom Color Off / Color On values.")]
        [SerializeField]
        private bool useColorStyleSheet = false;

        [SerializeField]
        private UIStyleSheet colorTransitionSheet;

        [SerializeField]
        private List<ColorTransitionEntry> colorTransitions = new List<ColorTransitionEntry>();

        [SerializeField, Min(0.001f)]
        private float colorDuration = 0.2f;

        [SerializeField]
        private AnimationCurve colorCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Group("Save")]
        [SerializeField]
        private bool saveValue = false;

        [SerializeField]
        private string saveKey = "Switch";

        [Group("Settings")]
        [SerializeField]
        private bool isOn = true;

        [SerializeField]
        private bool isInteractable = true;

        [SerializeField]
        private bool invokeOnAwake = false;

        [SerializeField]
        private bool useSounds = false;

        [SerializeField]
        private bool useUINavigation = false;

        [SerializeField, Range(1f, 20f)]
        private float highlightSpeed = 8f;

        [Group("Events")]
        [SerializeField]
        public SwitchEvent onValueChanged = new SwitchEvent();

        [SerializeField]
        public UnityEvent onEvents = new UnityEvent();

        [SerializeField]
        public UnityEvent offEvents = new UnityEvent();

        [System.Serializable]
        public class SwitchEvent : UnityEvent<bool>
        {
        }

        public enum AnimationMode { None, Animator, GenericHandle, Both }

        [System.Serializable]
        public class ColorTransitionEntry
        {
            public Image target;
            public Color colorOff = new Color(0.5f, 0.5f, 0.5f, 1f);
            public Color colorOn  = new Color(0.18f, 0.62f, 0.28f, 1f);
            [HideInInspector] public int slotOff = 0;
            [HideInInspector] public int slotOn  = 0;
        }

        #endregion

        #region State

        public bool IsOn => isOn;
        public bool IsInteractable => isInteractable;

        private string _prefsKey;

        // Highlight
        private float _highlightAlpha = 0f;
        private float _highlightTarget = 0f;

        // Animator disable timer
        private float _animatorTimer = 0f;
        private bool _animatorPending = false;

        // Handle slide
        private float _handleElapsed = 0f;
        private float _handleDuration = 0f;
        private float _handleFrom = 0f;
        private float _handleTo = 0f;
        private bool _handlePending = false;

        // Color transition
        private float _colorElapsed = 0f;
        private float _colorDuration = 0f;
        private bool _colorPending = false;
        private bool _colorToOn = false;
        private Color[] _colorFrom; // per entry, allocated lazily

        private bool _isInitialized = false;

        #endregion

        #endregion

        private void Awake()
        {
#if UNITY_EDITOR
            bool needsAnimator = animationMode == AnimationMode.Animator || animationMode == AnimationMode.Both;
            bool needsHandle   = animationMode == AnimationMode.GenericHandle || animationMode == AnimationMode.Both;
            if (needsAnimator && switchAnimator == null)
                Debug.LogWarning($"[UISwitchPro] Animator mode selected but switchAnimator is not assigned on '{name}'.", this);
            if (needsHandle && handleRect == null)
                Debug.LogWarning($"[UISwitchPro] GenericHandle mode selected but handleRect is not assigned on '{name}'.", this);
#endif
            _prefsKey = PrefsPrefix + saveKey;

            PrepareColorTargets();

            if (useUINavigation)
                EnsureNavButton();

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
            UIStyleSheet.OnThemeChanged += OnColorSheetChanged;

            if (!_isInitialized) return;

            // If animator or handle is mid-animation, let it finish
            if (!_animatorPending && !_handlePending)
                ApplyInstant(isOn);
        }

        private void OnDisable()
        {
            UIStyleSheet.OnThemeChanged -= OnColorSheetChanged;
        }

        private void Update()
        {
            if (!_animatorPending && !_handlePending && !_colorPending &&
                Mathf.Approximately(_highlightAlpha, _highlightTarget))
                return;

            TickHighlight();
            TickAnimatorDisable();
            TickHandle();
            TickColor();
        }

        #region Public API

        /// <summary>Toggle current state with animation.</summary>
        public void Toggle() => SetValue(!isOn, animated: true);

        /// <summary>Set switch to On with animation.</summary>
        public void SetOn() => SetValue(true, animated: true);

        /// <summary>Set switch to Off with animation.</summary>
        public void SetOff() => SetValue(false, animated: true);

        /// <summary>Set switch state. Skips if value hasn't changed.</summary>
        public void SetValue(bool value, bool animated = true)
        {
            if (isOn == value) return;
            ApplyValue(value, animated, fireEvents: true);
        }

        /// <summary>Force set switch state even if value hasn't changed. Useful for refresh after scene load.</summary>
        public void ForceSetValue(bool value, bool animated = false)
        {
            ApplyValue(value, animated, fireEvents: true);
        }

        /// <summary>Set interactable state. Clears highlight when disabled.</summary>
        public void SetInteractable(bool value)
        {
            isInteractable = value;
            if (!isInteractable)
                SetHighlightTarget(0f);
        }

        #endregion

        #region Pointer / Input handlers

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

        #endregion

        #region Core logic

        private void ApplyValue(bool value, bool animated, bool fireEvents)
        {
            isOn = value;

            if (animated) ApplyAnimated(isOn);
            else ApplyInstant(isOn);

            if (saveValue) SaveState(isOn);
            if (fireEvents) FireEvents(isOn);
        }

        private void ApplyAnimated(bool value)
        {
            if ((animationMode == AnimationMode.Animator || animationMode == AnimationMode.Both) && switchAnimator != null)
            {
                ActivateAnimator();
                switchAnimator.Play(value ? StateOn : StateOff);
                switchAnimator.Update(0f);
                float length = switchAnimator.GetCurrentAnimatorStateInfo(0).length;
                ScheduleAnimatorDisable(length);
            }

            if ((animationMode == AnimationMode.GenericHandle || animationMode == AnimationMode.Both) && handleRect != null)
                StartHandleSlide(value, instant: false);

            if (ColorTransitionActive)
                StartColorTween(value, instant: false);
        }

        private void ApplyInstant(bool value)
        {
            if ((animationMode == AnimationMode.Animator || animationMode == AnimationMode.Both) && switchAnimator != null)
            {
                ActivateAnimator();
                switchAnimator.Play(value ? StateOnInstant : StateOffInstant);
                switchAnimator.Update(0f);
                float length = switchAnimator.GetCurrentAnimatorStateInfo(0).length;
                ScheduleAnimatorDisable(length);
            }

            if ((animationMode == AnimationMode.GenericHandle || animationMode == AnimationMode.Both) && handleRect != null)
                StartHandleSlide(value, instant: true);

            if (ColorTransitionActive)
                StartColorTween(value, instant: true);
        }

        #endregion

        #region Animator

        private void ActivateAnimator()
        {
            switchAnimator.enabled = true;
            switchAnimator.Rebind();
            switchAnimator.Update(0f);
        }

        private void ScheduleAnimatorDisable(float duration)
        {
            _animatorTimer = duration + 0.05f;
            _animatorPending = true;
        }

        private void TickAnimatorDisable()
        {
            if (!_animatorPending) return;

            _animatorTimer -= Time.unscaledDeltaTime;
            if (_animatorTimer > 0f) return;

            switchAnimator.enabled = false;
            _animatorPending = false;
        }

        #endregion

        #region Generic handle animation

        private void StartHandleSlide(bool value, bool instant)
        {
            float target = value ? handlePosOn : handlePosOff;

            if (instant)
            {
                SetHandlePosition(target);
                _handlePending = false;
                return;
            }

            _handleFrom = handleRect.anchoredPosition.x;
            _handleTo = target;
            _handleElapsed = 0f;
            _handleDuration = handleDuration;
            _handlePending = true;
        }

        private void TickHandle()
        {
            if (!_handlePending) return;

            _handleElapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(_handleElapsed / _handleDuration);
            float x = Mathf.Lerp(_handleFrom, _handleTo, handleCurve.Evaluate(t));

            SetHandlePosition(x);

            if (t >= 1f)
                _handlePending = false;
        }

        private void SetHandlePosition(float x)
        {
            var pos = handleRect.anchoredPosition;
            pos.x = x;
            handleRect.anchoredPosition = pos;
        }

        #endregion

        #region Generic color transition

        private bool ColorTransitionActive =>
            useColorTransition && animationMode == AnimationMode.GenericHandle && colorTransitions.Count > 0;

        private Color ResolveEntryColor(ColorTransitionEntry e, bool forOn)
        {
            if (useColorStyleSheet && colorTransitionSheet != null)
                return colorTransitionSheet.GetColor(forOn ? e.slotOn : e.slotOff);

            return forOn ? e.colorOn : e.colorOff;
        }

        private void OnColorSheetChanged(UIStyleSheet changed)
        {
            if (changed != colorTransitionSheet || !ColorTransitionActive)
                return;

            if (!_colorPending)
                SetColorsInstant(isOn);
        }

        private void PrepareColorTargets()
        {
            foreach (var e in colorTransitions)
            {
                if (e?.target == null)
                    continue;

                if (e.target.TryGetComponent<ImageRounded>(out var rounded))
                    rounded.SetUseOwnColors(false);
            }
        }

        private void StartColorTween(bool value, bool instant)
        {
            if (instant)
            {
                SetColorsInstant(value);
                _colorPending = false;
                return;
            }

            if (_colorFrom == null || _colorFrom.Length != colorTransitions.Count)
                _colorFrom = new Color[colorTransitions.Count];

            for (int i = 0; i < colorTransitions.Count; i++)
            {
                var e = colorTransitions[i];
                _colorFrom[i] = e?.target != null ? e.target.color : Color.white;
            }

            _colorElapsed  = 0f;
            _colorDuration = colorDuration;
            _colorToOn     = value;
            _colorPending  = true;
        }

        private void TickColor()
        {
            if (!_colorPending)
                return;

            _colorElapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(_colorElapsed / _colorDuration);
            float k = colorCurve.Evaluate(t);

            for (int i = 0; i < colorTransitions.Count; i++)
            {
                var e = colorTransitions[i];
                if (e?.target == null)
                    continue;

                Color to = ResolveEntryColor(e, _colorToOn);
                e.target.color = Color.LerpUnclamped(_colorFrom[i], to, k);
            }

            if (t >= 1f)
                _colorPending = false;
        }

        private void SetColorsInstant(bool toOn)
        {
            foreach (var e in colorTransitions)
            {
                if (e?.target == null)
                    continue;

                e.target.color = ResolveEntryColor(e, toOn);
            }
        }

        #endregion

        #region Highlight

        private void SetHighlightTarget(float target)
        {
            if (highlightCG == null) return;
            _highlightTarget = target;
        }

        private void TickHighlight()
        {
            if (highlightCG == null) return;
            if (Mathf.Approximately(_highlightAlpha, _highlightTarget)) return;

            _highlightAlpha = Mathf.MoveTowards(
                _highlightAlpha,
                _highlightTarget,
                Time.unscaledDeltaTime * highlightSpeed);

            highlightCG.alpha = _highlightAlpha;
        }

        #endregion

        #region Save / Load

        private void LoadSavedState()
        {
            isOn = PlayerPrefs.GetInt(_prefsKey, isOn ? 1 : 0) == 1;
            ApplyInstant(isOn);
        }

        private void SaveState(bool value)
        {
            PlayerPrefs.SetInt(_prefsKey, value ? 1 : 0);
        }

        #endregion

        #region Events

        private void FireEvents(bool value)
        {
            if (value) onEvents.Invoke();
            else offEvents.Invoke();

            onValueChanged.Invoke(value);
        }

        #endregion

        #region Setup

        private void EnsureNavButton()
        {
            if (TryGetComponent<Button>(out var existing))
            {
                existing.transition = Selectable.Transition.None;
                return;
            }

            var btn = gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.navigation = new Navigation { mode = Navigation.Mode.Automatic };
        }

        #endregion

        #region Audio

        private void PlayClickSound()
        {
            if (!useSounds || UIAudio.Instance == null)
                return;
            UIAudio.Instance.audioSource.PlayOneShot(UIAudio.Instance.UIManagerAsset.ClickSound);
        }

        private void PlayHoverSound()
        {
            if (!useSounds || UIAudio.Instance == null)
                return;
            UIAudio.Instance.audioSource.PlayOneShot(UIAudio.Instance.UIManagerAsset.HoverSound);
        }

        #endregion

        #region ContextMenu - Debug / Preview (Editor only)

#if UNITY_EDITOR
        [ContextMenu("Debug / Toggle")]
        private void Debug_Toggle()
        {
            if (Application.isPlaying)
                Toggle();
            else
                isOn = !isOn;

            UnityEditor.EditorUtility.SetDirty(this);
        }

        [ContextMenu("Debug / Set On (animated)")]
        private void Debug_SetOn()
        {
            if (Application.isPlaying)
                SetOn();
            else
            {
                isOn = true;
                ApplyInstant(true);
            }

            UnityEditor.EditorUtility.SetDirty(this);
        }

        [ContextMenu("Debug / Set Off (animated)")]
        private void Debug_SetOff()
        {
            if (Application.isPlaying) SetOff();
            else
            {
                isOn = false;
                ApplyInstant(false);
            }

            UnityEditor.EditorUtility.SetDirty(this);
        }

        [ContextMenu("Debug / Snap Handle to On")]
        private void Debug_SnapHandleOn()
        {
            if (handleRect == null) { Debug.LogWarning("[UISwitchPro] handleRect is null.", this); return; }
            SetHandlePosition(handlePosOn);
            UnityEditor.EditorUtility.SetDirty(handleRect);
        }

        [ContextMenu("Debug / Snap Handle to Off")]
        private void Debug_SnapHandleOff()
        {
            if (handleRect == null) { Debug.LogWarning("[UISwitchPro] handleRect is null.", this); return; }
            SetHandlePosition(handlePosOff);
            UnityEditor.EditorUtility.SetDirty(handleRect);
        }

        public void Editor_SnapHandle(bool toOn)
        {
            if (handleRect == null) return;
            SetHandlePosition(toOn ? handlePosOn : handlePosOff);
            UnityEditor.EditorUtility.SetDirty(handleRect);
        }

        public void Editor_SnapColors(bool toOn)
        {
            foreach (var e in colorTransitions)
            {
                if (e?.target == null)
                    continue;

                if (e.target.TryGetComponent<ImageRounded>(out var rounded))
                {
                    rounded.SetUseOwnColors(false);
                    UnityEditor.EditorUtility.SetDirty(rounded);
                }

                e.target.color = ResolveEntryColor(e, toOn);
                UnityEditor.EditorUtility.SetDirty(e.target);
            }
        }

        [ContextMenu("Debug / Force Refresh UI")]
        private void Debug_ForceRefresh()
        {
            if (Application.isPlaying) ForceSetValue(isOn, animated: false);
            else ApplyInstant(isOn);

            UnityEditor.EditorUtility.SetDirty(this);
        }

        [ContextMenu("Debug / Print State")]
        private void Debug_PrintState()
        {
            Debug.Log(
                $"[UISwitchPro] '{name}'\n" +
                $"  isOn:            {isOn}\n" +
                $"  isInteractable:  {isInteractable}\n" +
                $"  animatorPending: {_animatorPending} (timer: {_animatorTimer:F3}s)\n" +
                $"  handlePending:   {_handlePending} (elapsed: {_handleElapsed:F3}/{_handleDuration:F3}s)\n" +
                $"  highlightAlpha:  {_highlightAlpha:F2} -> {_highlightTarget:F2}\n" +
                $"  prefsKey:        {_prefsKey}",
                this);
        }
#endif

        #endregion
    }
}
