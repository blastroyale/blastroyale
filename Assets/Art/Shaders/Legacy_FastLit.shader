Shader "FLG/FastToonShader"
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
            #pragma exclude_renderers gles xbox360 ps3
            #pragma exclude_renderers xbox360 ps3
            #pragma exclude_renderers gles xbox360 ps3
            #pragma exclude_renderers xbox360 ps3
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

            static half3 lightDir = float3(-1, 1, 0);
            static half3 lightColor = float3(0.6, 0.6, 0.6);
            static half3 grayScale = float3(0.299, 0.587, 0.114);

            half3 lambert(half3 lightColor, half3 lightDir, half3 normal)
            {
                return lightColor * dot(normal, lightDir);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.position = TransformObjectToHClip(IN.position.xyz);
                OUT.normal = TransformObjectToWorldNormal(IN.normal);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.lambert = lambert(lightColor, lightDir, OUT.normal);
                return OUT;
            }

            half4 frag(const Varyings IN) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                
                // Fast Toon shade
                color.rgb  
                
                    // Light shining (front part)
                    += lerp(0.1, 0.22, smoothstep(0.48, 0.51, IN.lambert))

                    // Shadow (back part)
                    - lerp(0.2, 0.05, smoothstep(0, 0.03, IN.lambert));

                return color;
            }
            ENDHLSL
        }
    }
}