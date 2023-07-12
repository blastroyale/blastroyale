Shader "Bakery/Light"
{
	Properties
	{
        _Color ("Main Color", Color) = (1,1,1,1)
        intensity ("intensity", Float) = 1.0
        _MainTex ("Texture", 2D) = "white" {}
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

			#include "UnityCG.cginc"

			struct v2f
			{
				float4 vertex : SV_POSITION;
                float2 texCoords : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
			};

            float4 _Color;
            float intensity;
            sampler2D _MainTex;

			v2f vert (appdata_full v)
			{
				v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				o.vertex = UnityObjectToClipPos(v.vertex);
                o.texCoords = v.texcoord;
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
                return _Color * intensity * tex2D(_MainTex, i.texCoords);
			}
			ENDCG
		}
	}
}
