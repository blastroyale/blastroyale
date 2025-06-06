Shader "FLG/Unlit/Water"
{
    Properties
    {
        // What color the water will sample when the surface below is shallow.
        _DepthGradientShallow("Depth Gradient Shallow", Color) = (0.325, 0.807, 0.971, 0.725)

        // What color the water will sample when the surface below is at its deepest.
        _DepthGradientDeep("Depth Gradient Deep", Color) = (0.086, 0.407, 1, 0.749)

        // Maximum distance the surface below the water will affect the color gradient.
        _DepthMaxDistance("Depth Maximum Distance", Float) = 1

        // Color to render the foam generated by objects intersecting the surface.
        _FoamColor("Foam Color", Color) = (1,1,1,1)

        // Noise texture used to generate waves.
        _SurfaceNoise("Surface Noise", 2D) = "white" {}

        // Speed, in UVs per second the noise will scroll. Only the xy components are used.
        _SurfaceNoiseScroll("Surface Noise Scroll Amount", Vector) = (0.03, 0.03, 0, 0)

        // Values in the noise texture above this cutoff are rendered on the surface.
        _SurfaceNoiseCutoff("Surface Noise Cutoff", Range(0, 1)) = 0.777

        // Red and green channels of this texture are used to offset the
        // noise texture to create distortion in the waves.
        _SurfaceDistortion("Surface Distortion", 2D) = "white" {}

        // Multiplies the distortion by this value.
        _SurfaceDistortionAmount("Surface Distortion Amount", Range(0, 1)) = 0.27

        // Control the distance that surfaces below the water will contribute
        // to foam being rendered.
        _FoamMaxDistance("Foam Maximum Distance", Float) = 0.4
        _FoamMinDistance("Foam Minimum Distance", Float) = 0.04
    }
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
        }

        Pass
        {
            // Transparent "normal" blending.
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            HLSLPROGRAM
            #define SMOOTHSTEP_AA 0.01

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"


            // Blends two colors using the same algorithm that our shader is using
            // to blend with the screen. This is usually called "normal blending",
            // and is similar to how software like Photoshop blends two layers.
            float4 alphaBlend(float4 top, float4 bottom)
            {
                float3 color = (top.rgb * top.a) + (bottom.rgb * (1 - top.a));
                float alpha = top.a + bottom.a * (1 - top.a);

                return float4(color, alpha);
            }

            struct appdata
            {
                float4 vertex : POSITION;
                float4 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 noiseUV : TEXCOORD0;
                float2 distortUV : TEXCOORD1;
                float4 screenPosition : TEXCOORD2;
                float3 viewNormal : NORMAL;
            };

            sampler2D _SurfaceNoise;
            float4 _SurfaceNoise_ST;

            sampler2D _SurfaceDistortion;
            float4 _SurfaceDistortion_ST;

            v2f vert(appdata v)
            {
                v2f o;

                const VertexPositionInputs inputs = GetVertexPositionInputs(v.vertex.xyz);

                o.vertex = inputs.positionCS;
                o.screenPosition = inputs.positionNDC;
                o.distortUV = TRANSFORM_TEX(v.uv, _SurfaceDistortion);
                o.noiseUV = TRANSFORM_TEX(v.uv, _SurfaceNoise);
                o.viewNormal = TransformWorldToViewNormal(TransformObjectToWorldNormal(v.normal));

                return o;
            }

            float4 _DepthGradientShallow;
            float4 _DepthGradientDeep;
            float4 _FoamColor;

            float _DepthMaxDistance;
            float _FoamMaxDistance;
            float _FoamMinDistance;
            float _SurfaceNoiseCutoff;
            float _SurfaceDistortionAmount;

            float2 _SurfaceNoiseScroll;

            sampler2D _CameraDepthTexture;
            sampler2D _CameraNormalsTexture;

            float4 frag(v2f i) : SV_Target
            {
                // Retrieve the current depth value of the surface behind the
                // pixel we are currently rendering.
                float existingDepth01 = tex2Dproj(_CameraDepthTexture, i.screenPosition).r;
                // Convert the depth from non-linear 0...1 range to linear
                // depth, in Unity units.
                float existingDepthLinear = LinearEyeDepth(existingDepth01, _ZBufferParams);

                // Difference, in Unity units, between the water's surface and the object behind it.
                float depthDifference = existingDepthLinear - i.screenPosition.w;

                // Calculate the color of the water based on the depth using our two gradient colors.
                float waterDepthDifference01 = saturate(depthDifference / _DepthMaxDistance);
                float4 waterColor = lerp(_DepthGradientShallow, _DepthGradientDeep, waterDepthDifference01);

                // Retrieve the view-space normal of the surface behind the
                // pixel we are currently rendering.
                float3 existingNormal = (float3) tex2Dproj(_CameraNormalsTexture, i.screenPosition);

                //return float4(existingNormal, 1);

                // Modulate the amount of foam we display based on the difference
                // between the normals of our water surface and the object behind it.
                // Larger differences allow for extra foam to attempt to keep the overall
                // amount consistent.
                float normalDot = saturate(dot(existingNormal, i.viewNormal));
                //return float4(normalDot, 1);
                float foamDistance = lerp(_FoamMaxDistance, _FoamMinDistance, normalDot);
                float foamDepthDifference01 = saturate(depthDifference / foamDistance);

                float surfaceNoiseCutoff = foamDepthDifference01 * _SurfaceNoiseCutoff;

                float2 distortSample = (tex2D(_SurfaceDistortion, i.distortUV).xy * 2 - 1) * _SurfaceDistortionAmount;

                // Distort the noise UV based off the RG channels (using xy here) of the distortion texture.
                // Also offset it by time, scaled by the scroll speed.
                float2 noiseUV = float2((i.noiseUV.x + _Time.y * _SurfaceNoiseScroll.x) + distortSample.x,
                                        (i.noiseUV.y + _Time.y * _SurfaceNoiseScroll
                                            .y) + distortSample.y);
                float surfaceNoiseSample = tex2D(_SurfaceNoise, noiseUV).r;

                // Use smoothstep to ensure we get some anti-aliasing in the transition from foam to surface.
                // Uncomment the line below to see how it looks without AA.
                // float surfaceNoise = surfaceNoiseSample > surfaceNoiseCutoff ? 1 : 0;
                float surfaceNoise = smoothstep(surfaceNoiseCutoff - SMOOTHSTEP_AA, surfaceNoiseCutoff + SMOOTHSTEP_AA,
                                                                 surfaceNoiseSample);

                float4 surfaceNoiseColor = _FoamColor;
                surfaceNoiseColor.a *= surfaceNoise;

                // Use normal alpha blending to combine the foam with the surface.
                return alphaBlend(surfaceNoiseColor, waterColor);
            }
            ENDHLSL
        }
    }
}