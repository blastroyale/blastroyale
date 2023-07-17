Shader "Hidden/ftCubemap2StripExport"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
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

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
                #if UNITY_UV_STARTS_AT_TOP
                o.uv.y = 1-o.uv.y;
                #endif
				return o;
			}

			samplerCUBE _MainTex;
            float4x4 transform;
            float4 isLinear_isDoubleLDR;

			float4 frag (v2f i) : SV_Target
			{
                float face = floor(i.uv.y * 6.0f);
                i.uv.y = frac(i.uv.y * 6.0f);

                float2 st = i.uv * 2.0f - 1.0f;
                float3 vec;
                if (face == 0.0f) {
                    vec = float3(1, -st.y, st.x);
                } else if (face == 1.0f) {
                    vec = float3(-1, -st.y, -st.x);
                } else if (face == 2.0f) {
                    vec = float3(-st.y, 1, -st.x);
                } else if (face == 3.0f) {
                    vec = float3(-st.y, -1, st.x);
                } else if (face == 4.0f) {
                    vec = float3(-st.x, -st.y, 1);
                } else {
                    vec = float3(st.x, -st.y, -1);
                }

                vec = mul((float3x3)transform, normalize(vec));

                float4 color = texCUBE(_MainTex, vec);
                if (isLinear_isDoubleLDR.x < 0.5f) color.rgb = pow(color.rgb, 2.2f);
                if (isLinear_isDoubleLDR.y > 0.5f) color.rgb *= 4.59f;
                return color;
			}
			ENDCG
		}
	}
}
