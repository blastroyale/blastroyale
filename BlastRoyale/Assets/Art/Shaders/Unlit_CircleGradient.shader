Shader "FLG/Unlit/CircleGradient"
{
    Properties
    {
        _InnerColor ("Inner Color", Color) = (1,1,1,1)
        _OuterColor ("Outer Color", Color) = (0,0,0,1)
        _Multiplier ("Multiplier", Float) = 1
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
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                half4 positionOS : POSITION;
                half2 uvOS : TEXCOORD0;
            };

            struct Varyings
            {
                half4 positionHCS : SV_POSITION;
                half2 uvOS : TEXCOORD0;
            };

            // CBUFFER_START(UnityPerMaterial)
            half4 _InnerColor;
            half4 _OuterColor;
            half _Multiplier;
            // CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uvOS = IN.uvOS;

                return OUT;
            }

            half4 frag(const Varyings IN) : SV_Target
            {
                return lerp(_InnerColor, _OuterColor, saturate(length((IN.uvOS - half2(0.5, 0.5)) * _Multiplier)));
            }
            ENDHLSL
        }
    }
}