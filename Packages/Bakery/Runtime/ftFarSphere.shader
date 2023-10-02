// Used on temporary objects captured by BakerySector

Shader "Hidden/ftFarSphere"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Geometry" }

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
                float3 normal : NORMAL0;
            };

            struct v2f
            {
                float3 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float fade : TEXCOORD1;
            };

            sampler2D _MainTex;;

            v2f vert (appdata v, uint vertID : SV_VertexID)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = float3(v.uv, 0);
                float3 wnormal = normalize(mul((float3x3)unity_ObjectToWorld, v.normal));
                float3 wpos = mul(unity_ObjectToWorld, v.vertex);
                float3 viewDir = normalize(_WorldSpaceCameraPos - wpos);
                o.fade = saturate(dot(viewDir, wnormal));
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float4 color = tex2D(_MainTex, i.uv);
                if (color.a < 0.9) discard;
                color.rgb *= i.fade;
                return color;
            }
            ENDCG
        }
    }
}
