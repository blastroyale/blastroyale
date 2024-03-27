//
// Per vertex 

Shader "FLG/Lit/FastLambertBush"
{
    Properties
    {
        _MainTex("Base Map", 2D) = "white" {}
        _Color("Color", Color) = (1, 1, 1, 1)
        
        _SwayStrength ("Sway Strength", Range(0, 1)) = 0.1
        _SwaySpeed ("Sway Speed", Range(0, 1)) = 1
        _SwayHeight ("Sway Height", Range(0, 1)) = 0.5
        _SwayNoiseTex ("Sway Noise Texture", 2D) = "gray" {}
        _SwayNoiseScale ("Sway Noise Scale", Range(0, 1)) = 1
        _SwayNoiseStrength ("Sway Noise Strength", Range(0, 1)) = 0.1
        _DeflectDistance ("Deflect Distance", float) = 1
        _DeflectStrength ("Deflect Strength", float) = 0.5
        _DeflectHeight ("Deflect Height", float) = 0.5
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
            #include "Includes/BakedBushInput.hlsl"

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
            half4 _Color;
            CBUFFER_END

            static const half3 lightDir = float3(-1, 1, 0);
            static const half3 lightPower = 0.2;

             float3 ApplySway(float3 vertexWorldPos)
            {
                float swayStrength = _SwayStrength; // define the strength of the sway animation
                float swaySpeed = _SwaySpeed; // define the speed of the sway animation
                float swayNoiseStrength = _SwayNoiseStrength; // define the strength of the noise effect
                float swayNoiseScale = _SwayNoiseScale; // define the scale of the noise effect
                float vertexHeight = vertexWorldPos.y;
                float2 playerPos2D = float2(_PlayerPos.x, _PlayerPos.z);
                float2 vertexPos2D = float2(vertexWorldPos.x, vertexWorldPos.z);
                float2 toPlayer = normalize(playerPos2D - vertexPos2D);
                float distToPlayer = length(playerPos2D - vertexPos2D);
                float deflectAmount = 1.0 - saturate(
                    (distToPlayer - _DeflectHeight) / (_DeflectDistance - _DeflectHeight));
                //float deflectAmount = 1.0 - saturate((distToPlayer - _DeflectHeight) / (_DeflectDistance - _DeflectHeight));
                // Calculate the deflection vector
                float deflectWeight = saturate((vertexHeight - _DeflectHeight) / (_DeflectHeight + 0.0001));
                //float deflectWeight = sin(vertexHeight);
                float2 deflectVec = toPlayer * deflectAmount * _DeflectStrength * deflectWeight;
                // Calculate the weight of the sway animation based on vertexHeight
                float swayWeight = saturate((vertexHeight - _SwayHeight) / (_SwayHeight + 0.0001));
                // Calculate the noise offsets based on time and vertex position
                float2 noiseOffset = float2(_Time.y * swaySpeed, 0);
                float2 noiseOffset2 = float2(0, _Time.y * swaySpeed);
                // Sample the noise texture at the vertex position and add it to the sway offset
                float2 noiseUV = float2(vertexWorldPos.x * swayNoiseScale, vertexWorldPos.z * swayNoiseScale) +
                    noiseOffset;
                float noiseOffsetX = (SAMPLE_TEXTURE2D_LOD(_SwayNoiseTex, sampler_SwayNoiseTex, noiseUV, 0).r - 0.5) *
                    swayNoiseStrength;
                float2 noiseUV2 = float2((vertexWorldPos.x + 123.4) * swayNoiseScale,
                                         (vertexWorldPos.z + 567.8) * swayNoiseScale) + noiseOffset2;
                float noiseOffsetZ = (SAMPLE_TEXTURE2D_LOD(_SwayNoiseTex, sampler_SwayNoiseTex, noiseUV2, 0).r - 0.5) *
                    swayNoiseStrength;
                // Apply the sway offset to the vertex position
                float3 swayOffset = float3(noiseOffsetX, 0, noiseOffsetZ) * swayStrength * swayWeight;
                vertexWorldPos += swayOffset - float3(deflectVec.x, 0, deflectVec.y);
                return vertexWorldPos;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.position = TransformObjectToHClip(IN.position.xyz);
                OUT.normal = TransformObjectToWorldNormal(IN.normal);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.lambert = dot(OUT.normal, lightDir) * lightPower;
                return OUT;
            }

            half4 frag(const Varyings IN) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                color.rgb += IN.lambert;
                color.rgb *= _Color.rgb;
                return color;
            }
            ENDHLSL
        }
    }
}