//
// Per vertex 

Shader "FLG/Lit/FastLambert"
{
    Properties
    {
        _MainTex("Base Map", 2D) = "white" {}
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Lighting Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                half4 position : POSITION;
                half2 uv : TEXCOORD0;
                half3 normal : NORMAL;
            };

            struct Varyings
            {
                half4 position : SV_POSITION;
                half2 uv : TEXCOORD0;
                half3 normal : TEXCOORD1;
                half lambert : TEXCOORD2;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
            half4 _MainTex_ST;
            CBUFFER_END

            static const half3 lightDir = float3(-1, 1, 0);
            static const half lightPower = 0.2;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.position = TransformObjectToHClip(IN.position.xyz);
                OUT.normal = TransformObjectToWorldNormal(IN.normal);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.lambert = (half) dot(OUT.normal, lightDir) * lightPower;
                return OUT;
            }

            half4 frag(const Varyings IN) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                color.rgb += IN.lambert;
                return color;
            }
            ENDHLSL
        }
    }
}