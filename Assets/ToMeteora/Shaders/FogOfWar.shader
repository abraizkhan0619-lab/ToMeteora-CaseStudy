Shader "ToMeteora/FogOfWar"
{
    Properties
    {
        _FogColor ("Fog Colour", Color) = (0.04, 0.06, 0.12, 1)
        [HideInInspector] _RevealCenter ("Reveal Centre", Vector) = (0, 0, 0, 0)
        [HideInInspector] _RevealRadius ("Reveal Radius", Float) = 7
        [HideInInspector] _EdgeSoftness ("Edge Softness", Float) = 1.5
        [HideInInspector] _Darkness ("Darkness", Float) = 0.85
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

            CBUFFER_START(UnityPerMaterial)
                half4 _FogColor;
                float4 _RevealCenter;
                float _RevealRadius;
                float _EdgeSoftness;
                float _Darkness;
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
                if (_FogActive <= 0.0)
                    return half4(0, 0, 0, 0);

                float2 screenUV = GetNormalizedScreenSpaceUV(input.positionCS);
                float depth = SampleSceneDepth(screenUV);
                #if !UNITY_REVERSED_Z
                    depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, depth);
                #endif
                float3 worldPos = ComputeWorldSpacePosition(screenUV, depth, UNITY_MATRIX_I_VP);

                float distanceToCentre = distance(worldPos.xz, _RevealCenter.xz);
                float pixelWidth = min(fwidth(distanceToCentre), 0.5);
                float softness = max(max(_EdgeSoftness, pixelWidth * 1.5), 0.001);
                float clear = 1.0 - smoothstep(_RevealRadius - softness, _RevealRadius, distanceToCentre);

                return half4(_FogColor.rgb, _Darkness * (1.0 - clear) * _FogActive);
            }
            ENDHLSL
        }
    }
}
