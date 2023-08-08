// For use in the moving background, it uses a black/white texture to apply a pattern above a gradient
Shader "FLG/Unlit/Scrolling Background"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _ColorTop ("Color Top", Color) = (0,0,1,1)
        _ColorBottom ("Color Bottom", Color) = (1,0,0,1)
        _SpeedX ("Speed X", Float) = 1
        _SpeedY ("Speed Y", Float) = 1
        _PatternStrength ("Pattern Strength", Range(0,1)) = 0.2
        _GradientSize ("Gradient Size", Float) = 0.8
    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "Albedo"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                half2 uv : TEXCOORD0;
                half2 uvTransformed : TEXCOORD1;
                half4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            half4 _ColorTop;
            half4 _ColorBottom;
            half _PatternStrength;
            half _GradientSize;
            half _SpeedX;
            half _SpeedY;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.uvTransformed = TRANSFORM_TEX(v.uv, _MainTex);
                o.uv = v.uv;

                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                half2 uvAnimated = frac(half2(i.uvTransformed.x + (_Time.x * _SpeedX), i.uvTransformed.y + (_Time.x * _SpeedY)));
                half tex = saturate(tex2D(_MainTex, uvAnimated).x + 1 - _PatternStrength);

                half gradient = (i.uv.x + i.uv.y) / 2;
                half gradient_scaled = saturate(lerp(-_GradientSize, 1 + _GradientSize, gradient));
                return lerp(_ColorBottom, _ColorTop, gradient_scaled) * tex;
            }
            ENDHLSL
        }

    }
}