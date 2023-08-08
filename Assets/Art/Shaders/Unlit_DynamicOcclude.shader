// For use with dynamic objects on the map (collectables / pickups / equipment / characters).
Shader "FLG/Unlit/Dynamic Occlusion"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
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
            Name "Occlusion"

            ZWrite Off
            ZTest NotEqual

            Blend SrcAlpha OneMinusSrcAlpha

            Stencil
            {
                Ref 1
                Comp Greater
                Pass Replace
                Fail Keep
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                half4 positionOS : POSITION;
            };

            struct Varyings
            {
                half4 positionHCS : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
            half4 _Color;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            half4 frag() : SV_Target
            {
                return _Color;
            }
            ENDHLSL
        }

    }
}