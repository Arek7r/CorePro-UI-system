Shader "CorePro/UI/RoundedUI"
{
    Properties
    {
        // Standard Unity UI properties – required for Canvas batching
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        // Required by Unity UI stencil system
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"             = "Transparent"
            "IgnoreProjector"   = "True"
            "RenderType"        = "Transparent"
            "PreviewType"       = "Plane"
            "CanUseSpriteAtlas" = "True"
            "RenderPipeline"    = "UniversalPipeline"
        }

        Stencil
        {
            Ref   [_Stencil]
            Comp  [_StencilComp]
            Pass  [_StencilOp]
            ReadMask  [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "RoundedUI"

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

            // Textures 

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            // Cbuffer (global per-material – only the tint and clip rect) 

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float4 _ClipRect;
                float  _UIMaskSoftnessX;
                float  _UIMaskSoftnessY;
            CBUFFER_END

            // Vertex input 
            //
            // UV layout packed by RoundedUIElement.cs:
            //
            // uv0   – original sprite UV (untouched)
            // uv1   – xy: rect size (pixels),   zw: cornerRadius (TL, TR)
            // uv2   – xy: cornerRadius (BR, BL), z: borderWidth,  w: softness
            // uv3   – border color (r, g, b, a) as raw [0..1] floats
            // color – fill color (set via Image.color — full float precision, no packing)
            //
            // This avoids any color packing/unpacking — both channels are native floats.

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color      : COLOR;      // fill color (set by Image.color)
                float2 uv0        : TEXCOORD0;
                float4 uv1        : TEXCOORD1;  // rectSize.xy | cornerRadius TL,TR
                float4 uv2        : TEXCOORD2;  // cornerRadius BR,BL | borderWidth, softness
                float4 uv3        : TEXCOORD3;  // border color rgba [0..1]
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 color       : COLOR;
                float2 uv0         : TEXCOORD0;
                float4 uv1         : TEXCOORD1;
                float4 uv2         : TEXCOORD2;
                float4 uv3         : TEXCOORD3;
                float4 worldPos    : TEXCOORD4;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // Vertex shader 

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.worldPos    = IN.positionOS;
                OUT.color       = IN.color;   // vertex color = fill color, no material tint
                OUT.uv0         = TRANSFORM_TEX(IN.uv0, _MainTex);
                OUT.uv1         = IN.uv1;
                OUT.uv2         = IN.uv2;
                OUT.uv3         = IN.uv3;
                return OUT;
            }

            // SDF rounded-rect helper 
            //
            // p        – fragment position in rect-local pixels (origin = rect centre)
            // halfSize – half-extents of the rect in pixels
            // r        – corner radii (TL, TR, BR, BL)
            //
            // Returns signed distance: negative = inside, positive = outside.

            float RoundedRectSDF(float2 p, float2 halfSize, float4 r)
            {
                // Select radius for the quadrant this fragment is in
                // Quadrant mapping: x>0,y>0 → TR; x<0,y>0 → TL; x<0,y<0 → BL; x>0,y<0 → BR
                float radius = (p.x > 0.0)
                    ? ((p.y > 0.0) ? r.y : r.z)   // TR or BR
                    : ((p.y > 0.0) ? r.x : r.w);   // TL or BL

                // Clamp radius so it never exceeds the smallest half-dimension
                radius = min(radius, min(halfSize.x, halfSize.y));

                float2 q = abs(p) - halfSize + radius;
                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - radius;
            }

            // Fragment shader 

            half4 frag(Varyings IN) : SV_Target
            {
                // Unpack per-element data 

                float2 rectSize    = IN.uv1.xy;
                float2 radiiTLTR   = IN.uv1.zw;
                float2 radiiBRBL   = IN.uv2.xy;
                float  borderWidth = IN.uv2.z;
                float  softness    = max(IN.uv2.w, 0.5);

                float4 radii = float4(radiiTLTR.x, radiiTLTR.y, radiiBRBL.x, radiiBRBL.y);

                // Fill color comes from vertex color (Image.color) — full float precision.
                // Border color comes from uv3 — also raw [0..1] floats, no unpacking needed.
                float4 fillColor   = IN.color;
                float4 borderColor = IN.uv3;

                // Rect-local coordinate (origin at rect centre) 
                // worldPos is in object space; UV0 gives us normalised [0,1] coords.
                // We convert to pixel-space centred at (0,0).
                float2 halfSize = rectSize * 0.5;
                float2 p        = (IN.uv0 - 0.5) * rectSize;   // pixel-space, centred

                // SDF evaluation 

                float dist      = RoundedRectSDF(p, halfSize, radii);
                float innerDist = RoundedRectSDF(p, halfSize - float2(borderWidth, borderWidth), radii - borderWidth);

                // Alpha masks 

                float outerAlpha = 1.0 - smoothstep(-softness, softness, dist);
                float innerAlpha = 1.0 - smoothstep(-softness, softness, innerDist);

                // Texture sample 

                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv0);

                // Composite fill + border 
                //
                // We use premultiplied-alpha blending to avoid colour fringing
                // when the border is transparent (or semi-transparent).
                //
                // Straight-alpha lerp breaks here because:
                //  lerp(borderColor{a=0}, fill, innerAlpha)
                // still carries fill RGB at the outer edge, creating a visible halo.
                //
                // Premultiplied approach:
                //  1. Scale each layer's RGB by its own alpha first.
                //  2. Composite with "over" operator.
                //  3. Apply outer shape alpha last.

                float4 fill = float4(fillColor.rgb * texColor.rgb, fillColor.a * texColor.a);

                // Premultiply both layers
                float4 fillPre   = float4(fill.rgb   * fill.a,        fill.a);
                float4 borderPre = float4(borderColor.rgb * borderColor.a, borderColor.a);

                // "over" composite: border on top of fill, blended by innerAlpha
                // innerAlpha == 1  → fully inside fill area  → pure fill
                // innerAlpha == 0  → fully in border band     → pure border
                float4 composited = lerp(borderPre, fillPre, innerAlpha);

                // Apply outer shape alpha (the rounded clip)
                composited *= outerAlpha;

                // Un-premultiply for the standard SrcAlpha blend mode Unity UI uses.
                // Guard against divide-by-zero on fully transparent pixels.
                if (composited.a > 0.0001)
                    composited.rgb /= composited.a;

                // Unity UI clip rect 

                #ifdef UNITY_UI_CLIP_RECT
                    float2 clipUV  = IN.worldPos.xy;
                    float2 inside  = step(_ClipRect.xy, clipUV) * step(clipUV, _ClipRect.zw);
                    composited.a  *= inside.x * inside.y;
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                    clip(composited.a - 0.001);
                #endif

                return composited;
            }
            ENDHLSL
        }
    }

    FallBack "UI/Default"
    CustomEditor "CorePro.UI.Editor.RoundedUIShaderGUI"
}
