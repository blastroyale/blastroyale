// Draws the minimap
Shader "FLG/UI/Minimap"
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
        _EnemiesColor ("Enemies Color", Color) = (1,0,1,1)
        _PlayersOutlineColor ("Players Outline Color", Color) = (1,0,1,1)
        _PingColor ("Ping Color", Color) = (0,0,1,1)
        _PingProgress ("Ping Progress", Float) = 1

        _PlayersSize("Players Size", Float) = 0.01
        _PingSize("Ping Size", Float) = 0.01
        _PingWidth("Ping Width", Float) = 0.1
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
            half4 _ClipRect;
            half _UIMaskSoftnessX;
            half _UIMaskSoftnessY;

            float4 _UvRect;

            half _RingWidth;
            half _OuterRindWidth;

            half _SafeAreaSize;
            half4 _SafeAreaOffset;

            half _DangerAreaSize;
            half4 _DangerAreaOffset;

            fixed _PlayersSize;

            fixed4 _SafeAreaColor;
            fixed4 _DangerAreaColor;
            fixed4 _DangerRingColor;
            fixed4 _OuterRingColor;
            fixed4 _EnemiesColor;
            fixed4 _PlayersOutlineColor;
            fixed4 _PingColor;

            int _EnemiesCount = 0;
            half4 _Enemies[30];
            int _FriendliesCount = 0;
            half4 _Friendlies[30];
            fixed4 _FriendliesColors[30];
            half _EnemiesOpacity;

            half _PingSize;
            half _PingWidth;
            half4 _PingPosition;
            half _PingProgress;

            inline float circle(in float2 st, in float radius)
            {
                return step(distance(st, float2(0.5, 0.5)), radius / 2.0);
            }

            v2f vert(appdata_t v)
            {
                v2f OUT;

                half4 vPosition = UnityObjectToClipPos(v.vertex);
                OUT.worldPosition = v.vertex;
                OUT.vertex = vPosition;

                half2 pixelSize = vPosition.w;
                pixelSize /= half2(1, 1) * abs(mul((half2x2)UNITY_MATRIX_P, _ScreenParams.xy));

                half4 clampedRect = clamp(_ClipRect, -2e10, 2e10);
                OUT.texcoord = v.texcoord.xy;
                OUT.mask = half4(v.vertex.xy * 2 - clampedRect.xy - clampedRect.zw,
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
                const half2 st = IN.texcoord;
                const half2 stMod = half2(_UvRect.x + _UvRect.z * st.x, _UvRect.y + _UvRect.w * st.y);

                // Draw base texture
                fixed4 color = IN.color * (tex2D(_MainTex, stMod) + _TextureSampleAdd);

                // Draw danger area
                const half2 dangerAreaPosition = stMod - _DangerAreaOffset;
                const half dangerAreaCircle = circle(dangerAreaPosition, _DangerAreaSize);
                const half dangerAreaCircleInv = 1 - dangerAreaCircle;
                color = color * dangerAreaCircleInv * _DangerAreaColor + dangerAreaCircle * color;

                // Draw safe ring
                const half2 safeAreaPosition = stMod - _SafeAreaOffset;
                const half safeAreaCircle = circle(safeAreaPosition, _SafeAreaSize) - circle(
                    safeAreaPosition, _SafeAreaSize - _RingWidth);
                color = color * (1 - safeAreaCircle) + _SafeAreaColor * safeAreaCircle;

                // Draw danger ring
                const half dangerCircle = dangerAreaCircle - circle(dangerAreaPosition, _DangerAreaSize - _RingWidth);
                color = color * (1 - dangerCircle) + _DangerRingColor * dangerCircle;

                // Draw player circles
                for (int i = 0; i < _EnemiesCount; i++)
                {
                    const half2 playerPos = stMod - _Enemies[i];
                    const half playerCircle = circle(playerPos, _PlayersSize) * _EnemiesOpacity;
                    const half playerCircleOuter = circle(playerPos, _PlayersSize * 1.15) * _EnemiesOpacity;

                    color = color * (1 - playerCircleOuter) + playerCircleOuter * _PlayersOutlineColor;
                    color = color * (1 - playerCircle) + playerCircle * _EnemiesColor;
                }

                for (int i = 0; i < _FriendliesCount; i++)
                {
                    const half2 playerPos = stMod - _Friendlies[i];
                    const half playerCircle = circle(playerPos, _PlayersSize);
                    const half playerCircleOuter = circle(playerPos, _PlayersSize * 1.15);

                    color = color * (1 - playerCircleOuter) + playerCircleOuter * _PlayersOutlineColor;
                    color = color * (1 - playerCircle) + playerCircle * _FriendliesColors[i];
                }

                // Draw Ping ring
                const half2 pingPos = stMod - _PingPosition;
                const half progressCubic = _PingProgress * _PingProgress;
                // This is the easeOutCubic of _PingProgress, for the size
                const half progressQuint = progressCubic * _PingProgress * _PingProgress *
                    _PingProgress; // This is the easeOutQuint of _PingProgress, for the "alpha"
                const half pingRing = circle(pingPos, _PingSize * progressCubic) - circle(
                    pingPos, _PingSize * progressCubic - _PingWidth);
                color = (color * (1 - pingRing) + _PingColor * pingRing) * (1 - progressQuint) + color * progressQuint;

                // Draw outer ring
                const half outerRing = circle(st, 1) - circle(st, 1.0 - _OuterRindWidth);
                color = color * (1 - outerRing) + outerRing * _OuterRingColor;

                // Fade
                const half fade = 1 - max(0, distance(st, float2(0.5, 0.5)) - 0.405) * 10.0;
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