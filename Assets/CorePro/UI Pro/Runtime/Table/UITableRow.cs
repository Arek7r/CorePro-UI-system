using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CorePro.UI
{
    /// <summary>
    /// Represents a single row within a UITable.
    /// Handles dynamic column filling, visual styling (colors/zebra), tooltips, and click events.
    /// </summary>
    public class UITableRow : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public enum RowStyle { Normal, Highlight, Warning, Success, Info, Disabled }

        [Header("UI References")]
        [Tooltip("Array of text components representing columns in this row.")]
        [SerializeField] private TextMeshProUGUI[] columns;

        [Tooltip("Optional image component to display an icon for this row.")]
        [SerializeField] private Image iconImage;

        [Tooltip("Optional background image component.")]
        [SerializeField] private Image backgroundImage;

        [Header("Text Styles (Colors)")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color highlightColor = new Color(1f, 0.95f, 0.6f);
        [SerializeField] private Color warningColor = new Color(1f, 0.5f, 0.5f);
        [SerializeField] private Color successColor = new Color(0.5f, 1f, 0.5f);
        [SerializeField] private Color infoColor = new Color(0.5f, 0.8f, 1f);
        [SerializeField] private Color disabledColor = new Color(0.7f, 0.7f, 0.7f);

        [Header("Background Styles")]
        [SerializeField] private Color normalBackground = new Color(0f, 0f, 0f, 0f);
        [SerializeField] private Color zebraBackground = new Color(1f, 1f, 1f, 0.05f);

        // Events for external systems (e.g., Table Manager or Tooltip UI)
        public Action<UITableRow> OnRowClicked;
        public Action<string> OnShowTooltip;
        public Action OnHideTooltip;

        private string _currentTooltip;
        private UITableRowAnimator _animator;

        private void Awake()
        {
            _animator = GetComponent<UITableRowAnimator>();
        }

        /// <summary>
        /// Configures the row with data and visual style.
        /// Reuses existing text components to match provided values.
        /// </summary>
        /// <param name="icon">Optional icon to display.</param>
        /// <param name="style">Visual style affecting text color.</param>
        /// <param name="isZebra">If true, applies the zebra background color.</param>
        /// <param name="tooltip">Tooltip text displayed on hover.</param>
        /// <param name="values">Text content for each column.</param>
        public void Setup(Sprite icon, RowStyle style, bool isZebra, string tooltip = "", params string[] values)
        {
            _currentTooltip = tooltip;
            gameObject.SetActive(true);

            // Populate column texts based on provided values
            for (int i = 0; i < columns.Length; i++)
            {
                if (columns[i] == null) continue;
                columns[i].text = (i < values.Length) ? values[i] : string.Empty;
            }

            // Setup optional icon
            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.gameObject.SetActive(icon != null);
            }

            ApplyStyle(style, isZebra);

            // Trigger entry animation
            if (_animator != null) _animator.PlayShow();
        }

        /// <summary>
        /// Initiates the hide sequence. If an animator is present, it plays the hide animation
        /// before disabling the object.
        /// </summary>
        public void Hide()
        {
            if (!gameObject.activeSelf) return;

            if (_animator != null)
            {
                _animator.PlayHide(() =>
                {
                    gameObject.SetActive(false);
                    // Reset visual state so it's ready for the next time it's pulled from pool
                    _animator.Reset();
                });
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Updates colors of text columns and background based on selected style and zebra state.
        /// </summary>
        private void ApplyStyle(RowStyle style, bool isZebra)
        {
            Color textColor = style switch
            {
                RowStyle.Highlight => highlightColor,
                RowStyle.Warning => warningColor,
                RowStyle.Success => successColor,
                RowStyle.Info => infoColor,
                RowStyle.Disabled => disabledColor,
                _ => normalColor
            };

            foreach (var col in columns)
            {
                if (col != null) col.color = textColor;
            }

            if (backgroundImage != null)
            {
                backgroundImage.color = isZebra ? zebraBackground : normalBackground;
            }
        }

        #region Pointer Events

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!string.IsNullOrEmpty(_currentTooltip)) 
                OnShowTooltip?.Invoke(_currentTooltip);
        }

        public void OnPointerExit(PointerEventData eventData) => OnHideTooltip?.Invoke();

        public void OnPointerClick(PointerEventData eventData) => OnRowClicked?.Invoke(this);

        #endregion
    }
}