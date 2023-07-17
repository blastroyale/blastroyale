Shader "Hidden/ftOverlapTest"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always
        Blend One One

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

            float uvSet;

			struct appdata
			{
                float2 uv0 : TEXCOORD0;
				float2 uv1 : TEXCOORD1;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
			};

			v2f vert (appdata v)
			{
                v2f o;
                float2 uv = uvSet > 0.0f ? v.uv1 : v.uv0;
				o.vertex = float4(uv*2-1, 0.5, 1);
                return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
                return float4(1,1,1,1) * (1.0f / 255.0f);
			}
			ENDCG
		}
	}
}
