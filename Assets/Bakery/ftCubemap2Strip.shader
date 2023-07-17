Shader "Hidden/ftCubemap2Strip"
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
            float gammaMode;

			fixed4 frag (v2f i) : SV_Target
			{
                float3 vec;
                int quad = floor(i.uv.x * 6);

                float2 st = frac(i.uv * float2(6,1)) * 2.0 - 1.0;
                st.x = -st.x;
                //st.y = -st.y;

                if (quad == 0) {
                    vec = float3(1, -st.y, st.x);
                } else if (quad == 1) {
                    vec = float3(-1, -st.y, -st.x);
                } else if (quad == 2) {
                    vec = float3(-st.y, 1, -st.x);
                } else if (quad == 3) {
                    vec = float3(-st.y, -1, st.x);
                } else if (quad == 4) {
                    vec = float3(-st.x, -st.y, 1);
                } else {
                    vec = float3(st.x, -st.y, -1);
                }

                vec = -vec;

				float4 col = texCUBE(_MainTex, vec);

                if (gammaMode > 0.5f) col.rgb = pow(col.rgb, 2.2f);

				return col;
			}
			ENDCG
		}
	}
}
