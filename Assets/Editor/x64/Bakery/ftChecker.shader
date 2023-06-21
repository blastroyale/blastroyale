Shader "Hidden/ftChecker"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}

    SubShader {

        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard vertex:vert noinstancing

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;
        float bakeryLightmapSize;
        float3 bakeryLightmapID;

        struct Input {
            float2 texcoord1;
            float3 worldPos;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;

        float2 pri( in float2 x )
        {
            // see https://www.shadertoy.com/view/MtffWs
            float2 h = frac(x/2.0)-0.5;
            return x*0.5 + h*(1.0-2.0*abs(h));
        }

        float2 tri( in float2 x )
        {
            float2 h = frac(x/2.0)-0.5;
            return 1.0-2.0*abs(h);
        }

        struct vinput
        {
            float4 vertex : POSITION;
            float2 texcoord1 : TEXCOORD1;
            float2 texcoord2 : TEXCOORD2;
            float3 normal : NORMAL0;
            float2 texcoord : TEXCOORD0;
            float4 tangent : TANGENT;
        };

        void vert (inout vinput v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input,o);
            o.texcoord1 = v.texcoord1 * unity_LightmapST.xy + unity_LightmapST.zw;
        }

        void surf (Input IN, inout SurfaceOutputStandard o) {
            o.Albedo = 0;
            o.Smoothness = 0;

            //float width, height;
            //unity_Lightmap.GetDimensions(width, height);
            //float2 resolution = float2(width, height);

            // Filtered checker from https://www.shadertoy.com/view/llffWs
            float2 uv = IN.texcoord1 * bakeryLightmapSize;// * 0.5f;
            float2 uvDx = ddx(uv);
            float2 uvDy = ddy(uv);

            float2 w = max(abs(uvDx), abs(uvDy)) + 0.01;   // filter kernel
            float2 i = (tri(uv+0.5*w)-tri(uv-0.5*w))/w;    // analytical integral (box filter)
            float checker = 0.5 - 0.5*i.x*i.y;              // xor pattern

            float3 color = DecodeLightmap(UNITY_SAMPLE_TEX2D(unity_Lightmap, IN.texcoord1));
            color = lerp(saturate(color), checker * bakeryLightmapID, 0.5f);

            o.Emission = color;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
