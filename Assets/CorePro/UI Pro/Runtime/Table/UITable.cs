using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace CorePro.UI
{
    /// <summary>
    /// A dynamic UI Table component that manages rows, headers, and titles.
    /// Supports row pooling, zebra-striped backgrounds, and staggered animations.
    /// </summary>
    public class UITable : MonoBehaviour
    {
        [Header("Title Settings")]
        [Tooltip("Reference to the main title text of the table.")]
        [SerializeField] private TextMeshProUGUI titleText;

        [Tooltip("If true, the title will be hidden automatically when there are no rows.")]
        [SerializeField] private bool hideTitleWhenEmpty = true;

        [Header("Column Header Settings")]
        [Tooltip("The parent object containing all header elements.")]
        [SerializeField] private GameObject headersRoot;

        [Tooltip("Array of text components for the individual column headers.")]
        [SerializeField] private TextMeshProUGUI[] headerTexts;

        [Header("Row Management")]
        [Tooltip("The parent transform where new rows will be instantiated.")]
        [SerializeField] private Transform rowsRoot;

        [Tooltip("Prefab used to create new table rows.")]
        [SerializeField] private UITableRow rowPrefab;

        [Tooltip("Internal list of rows used for pooling (active and inactive).")]
        [SerializeField] private List<UITableRow> _rows = new();

        [Header("Animation (Stagger)")]
        [Tooltip("If enabled, rows will appear one by one with an increasing delay.")]
        [SerializeField] private bool enableStaggerAnimation = true;

        [Tooltip("Initial delay before the first row starts its animation.")]
        [SerializeField] private float baseShowDelay = 0f;

        [Tooltip("Time added to the delay for each subsequent row.")]
        [SerializeField] private float staggerStep = 0.03f;

        /// <summary>
        /// Tracks how many rows are currently being displayed.
        /// </summary>
        private int _shownRowCount = 0;

        #region Title Methods

        /// <summary>
        /// Updates the table's title text and optionally sets its visibility.
        /// </summary>
        /// <param name="text">The new title string.</param>
        /// <param name="visible">Should the title be active?</param>
        public void SetTitle(string text, bool visible = true)
        {
            if (titleText == null) return;

            titleText.text = text;
            titleText.gameObject.SetActive(visible);
        }

        /// <summary>
        /// Toggles the visibility of the table title.
        /// </summary>
        public void SetTitleVisible(bool visible)
        {
            if (titleText == null) return;
            titleText.gameObject.SetActive(visible);
        }

        #endregion

        #region Header Methods

        /// <summary>
        /// Sets text for a specific column header by its index.
        /// </summary>
        /// <param name="index">The zero-based index of the column.</param>
        /// <param name="text">The text to display.</param>
        /// <param name="visible">Should this specific header be active?</param>
        public void SetHeader(int index, string text, bool visible = true)
        {
            if (headerTexts == null || index < 0 || index >= headerTexts.Length) return;

            var header = headerTexts[index];
            if (header == null) return;

            header.text = text;
            header.gameObject.SetActive(visible);
        }

        /// <summary>
        /// Toggles visibility for all headers. 
        /// If 'headersRoot' is assigned, it toggles the root. Otherwise, it toggles individual texts.
        /// </summary>
        public void SetHeadersVisible(bool visible)
        {
            if (headersRoot != null)
            {
                headersRoot.SetActive(visible);
                return;
            }

            if (headerTexts == null) return;

            foreach (var header in headerTexts)
            {
                if (header != null)
                    header.gameObject.SetActive(visible);
            }
        }

        #endregion

        #region Row / Pooling Logic

        /// <summary>
        /// Hides all active rows and returns them to the pool.
        /// Also handles title hiding if 'hideTitleWhenEmpty' is enabled.
        /// </summary>
        public void HideAllRows()
        {
            foreach (var row in _rows)
            {
                if (row != null)
                    row.Hide();
            }

            _shownRowCount = 0;

            if (hideTitleWhenEmpty && titleText != null)
                titleText.gameObject.SetActive(false);
        }

        /// <summary>
        /// Clears the table by hiding all rows. Identical to HideAllRows().
        /// </summary>
        public void Clear() => HideAllRows();

        /// <summary>
        /// The primary method to display a row with advanced settings.
        /// Uses pooling to reuse existing row instances.
        /// </summary>
        /// <param name="icon">Optional icon sprite for the row.</param>
        /// <param name="style">The visual style (Normal, Warning, Success).</param>
        /// <param name="tooltip">Optional tooltip text for the row.</param>
        /// <param name="columnValues">The text values for each column.</param>
        /// <returns>The instance of the displayed UITableRow.</returns>
        public UITableRow ShowRow(Sprite icon, UITableRow.RowStyle style, string tooltip, params string[] columnValues)
        {
            var row = GetOrCreateRow();

            // Calculate if this row should have a 'zebra' background (alternating colors)
            bool isZebra = _shownRowCount % 2 != 0;

            // Apply staggered animation delay if the animator component exists
            if (enableStaggerAnimation && row.TryGetComponent<UITableRowAnimator>(out var animator))
            {
                float delay = baseShowDelay + (_shownRowCount * staggerStep);
                animator.SetDelay(delay);
            }

            // Initialize row data
            row.Setup(icon, style, isZebra, tooltip, columnValues);

            _shownRowCount++;

            // Show title if we now have at least one row
            if (titleText != null && hideTitleWhenEmpty)
                titleText.gameObject.SetActive(true);

            return row;
        }

        /// <summary>
        /// Standard helper: Shows a row with simple text columns.
        /// </summary>
        public UITableRow ShowRow(params string[] values)
            => ShowRow(null, UITableRow.RowStyle.Normal, "", values);

        /// <summary>
        /// Warning helper: Shows a row with a warning style (e.g., Red/Orange).
        /// </summary>
        public UITableRow ShowWarningRow(params string[] values)
            => ShowRow(null, UITableRow.RowStyle.Warning, "", values);

        /// <summary>
        /// Success helper: Shows a row with a success style (e.g., Green).
        /// </summary>
        public UITableRow ShowSuccessRow(params string[] values)
            => ShowRow(null, UITableRow.RowStyle.Success, "", values);

        /// <summary>
        /// Internal pooling helper. Finds an inactive row or creates a new one.
        /// </summary>
        private UITableRow GetOrCreateRow()
        {
            // 1. Try to find an inactive row in the existing list
            foreach (var row in _rows)
            {
                if (row != null && !row.gameObject.activeSelf) 
                    return row;
            }

            // 2. If no inactive row found, instantiate a new one
            var parent = rowsRoot != null ? rowsRoot : transform;
            var instance = Instantiate(rowPrefab, parent);
            _rows.Add(instance);
        
            return instance;
        }

        #endregion
    }
}