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

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
            float4 _Color;
            half _ShadowStart;
            half _ShadowEnd;
            CBUFFER_END

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.uv = v.uv;
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                return ((1 - distance(i.uv, 0.5) * 2) - _ShadowEnd) / (_ShadowStart - _ShadowEnd) * _Color;
            }
            ENDHLSL
        }

    }
}