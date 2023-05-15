Shader "FLG/FastLit"
{
    Properties
    {
        _BaseMap("Base Map", 2D) = "white"
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
                float4 position : POSITION;
                float2 uv : TEXCOORD0;
                half3 normal : NORMAL;
            };

            struct Varyings
            {
                float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
                half3 normal : TEXCOORD1;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
            // The following line declares the _BaseMap_ST variable, so that you
            // can use the _BaseMap variable in the fragment shader. The _ST 
            // suffix is necessary for the tiling and offset function to work.
            float4 _BaseMap_ST;
            CBUFFER_END

            half3 lambert(float3 lightColor, float3 lightDir, float3 normal)
            {
                const float NdotL = saturate(dot(normal, lightDir));
                return lightColor * NdotL * 0.05;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.position = TransformObjectToHClip(IN.position.xyz);
                OUT.normal = TransformObjectToWorldNormal(IN.normal);

                // This can be simplified with OUT.uv = IN.uv if we don't need tiling / offset
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);

                return OUT;
            }

            half4 frag(const Varyings IN) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);

                // Apply Lambert lighting.
                color.rgb += lambert(_MainLightColor * unity_LightData.z, _MainLightPosition.xyz, IN.normal);

                return color;
            }
            ENDHLSL
        }
    }
}