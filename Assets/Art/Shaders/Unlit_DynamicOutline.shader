// For use with the FLG/Unlit/Dynamic Object shader to create an outline around an object.
Shader "FLG/Unlit/Dynamic Outline"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _Width ("Width", float) = 0.01
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque" "Queue"="Geometry+2"
        }

        Pass
        {
            Name "Normal"

            Cull Off

            Stencil
            {
                Ref 69
                Comp NotEqual
            }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            float4 _Color;
            float _Width;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex.xyz + v.normal * _Width);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return _Color;
            }
            ENDCG
        }
    }
}