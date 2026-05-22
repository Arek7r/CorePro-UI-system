using System;
using System.Collections;
using UnityEngine;

namespace CorePro.UI
{
    /// <summary>
    /// Handles smooth Fade and Scale animations for table rows using CanvasGroup.
    /// Support staggered starts via delay.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class UITableRowAnimator : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Default delay before starting the 'Show' animation. Can be overridden via code.")]
        [SerializeField] private float delayBeforeShow = 0f;

        [Header("Show Animation")]
        [SerializeField] private bool useShowAnimation = true;
        [SerializeField] private float showDuration = 0.2f;
        [SerializeField] private Vector3 showStartScale = new Vector3(0.95f, 0.95f, 1f);

        [Header("Hide Animation")]
        [SerializeField] private bool useHideAnimation = true;
        [SerializeField] private float hideDuration = 0.15f;
        [SerializeField] private Vector3 hideTargetScale = new Vector3(0.95f, 0.95f, 1f);

        private CanvasGroup _canvasGroup;
        private Coroutine _currentRoutine;

        private void Awake()
        {
            if (_canvasGroup == null)
                _canvasGroup = GetComponent<CanvasGroup>();
        }

        /// <summary>
        /// Sets the delay (in seconds) before the show animation starts.
        /// Useful for staggered row entries.
        /// </summary>
        public void SetDelay(float delaySeconds)
        {
            delayBeforeShow = Mathf.Max(0f, delaySeconds);
        }

        /// <summary>
        /// Immediately resets the row to its fully visible state (Alpha 1, Scale 1).
        /// Useful for pooling or skipping animations.
        /// </summary>
        public void Reset()
        {
            if (_currentRoutine != null) 
                StopCoroutine(_currentRoutine);
        
            if (_canvasGroup != null) 
                _canvasGroup.alpha = 1f;
            
            transform.localScale = Vector3.one;
            _currentRoutine = null;
        }

        /// <summary>
        /// Starts the 'Show' animation (Fade In + Scale Up).
        /// </summary>
        public void PlayShow()
        {
            if (!useShowAnimation)
            {
                Reset(); 
                return;
            }

            StopActiveRoutine();
            _currentRoutine = StartCoroutine(ShowRoutine());
        }

        /// <summary>
        /// Starts the 'Hide' animation (Fade Out + Scale Down).
        /// </summary>
        /// <param name="onComplete">Callback executed after animation finishes.</param>
        public void PlayHide(Action onComplete = null)
        {
            if (!useHideAnimation)
            {
                onComplete?.Invoke();
                return;
            }

            StopActiveRoutine();
            _currentRoutine = StartCoroutine(HideRoutine(onComplete));
        }

        private void StopActiveRoutine()
        {
            if (_currentRoutine != null)
                StopCoroutine(_currentRoutine);
        }

        private IEnumerator ShowRoutine()
        {
            // Initial state: invisible and slightly scaled down
            if (_canvasGroup != null) 
                _canvasGroup.alpha = 0f;
            
            transform.localScale = showStartScale;

            // Optional delay (stagger effect)
            if (delayBeforeShow > 0f)
            {
                yield return new WaitForSecondsRealtime(delayBeforeShow);
            }

            float elapsedTime = 0f;
            while (elapsedTime < showDuration)
            {
                elapsedTime += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsedTime / showDuration);

                if (_canvasGroup != null) 
                    _canvasGroup.alpha = progress;
            
                // Smoothly interpolate scale
                float currentScale = Mathf.SmoothStep(showStartScale.x, 1f, progress);
                transform.localScale = new Vector3(currentScale, currentScale, 1f);

                yield return null;
            }

            Reset(); // Ensure final state is perfect
        }

        private IEnumerator HideRoutine(Action onComplete)
        {
            float elapsedTime = 0f;
            float startAlpha = (_canvasGroup != null) ? _canvasGroup.alpha : 1f;
            Vector3 startScale = transform.localScale;

            while (elapsedTime < hideDuration)
            {
                elapsedTime += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsedTime / hideDuration);

                if (_canvasGroup != null) 
                    _canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, progress);
            
                float currentScale = Mathf.Lerp(startScale.x, hideTargetScale.x, progress);
                transform.localScale = new Vector3(currentScale, currentScale, 1f);

                yield return null;
            }

            _currentRoutine = null;
            onComplete?.Invoke();
        }
    }
}