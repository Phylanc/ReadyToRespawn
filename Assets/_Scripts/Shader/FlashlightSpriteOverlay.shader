Shader "Custom/FlashlightSpriteOverlay"
{
    Properties
    {
        _MainTex       ("Sprite Texture", 2D)        = "white" {}
        _DistortAmount ("Distort Amount", Range(0,1)) = 0
        _DistortSpeed  ("Speed",  Float)              = 2.0
        _DistortScale  ("Scale",  Float)              = 5.0
        _OverlayColor  ("Overlay Color", Color)       = (0.4, 0.0, 0.8, 1.0)
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Transparent"
            "Queue"          = "Transparent+1"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector"= "True"
        }

        Pass
        {
            Name "SpriteOverlayPass"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _OverlayColor;
                float  _DistortAmount;
                float  _DistortSpeed;
                float  _DistortScale;
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
                    lerp(dot(hash2(i),               f),
                         dot(hash2(i + float2(1,0)), f - float2(1,0)), u.x),
                    lerp(dot(hash2(i + float2(0,1)), f - float2(0,1)),
                         dot(hash2(i + float2(1,1)), f - float2(1,1)), u.x),
                    u.y);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.posCS = TransformObjectToHClip(IN.posOS.xyz);
                OUT.uv    = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Сэмплируем оригинальный спрайт чтобы взять альфу
                half4 spriteColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);

                // Если пиксель прозрачный в спрайте - не рисуем оверлей
                clip(spriteColor.a - 0.1);

                float t = _Time.y * _DistortSpeed;

                // Шумовой паттерн
                float n1 = noise(IN.uv * _DistortScale + float2(t, 0));
                float n2 = noise(IN.uv * _DistortScale + float2(0, t * 0.7));

                float pattern = abs(sin((n1 + n2) * 6.28 + t));

                // Цветовой сдвиг overlay
                float3 col = _OverlayColor.rgb;
                col = lerp(col, col.gbr, n1 * 0.5 * _DistortAmount);

                // Альфа: паттерн * сила эффекта * альфа спрайта
                float alpha = pattern * _DistortAmount * 0.7 * spriteColor.a;

                return half4(col, alpha);
            }
            ENDHLSL
        }
    }
}