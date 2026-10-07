// Writes depth and no colour: the sprite bake draws the body with it while baking a helm, weapon or off-hand layer,
// so the piece comes out cut where the body hides it (see ARPG.Editor.SpriteBaker). Editor only; nothing in the game
// uses it. _PushBack moves it away from the camera (world units) and _KeepOut skips the parts a helm covers: a helm is cut only where the body is clearly in
// front of it (an arm, a hand), not where her hair or cheek pokes a little through the cloth as her head turns
// (2026-10-07: the hood was cut open on her face and shoulder in the idle facing left).
Shader "ARPG/Depth Only"
{
    Properties
    {
        _PushBack ("Push back", Float) = 0
        _KeepOut ("Skip vertices marked in the colour's alpha", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry-100" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            ColorMask 0
            ZWrite On
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float4 color : COLOR;
            };

            float _PushBack;
            float _KeepOut;

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float cover : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 view = TransformWorldToView(TransformObjectToWorld(input.positionOS));
                view.z -= _PushBack;
                output.positionCS = TransformWViewToHClip(view);
                output.cover = input.color.a;
                return output;
            }

            // With _KeepOut on (a helm baking), the parts the helm covers (her head, hair, neck, upper chest and
            // shoulders, marked by the bake in the vertex colour's alpha) do not cut it, however her head turns.
            half4 Frag(Varyings input) : SV_Target
            {
                if (_KeepOut > 0.5 && input.cover > 0.5)
                    discard;
                return 0;
            }
            ENDHLSL
        }
    }
}
