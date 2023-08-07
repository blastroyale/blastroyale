Shader "GSpawn/SimpleDiffuse" 
{
	Properties
	{
		_Color		("Color", Color)	= (1,1,1,1)
		_ZTest		("ZTest", int)		= 0
		_ZWrite		("ZWrite", int)		= 0
		_CullMode	("Cull mode", int)	= 2
	}

	Subshader
	{
		Pass
		{
			Tags { "RenderType"="Transparent"  "Queue"="Transparent" }

			Blend	SrcAlpha OneMinusSrcAlpha
			ZWrite	[_ZWrite]
			ZTest	[_ZTest]
			Cull	[_CullMode]

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			float4 _Color;

			struct vInput 
			{
				float4 vertexPos	: POSITION;
			};

			struct vOutput
			{
				float4 clipPos		: SV_POSITION;
			};

			vOutput vert(vInput input)
			{
				vOutput o;
				o.clipPos = UnityObjectToClipPos(input.vertexPos);

				return o;
			}

			float4 frag(vOutput input) : COLOR
			{		
				return _Color;
			}
			ENDCG
		}
	}
}