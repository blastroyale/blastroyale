// 1px dilate

Shader "Hidden/ftFarSphereDilate"
{
    Properties
    {
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert (uint id : SV_VertexID)
            {
                v2f o;
                float4 pos[6];
                pos[0] = float4(-1, -1, 0.5, 1);
                pos[1] = float4(-1,  1, 0.5, 1);
                pos[2] = float4(1,   1, 0.5, 1);

                pos[3] = float4(1,   1, 0.5, 1);
                pos[4] = float4(1,  -1, 0.5, 1);
                pos[5] = float4(-1,  -1, 0.5, 1);

                o.vertex = pos[id];
                o.uv = o.vertex.xy * 0.5f + 0.5f;
                o.uv.y = 1.0f - o.uv.y;
                return o;
            }

            sampler2D _MainTex;
            float _InvCubeSize;

            float4 frag (v2f i) : SV_Target
            {
                float4 c = tex2Dlod(_MainTex, float4(i.uv,0,0));
                if (c.a > 0) return c;

                int dilate = 1;
                for(int y=-dilate; y<=dilate; y++)
                {
                    for(int x=-dilate; x<=dilate; x++)
                    {
                        float4 c2 = tex2Dlod(_MainTex, float4(i.uv + float2(x,y) * _InvCubeSize,0,0));
                        if (c2.a > 0) c = c2;
                    }
                }

                return c;
            }
            ENDCG
        }
    }
}
