using UnityEngine;
using UnityEngine.UI;

namespace CorePro.UI
{
    [AddComponentMenu("CorePro/UI/UIStyle Image")]
    [RequireComponent(typeof(Image))]
    public sealed class UIStyleImage : UIStyle
    {
        // Stored as int – no compile-time dependency on the generated ColorSlot enum.
        // Inspector shows a named dropdown via UIStyleImageEditor.
        [SerializeField] internal int colorSlot;

        [Tooltip("Keep the component's current alpha; replace RGB only.")]
        [SerializeField] private bool preserveAlpha;

        [Tooltip("Skip color application and leave the color set manually.")]
        [SerializeField] private bool useCustomColor;

        private Image _image;

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
            if (_image == null) 
                _image = GetComponent<Image>();
        }

        protected override void Apply(UIStyleSheet sheet)
        {
            if (_image == null || useCustomColor)
                return;

            Color src = sheet.GetColor(colorSlot);
            
            if (_image.color != src)
            {
                _image.color = preserveAlpha
                    ? new Color(src.r, src.g, src.b, _image.color.a)
                    : src;
            }
        }
    }
}
