// For use with dynamic objects on the map (collectables / pickups / equipment / characters).
Shader "FLG/Unlit/Shadow"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _ShadowStart ("Shadow Start", Range(0, 1)) = 1
        _ShadowEnd ("Shadow End", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            Name "Albedo"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uvOS : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uvOS : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
            float4 _Color;
            half _ShadowStart;
            half _ShadowEnd;
            CBUFFER_END

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uvOS = v.uvOS;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                return ((1 - distance(i.uvOS, 0.5) * 2) - _ShadowEnd) / (_ShadowStart - _ShadowEnd) * _Color;
            }
            ENDHLSL
        }

    }
}