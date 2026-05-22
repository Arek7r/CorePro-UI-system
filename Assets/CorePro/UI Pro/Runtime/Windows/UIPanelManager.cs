using System.Collections.Generic;
using CorePro;
using InspectorPro;
using UnityEngine;
using UnityEngine.Events;
using CorePro.UI;

namespace CorePro.UI
{
    public class UIPanelManager : MonoBehaviourCorePro
    {
        [Group("Panels")]
        [SerializeField] private List<PanelEntry> panels = new List<PanelEntry>();

        [Group("Settings")]
        [SerializeField, Min(-1)] private int defaultPanelIndex = 0;
        [SerializeField] private bool cullInactivePanels = true;
        //[SerializeField] private bool useUnscaledTime = true;

        [Group("Back Button")]
        [Tooltip("Global Back button visible on panels where EnableBackButton = true.")]
        [SerializeField] private ButtonPro globalBackButton;

        [Group("Back Stack", true)]
        [SerializeField] private bool enableBackStack = true;
        [SerializeField, Min(1)] private int maxHistoryDepth = 20;

        [Group("Events", true)]
        public UnityEvent<int>    onPanelOpened      = new UnityEvent<int>();
        public UnityEvent<int>    onPanelClosed      = new UnityEvent<int>();
        public UnityEvent<string> onPanelOpenedByName = new UnityEvent<string>();

        private int _currentIndex = -1;
        private readonly Stack<int> _backStack = new Stack<int>();

        public int  CurrentIndex => _currentIndex;
        public bool CanGoBack    => enableBackStack && _backStack.Count > 0;

        private void Awake()
        {
            ValidatePanels();

            if (globalBackButton != null)
            {
                globalBackButton.onClick.AddListener(Back);
                globalBackButton.gameObject.SetActive(false);
            }

            if (cullInactivePanels)
                for (int i = 0; i < panels.Count; i++)
                    if (panels[i].panel != null) panels[i].panel.HideImmediate();

            if (defaultPanelIndex >= 0 && defaultPanelIndex < panels.Count)
                OpenPanelImmediate(FindFirstUnseenPanel(defaultPanelIndex));
        }

        private void OnDestroy()
        {
            if (globalBackButton != null)
                globalBackButton.onClick.RemoveListener(Back);
        }

        // Public API - by index 

        public void OpenPanel(int index)
        {
            if (!IsValidIndex(index)) return;
            if (_currentIndex == index) return;
            PushToHistory(_currentIndex);
            TransitionTo(index, false, closeCurrentPanel: !panels[index].isPopup);
        }

        public void OpenPanelImmediate(int index)
        {
            if (!IsValidIndex(index)) return;
            PushToHistory(_currentIndex);
            TransitionTo(index, true, closeCurrentPanel: !panels[index].isPopup);
        }

        // Public API - by name 

        public void OpenPanel(string panelName)
        {
            int index = IndexOf(panelName);
            if (index < 0) { Debug.LogWarning($"[UIPanelManager] Panel '{panelName}' not found.", this); return; }
            OpenPanel(index);
        }

        public void OpenPanelImmediate(string panelName)
        {
            int index = IndexOf(panelName);
            if (index < 0) { Debug.LogWarning($"[UIPanelManager] Panel '{panelName}' not found.", this); return; }
            OpenPanelImmediate(index);
        }

        // Public API - navigation 

        public void Back()
        {
            if (!CanGoBack) return;
            int previous = _backStack.Pop();
            TransitionTo(previous, false);
        }

        public void Next()
        {
            int next = _currentIndex + 1;
            if (next < panels.Count && !panels[next].disableNavigation)
                OpenPanel(next);
        }

        public void Previous()
        {
            int prev = _currentIndex - 1;
            if (prev >= 0 && !panels[prev].disableNavigation)
                OpenPanel(prev);
        }

        public void CloseCurrentPanel()
        {
            if (_currentIndex < 0 || panels[_currentIndex].panel == null) return;
            panels[_currentIndex].panel.Hide(() => onPanelClosed.Invoke(_currentIndex));
            _currentIndex = -1;
            RefreshBackButton();
        }

        public void ClearHistory()
        {
            _backStack.Clear();
            RefreshBackButton();
        }

        // Internal 

        private void TransitionTo(int newIndex, bool immediate, bool closeCurrentPanel = true)
        {
            // Hide current - skipped when opening a popup so the underlying panel stays visible
            if (closeCurrentPanel && _currentIndex >= 0 && _currentIndex < panels.Count)
            {
                var current = panels[_currentIndex];
                if (current.panel != null)
                {
                    int closedIndex = _currentIndex;
                    if (immediate) current.panel.HideImmediate();
                    else           current.panel.Hide(() => onPanelClosed.Invoke(closedIndex));
                }
            }

            _currentIndex = newIndex;

            // Show new
            var entry = panels[newIndex];
            if (entry.panel == null) return;

            if (immediate) entry.panel.ShowImmediate();
            else           entry.panel.Show(() => onPanelOpened.Invoke(newIndex));

            onPanelOpenedByName.Invoke(entry.panelName);
            onPanelOpened.Invoke(newIndex);

            RefreshBackButton();
        }

        /// <summary>
        /// Shows or hides the global back button based on:
        /// 1. Whether the current panel has EnableBackButton = true
        /// 2. Whether there is history to go back to
        /// </summary>
        private void RefreshBackButton()
        {
            if (globalBackButton == null) return;

            bool panelAllowsBack = _currentIndex >= 0
                                   && panels[_currentIndex].panel != null
                                   && panels[_currentIndex].panel.EnableBackButton;

            bool shouldShow = panelAllowsBack && CanGoBack;
            globalBackButton.gameObject.SetActive(shouldShow);
        }

        private void PushToHistory(int index)
        {
            if (!enableBackStack || index < 0) return;

            if (_backStack.Count >= maxHistoryDepth)
            {
                var temp = new int[_backStack.Count];
                _backStack.CopyTo(temp, 0);
                _backStack.Clear();
                for (int i = temp.Length - 2; i >= 0; i--)
                    _backStack.Push(temp[i]);
            }

            _backStack.Push(index);
        }

        private int IndexOf(string panelName)
        {
            for (int i = 0; i < panels.Count; i++)
                if (panels[i].panelName == panelName) return i;
            return -1;
        }

        private bool IsValidIndex(int index)
        {
            if (index < 0 || index >= panels.Count)
            {
                Debug.LogWarning($"[UIPanelManager] Index {index} out of range (count: {panels.Count}).", this);
                return false;
            }
            if (panels[index].panel == null)
            {
                Debug.LogWarning($"[UIPanelManager] Panel at index {index} has no PanelBase assigned.", this);
                return false;
            }
            return true;
        }

        private void ValidatePanels()
        {
#if UNITY_EDITOR
            for (int i = 0; i < panels.Count; i++)
                if (panels[i].panel == null)
                    Debug.LogWarning($"[UIPanelManager] '{panels[i].panelName}' at index {i} has no panel assigned.", this);
#endif
        }

        /// <summary>
        /// Starting from <paramref name="startIndex"/>, returns the first panel index that should NOT be skipped.
        /// A panel is skipped when <see cref="PanelEntry.skipIfAlreadyOpened"/> is true AND
        /// <see cref="PanelBase.WasEverOpened"/> returns true.
        /// If every panel from startIndex onward qualifies for skipping, returns startIndex as a safe fallback.
        /// </summary>
        private int FindFirstUnseenPanel(int startIndex)
        {
            for (int i = startIndex; i < panels.Count; i++)
            {
                var entry = panels[i];
                if (!entry.skipIfAlreadyOpened) return i;
                if (entry.panel == null)        return i;
                if (!entry.panel.WasEverOpened()) return i;
            }

            // All remaining panels were already seen - fall back to startIndex
            return startIndex;
        }

        // Types 
        [System.Serializable]
        public class PanelEntry
        {
            [Tooltip("Unique identifier used in OpenPanel(string).")]
            public string panelName = "Panel";

            [Tooltip("PanelBase component for this panel.")]
            public PanelBase panel;

            [Tooltip("Override fade/animation duration. 0 = use panel default.")]
            [Min(0f)] public float overrideAnimDuration = 0f;

            [Tooltip("If true, this panel is skipped in Next/Previous navigation.")]
            public bool disableNavigation = false;

            [Tooltip("If true, this panel is skipped on startup when PanelBase.WasEverOpened() returns true. " +
                     "Requires 'Track Opened' enabled on the PanelBase.")]
            public bool skipIfAlreadyOpened = false;

            [Tooltip("If true, inactive panels are not deactivated.")]
            public bool disableCulling = false;

            [Tooltip("If true, opening this panel does not hide the previous one. " +
                     "Back() closes only this panel and returns to the panel beneath.")]
            public bool isPopup = false;

            [HideInInspector] public bool isDefaultPanel = false;
        }
    }
}