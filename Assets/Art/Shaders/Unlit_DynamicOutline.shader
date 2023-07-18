// For use with the FLG/Unlit/Dynamic Object shader to create an outline around an object.
Shader "FLG/Unlit/Dynamic Outline"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _Width ("Width", float) = 0.01
    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "Normal"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"


            struct Attributes
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct Varyings
            {
                float4 vertex : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
            float4 _Color;
            float _Width;
            CBUFFER_END

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.vertex = TransformObjectToHClip(v.vertex.xyz + v.normal * _Width);
                return o;
            }

            half4 frag() : SV_Target
            {
                return _Color;
            }
            ENDHLSL
        }
    }
}