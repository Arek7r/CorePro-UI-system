using UnityEngine;
using UnityEngine.UI;

namespace CorePro.UI
{
    /// <summary>
    /// Adds procedural rounded corners + border to any UI Image without
    /// breaking Canvas batching.
    ///
    /// How it works:
    /// Instead of setting shader parameters on a per-instance material
    /// (which breaks batching), this component implements BaseMeshEffect
    /// and writes all parameters directly into the mesh UV channels:
    ///
    ///   UV0  – original sprite UVs (untouched, required for textured buttons)
    ///   UV1  – xy: rect size px | zw: corner radii TL, TR
    ///   UV2  – xy: corner radii BR, BL | z: border width | w: softness
    ///   UV3  – border color (r, g, b, a) as raw [0..1] floats
    ///   COLOR (vertex) – fill color written directly as Color32 per vertex -
    ///                    avoids touching Image.color during rebuild loop
    ///
    /// All elements can share a single material → 1 draw call instead of N.
    ///
    /// UIStyleSheet integration:
    /// Assign a sheet and pick color slots for fill / border.
    /// Colors update automatically when the active theme changes.
    /// Set <see cref="useCustomFillColor"/> / <see cref="useCustomBorderColor"/>
    /// to override individual elements without disconnecting the sheet.
    /// </summary>
    [AddComponentMenu("CorePro/UI/Rounded UI Element")]
    [RequireComponent(typeof(Image))]
    [DisallowMultipleComponent]
    public sealed class ImageRounded : BaseMeshEffect
    {
        // Shared material cache 
        private static Material _sharedMaterial;

        private static Material SharedMaterial
        {
            get
            {
                if (_sharedMaterial != null) return _sharedMaterial;

                var shader = Shader.Find("CorePro/UI/RoundedUI");
                if (shader == null)
                {
                    Debug.LogError("[ImageRounded] Shader 'CorePro/UI/RoundedUI' not found. " +
                                   "Make sure RoundedUI.shader is in your project.");
                    return null;
                }

                _sharedMaterial = new Material(shader)
                {
                    name      = "RoundedUI_Shared",
                    hideFlags = HideFlags.DontSave
                };
                return _sharedMaterial;
            }
        }

        // Inspector – Corner radii 
        [Header("Corner Radii (px)")]
        [Tooltip("Use one value for all corners.")]
        [SerializeField] private bool uniformRadius = true;

        [SerializeField] [Min(0f)] private float radius = 30f;

        [SerializeField] [Min(0f)] private float radiusTopLeft     = 30f;
        [SerializeField] [Min(0f)] private float radiusTopRight    = 30f;
        [SerializeField] [Min(0f)] private float radiusBottomRight = 30f;
        [SerializeField] [Min(0f)] private float radiusBottomLeft  = 30f;

        // Inspector – Border 
        [Header("Border")]
        [SerializeField] [Min(0f)] private float borderWidth = 0f;

        [Tooltip("Anti-aliasing softness in pixels. 1 is good for most cases.")]
        [SerializeField] [Min(0.5f)] private float softness = 1f;

        // Inspector – Fill color 
        [Header("Fill Color")]
        [Tooltip("When assigned, fill color is driven by this style sheet slot.")]
        [SerializeField] private UIStyleSheet styleSheet;

        [SerializeField] internal int fillColorSlot  = 0;
        [SerializeField] internal int borderColorSlot = 1;

        [Tooltip("Ignore the style sheet and use the color below instead.")]
        [SerializeField] private bool useCustomFillColor   = false;
        [SerializeField] private bool useCustomBorderColor = false;

        [SerializeField] private Color customFillColor   = Color.white;
        [SerializeField] private Color customBorderColor = Color.black;

        // Runtime state 
        private Image          _image;
        private RectTransform  _rect;

        // Unity lifecycle 
        protected override void Awake()
        {
            _image = GetComponent<Image>();
            _rect  = GetComponent<RectTransform>();
            base.Awake();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            ApplySharedMaterial();
            UIStyleSheet.OnThemeChanged += OnThemeChanged;
            SetVerticesDirty();
        }

        protected override void OnDisable()
        {
            UIStyleSheet.OnThemeChanged -= OnThemeChanged;
            RestoreDefaultMaterial();
            base.OnDisable();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            SetVerticesDirty();
        }
#endif

        // IMeshEffect 

        /// <summary>
        /// Called by the Canvas whenever the mesh is rebuilt.
        /// We write our parameters into the UV channels here - zero per-frame
        /// overhead when nothing changes.
        /// </summary>
        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || vh.currentVertCount == 0) return;

            Vector2 rectSize = _rect != null ? _rect.rect.size : Vector2.one * 100f;

            // Resolve corner radii
            float tl, tr, br, bl;
            if (uniformRadius)
            {
                tl = tr = br = bl = radius;
            }
            else
            {
                tl = radiusTopLeft;
                tr = radiusTopRight;
                br = radiusBottomRight;
                bl = radiusBottomLeft;
            }

            // Clamp so radii never exceed half the shorter side
            float maxR = Mathf.Min(rectSize.x, rectSize.y) * 0.5f;
            tl = Mathf.Min(tl, maxR);
            tr = Mathf.Min(tr, maxR);
            br = Mathf.Min(br, maxR);
            bl = Mathf.Min(bl, maxR);

            Color fill   = ResolveFillColor();
            Color border = ResolveBorderColor();

            // Unity UI vertex colors are expected in linear space when the project
            // uses Linear color space (Project Settings → Player → Color Space).
            // Color32 cast alone skips gamma→linear conversion, causing hue/brightness shift.
            // gamma.linear does the correct conversion before we bake into vertex.color.
            var fillLinear = fill.linear;
            var fillColor32 = new Color32(
                (byte)Mathf.RoundToInt(fillLinear.r * 255f),
                (byte)Mathf.RoundToInt(fillLinear.g * 255f),
                (byte)Mathf.RoundToInt(fillLinear.b * 255f),
                (byte)Mathf.RoundToInt(fillLinear.a * 255f)
            );
            var borderLinear = border.linear;
            Vector4 uv1Data   = new Vector4(rectSize.x, rectSize.y, tl, tr);
            Vector4 uv2Data   = new Vector4(br, bl, borderWidth, softness);
            Vector4 uv3Data   = new Vector4(borderLinear.r, borderLinear.g, borderLinear.b, borderLinear.a);

            UIVertex vertex = default;
            for (int i = 0; i < vh.currentVertCount; i++)
            {
                vh.PopulateUIVertex(ref vertex, i);

                vertex.color = fillColor32;  // fill - Color32, 8bit/channel, fine for UI
                vertex.uv1   = uv1Data;
                vertex.uv2   = uv2Data;
                vertex.uv3   = uv3Data;      // border color - raw float4

                vh.SetUIVertex(vertex, i);
            }
        }

        // Color resolution 
        private Color ResolveFillColor()
        {
            if (useCustomFillColor || styleSheet == null)
                return customFillColor;

            return styleSheet.GetColor(fillColorSlot);
        }

        private Color ResolveBorderColor()
        {
            if (useCustomBorderColor || styleSheet == null || borderWidth <= 0f)
                return customBorderColor;

            return styleSheet.GetColor(borderColorSlot);
        }

        // Material management 
        private void ApplySharedMaterial()
        {
            if (_image == null) _image = GetComponent<Image>();
            if (_image == null) return;

            var mat = SharedMaterial;
            if (mat != null) _image.material = mat;
        }

        private void RestoreDefaultMaterial()
        {
            if (_image == null) return;
            _image.material = null; // resets to default UI material
        }

        // Theme change callback 
        private void OnThemeChanged(UIStyleSheet changed)
        {
            if (changed != styleSheet) return;
            SetVerticesDirty();
        }

        // === Public API === 

        /// <summary>Sets all corner radii to the same value and rebuilds.</summary>
        public void SetRadius(float value)
        {
            uniformRadius = true;
            radius        = Mathf.Max(0f, value);
            SetVerticesDirty();
        }

        /// <summary>Sets individual corner radii and rebuilds.</summary>
        public void SetRadii(float topLeft, float topRight, float bottomRight, float bottomLeft)
        {
            uniformRadius       = false;
            radiusTopLeft       = Mathf.Max(0f, topLeft);
            radiusTopRight      = Mathf.Max(0f, topRight);
            radiusBottomRight   = Mathf.Max(0f, bottomRight);
            radiusBottomLeft    = Mathf.Max(0f, bottomLeft);
            SetVerticesDirty();
        }

        /// <summary>Changes the border width and rebuilds.</summary>
        public void SetBorderWidth(float width)
        {
            borderWidth = Mathf.Max(0f, width);
            SetVerticesDirty();
        }

        /// <summary>
        /// Overrides the fill color, disconnecting it from the style sheet.
        /// Pass null styleSheet or set useCustomFillColor to permanently detach.
        /// </summary>
        public void SetFillColor(Color color)
        {
            useCustomFillColor = true;
            customFillColor    = color;
            SetVerticesDirty();
        }

        /// <summary>
        /// Overrides the border color, disconnecting it from the style sheet.
        /// </summary>
        public void SetBorderColor(Color color)
        {
            useCustomBorderColor = true;
            customBorderColor    = color;
            SetVerticesDirty();
        }

        /// <summary>
        /// Re-links this element to the style sheet for the given slots
        /// and clears any custom color overrides.
        /// </summary>
        public void SetStyleSheet(UIStyleSheet sheet, int fillSlot, int borderSlot = -1)
        {
            styleSheet           = sheet;
            fillColorSlot        = fillSlot;
            borderColorSlot      = borderSlot >= 0 ? borderSlot : borderColorSlot;
            useCustomFillColor   = false;
            useCustomBorderColor = borderSlot < 0;
            SetVerticesDirty();
        }

        // Helpers 

        private void SetVerticesDirty()
        {
            if (_image != null) 
                _image.SetVerticesDirty();
        }
    }
}