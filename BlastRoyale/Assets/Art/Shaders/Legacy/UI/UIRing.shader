Shader "UI/Ring"
{
	Properties
	{
		_Color ("Tint", Color) = (1,1,1,1)
		_Size ("Size", Float) = 1
		_Width ("Width", Float) = 0.1
		_Offset("Offset", Vector) = (0,0,0,0)
		[Toggle(INVERT)] _Invert ("Invert", Float) = 0

		[HideInInspector]_StencilComp ("Stencil Comparison", Float) = 8
		[HideInInspector]_Stencil ("Stencil ID", Float) = 0
		[HideInInspector]_StencilOp ("Stencil Operation", Float) = 0
		[HideInInspector]_StencilWriteMask ("Stencil Write Mask", Float) = 255
		[HideInInspector]_StencilReadMask ("Stencil Read Mask", Float) = 255

		[HideInInspector]_ColorMask ("Color Mask", Float) = 15

		[Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
	}

	SubShader
	{
		Tags
		{
			"Queue"="Transparent"
			"IgnoreProjector"="True"
			"RenderType"="Transparent"
			"PreviewType"="Plane"
			"CanUseSpriteAtlas"="True"
		}

		Stencil
		{
			Ref [_Stencil]
			Comp [_StencilComp]
			Pass [_StencilOp]
			ReadMask [_StencilReadMask]
			WriteMask [_StencilWriteMask]
		}

		Cull Off
		Lighting Off
		ZWrite Off
		ZTest [unity_GUIZTestMode]
		Blend One OneMinusSrcAlpha
		ColorMask [_ColorMask]

		Pass
		{
			Name "Default"
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0

			#include "UnityCG.cginc"

			#pragma multi_compile_local _ UNITY_UI_CLIP_RECT
			#pragma multi_compile_local _ UNITY_UI_ALPHACLIP

			struct appdata_t
			{
				float4 vertex : POSITION;
				float4 color : COLOR;
				float2 texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				fixed4 color : COLOR;
				float2 texcoord : TEXCOORD0;
				float4 worldPosition : TEXCOORD1;
				float4 mask : TEXCOORD2;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			fixed4 _Color;
			fixed4 _TextureSampleAdd;
			float4 _ClipRect;
			float _UIMaskSoftnessX;
			float _UIMaskSoftnessY;
			float _Size;
			float _Width;
			float _Invert;
			float4 _Offset;

			inline float circle(in float2 st, in float radius)
            {
                return step(distance(st, float2(0.5, 0.5)), radius / 2.0);
            }
			
			v2f vert(appdata_t v)
			{
				v2f OUT;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
				float4 vPosition = UnityObjectToClipPos(v.vertex);
				OUT.worldPosition = v.vertex;
				OUT.vertex = vPosition;

				float2 pixelSize = vPosition.w;
				pixelSize /= float2(1, 1) * abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));

				float4 clampedRect = clamp(_ClipRect, -2e10, 2e10);
				OUT.texcoord = v.texcoord.xy;
				OUT.mask = float4(v.vertex.xy * 2 - clampedRect.xy - clampedRect.zw,
				                  0.25 / (0.25 * half2(_UIMaskSoftnessX, _UIMaskSoftnessY) + abs(pixelSize.xy)));

				OUT.color = v.color * _Color;
				return OUT;
			}

			fixed4 frag(v2f IN) : SV_Target
			{
				//Round up the alpha color coming from the interpolator (to 1.0/256.0 steps)
				//The incoming alpha could have numerical instability, which makes it very sensible to
				//HDR color transparency blend, when it blends with the world's texture.
				const half alphaPrecision = half(0xff);
				const half invAlphaPrecision = half(1.0 / alphaPrecision);
				IN.color.a = round(IN.color.a * alphaPrecision) * invAlphaPrecision;

				// BEGIN CUSTOM DRAW
				float2 st = IN.texcoord;
				st -= _Offset;

				float pct = circle(st, _Size) - circle(st, _Size - _Width);
				pct = pct * (1 - _Invert) + (1 - pct) * _Invert;

				half4 color = IN.color * pct;
				// END CUSTOM DRAW

				#ifdef UNITY_UI_CLIP_RECT
                half2 m = saturate((_ClipRect.zw - _ClipRect.xy - abs(IN.mask.xy)) * IN.mask.zw);
                color.a *= m.x * m.y;
				#endif

				#ifdef UNITY_UI_ALPHACLIP
                clip (color.a - 0.001);
				#endif

				color.rgb *= color.a;

				return color;
			}
			ENDCG
		}
	}
}