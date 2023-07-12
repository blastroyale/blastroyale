Shader "Bakery/Simple Vertex Lightmapped"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
                float4 color : COLOR0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				UNITY_FOG_COORDS(1)
                float3 color : TEXCOORD2;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
            float3 _Color;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				UNITY_TRANSFER_FOG(o,o.vertex);

                // Decode baked HDR vertex color (RGBM)
                o.color = v.color.rgb * v.color.a * 8.0;
                o.color *= o.color;

				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				float4 color = tex2D(_MainTex, i.uv);
                color.rgb *= _Color;

                // Apply vertex lightmap
                color.rgb *= i.color;

				UNITY_APPLY_FOG(i.fogCoord, color);
				return color;
			}
			ENDCG
		}

        Pass
        {
            Name "META"
            Tags {"LightMode"="Meta"}
            Cull Off
            CGPROGRAM

            #include"UnityStandardMeta.cginc"

            float4 frag_meta2 (v2f_meta i): SV_Target
            {
                UnityMetaInput o;
                UNITY_INITIALIZE_OUTPUT(UnityMetaInput, o);
                o.Albedo = tex2D(_MainTex, i.uv) * _Color;
                return UnityMetaFragment(o);
            }

            #pragma vertex vert_meta
            #pragma fragment frag_meta2
            ENDCG
        }
	}
}
