using UnityEngine;
using TMPro;

namespace CorePro.UI
{
    [AddComponentMenu("CorePro/UI/UIStyle Text")]
    [RequireComponent(typeof(TextMeshProUGUI))]
    public sealed class UIStyleText : UIStyle
    {
        [SerializeField] internal int fontColorSlot;
        [SerializeField] internal int fontSlot;

        [Tooltip("Keep the component's current alpha; replace RGB only.")]
        [SerializeField] private bool preserveAlpha;

        [Tooltip("Skip color application and leave the color set manually.")]
        [SerializeField] private bool useCustomColor;

        [Tooltip("Skip font and size application and leave them set manually.")]
        [SerializeField] private bool useCustomFont;

        [Tooltip("Keep the current fontSize on the TMP component; do not overwrite it from the slot.")]
        [SerializeField] private bool useCustomFontSize;

        [SerializeField] private TextMeshProUGUI _text;

        private new void OnReset()   
        {   
            GetReferences();
            base.OnReset();
        } 
        
        protected override void Awake()
        {
            GetReferences();
            base.Awake();
        }

        protected override void OnEnable()
        {
            GetReferences();
            base.OnEnable();
        }

        private void GetReferences()
        {
            if (_text == null)
                _text = GetComponent<TextMeshProUGUI>();
        }

        protected override void Apply(UIStyleSheet sheet)
        {
            if (_text == null) 
                return;

            // Font & size 
            if (!useCustomFont && sheet.TryGetFont(fontSlot, out TMP_FontAsset font, out float slotSize))
            {
                if (font != null && !ReferenceEquals(_text.font, font))
                    _text.font = font;

                // useCustomFontSize = true means "keep whatever size TMP already has".
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (!useCustomFontSize && _text.fontSize != slotSize)
                    _text.fontSize = slotSize;
            }

            // Color 
            if (useCustomColor) 
                return;

            Color src = sheet.GetFontColor(fontColorSlot);
         
            if (_text.color != src)
            {
                _text.color = preserveAlpha
                    ? new Color(src.r, src.g, src.b, _text.color.a)
                    : src;
            }
        }
    }
}
