// A sprite's hit flash and death dissolve (see ARPG.SpriteEffects). Unlit: it only replaces a character's lit material
// for the few frames of a flash or a dissolve, and _Shade darkens it toward what the scene's lighting would show.
Shader "ARPG/Sprite Effect"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Flash ("Flash", Range(0, 1)) = 0
        _FlashColor ("Flash Color", Color) = (1, 1, 1, 1)
        _Dissolve ("Dissolve", Range(0, 1)) = 0
        _EdgeColor ("Edge Color", Color) = (1, 0.55, 0.15, 1)
        _Shade ("Shade", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _FlashColor;
                half4 _EdgeColor;
                half _Flash;
                half _Dissolve;
                half _Shade;
            CBUFFER_END

            float Hash(float2 p)
            {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }

            float Noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = Hash(i);
                float b = Hash(i + float2(1, 0));
                float c = Hash(i + float2(0, 1));
                float d = Hash(i + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.color = input.color;
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 texel = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * input.color;
                half3 rgb = lerp(texel.rgb * _Shade, _FlashColor.rgb, _Flash);

                // Burns away in blotches: a pixel goes once the threshold passes its noise, glowing just before.
                float n = Noise(input.uv * 9.0) * 0.7 + Noise(input.uv * 23.0) * 0.3;
                float threshold = _Dissolve * 1.15;
                float edge = _Dissolve > 0 ? 1.0 - smoothstep(threshold, threshold + 0.12, n) : 0.0;
                rgb = lerp(rgb, _EdgeColor.rgb, edge);
                half alpha = texel.a * step(threshold, n);
                return half4(rgb, alpha);
            }
            ENDHLSL
        }
    }
}
