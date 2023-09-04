Shader "GSpawn/SimpleDiffuseTex" 
{
	Properties 
	{
		_Color		("Color", Color)	= (1,1,1,1)
		_MainTex	("MainTexture", 2D) = "white" {}
		_ZTest		("ZTest", int)		= 0
		_ZWrite		("ZWrite", int)		= 1
	}
	SubShader 
	{
		Pass
		{
			Tags { "RenderType"="Transparent"  "Queue"="Transparent" }

			Blend	SrcAlpha OneMinusSrcAlpha
			Cull	Off
			ZWrite	[_ZWrite]
			ZTest	[_ZTest]

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			sampler2D _MainTex;
			float4 _Color;

			struct vInput 
			{
				float4 vertexPos	: POSITION;
				float2 vertexUV		: TEXCOORD0;
			};

			struct vOutput
			{
				float4 clipPos		: SV_POSITION;
				float2 uv			: TEXCOORD0;
			};

			vOutput vert(vInput input)
			{
				vOutput o;
				o.clipPos	= UnityObjectToClipPos(input.vertexPos);
				o.uv		= input.vertexUV;

				return o;
			}

			float4 frag(vOutput input) : COLOR
			{		
				return tex2D(_MainTex, input.uv) * _Color;
			}
			ENDCG
		}
	}
}
