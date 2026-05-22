using UnityEngine;
using UnityEngine.UI;

namespace CorePro.UI
{
    /// <summary>
    /// A professional Vertical Layout Group that supports both Top-to-Bottom and Bottom-to-Up growth.
    /// It automatically manages RectTransform pivots and anchors to ensure the layout expands correctly.
    /// </summary>
    [ExecuteAlways]
    [AddComponentMenu("Layout/Vertical Layout Group PRO")]
    public class VerticalLayoutGroupPro : LayoutGroup
    {
        public enum GrowthDirection
        {
            /// <summary> Pinned to TOP. New elements expand the panel DOWNWARDS. </summary>
            TopDown,

            /// <summary> Pinned to BOTTOM. New elements expand the panel UPWARDS. </summary>
            BottomUp
        }

        [Header("Direction Settings")]
        [Tooltip("TopDown: Starts from top and grows down. BottomUp: Starts from bottom and grows up.")]
        public GrowthDirection direction = GrowthDirection.TopDown;

        [Tooltip("Spacing between elements.")]
        public float spacing = 4f;

        [Header("Sizing Overrides")]
        [Tooltip("If true, all children will be forced to the Global Height value.")]
        public bool overrideAllHeights = false;

        public float globalHeight = 100f;

        [Tooltip("If true, uses the perChildHeights array to define specific heights for specific child indices.")]
        public bool usePerChildOverride = false;

        public float[] perChildHeights = new float[0];

        [Header("Parent Auto Expand")]
        [Tooltip("Should this RectTransform resize itself to fit all children?")]
        public bool autoExpand = true;

        [Tooltip("If true, auto-expand works during Play Mode. If false, only in Editor.")]
        public bool autoExpandRuntime = false;

        [Tooltip("The minimum height the panel will ever have.")]
        public new float minHeight = 0f;

        [Tooltip("The maximum height the panel can reach.")]
        public float maxHeight = 2000f;

        private bool _initialized = false;

        // --- Unity Layout System Overrides ---

        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();
        }

        public override void CalculateLayoutInputVertical()
        {
            // One-time initialization of minHeight if it was left at 0
            if (!_initialized)
            {
                if (minHeight <= 0)
                    minHeight = rectTransform.rect.height;
                _initialized = true;
            }

            // 1. Calculate the total required height
            float totalHeight = padding.top + padding.bottom;
            int count = rectChildren.Count;
            EnsureOverrideArraySize(count);

            for (int i = 0; i < count; i++)
            {
                totalHeight += GetHeight(rectChildren[i], i);
                if (i < count - 1) totalHeight += spacing;
            }

            // 2. Adjust parent height if auto-expand is enabled
            if (autoExpand && (Application.isEditor || autoExpandRuntime))
            {
                UpdateParentHeight(totalHeight);
            }

            // 3. IMPORTANT: Align Pivot and Anchors based on direction
            // This ensures the object grows "away" from its pinned side.
            SyncPivotAndAnchors();
        }

        public override void SetLayoutHorizontal()
        {
            float width = rectTransform.rect.width - padding.left - padding.right;
            for (int i = 0; i < rectChildren.Count; i++)
            {
                // Position children to fill the horizontal width
                SetChildAlongAxis(rectChildren[i], 0, padding.left, width);
            }
        }

        public override void SetLayoutVertical()
        {
            int count = rectChildren.Count;
            if (count == 0) return;

            if (direction == GrowthDirection.TopDown)
            {
                // START FROM TOP
                // currentY moves from 0 (Top Anchor) positive downwards in Unity UI Logic
                float currentY = padding.top;
                for (int i = 0; i < count; i++)
                {
                    float h = GetHeight(rectChildren[i], i);
                    SetChildAlongAxis(rectChildren[i], 1, currentY, h);
                    currentY += h + spacing;
                }
            }
            else
            {
                // START FROM BOTTOM
                // currentY moves from 0 (Bottom Anchor) positive upwards
                float currentY = padding.bottom;
                for (int i = 0; i < count; i++)
                {
                    float h = GetHeight(rectChildren[i], i);
                    SetChildAlongAxis(rectChildren[i], 1, currentY, h);
                    currentY += h + spacing;
                }
            }
        }

        // --- Private Helpers ---

        /// <summary>
        /// Forces the RectTransform Pivot and Anchors to match the selected growth direction.
        /// TOP: Pivot Y = 1, Anchors Y = 1
        /// BOTTOM: Pivot Y = 0, Anchors Y = 0
        /// </summary>
        private void SyncPivotAndAnchors()
        {
            float targetY = (direction == GrowthDirection.TopDown) ? 1f : 0f;

            // Update Pivot
            if (!Mathf.Approximately(rectTransform.pivot.y, targetY))
            {
                Vector2 p = rectTransform.pivot;
                p.y = targetY;
                rectTransform.pivot = p;
            }

            // Update Anchors
            if (!Mathf.Approximately(rectTransform.anchorMin.y, targetY) || !Mathf.Approximately(rectTransform.anchorMax.y, targetY))
            {
                Vector2 min = rectTransform.anchorMin;
                Vector2 max = rectTransform.anchorMax;
                min.y = targetY;
                max.y = targetY;
                rectTransform.anchorMin = min;
                rectTransform.anchorMax = max;
            }
        }

        private void UpdateParentHeight(float preferredHeight)
        {
            if (rectTransform != null)
            {
                float wantedHeight = Mathf.Clamp(preferredHeight, minHeight, maxHeight);
                Vector2 sd = rectTransform.sizeDelta;
                sd.y = wantedHeight;
                rectTransform.sizeDelta = sd;
            }
        }

        private float GetHeight(RectTransform child, int index)
        {
            if (overrideAllHeights) return globalHeight;
            if (usePerChildOverride && index < perChildHeights.Length && perChildHeights[index] > 0)
                return perChildHeights[index];

            // Get the preferred height from child components (e.g. Image, Text, LayoutElement)
            float preferred = LayoutUtility.GetPreferredHeight(child);
            return preferred > 0 ? preferred : globalHeight;
        }

        private void EnsureOverrideArraySize(int count)
        {
            if (perChildHeights.Length != count)
            {
                System.Array.Resize(ref perChildHeights, count);
            }
        }
    }
}