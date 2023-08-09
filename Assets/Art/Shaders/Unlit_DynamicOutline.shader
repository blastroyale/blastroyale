// For use with the FLG/Unlit/Dynamic Object shader to create an outline around an object.
Shader "FLG/Unlit/Dynamic Outline"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _Width ("Width", float) = 0.01
        [MaterialToggle] _UsePhysicalSize ("Use physical screen size", Float) = 0
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
            Name "Outline"

            ZWrite On // No idea why it doesn't work without this.
            ZTest Always

            Stencil
            {
                Ref 1
                Comp NotEqual
                Pass Keep
                Fail Keep
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                half4 positionOS : POSITION;
                half3 normalOS : NORMAL;
            };

            struct Varyings
            {
                half4 positionHCS : SV_POSITION;
            };

            float4 _PhysicalScreenSize;

            CBUFFER_START(UnityPerMaterial)
            half4 _Color;
            half _Width;
            half _UsePhysicalSize;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                // Transform vertex from object space to clip space.
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);

                // Transform normal vector from object space to clip space.
                float3 normalHCS = mul((float3x3)UNITY_MATRIX_VP, mul((float3x3)UNITY_MATRIX_M, IN.normalOS));

                // Move vertex along normal vector in clip space.
                OUT.positionHCS.xy += normalize(normalHCS.xy) / _ScreenParams.xy * OUT.positionHCS.w * _Width *
                    (_UsePhysicalSize ? _PhysicalScreenSize.x : 1);

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