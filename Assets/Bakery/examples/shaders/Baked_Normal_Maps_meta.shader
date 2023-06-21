Shader "Bakery/Example shader with Normal Meta Pass"
{
    Properties
    {
        _MainTex ("Albedo", 2D) = "white" { }
        _BumpMap ("Normal map", 2D) = "bump" { }
    }
    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vs
            #pragma fragment ps
            #include "UnityCG.cginc"

            sampler2D _MainTex, _BumpMap;

            struct pi
            {
                float4 Position : SV_POSITION;
                float2 TexCoords : TEXCOORD0;
                float2 TexCoords2 : TEXCOORD1;
            };

            void vs(in appdata_full IN, out pi OUT)
            {
                OUT.Position = UnityObjectToClipPos(IN.vertex);
                OUT.TexCoords = IN.texcoord.xy;
                OUT.TexCoords2 = IN.texcoord1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
            }

            float4 ps( in pi IN ) : COLOR
            {
                float4 tex = tex2D(_MainTex, IN.TexCoords);
                float3 lm = DecodeLightmap(UNITY_SAMPLE_TEX2D(unity_Lightmap, IN.TexCoords2));
                tex.rgb *= lm;
                return tex;
            }
            ENDCG
        }

        Pass
        {
            // Normal-map enabled Bakery-specific meta pass
            Name "META_BAKERY"
            Tags {"LightMode"="Meta"}
            Cull Off
            CGPROGRAM

            #include"UnityStandardMeta.cginc"

            // Include Bakery meta pass
            #include"Bakery/BakeryMetaPass.cginc"

            float4 frag_customMeta (v2f_bakeryMeta i): SV_Target
            {
                UnityMetaInput o;
                UNITY_INITIALIZE_OUTPUT(UnityMetaInput, o);

                // Output custom normal to use with Bakery's "Baked Normal Map" mode
                if (unity_MetaFragmentControl.z)
                {
                    // Calculate custom normal
                    float3 customNormalMap = UnpackNormal(tex2D(_BumpMap, pow(abs(i.uv), 1.5))); // example: UVs are procedurally distorted
                    float3 customWorldNormal = TransformNormalMapToWorld(i, customNormalMap);

                    // Output
                    return float4(BakeryEncodeNormal(customWorldNormal),1);
                }

                // Regular Unity meta pass
                o.Albedo = tex2D(_MainTex, i.uv);
                return UnityMetaFragment(o);
            }

            // Must use vert_bakerymt vertex shader
            #pragma vertex vert_bakerymt
            #pragma fragment frag_customMeta
            ENDCG
        }
    }
}
