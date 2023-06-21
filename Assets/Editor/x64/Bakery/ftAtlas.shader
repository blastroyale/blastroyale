Shader "Hidden/ftAtlas"
{
	Properties
	{
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
            Blend One One
            ZWrite Off
            ZTest Always
            Cull Off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct v2f
			{
				float4 vertex : SV_POSITION;
			};

			v2f vert (appdata_full v)
			{
				v2f o;
                float2 uv1 = v.texcoord1 * unity_LightmapST.xy + unity_LightmapST.zw;
				o.vertex = float4(uv1.xy*2-1, 0.5, 1);
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
                return 0.1;
			}
			ENDCG
		}
	}
}
