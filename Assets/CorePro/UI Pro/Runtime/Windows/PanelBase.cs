using InspectorPro;
using UnityEngine;

namespace CorePro.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class PanelBase : MonoBehaviourCorePro
    {
        [Group("Navigation", true)]
        [SerializeField] private bool enableBackButton = false;
        public bool EnableBackButton => enableBackButton;

        [Group("Save", true)]
        [SerializeField] private bool   trackOpened   = false;
        [ShowIf(nameof(trackOpened), true)]
        [SerializeField] private string openedSaveKey = "Panel";

        [Group("Animation", true)]
        [SerializeField] private AnimationMode animationMode = AnimationMode.Generic;

        [ShowIf(nameof(animationMode), AnimationMode.Animator)]
        [SerializeField] private Animator panelAnimator;

        [ShowIf(nameof(animationMode), AnimationMode.Generic)]
        [SerializeField] private CanvasGroup canvasGroup;

        [ShowIf(nameof(animationMode), AnimationMode.Generic)]
        [SerializeField] private bool useUnscaledTime = true;

        [ShowIf(nameof(animationMode), AnimationMode.Generic)]
        [SerializeField] private GenericAnimationType genericAnimationType = GenericAnimationType.Fade;

        [ShowIf(nameof(animationMode), AnimationMode.Generic)]
        [SerializeField, Min(0.001f)] private float fadeInDuration = 0.25f;

        [ShowIf(nameof(animationMode), AnimationMode.Generic)]
        [SerializeField] private AnimationCurve fadeInCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [ShowIf(nameof(animationMode), AnimationMode.Generic)]
        [SerializeField] private bool overrideFadeOut = false;

        [ShowIf(nameof(overrideFadeOut), true)]
        [SerializeField, Min(0.001f)] private float fadeOutDuration = 0.15f;

        [ShowIf(nameof(overrideFadeOut), true)]
        [SerializeField] private AnimationCurve fadeOutCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [ShowIf(nameof(animationMode), AnimationMode.Generic)]
        [SerializeField] private float hiddenScale = 0f;

        [Group("Behaviour", true)]
        [SerializeField] private bool showDebugLogs;
        [SerializeField] private bool disableGameObjectWhenHidden = true;

        public bool IsVisible   { get; private set; }
        public bool IsAnimating { get; private set; }

        private AnimState      _state            = AnimState.Idle;
        private float          _elapsed          = 0f;
        private float          _startAlpha       = 0f;
        private float          _targetAlpha      = 0f;
        private float          _startScale       = 1f;
        private float          _targetScale      = 1f;
        private float          _activeDuration   = 0f;
        private AnimationCurve _activeCurve;
        private float          _animatorTimeLeft = 0f;
        private System.Action  _onComplete;
        private string         _openedPrefsKey;
        private RectTransform  _rectTransform;

        private const string        PrefsPrefix     = "Panel_";
        private static readonly int AnimSpeedHash  = Animator.StringToHash("AnimSpeed");
        private const string StateIn        = "Panel In";
        private const string StateOut       = "Panel Out";
        private const string StateInstantIn = "Panel Instant In";

        private enum AnimState { Idle, FadingIn, FadingOut, WaitingAnimatorIn, WaitingAnimatorOut }

        private void Reset()
        {
            canvasGroup    ??= GetComponent<CanvasGroup>();
            _rectTransform ??= GetComponent<RectTransform>();
        }

        protected virtual void Awake()
        {
            canvasGroup     ??= GetComponent<CanvasGroup>();
            _rectTransform  ??= GetComponent<RectTransform>();
            _openedPrefsKey   = PrefsPrefix + openedSaveKey;
        }

        protected virtual void Update()
        {
            if (_state == AnimState.Idle) return;

            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

            if (_state == AnimState.WaitingAnimatorIn || _state == AnimState.WaitingAnimatorOut)
            {
                _animatorTimeLeft -= dt;
                if (_animatorTimeLeft > 0f) return;
                if (panelAnimator != null) panelAnimator.enabled = false;
                FinishTransition(_state == AnimState.WaitingAnimatorIn);
                return;
            }

            _elapsed += dt;
            float t  = Mathf.Clamp01(_elapsed / _activeDuration);
            float ev = _activeCurve.Evaluate(t);

            if (AnimatesFade())
                canvasGroup.alpha = Mathf.Lerp(_startAlpha, _targetAlpha, ev);

            if (AnimatesScale() && _rectTransform != null)
            {
                float s = Mathf.Lerp(_startScale, _targetScale, ev);
                _rectTransform.localScale = new Vector3(s, s, 1f);
            }

            if (t >= 1f) FinishTransition(_state == AnimState.FadingIn);
        }

        [ContextMenu("Show")]
        public void Show() => Show(null);

        public void Show(System.Action onComplete)
        {
            if (IsVisible && !IsAnimating) return;

            SetInteractable(false);
            OnBeforeShow();

            IsVisible   = true;
            IsAnimating = true;
            _onComplete = onComplete;

            if (animationMode == AnimationMode.Animator && panelAnimator != null)
            {
                panelAnimator.enabled = true;
                panelAnimator.SetFloat(AnimSpeedHash, 1f);
                panelAnimator.Play(StateIn);
                _animatorTimeLeft = GetAnimatorClipLength(StateIn);
                _state = AnimState.WaitingAnimatorIn;
            }
            else
            {
                _elapsed        = 0f;
                _activeDuration = fadeInDuration;
                _activeCurve    = fadeInCurve;
                _startAlpha     = AnimatesFade() ? canvasGroup.alpha : 1f;
                _targetAlpha    = 1f;
                _startScale     = AnimatesScale() ? hiddenScale : 1f;
                _targetScale    = 1f;
                if (!AnimatesFade()) canvasGroup.alpha = 1f;
                _state          = AnimState.FadingIn;
            }

            gameObject.SetActive(true);
        }

        [ContextMenu("Hide")]
        public void Hide() => Hide(null);

        public void Hide(System.Action onComplete)
        {
            if (!IsVisible && !IsAnimating)
            {
                if (showDebugLogs)
                    Debug.Log($"Panel {gameObject.name}: Hide - return");
                return;
            }

            if (showDebugLogs) 
                Debug.Log($"Panel {gameObject.name}: Hide");

            SetInteractable(false);
            OnBeforeHide();

            IsAnimating = true;
            _onComplete = onComplete;

            if (animationMode == AnimationMode.Animator && panelAnimator != null)
            {
                panelAnimator.enabled = true;
                panelAnimator.SetFloat(AnimSpeedHash, 1f);
                panelAnimator.Play(StateOut);
                _animatorTimeLeft = GetAnimatorClipLength(StateOut);
                _state = AnimState.WaitingAnimatorOut;
            }
            else
            {
                _elapsed        = 0f;
                _activeDuration = overrideFadeOut ? fadeOutDuration : fadeInDuration;
                _activeCurve    = overrideFadeOut ? fadeOutCurve    : fadeInCurve;
                _startAlpha     = canvasGroup.alpha;
                _targetAlpha    = AnimatesFade() ? 0f : canvasGroup.alpha;
                _startScale     = 1f;
                _targetScale    = AnimatesScale() ? hiddenScale : 1f;
                _state          = AnimState.FadingOut;
            }
        }

        public void ShowImmediate()
        {
            CancelAnimation();
            gameObject.SetActive(true);
            if (animationMode == AnimationMode.Animator && panelAnimator != null)
            {
                panelAnimator.enabled = true;
                panelAnimator.Play(StateInstantIn);
                panelAnimator.enabled = false;
            }
            OnBeforeShow();
            ApplyVisibleImmediate();
            SaveOpenedState();
            OnAfterShow();
        }

        public void HideImmediate()
        {
            if (showDebugLogs) Debug.Log($"Panel {gameObject.name}: HideImmediate");
            CancelAnimation();
            OnBeforeHide();
            ApplyHiddenImmediate();
            OnAfterHide();
        }

        public void Toggle()
        {
            if (IsVisible) Hide();
            else           Show();
        }

        protected virtual void OnBeforeShow()  { }
        protected virtual void OnAfterShow()   { }
        protected virtual void OnBeforeHide()  { }
        protected virtual void OnAfterHide()   { }

        private void FinishTransition(bool showingIn)
        {
            _state      = AnimState.Idle;
            IsAnimating = false;

            if (showingIn)
            {
                ApplyVisibleImmediate();
                SaveOpenedState();
                OnAfterShow();
            }
            else
            {
                IsVisible = false;
                if (AnimatesFade()) canvasGroup.alpha = 0f;
                if (AnimatesScale() && _rectTransform)
                    _rectTransform.localScale = new Vector3(hiddenScale, hiddenScale, 1f);
                SetInteractable(false);
                if (disableGameObjectWhenHidden)
                {
                    if (showDebugLogs) Debug.Log($"Panel {gameObject.name}: FinishTransition - SetActive(false)");
                    gameObject.SetActive(false);
                }
                OnAfterHide();
            }

            var cb = _onComplete;
            _onComplete = null;
            cb?.Invoke();
        }

        private void CancelAnimation()
        {
            _state      = AnimState.Idle;
            IsAnimating = false;
            _onComplete = null;
        }

        private void ApplyVisibleImmediate()
        {
            if (canvasGroup) canvasGroup.alpha = 1f;
            if (AnimatesScale() && _rectTransform) _rectTransform.localScale = Vector3.one;
            SetInteractable(true);
            IsVisible   = true;
            IsAnimating = false;
            gameObject.SetActive(true);
        }

        private void ApplyHiddenImmediate()
        {
            if (canvasGroup) canvasGroup.alpha = AnimatesFade() ? 0f : 1f;
            if (AnimatesScale() && _rectTransform)
                _rectTransform.localScale = new Vector3(hiddenScale, hiddenScale, 1f);
            SetInteractable(false);
            IsVisible   = false;
            IsAnimating = false;
            if (disableGameObjectWhenHidden)
            {
                if (showDebugLogs) Debug.Log($"Panel {gameObject.name}: ApplyHiddenImmediate - SetActive(false)");
                gameObject.SetActive(false);
            }
        }

        private void SetInteractable(bool value)
        {
            if (canvasGroup)
            {
                canvasGroup.interactable   = value;
                canvasGroup.blocksRaycasts = value;
            }
        }

        private float GetAnimatorClipLength(string clipName)
        {
            if (panelAnimator == null || panelAnimator.runtimeAnimatorController == null)
                return 0.3f;
            
            var clips = panelAnimator.runtimeAnimatorController.animationClips;
            for (int i = 0; i < clips.Length; i++)
                if (clips[i].name == clipName) return clips[i].length;
            return 0.3f;
        }

        // =============================================
        // Save / Load - opened state
        // =============================================

        /// <summary>Returns true if this panel was ever shown (requires trackOpened + unique saveKey).</summary>
        public bool WasEverOpened()
        {
            if (!trackOpened) 
                return false;
            return PlayerPrefs.GetInt(_openedPrefsKey, 0) == 1;
        }

        /// <summary>Clears the saved opened state for this panel.</summary>
        public void ResetOpenedState()
        {
            if (!trackOpened) return;
            PlayerPrefs.DeleteKey(_openedPrefsKey);
        }

        private void SaveOpenedState()
        {
            if (!trackOpened) return;
            PlayerPrefs.SetInt(_openedPrefsKey, 1);
        }
        
        [ContextMenu("DeleteSave")]
        private void DeleteSave ()   
        {   
            _openedPrefsKey = PrefsPrefix + openedSaveKey;
            PlayerPrefs.DeleteKey(_openedPrefsKey);
        } 

        private bool AnimatesScale() =>
            animationMode == AnimationMode.Generic &&
            (genericAnimationType == GenericAnimationType.ScaleUp ||
             genericAnimationType == GenericAnimationType.FadeAndScale);

        private bool AnimatesFade() =>
            animationMode != AnimationMode.Animator &&
            genericAnimationType != GenericAnimationType.ScaleUp;

        private enum AnimationMode { Generic, Animator }

        public enum GenericAnimationType { Fade, ScaleUp, FadeAndScale }
    }
}