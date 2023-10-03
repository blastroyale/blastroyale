Shader "Hidden/ftBlack"
{
    SubShader
    {
        Pass
        {
            Name "META"
            Tags {"LightMode"="Meta"}
            Cull Off
            CGPROGRAM

            #include "UnityCG.cginc"

            struct VertexInput
            {
                float2 uv1 : TEXCOORD1;
            };

            struct v2f_meta2
            {
                float4 pos      : SV_POSITION;
            };

            v2f_meta2 vert_meta2 (VertexInput v)
            {
                v2f_meta2 o;
                o.pos = float4(((v.uv1.xy * unity_LightmapST.xy + unity_LightmapST.zw)*2-1) * float2(1,-1), 0.5, 1);
                return o;
            }

            float4 frag_meta2 (v2f_meta2 i): SV_Target
            {
                return float4(0,0,0,1);
            }

            #pragma vertex vert_meta2
            #pragma fragment frag_meta2
            ENDCG
        }
    }
}

