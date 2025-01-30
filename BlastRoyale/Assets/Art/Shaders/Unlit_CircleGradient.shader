Shader "FLG/Unlit/CircleGradient"
{
    Properties
    {
        _Color1 ("Center Color", Color) = (1, 0, 0, 1)
        _Color2 ("Midpoint Color 1", Color) = (0, 1, 0, 1)
        _Color3 ("Midpoint Color 2", Color) = (0, 0, 1, 1)
        _Color4 ("Outer Color", Color) = (1, 1, 0, 1)
        _Radius1 ("Radius 1", Range(0, 1)) = 0.25
        _Radius2 ("Radius 2", Range(0, 1)) = 0.5
        _Radius3 ("Radius 3", Range(0, 1)) = 0.75
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

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 pos : SV_POSITION;
            };

            float4 _Color1;
            float4 _Color2;
            float4 _Color3;
            float4 _Color4;
            float _Radius1;
            float _Radius2;
            float _Radius3;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv * 2.0 - 1.0; // Transform UVs to range [-1, 1] for radial calculation
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float distance = length(i.uv); // Distance from the center

                // Determine the gradient step
                float4 color;
                if (distance < _Radius1)
                {
                    color = _Color1;
                }
                else if (distance < _Radius2)
                {
                    float t = (distance - _Radius1) / (_Radius2 - _Radius1);
                    color = lerp(_Color1, _Color2, t);
                }
                else if (distance < _Radius3)
                {
                    float t = (distance - _Radius2) / (_Radius3 - _Radius2);
                    color = lerp(_Color2, _Color3, t);
                }
                else
                {
                    float t = (distance - _Radius3) / (1.0 - _Radius3);
                    color = lerp(_Color3, _Color4, t);
                }

                return color;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}