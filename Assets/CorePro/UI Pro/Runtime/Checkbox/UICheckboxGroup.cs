using System.Collections.Generic;
using InspectorPro;
using UnityEngine;
using UnityEngine.Events;

namespace CorePro.UI
{
    /// <summary>
    /// Optional group controller for UICheckboxPro components (radio button behavior).
    /// Checkboxes don't know about the group - group listens to their events.
    /// Add this to a parent GameObject and assign checkboxes in the inspector.
    /// </summary>
    public class UICheckboxGroup : MonoBehaviourCorePro
    {
        private const string PrefsPrefix = "CheckboxGroup_";

        // =============================================
        // Inspector
        // =============================================

        [Group("References")]
        [Tooltip("All checkboxes managed by this group.")]
        [SerializeField] private List<UICheckboxPro> checkboxes = new List<UICheckboxPro>();

        [Group("Settings")]
        [Tooltip("If true, exactly one checkbox must always be selected (radio buttons). Prevents deselecting the last active.")]
        [SerializeField] private bool requireSelection = true;

        [Tooltip("If true, only one checkbox can be active at a time.")]
        [SerializeField] private bool singleSelection = true;

        [Tooltip("Index of checkbox selected on Awake. -1 = none (only valid when requireSelection is false).")]
        [SerializeField, Min(-1)] private int defaultIndex = 0;

        [Group("Save")]
        [SerializeField] private bool   saveValue = false;
        [SerializeField] private string saveKey   = "Group";

        [Group("Events")]
        [SerializeField] public GroupEvent onSelectionChanged = new GroupEvent();

        [System.Serializable]
        public class GroupEvent : UnityEvent<int> { } // fires with index of newly selected checkbox (-1 = none)

        // =============================================
        // State
        // =============================================

        public int  CurrentIndex  => _currentIndex;
        public bool HasSelection  => _currentIndex >= 0;

        private int  _currentIndex = -1;
        private string _prefsKey;

        // =============================================
        // Unity
        // =============================================

        private void Awake()
        {
            _prefsKey = PrefsPrefix + saveKey;

            SubscribeAll();

            int startIndex = saveValue
                ? PlayerPrefs.GetInt(_prefsKey, defaultIndex)
                : defaultIndex;

            // Clamp to valid range
            startIndex = Mathf.Clamp(startIndex, requireSelection ? 0 : -1, checkboxes.Count - 1);

            InitialSync(startIndex);
        }

        private void OnDestroy()
        {
            UnsubscribeAll();
        }

        // =============================================
        // Public API
        // =============================================

        /// <summary>Select checkbox by index.</summary>
        public void Select(int index)
        {
            if (!IsValidIndex(index)) return;
            ApplySelection(index, animated: true);
        }

        /// <summary>Select checkbox by reference.</summary>
        public void Select(UICheckboxPro checkbox)
        {
            int index = checkboxes.IndexOf(checkbox);
            if (index < 0) return;
            ApplySelection(index, animated: true);
        }

        /// <summary>Deselect all. Only valid when requireSelection is false.</summary>
        public void DeselectAll()
        {
            if (requireSelection) return;
            ApplySelection(-1, animated: true);
        }

        /// <summary>Get checkbox at index. Returns null if out of range.</summary>
        public UICheckboxPro GetCheckbox(int index)
        {
            return IsValidIndex(index) ? checkboxes[index] : null;
        }

        // =============================================
        // Internal
        // =============================================

        private void SubscribeAll()
        {
            for (int i = 0; i < checkboxes.Count; i++)
            {
                if (checkboxes[i] == null) continue;

                // Capture index for closure
                int capturedIndex = i;
                checkboxes[i].onValueChanged.AddListener(value => OnCheckboxChanged(capturedIndex, value));
            }
        }

        private void UnsubscribeAll()
        {
            for (int i = 0; i < checkboxes.Count; i++)
            {
                if (checkboxes[i] == null) continue;
                checkboxes[i].onValueChanged.RemoveAllListeners();
            }
        }

        /// <summary>Called when any checkbox fires onValueChanged.</summary>
        private void OnCheckboxChanged(int index, bool value)
        {
            if (!singleSelection) return;

            if (value)
            {
                // New checkbox checked - uncheck others silently
                ApplySelection(index, animated: true, fromCallback: true);
            }
            else
            {
                // Checkbox unchecked by user
                if (requireSelection && _currentIndex == index)
                {
                    // Revert - can't deselect last active in requireSelection mode
                    checkboxes[index].ForceSetValue(true, animated: false);
                }
                else if (_currentIndex == index)
                {
                    _currentIndex = -1;
                    onSelectionChanged.Invoke(-1);
                }
            }
        }

        private void ApplySelection(int newIndex, bool animated, bool fromCallback = false)
        {
            if (_currentIndex == newIndex) return;

            int previousIndex = _currentIndex;
            _currentIndex = newIndex;

            // Uncheck all except new selection
            for (int i = 0; i < checkboxes.Count; i++)
            {
                if (checkboxes[i] == null) continue;

                bool shouldBeOn = (i == newIndex);

                // Use silent to avoid re-triggering OnCheckboxChanged loop
                if (checkboxes[i].IsOn != shouldBeOn)
                    checkboxes[i].SetValueSilent(shouldBeOn, animated);
            }

            if (saveValue)
                PlayerPrefs.SetInt(_prefsKey, newIndex);

            onSelectionChanged.Invoke(newIndex);
        }

        /// <summary>Sync all checkboxes to initial state without firing group events.</summary>
        private void InitialSync(int selectedIndex)
        {
            _currentIndex = selectedIndex;

            for (int i = 0; i < checkboxes.Count; i++)
            {
                if (checkboxes[i] == null) continue;
                checkboxes[i].SetValueSilent(i == selectedIndex, animated: false);
            }
        }

        private bool IsValidIndex(int index)
        {
            if (index < 0 || index >= checkboxes.Count)
            {
                Debug.LogWarning($"[UICheckboxGroup] Index {index} out of range (count: {checkboxes.Count}).", this);
                return false;
            }

            if (checkboxes[index] == null)
            {
                Debug.LogWarning($"[UICheckboxGroup] Checkbox at index {index} is null.", this);
                return false;
            }

            return true;
        }

        // =============================================
        // ContextMenu - Debug / Preview
        // =============================================

#if UNITY_EDITOR
        [ContextMenu("Debug / Select First")]
        private void Debug_SelectFirst()
        {
            if (Application.isPlaying) Select(0);
            else                       InitialSync(0);
        }

        [ContextMenu("Debug / Select Last")]
        private void Debug_SelectLast()
        {
            int last = checkboxes.Count - 1;
            if (Application.isPlaying) Select(last);
            else                       InitialSync(last);
        }

        [ContextMenu("Debug / Deselect All")]
        private void Debug_DeselectAll()
        {
            if (Application.isPlaying) DeselectAll();
            else                       InitialSync(-1);
        }

        [ContextMenu("Debug / Print State")]
        private void Debug_PrintState()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"[UICheckboxGroup] '{name}'");
            sb.AppendLine($"  currentIndex:     {_currentIndex}");
            sb.AppendLine($"  requireSelection: {requireSelection}");
            sb.AppendLine($"  singleSelection:  {singleSelection}");
            sb.AppendLine($"  checkboxes ({checkboxes.Count}):");

            for (int i = 0; i < checkboxes.Count; i++)
            {
                string state = checkboxes[i] != null
                    ? (checkboxes[i].IsOn ? "✓ ON" : "  off")
                    : "  NULL";
                string marker = i == _currentIndex ? " ◄" : "";
                sb.AppendLine($"    [{i}] {state}{marker}");
            }

            Debug.Log(sb.ToString(), this);
        }
#endif
    }
}