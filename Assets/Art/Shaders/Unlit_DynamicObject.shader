// For use with dynamic objects on the map (collectables / pickups / equipment / characters).
Shader "FLG/Unlit/Dynamic Object"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _OutlineColor ("Outline Color", Color) = (.3,.3,1,1)
        _Width ("Width", float) = 0.01

    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque" "Queue"="Geometry+1"
        }
        LOD 100

        Pass
        {
            Name "Normal"

            Tags
            {
                "LightMode" = "SRPDefaultUnlit"
            }

            // Here for FLG/Unlit/Dynamic Outline
            Stencil
            {
                Ref 69
                Comp Always
                Pass Replace
            }

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
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _Color;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv) * _Color;
                return tex;
            }
            ENDCG
        }

    }
}