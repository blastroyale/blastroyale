Shader "Hidden/ftUVNormalMap"
{
    Properties
    {
        _BumpMap ("Normal map", 2D) = "bump" { }
        bestFitNormalMap ("Best fit normals texture", 2D) = "white" { }
    }
    SubShader
    {
        Pass
        {
            //Name "META"
            //Tags {"LightMode"="Meta"}
            Cull Off
            CGPROGRAM

            #define _TANGENT_TO_WORLD
            #define UNITY_PASS_META
            #include "UnityStandardMeta.cginc"

            Texture2D bestFitNormalMap;
            //sampler2D _BumpMap;
            float4 _BumpMap_scaleOffset;
            float _IsTerrain, _IsPerPixel;

            struct v2f_meta2
            {
                float4 pos      : SV_POSITION;
                float2 uv       : TEXCOORD0;
                float3 normal   : TEXCOORD1;
                float4 tangent  : TEXCOORD2;
                float3 binormal : TEXCOORD3;
            };

            v2f_meta2 vert_meta2 (VertexInput v)
            {
                v2f_meta2 o;
                o.pos = float4(((v.uv1.xy * unity_LightmapST.xy + unity_LightmapST.zw)*2-1) * float2(1,-1), 0.5, 1);
                //  UnityMetaVertexPosition(v.vertex, v.uv1.xy, v.uv2.xy, unity_LightmapST, unity_DynamicLightmapST);
                o.uv = v.uv0 * _BumpMap_scaleOffset.xy + _BumpMap_scaleOffset.zw;
                o.normal = normalize(mul((float3x3)unity_ObjectToWorld, v.normal).xyz);

                if (_IsTerrain == 0.0f)
                {
                    o.tangent.xyz = normalize(mul((float3x3)unity_ObjectToWorld, v.tangent.xyz).xyz);
                    o.binormal = cross(o.normal, o.tangent.xyz) * v.tangent.w;
                    o.tangent.w = v.tangent.w;
                }
                else
                {
                    o.tangent.xyz = cross(o.normal, float3(0,0,1));
                    o.binormal = cross(o.normal, o.tangent.xyz) * -1;
                    o.tangent.w = -1;
                }
                return o;
            }

            float3 EncodeNormalBestFit(float3 n)
            {
                float3 nU = abs(n);
                float maxNAbs = max(nU.z, max(nU.x, nU.y));
                float2 TC = nU.z<maxNAbs? (nU.y<maxNAbs? nU.yz : nU.xz) : nU.xy;
                //if (TC.x != TC.y)
                //{
                    TC = TC.x<TC.y? TC.yx : TC.xy;
                    TC.y /= TC.x;

                    n /= maxNAbs;
                    float fittingScale = bestFitNormalMap.Load(int3(TC.x*1023, TC.y*1023, 0)).a;
                    n *= fittingScale;
                //}
                return n*0.5+0.5;
            }

            float4 frag_meta2 (v2f_meta2 i): SV_Target
            {
                if (_IsPerPixel > 0.0f) i.binormal = cross(i.normal, i.tangent) * i.tangent.w;

                float3 normalMap = UnpackNormal(tex2D(_BumpMap, i.uv));
                float3 normal = normalize(i.tangent.xyz * normalMap.x + i.binormal * normalMap.y + i.normal * normalMap.z);
                return float4(EncodeNormalBestFit(normal),1);
            }

            #pragma vertex vert_meta2
            #pragma fragment frag_meta2
            ENDCG
        }
    }
}
