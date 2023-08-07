Shader "GSpawn/XZGridCoordSystemLine"
{
	Properties
	{
		_Color			("Color", Color)				= (1,1,1,1)
		_FarPlaneDist	("Far plane distance", float)	= 1000
		_CameraPos		("Camera position", Vector)		= (0,0,0,0)
	}

	Subshader
	{	
		Pass
		{
			Blend	SrcAlpha OneMinusSrcAlpha
			Cull	Off
			ZWrite	Off

			CGPROGRAM
			#include "UnityCG.cginc"
			#include "XZGridInclude.cginc"
			#pragma fragment frag
			#pragma vertex vert
			#pragma target 2.5	

			float4	_Color;
			float	_FarPlaneDist;
			float3	_CameraPos;

			struct vInput
			{
				float4 vertexPos	: POSITION;
			};

			struct vOutput
			{
				float3 viewPos		: TEXCOORD0;
				float4 clipPos		: SV_POSITION;
			};

			vOutput vert(vInput input)
			{
				vOutput output;
				output.clipPos = UnityObjectToClipPos(input.vertexPos);
				output.viewPos = UnityObjectToViewPos(input.vertexPos);

				return output;
			}

			float4 frag(vOutput input) : COLOR
			{
				return float4(_Color.r, _Color.g, _Color.b, calculateCamAlphaScale(input.viewPos, _FarPlaneDist, _CameraPos) * _Color.a);
			}
			ENDCG
		}
	}
}