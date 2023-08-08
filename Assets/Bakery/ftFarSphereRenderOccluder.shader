// Use to cut holes in captured geometry, fills depth before the color shader

Shader "Hidden/ftFarSphereRenderOccluder"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100
        ColorMask 0
        //ZTest Always

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

			sampler2D _MainTex;
			float4 _MainTex_ST;

            struct MRT
            {
                float4 AlbedoAlpha : SV_Target0;
                float4 Normal : SV_Target1;
            };

			v2f vert (appdata v)
			{
				v2f o;

                o.vertex = mul(unity_ObjectToWorld, v.vertex);
                //o.vertex.xyz += normalize(_WorldSpaceCameraPos - o.vertex.xyz) * 0.01;
                o.vertex = mul(UNITY_MATRIX_VP, float4(o.vertex.xyz,1));
                o.vertex.z += 0.001;

				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}

			MRT frag (v2f i)
			{
                MRT mrt;
                mrt.AlbedoAlpha = 0;
                mrt.Normal = 0;
				return mrt;
			}
			ENDCG
		}
	}
}
