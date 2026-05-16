Shader "Custom/FlashlightDistort"
{
    Properties
    {
        _BaseMap      ("Texture", 2D)            = "white" {}
        _BaseColor    ("Color", Color)            = (1,1,1,1)
        _DistortAmount("Distort Amount", Range(0,1)) = 0
        _DistortSpeed ("Speed", Float)            = 2.0
        _DistortScale ("Scale", Float)            = 5.0
        _VertexWobble ("Vertex Wobble", Range(0,0.15)) = 0.04
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float  _DistortAmount;
                float  _DistortSpeed;
                float  _DistortScale;
                float  _VertexWobble;
            CBUFFER_END

            struct Attributes { float4 posOS : POSITION; float2 uv : TEXCOORD0; float3 normOS : NORMAL; };
            struct Varyings   { float4 posCS : SV_POSITION; float2 uv : TEXCOORD0; };

            // Простой hash-шум без текстуры
            float2 hash2(float2 p)
            {
                p = float2(dot(p, float2(127.1,311.7)), dot(p, float2(269.5,183.3)));
                return -1.0 + 2.0 * frac(sin(p) * 43758.5453);
            }
            float noise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                float2 u = f*f*(3.0-2.0*f);
                return lerp(lerp(dot(hash2(i),            f),
                                 dot(hash2(i+float2(1,0)),f-float2(1,0)),u.x),
                            lerp(dot(hash2(i+float2(0,1)),f-float2(0,1)),
                                 dot(hash2(i+float2(1,1)),f-float2(1,1)),u.x),u.y);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 pos = IN.posOS.xyz;

                // Вершины "плывут" по нормали
                if (_DistortAmount > 0.001)
                {
                    float t = _Time.y * _DistortSpeed;
                    float n = noise(pos.xz * _DistortScale + t);
                    pos += IN.normOS * n * _VertexWobble * _DistortAmount;
                }

                OUT.posCS = TransformObjectToHClip(pos);
                OUT.uv    = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;
                float  t  = _Time.y * _DistortSpeed;

                // Шумовое смещение UV
                float2 noiseUV = uv * _DistortScale;
                float  nx = noise(noiseUV + float2(t,       0));
                float  ny = noise(noiseUV + float2(0, t*0.7));
                float2 offset = float2(nx, ny) * 0.06 * _DistortAmount;

                // RGB-split прямо на поверхности объекта
                float r = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv + offset    ).r;
                float g = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv             ).g;
                float b = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv - offset    ).b;

                half4 col = half4(r,g,b,1) * _BaseColor;

                // Сдвиг цветовых каналов — "потусторонний" оттенок
                col.rgb = lerp(col.rgb, col.gbr * float3(1.3, 0.7, 1.2), _DistortAmount * 0.4);

                return col;
            }
            ENDHLSL
        }
    }
}