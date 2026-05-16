Shader "Custom/Dissolve_Sprite"
{
    Properties
    {
        [PerRendererData] _MainTex      ("Sprite Texture", 2D) = "white" {}
        _BaseColor      ("Base Color",  Color)         = (1, 0.2, 0.2, 1)
        [HDR] _EdgeColor("Edge Color",  Color)         = (1, 0, 0, 1)
        _NoiseScale     ("Noise Scale", Float)         = 50.0
        _NoiseStrength  ("Noise Strength", Range(0,1)) = 1.0
        _EdgeWidth      ("Edge Width",  Range(0, 0.2)) = 0.05
        _CutoffHeight   ("Cutoff Height", Range(-0.1, 1.1)) = 0.0
        [Toggle] _VoronoiNoise("Voronoi Noise", Float) = 1.0
        _VoronoiDensity ("Voronoi Cell Density", Float) = 100.0
    }

    SubShader
    {
        Tags
        {
            "RenderType"      = "Transparent"
            "Queue"           = "Transparent"
            "RenderPipeline"  = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "PreviewType"     = "Plane"
        }

        Pass
        {
            Name "SpriteDissolve"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma shader_feature _ _VORONOINOISE_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _BaseColor;
                float4 _EdgeColor;
                float  _NoiseScale;
                float  _NoiseStrength;
                float  _EdgeWidth;
                float  _CutoffHeight;
                float  _VoronoiNoise;
                float  _VoronoiDensity;
            CBUFFER_END

            struct Attributes
            {
                float4 posOS : POSITION;
                float2 uv    : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 posCS : SV_POSITION;
                float2 uv    : TEXCOORD0;
                float4 color : COLOR;
            };

            // ── Simple Noise (Value Noise) ──────────────────────────
            float2 _hash2(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)),
                           dot(p, float2(269.5, 183.3)));
                return frac(sin(p) * 43758.5453);
            }

            float SimpleNoise(float2 uv, float scale)
            {
                float2 p = uv * scale;
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f); // smoothstep

                float a = _hash2(i).x;
                float b = _hash2(i + float2(1,0)).x;
                float c = _hash2(i + float2(0,1)).x;
                float d = _hash2(i + float2(1,1)).x;

                return lerp(lerp(a, b, u.x),
                            lerp(c, d, u.x), u.y);
            }

            // ── Voronoi Noise ───────────────────────────────────────
            float VoronoiNoise(float2 uv, float density, float angleOffset)
            {
                float2 p     = uv * density;
                float2 i     = floor(p);
                float2 f     = frac(p);
                float  minDist = 8.0;

                for (int y = -1; y <= 1; y++)
                for (int x = -1; x <= 1; x++)
                {
                    float2 cell   = float2(x, y);
                    float2 h      = _hash2(i + cell);
                    // вращаем точку внутри ячейки (как в Shader Graph)
                    float  angle  = h.x * 6.2831853 + angleOffset;
                    float2 offset = 0.5 * float2(cos(angle), sin(angle)) + 0.5;
                    float2 r      = cell + offset - f;
                    float  d      = dot(r, r);
                    if (d < minDist) minDist = d;
                }
                return sqrt(minDist);
            }

            // ── Vertex ──────────────────────────────────────────────
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.posCS = TransformObjectToHClip(IN.posOS.xyz);
                OUT.uv    = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color = IN.color; // цвет вертекса SpriteRenderer
                return OUT;
            }

            // ── Fragment ────────────────────────────────────────────
            half4 frag(Varyings IN) : SV_Target
            {
                // Оригинальный спрайт
                half4 sprite = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                sprite      *= IN.color * float4(_BaseColor.rgb, 1.0);

                // Прозрачные пиксели спрайта — сразу отбрасываем
                clip(sprite.a - 0.01);

                // Шум: Voronoi или Simple
                float noise;
                if (_VoronoiNoise > 0.5)
                    noise = VoronoiNoise(IN.uv, _VoronoiDensity, 2.0);
                else
                    noise = SimpleNoise(IN.uv, _NoiseScale);

                // Нормализуем в 0..1 и применяем силу
                noise = saturate(noise * _NoiseStrength);

                // Dissolve: пиксели ниже порога — вырезаем
                float cutoff = _CutoffHeight;
                clip(noise - cutoff);

                // Edge: пиксели чуть выше порога светятся EdgeColor
                float edgeMask = 1.0 - saturate((noise - cutoff) / max(_EdgeWidth, 0.0001));
                float3 finalRGB = lerp(sprite.rgb, _EdgeColor.rgb, edgeMask);

                // Альфа = альфа спрайта (край не делаем прозрачнее)
                return half4(finalRGB, sprite.a);
            }
            ENDHLSL
        }
    }
    CustomEditor "UnityEditor.ShaderGUI"
}