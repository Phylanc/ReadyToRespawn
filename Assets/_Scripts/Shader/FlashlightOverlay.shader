Shader "Custom/FlashlightOverlay"
{
    Properties
    {
        _DistortAmount ("Distort Amount", Range(0,1)) = 0
        _DistortSpeed  ("Speed",  Float) = 2.0
        _DistortScale  ("Scale",  Float) = 5.0
        _VertexWobble  ("Vertex Wobble", Range(0,0.15)) = 0.04
        _OverlayColor  ("Overlay Color", Color) = (0.4, 0.0, 0.8, 1.0)
    }

    SubShader
    {
        Tags
        {
            "RenderType"      = "Transparent"
            "Queue"           = "Transparent+1"
            "RenderPipeline"  = "UniversalPipeline"
        }

        Pass
        {
            Name "FlashlightOverlayPass"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _OverlayColor;
                float  _DistortAmount;
                float  _DistortSpeed;
                float  _DistortScale;
                float  _VertexWobble;
            CBUFFER_END

            struct Attributes
            {
                float4 posOS   : POSITION;
                float2 uv      : TEXCOORD0;
                float3 normOS  : NORMAL;
            };

            struct Varyings
            {
                float4 posCS : SV_POSITION;
                float2 uv    : TEXCOORD0;
            };

            float2 hash2(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)),
                           dot(p, float2(269.5, 183.3)));
                return -1.0 + 2.0 * frac(sin(p) * 43758.5453);
            }

            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(
                    lerp(dot(hash2(i),                f),
                         dot(hash2(i + float2(1,0)),  f - float2(1,0)), u.x),
                    lerp(dot(hash2(i + float2(0,1)),  f - float2(0,1)),
                         dot(hash2(i + float2(1,1)),  f - float2(1,1)), u.x),
                    u.y);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 pos = IN.posOS.xyz;

                if (_DistortAmount > 0.001)
                {
                    float t = _Time.y * _DistortSpeed;
                    float n = noise(pos.xz * _DistortScale + t);
                    pos += IN.normOS * n * _VertexWobble * _DistortAmount;
                }

                OUT.posCS = TransformObjectToHClip(pos);
                OUT.uv    = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float t  = _Time.y * _DistortSpeed;
                float n  = noise(IN.uv * _DistortScale + t);

                // Пульсирующий паттерн поверх объекта
                float pattern = abs(sin(n * 6.28 + t));
                float alpha   = pattern * _DistortAmount * 0.55;

                // Цветовой сдвиг
                float3 col = _OverlayColor.rgb;
                col = lerp(col, col.gbr, n * 0.5 * _DistortAmount);

                return half4(col, alpha);
            }
            ENDHLSL
        }
    }
}