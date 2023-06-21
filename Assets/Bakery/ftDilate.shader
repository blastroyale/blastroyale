Shader "Hidden/ftDilate"
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
            #pragma exclude_renderers gles gles3

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
				return o;
			}

			UNITY_DECLARE_TEX2D(_MainTex);

			fixed4 frag (v2f i) : SV_Target
			{
                uint width, height;
                _MainTex.GetDimensions(width, height);
                int3 center = int3(i.vertex.xy, 0);

                float4 c = _MainTex.Load(center);
                if (c.w > 0) return c;

                uint total = 0;
                float4 c2 = _MainTex.Load(center, int2(-1,-1));
                if (c2.w>0) {
                    c += c2;
                    total++;
                }

                c2 = _MainTex.Load(center, int2(0,-1));
                if (c2.w>0) {
                    c += c2;
                    total++;
                }

                c2 = _MainTex.Load(center, int2(1,-1));
                if (c2.w>0) {
                    c += c2;
                    total++;
                }

                c2 = _MainTex.Load(center, int2(-1,0));
                if (c2.w>0) {
                    c += c2;
                    total++;
                }

                c2 = _MainTex.Load(center, int2(1,0));
                if (c2.w>0) {
                    c += c2;
                    total++;
                }

                c2 = _MainTex.Load(center, int2(-1,1));
                if (c2.w>0) {
                    c += c2;
                    total++;
                }

                c2 = _MainTex.Load(center, int2(1,1));
                if (c2.w>0) {
                    c += c2;
                    total++;
                }

                if (total > 0)
                {
                    c /= total;
                    return float4(c.rgb, 1.0f);
                }

                return float4(0,0,0,0);
			}
			ENDCG
		}
	}
}

