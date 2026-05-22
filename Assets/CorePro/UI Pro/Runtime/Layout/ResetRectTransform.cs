using InspectorPro;
using UnityEngine;

namespace CorePro.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class ResetRectTransform : MonoBehaviourCorePro
    {
        void Awake()
        {
            ResetRect();
        }
 
        [Button]
        public void ResetRect()
        {
            RectTransform rect = GetComponent<RectTransform>();
 
            rect.anchorMin = Vector2.zero;          // X 0, Y 0
            rect.anchorMax = Vector2.one;           // X 1, Y 1
            rect.pivot = new Vector2(0.5f, 0.5f);   // X 0.5, Y 0.5
 
            rect.offsetMin = Vector2.zero;          // Left 0, Bottom 0
            rect.offsetMax = Vector2.zero;          // Right 0, Top 0
 
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
        }
    }
}