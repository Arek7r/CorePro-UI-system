using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

namespace CorePro.UI
{
    [DisallowMultipleComponent]
    public class HorizontalSelectorPro : MonoBehaviour
    {
        // =========================================================================
        //  Nested types
        // =========================================================================

        [System.Serializable]
        public class Item
        {
            public string itemTitle      = "Item Title";
            public Animator animator;
            public Sprite itemIcon;
            public UnityEvent onItemSelect = new UnityEvent();
        }

        [System.Serializable]
        public class HorizontalSelectorEvent : UnityEvent<int> { }

        // =========================================================================
        //  Serialized fields – Resources
        // =========================================================================

        [SerializeField] private Animator selectorAnimator;
        
        [SerializeField] private TextMeshProUGUI labelIncoming;
        [SerializeField] private TextMeshProUGUI labelOutgoing;
        [SerializeField] private TextMeshProUGUI labelInstant;

        [SerializeField] private Image iconIncoming;
        [SerializeField] private Image iconOutgoing;

        [SerializeField] private Transform indicatorParent;
        [SerializeField] private SelectorIndicator indicatorPrefab;

        [SerializeField, Tooltip("When disabled, prevButton and nextButton are ignored entirely.")]
        private bool useNavigationButtons;
        [SerializeField] private ButtonPro prevButton;
        [SerializeField] private ButtonPro nextButton;

        // =========================================================================
        //  Serialized fields – Settings
        // =========================================================================

        [SerializeField] private bool saveSelected;
        [SerializeField, Tooltip("Unique key used to persist the selected index in PlayerPrefs.")]
        private string saveKey = "Selector";

        [SerializeField, Tooltip("Fires onValueChanged and onItemSelect for the default item on Awake.")]
        private bool invokeOnAwake = false;
        [SerializeField, Tooltip("Swaps the Prev and Next transition animations.")]
        private bool invertAnimation;
        [SerializeField, Tooltip("Wraps selection from last item back to first and vice versa.")]
        private bool loopSelection;
        
        [SerializeField] private int defaultIndex = 0;
        [SerializeField] private List<Item> items = new List<Item>();

        // =========================================================================
        //  Public API
        // =========================================================================

        /// <summary>Current selected index.</summary>
        public int Index => _index;

        /// <summary>Read-only view of the items list.</summary>
        public IReadOnlyList<Item> Items => items;

        public HorizontalSelectorEvent onValueChanged = new HorizontalSelectorEvent();

        // =========================================================================
        //  Private state
        // =========================================================================

        private int _index;
        private float _cachedStateLength;
        private readonly List<SelectorIndicator> _indicators = new List<SelectorIndicator>();
        
        private Coroutine _disableAnimatorCoroutine;

        private const string SaveKeyPrefix = "HorizontalSelector_";
        private const string AnimClipNext   = "HorizontalSelector_Next";
        private const string AnimStatePrev  = "Prev";
        private const string AnimStateNext  = "Next";
        private const string AnimItemON     = "ON";
        private const string AnimItemOFF    = "OFF";

        // =========================================================================
        //  Unity lifecycle
        // =========================================================================

        private void Awake()
        {
            if (labelIncoming == null && labelOutgoing == null)
                Debug.LogWarning("[HorizontalSelector] Both labelIncoming and labelOutgoing are missing – text display will be skipped.", this);

            InitializeSelector();
            InitializeItemAnimators();
            WireNavigationButtons();
            UpdateButtonStates();

            if (selectorAnimator != null)
                _cachedStateLength = GetAnimatorClipLength(AnimClipNext);

            if (invokeOnAwake && items.Count > 0)
            {
                items[_index].onItemSelect.Invoke();
                onValueChanged.Invoke(_index);
            }
        }

        private float GetAnimatorClipLength(string clipName)   
        {   
            if (selectorAnimator == null || selectorAnimator.runtimeAnimatorController == null)
                return 0.3f;
            
            var clips = selectorAnimator.runtimeAnimatorController.animationClips;
            for (int i = 0; i < clips.Length; i++)
                if (clips[i].name == clipName) return clips[i].length;
            return 0.3f;
        } 

        private void OnEnable()
        {
            RestartDisableAnimatorCoroutine();
        }

        private void OnDestroy()
        {
            prevButton?.onClick.RemoveListener(PreviousItem);
            nextButton?.onClick.RemoveListener(NextItem);
        }

        // =========================================================================
        //  Initialization helpers
        // =========================================================================

        private void InitializeItemAnimators()
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].animator != null)
                    items[i].animator.gameObject.SetActive(i == _index);
            }
        }

        private void WireNavigationButtons()
        {
            if (!useNavigationButtons) return;
            prevButton?.onClick.AddListener(PreviousItem);
            nextButton?.onClick.AddListener(NextItem);
        }

        // =========================================================================
        //  Navigation
        // =========================================================================

        public void PreviousItem() => NavigateTo(direction: -1);
        public void NextItem()     => NavigateTo(direction: +1);

        /// <summary>Advances selection by <paramref name="direction"/> steps (+1 or -1), respecting loop and boundary settings.</summary>
        private void NavigateTo(int direction)
        {
            if (items.Count == 0)
                return;

            int proposed = _index + direction;

            if (loopSelection)
            {
                proposed = (proposed + items.Count) % items.Count;
            }
            else
            {
                if (proposed < 0 || proposed >= items.Count) return;
            }

            PrepareAnimator();
            CopyToOutgoing();

            int previousIndex = _index;
            _index = proposed;

            ApplyCurrentItemToUI();
            PlayItemTransitionAnimations(previousIndex, _index);

            items[_index].onItemSelect.Invoke();
            onValueChanged.Invoke(_index);

            PlaySelectorAnimation(direction);
            PostNavigationCleanup();
        }

        // =========================================================================
        //  UI application
        // =========================================================================

        /// <summary>Copies current item data (labelIncoming, icon, title) to the visible UI.</summary>
        private void ApplyCurrentItemToUI()
        {
            Item current = items[_index];

            if (labelIncoming != null)
                labelIncoming.text = current.itemTitle;

            ApplyInstantLabel(current.itemTitle);

            if (iconIncoming != null)
                iconIncoming.sprite = current.itemIcon;
        }

        private void ApplyInstantLabel(string text)
        {
            if (labelInstant != null)
                labelInstant.text = text;
        }

        private void CopyToOutgoing()
        {
            if (labelIncoming != null && labelOutgoing != null)
                labelOutgoing.text = labelIncoming.text;

            if (iconIncoming != null && iconOutgoing != null)
                iconOutgoing.sprite = iconIncoming.sprite;
        }

        // =========================================================================
        //  Animation
        // =========================================================================

        private void PrepareAnimator()
        {
            if (selectorAnimator == null)
                return;
            
            StopDisableAnimatorCoroutine();
            selectorAnimator.enabled = true;
            selectorAnimator.Rebind();
        }

        private void PlaySelectorAnimation(int direction)
        {
            if (selectorAnimator == null) return;

            selectorAnimator.Play(null);
            selectorAnimator.StopPlayback();

            bool playNext = (direction > 0) != invertAnimation;
            selectorAnimator.Play(playNext ? AnimStateNext : AnimStatePrev);
        }

        private void PlayItemTransitionAnimations(int previousIndex, int newIndex)
        {
            PlayItemAnimation(previousIndex, AnimItemOFF, activate: false);
            PlayItemAnimation(newIndex,      AnimItemON,  activate: true);
        }

        private void PlayItemAnimation(int itemIndex, string stateName, bool activate)
        {
            if ((uint)itemIndex >= (uint)items.Count) 
                return;

            Animator anim = items[itemIndex].animator;
            if (anim == null) 
                return;

            if (activate)
                anim.gameObject.SetActive(true);
            
            if (Application.isPlaying)
                anim.Play(stateName);
        }

        private void PostNavigationCleanup()
        {
            UpdateButtonStates();
            PersistSelectionIfNeeded();
            RefreshIndicators();
            RestartDisableAnimatorCoroutine();
        }

        private void RestartDisableAnimatorCoroutine()
        {
            if (Application.isPlaying == false)
                return;
            
            if (!gameObject.activeInHierarchy || selectorAnimator == null) 
                return;
            
            StopDisableAnimatorCoroutine();
            
            _disableAnimatorCoroutine = StartCoroutine(DisableAnimatorAfterDelay());
        }

        private void StopDisableAnimatorCoroutine()
        {
            if (_disableAnimatorCoroutine != null)
            {
                StopCoroutine(_disableAnimatorCoroutine);
                _disableAnimatorCoroutine = null;
            }
        }

        private IEnumerator DisableAnimatorAfterDelay()
        {
            yield return new WaitForSeconds(_cachedStateLength + 0.1f);
            if (selectorAnimator != null)
                selectorAnimator.enabled = false;
            _disableAnimatorCoroutine = null;
        }

        // =========================================================================
        //  Button state
        // =========================================================================

        private void UpdateButtonStates()
        {
            if (!useNavigationButtons) return;

            prevButton?.gameObject.SetActive(loopSelection || _index > 0);
            nextButton?.gameObject.SetActive(loopSelection || _index < items.Count - 1);
        }

        // =========================================================================
        //  Indicators
        // =========================================================================

        public void UpdateIndicators()
        {
            if (indicatorParent == null || indicatorPrefab == null) return;

            for (int i = indicatorParent.childCount - 1; i >= 0; i--)
                Destroy(indicatorParent.GetChild(i).gameObject);

            _indicators.Clear();

            for (int i = 0; i < items.Count; i++)
            {
                SelectorIndicator indicator = Instantiate(indicatorPrefab, indicatorParent);
                indicator.name = items[i].itemTitle;
                indicator.SetActive(i == _index);
                _indicators.Add(indicator);
            }
        }

        private void RefreshIndicators()
        {
            for (int i = 0; i < _indicators.Count; i++)
                _indicators[i].SetActive(i == _index);
        }


        // =========================================================================
        //  Public API
        // =========================================================================

        public void UpdateUI()
        {
            if (items.Count == 0) return;

            if (selectorAnimator != null)
            {
                selectorAnimator.enabled = true;
                selectorAnimator.Rebind();
            }

            ApplyCurrentItemToUI();
            UpdateIndicators();
            UpdateButtonStates();
            RestartDisableAnimatorCoroutine();
        }

        public void InitializeSelector()
        {
            if (items.Count == 0) return;

            LoadOrSeedSaveData();

            _index = Mathf.Clamp(defaultIndex, 0, items.Count - 1);

            string displayText = items[_index].itemTitle;
            if (labelIncoming != null) labelIncoming.text = displayText;
            if (labelOutgoing != null) labelOutgoing.text = displayText;
            ApplyInstantLabel(displayText);

            if (iconIncoming != null)
            {
                iconIncoming.sprite = items[_index].itemIcon;
                if (iconOutgoing != null)
                    iconOutgoing.sprite = iconIncoming.sprite;
            }

            UpdateIndicators();
        }

        /// <summary>Navigates directly to <paramref name="targetIndex"/> without animation.</summary>
        public void SetIndex(int targetIndex)
        {
            if ((uint)targetIndex >= (uint)items.Count)
            {
                Debug.LogWarning($"[HorizontalSelector] SetIndex({targetIndex}) is out of range (count={items.Count}).", this);
                return;
            }

            _index = targetIndex;
            ApplyCurrentItemToUI();
            RefreshIndicators();
            UpdateButtonStates();
            items[_index].onItemSelect.Invoke();
            onValueChanged.Invoke(_index);
            PersistSelectionIfNeeded();
        }

        public void CreateNewItem(string title)
        {
            items.Add(new Item { itemTitle = title });
        }

        public void AddNewItem() => items.Add(new Item());

        public void RemoveItem(string itemTitle)
        {
            int idx = items.FindIndex(x => x.itemTitle == itemTitle);
            if (idx < 0)
            {
                Debug.LogWarning($"[HorizontalSelector] RemoveItem: '{itemTitle}' not found.", this);
                return;
            }
            items.RemoveAt(idx);
            InitializeSelector();
        }

        // =========================================================================
        //  Persistence
        // =========================================================================

        private string FullSaveKey => SaveKeyPrefix + saveKey;

        private void LoadOrSeedSaveData()
        {
            if (!saveSelected) return;

            if (PlayerPrefs.HasKey(FullSaveKey))
                defaultIndex = PlayerPrefs.GetInt(FullSaveKey);
            else
                PlayerPrefs.SetInt(FullSaveKey, defaultIndex);
        }

        private void PersistSelectionIfNeeded()
        {
            if (saveSelected)
                PlayerPrefs.SetInt(FullSaveKey, _index);
        }

        [ContextMenu("Clear Saved Selection")]
        private void ClearSavedSelection()
        {
            if (!PlayerPrefs.HasKey(FullSaveKey))
            {
                Debug.Log($"[HorizontalSelectorPro] No saved data found for key '{FullSaveKey}'.");
                return;
            }
            PlayerPrefs.DeleteKey(FullSaveKey);
            Debug.Log($"[HorizontalSelectorPro] Cleared saved selection for key '{FullSaveKey}'.");
        }

        [ContextMenu("Log Current State")]
        private void LogCurrentState()
        {
            Debug.Log($"[HorizontalSelectorPro] '{name}' - index: {_index} / {items.Count - 1}" +
                      $", item: '{(items.Count > 0 ? items[_index].itemTitle : "none")}'" +
                      $", saveKey: '{FullSaveKey}'" +
                      $", saved: {saveSelected}", this);
        }
    }
}