Shader "FLG/FastLit"
{
    Properties
    {
        _MainTex("Base Map", 2D) = "white"
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalRenderPipeline"
        }

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
                half3 lambert : TEXCOORD2;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
            half4 _MainTex_ST;
            CBUFFER_END

            half3 lambert(half3 lightColor, half3 lightDir, half3 normal)
            {
                return lightColor * saturate(dot(normal, lightDir));
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.position = TransformObjectToHClip(IN.position.xyz);
                OUT.normal = TransformObjectToWorldNormal(IN.normal);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.lambert = lambert(_MainLightColor * unity_LightData.z, _MainLightPosition.xyz, OUT.normal);
                return OUT;
            }

            half4 frag(const Varyings IN) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                color.rgb *= 0.8;
                color.rgb += IN.lambert; 
                return color;
            }
            ENDHLSL
        }
    }
}