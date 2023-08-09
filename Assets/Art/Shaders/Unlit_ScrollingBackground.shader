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

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uvOS : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uvOS : TEXCOORD0;
                float2 uvTransformed : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            float4 _ColorTop;
            float4 _ColorBottom;
            float _PatternStrength;
            float _GradientSize;
            float _SpeedX;
            float _SpeedY;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uvTransformed = TRANSFORM_TEX(IN.uvOS, _MainTex);
                OUT.uvOS = IN.uvOS;

                return OUT;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                const float2 uvAnimated = float2(IN.uvTransformed.x + frac(_Time.x * _SpeedX),
                                                 IN.uvTransformed.y + frac(_Time.x * _SpeedY));
                const float tex = saturate(tex2D(_MainTex, uvAnimated).x + 1 - _PatternStrength);

                const float gradient = (IN.uvOS.x + IN.uvOS.y) / 2;
                const float gradient_scaled = saturate(lerp(-_GradientSize, 1 + _GradientSize, gradient));

                return lerp(_ColorBottom, _ColorTop, gradient_scaled) * tex;
            }
            ENDHLSL
        }

    }
}