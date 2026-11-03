Shader "ToMeteora/FogOfWar"
{
    Properties
    {
        _FogColor ("Fog Colour", Color) = (0.04, 0.06, 0.12, 1)
        [HideInInspector] _FogTex ("Fog (alpha = darkness)", 2D) = "black" {}
        [HideInInspector] _FogArea ("Fog Area (min x, min z, size x, size z)", Vector) = (-20, -20, 40, 40)
        [HideInInspector] _FogActive ("Fog Active", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent+100" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            ZWrite Off
            ZTest Always
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            TEXTURE2D(_FogTex);
            SAMPLER(sampler_FogTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _FogColor;
                float4 _FogArea;
                float _FogActive;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 screenUV = GetNormalizedScreenSpaceUV(input.positionCS);
                float depth = SampleSceneDepth(screenUV);
                #if !UNITY_REVERSED_Z
                    depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, depth);
                #endif
                float3 worldPos = ComputeWorldSpacePosition(screenUV, depth, UNITY_MATRIX_I_VP);

                float2 fogUV = (worldPos.xz - _FogArea.xy) / _FogArea.zw;
                half darkness = SAMPLE_TEXTURE2D(_FogTex, sampler_FogTex, fogUV).a;
                return half4(_FogColor.rgb, darkness * _FogActive);
            }
            ENDHLSL
        }
    }
}
