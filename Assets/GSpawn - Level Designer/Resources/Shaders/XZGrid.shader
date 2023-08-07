Shader "GSpawn/XZGrid"
{
	Properties
	{
		_FillColor		("Fill color", Color)			= (0,0,0,0)
		_WireColor		("Wire color", Color)			= (0,0,0,1)
		_CellSizeX		("Cell size X", float)			= 1.0
		_CellSizeZ		("Cell size Z", float)			= 1.0
		_FarPlaneDist	("Far plane distance", float)	= 1000			
		_CameraPos		("Camera position", Vector)		= (0,0,0,0)
		_Origin			("Origin", Vector)				= (0,0,0,0)		
		_Right			("Right", Vector)				= (1,0,0,0)
		_Look			("Look", Vector)				= (0,0,1,0)
		_ZTest			("ZTest", int)					= 0
	}

	Subshader
	{	
		Pass
		{
			Blend	SrcAlpha OneMinusSrcAlpha
			Cull	Off
			ZWrite	Off
			ZTest	[_ZTest]

			CGPROGRAM
			#include "UnityCG.cginc"
			#include "XZGridInclude.cginc"
			#pragma fragment frag
			#pragma vertex vert
			#pragma target 2.5	

			float4	_FillColor;
			float	_FarPlaneDist;
			float3	_CameraPos;

			struct vInput
			{
				float4 vertexPos	: POSITION;
			};

			struct vOutput
			{
				float3 viewPos		: TEXCOORD1;
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
				float alphaScale = calculateCamAlphaScale(input.viewPos, _FarPlaneDist, _CameraPos);
				return float4(_FillColor.r, _FillColor.g, _FillColor.b, _FillColor.a * alphaScale);
			}
			ENDCG
		}

		Pass
		{
			Blend	SrcAlpha OneMinusSrcAlpha
			Cull	Off
			ZWrite	Off
			ZTest	[_ZTest]

			CGPROGRAM
			#include "UnityCG.cginc"
			#include "XZGridInclude.cginc"
			#pragma fragment frag
			#pragma vertex vert
			#pragma target 2.5	

			float4	_WireColor;
			float	_CellSizeX;
			float	_CellSizeZ;
			float	_FarPlaneDist;
			float3	_CameraPos;
			float3	_Origin;
			float3	_Right;
			float3	_Look;

			struct vInput
			{
				float4 vertexPos	: POSITION;
			};

			struct vOutput
			{
				float3 worldPos		: TEXCOORD0;
				float3 viewPos		: TEXCOORD1;
				float4 clipPos		: SV_POSITION;
			};

			vOutput vert(vInput input)
			{
				vOutput output;

				output.clipPos	= UnityObjectToClipPos(input.vertexPos);
				output.worldPos = mul(unity_ObjectToWorld, input.vertexPos);
				output.viewPos	= UnityObjectToViewPos(input.vertexPos);

				return output;
			}

			float4 frag(vOutput input) : COLOR
			{
				float3 modelPos		= input.worldPos.xyz - _Origin;
				float3 offsets		= modelPos;
				offsets.x			= dot(modelPos, _Right);
				offsets.z			= dot(modelPos, _Look);

				float2 xzCoords		= offsets.xz * float2(1.0f / _CellSizeX, 1.0f / _CellSizeZ);		 
				float2 grid			= abs(frac(xzCoords - 0.5) - 0.5) / fwidth(xzCoords);	
				float a				= min(grid.x, grid.y);

				float4 wireColor	= _WireColor;
				return float4(wireColor.r, wireColor.g, wireColor.b, calculateCamAlphaScale(input.viewPos, _FarPlaneDist, _CameraPos) * wireColor.a * (1.0 - min(a, 1.0)));
			}
			ENDCG
		}
	}
}