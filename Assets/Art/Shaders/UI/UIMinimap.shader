Shader "Custom/UI/Minimap"
{
	Properties
	{
		_MainTex ("Sprite Texture", 2D) = "white" {}
		_Color ("Tint", Color) = (1,1,1,1)
		_UvRect("UV Rect", Vector) = (0,0,1,1)

		_RingWidth ("Ring Width", Float) = 0.1
		_OuterRindWidth ("Outline Width", Float) = 0.01

		_SafeAreaColor ("Safe Area Color", Color) = (1,1,1,1)
		_DangerAreaColor ("Danger Area Color", Color) = (1,1,1,1)
		_DangerRingColor ("Danger Ring Color", Color) = (1,1,1,1)
		_OuterRingColor ("Outer Ring Color", Color) = (1,1,1,1)
		_PlayersColor ("Players Color", Color) = (1,0,1,1)

		_PlayersSize("Players Size", Float) = 0.01
		_SafeAreaSize ("Safe Area Size", Float) = 1
		_SafeAreaOffset("Safe Area Offset", Vector) = (0,0,0,0)
		_DangerAreaSize ("Danger Area Size", Float) = 1
		_DangerAreaOffset("Danger Area Offset", Vector) = (0,0,0,0)

		[HideInInspector]_StencilComp ("Stencil Comparison", Float) = 8
		[HideInInspector]_Stencil ("Stencil ID", Float) = 0
		[HideInInspector]_StencilOp ("Stencil Operation", Float) = 0
		[HideInInspector]_StencilWriteMask ("Stencil Write Mask", Float) = 255
		[HideInInspector]_StencilReadMask ("Stencil Read Mask", Float) = 255

		[HideInInspector]_ColorMask ("Color Mask", Float) = 15
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
			#include "UIShaderShared.cginc"

			#pragma shader_feature _ MINIMAP_DRAW_PLAYERS

			struct appdata_t
			{
				float4 vertex : POSITION;
				float4 color : COLOR;
				float2 texcoord : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				fixed4 color : COLOR;
				float2 texcoord : TEXCOORD0;
				float4 worldPosition : TEXCOORD1;
				float4 mask : TEXCOORD2;
			};

			sampler2D _MainTex;
			sampler2D _OverlayTex;
			fixed4 _Color;
			fixed4 _TextureSampleAdd;
			float4 _ClipRect;
			float _UIMaskSoftnessX;
			float _UIMaskSoftnessY;

			float4 _UvRect;

			float _RingWidth;
			float _OuterRindWidth;

			float _SafeAreaSize;
			float4 _SafeAreaOffset;

			float _DangerAreaSize;
			float4 _DangerAreaOffset;

			float _PlayersSize;

			fixed4 _SafeAreaColor;
			fixed4 _DangerAreaColor;
			fixed4 _DangerRingColor;
			fixed4 _OuterRingColor;
			fixed4 _PlayersColor;

			#ifdef MINIMAP_DRAW_PLAYERS
			int _PlayersCount = 0;
			float4 _Players[30];
			#endif

			v2f vert(appdata_t v)
			{
				v2f OUT;

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
				// Round up the alpha color coming from the interpolator (to 1.0/256.0 steps)
				// The incoming alpha could have numerical instability, which makes it very sensible to
				// HDR color transparency blend, when it blends with the world's texture.
				const half alphaPrecision = half(0xff);
				const half invAlphaPrecision = half(1.0 / alphaPrecision);
				IN.color.a = round(IN.color.a * alphaPrecision) * invAlphaPrecision;

				// BEGIN CUSTOM DRAW
				const float2 st = IN.texcoord;
				const float2 stMod = float2(_UvRect.x + _UvRect.z * st.x, _UvRect.y + _UvRect.w * st.y);

				// Draw base texture
				half4 color = IN.color * (tex2D(_MainTex, stMod) + _TextureSampleAdd);

				// Draw danger area
				const float2 dangerAreaPosition = stMod - _DangerAreaOffset;
				const float dangerAreaCircle = circle(dangerAreaPosition, _DangerAreaSize);
				const float dangerAreaCircleInv = 1 - dangerAreaCircle;
				color = color * dangerAreaCircleInv * _DangerAreaColor + dangerAreaCircle * color;

				// Draw safe ring
				const float2 safeAreaPosition = stMod - _SafeAreaOffset;
				const float safeAreaCircle = circle(safeAreaPosition, _SafeAreaSize) - circle(
					safeAreaPosition, _SafeAreaSize - _RingWidth);
				color = color * (1 - safeAreaCircle) + _SafeAreaColor * safeAreaCircle;

				// Draw danger ring
				const float dangerCircle = dangerAreaCircle - circle(dangerAreaPosition, _DangerAreaSize - _RingWidth);
				color = color * (1 - dangerCircle) + _DangerRingColor * dangerCircle;

				// Draw player circles
				#ifdef MINIMAP_DRAW_PLAYERS
				for (int i = 0; i < _PlayersCount; i++)
				{
					const float playerCircle = circle(stMod - _Players[i], _PlayersSize);
					color = color * (1 - playerCircle) + playerCircle * _PlayersColor;
				}
				#endif

				// Draw outer ring
				const float outerRing = circle(st, 1) - circle(st, 1.0 - _OuterRindWidth);
				color = color * (1 - outerRing) + outerRing * _OuterRingColor;

				// Fade
				const float fade = 1 - max(0, distance(st, float2(0.5, 0.5)) - 0.405) * 10.0;
				color.a = fade + outerRing * _OuterRingColor.a;

				// END CUSTOM DRAW

				// clip (color.a - 0.001);

				color.rgb *= color.a;

				return color;
			}
			ENDCG
		}
	}
}