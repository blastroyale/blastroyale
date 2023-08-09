// For use with dynamic objects on the map (collectables / pickups / equipment / characters).
Shader "FLG/Unlit/Dynamic Object"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
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
            Name "Albedo"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                half4 positionOS : POSITION;
                half2 uvOS : TEXCOORD0;
                half2 normalOS: NORMAL; // Ok so without this (and passing to frag) DynamicOutline shader doesn't work, doesn't receive the correct normals. What the actual fuck.
            };

            struct Varyings
            {
                half4 positionHCS : SV_POSITION;
                half2 uvOS : TEXCOORD0;
                half2 normalOS: TEXCOORD1;
            };

            sampler2D _MainTex;

            CBUFFER_START(UnityPerMaterial)
            half4 _Color;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uvOS = IN.uvOS;
                OUT.normalOS = IN.normalOS;
                return OUT;
            }

            half4 frag(const Varyings IN) : SV_Target
            {
                half4 tex = tex2D(_MainTex, IN.uvOS) * _Color;
                return tex;
            }
            ENDHLSL
        }

    }
}