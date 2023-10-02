// Overlaps two projections, prioritizes closer pixels

Shader "Hidden/ftFarSphereProjClip"
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

            sampler2D _CurDepth, _ProjDepth, _CurNormal;
            float4x4 _CurInvViewProj, _ProjViewProj;
            float3 _CurPos, _ProjPos;
            float _InvCubeSize;

            float4 ComputeClipSpacePosition(float2 positionNDC, float deviceDepth)
            {
                float4 positionCS = float4(positionNDC * 2.0 - 1.0, deviceDepth, 1.0);
                positionCS.y = -positionCS.y;
                return positionCS;
            }

            float3 ComputeViewSpacePosition(float2 positionNDC, float deviceDepth, float4x4 invProjMatrix)
            {
                float4 positionCS  = ComputeClipSpacePosition(positionNDC, deviceDepth);
                float4 hpositionVS = mul(invProjMatrix, positionCS);
                return hpositionVS.xyz / hpositionVS.w;
            }

            float4 frag (v2f i) : SV_Target
            {
                float curDepth = tex2D(_CurDepth, i.uv).r; // closer is whiter
                float3 curWPos = ComputeViewSpacePosition(i.uv, curDepth, _CurInvViewProj); // use different matrix to world

                float distToCur = distance(_CurPos, curWPos);
                float distToProj = distance(_ProjPos, curWPos);
                if (distToCur < distToProj) discard; // keep current if it's closer

                float texelOffset = _InvCubeSize;

                float3 curWNormal = tex2D(_CurNormal, i.uv).xyz * 2 - 1;
                float3 dir = normalize(curWPos - _ProjPos);
                if (dot(-dir, curWNormal) <= 0) discard; // keep backfacing

                float4 projPos = mul(_ProjViewProj, float4(curWPos, 1));
                projPos.xyz /= projPos.w;
                if (projPos.x < -1 || projPos.y < -1 || projPos.x > 1 || projPos.y > 1 || projPos.z < 0) discard; // keep current outside of projection
                projPos.y = -projPos.y;

                float2 projUV = projPos.xy * 0.5f + 0.5f;
                float occlusion = 0;
                int holeDilate = 2;
                for(int y=-holeDilate; y<=holeDilate; y++)
                {
                    for(int x=-holeDilate; x<=holeDilate; x++)
                    {
                        float projDepth = tex2D(_ProjDepth, projUV + float2(x,y) * texelOffset).r;
                        projDepth -= 0.0001f;

                        bool occluded = projDepth > projPos.z;
                        if (occluded) occlusion += 1;
                    }
                }
                if (occlusion > 0) discard;

                // clear curDepth
                return 0;
            }
            ENDCG
        }
    }
}
