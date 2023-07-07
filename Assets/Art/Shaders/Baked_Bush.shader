// For use with bushed (includes vertex animation and displacement via _PlayerPos).
Shader "FLG/Baked/Bush"
{
    Properties
    {
        [MainTexture] _BaseMap("Texture", 2D) = "white" {}
        [MainColor] _BaseColor("Color", Color) = (1, 1, 1, 1)
        _SwayStrength ("Sway Strength", Range(0, 1)) = 0.1
        _SwaySpeed ("Sway Speed", Range(0, 1)) = 1
        _SwayHeight ("Sway Height", Range(0, 1)) = 0.5
        _SwayNoiseTex ("Sway Noise Texture", 2D) = "gray" {}
        _SwayNoiseScale ("Sway Noise Scale", Range(0, 1)) = 1
        _SwayNoiseStrength ("Sway Noise Strength", Range(0, 1)) = 0.1
        _DeflectDistance ("Deflect Distance", float) = 1
        _DeflectStrength ("Deflect Strength", float) = 0.5
        _DeflectHeight ("Deflect Height", float) = 0.5
        [HideInInspector][NoScaleOffset]unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset]unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset]unity_ShadowMasks("unity_ShadowMasks", 2DArray) = "" {}
    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "IgnoreProjector" = "True"
            "RenderPipeline" = "UniversalPipeline"
        }
        LOD 100
        Blend One Zero
        ZWrite On
        Cull Back
        Pass
        {
            Name "FastBaked"
            Tags
            {
                "LightMode" = "UniversalForwardOnly"
            }
            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DEBUG_DISPLAY
            #include "Packages/com.unity.render-pipelines.universal/Shaders/BakedLitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            float _SwayStrength;
            float _SwaySpeed;
            float _SwayHeight;
            float _SwayNoiseStrength;
            float _SwayNoiseScale;
            float4 _PlayerPos;
            float _DeflectDistance;
            float _DeflectStrength;
            float _DeflectHeight;
            TEXTURE2D(_SwayNoiseTex);
            // This macro declares the sampler for the _BaseMap texture.
            SAMPLER(sampler_SwayNoiseTex);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float2 staticLightmapUV : TEXCOORD1;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 uv0AndFogCoord : TEXCOORD0; // xy: uv0, z: fogCoord
                DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 1);
                half3 normalWS : TEXCOORD2;
                #if defined(DEBUG_DISPLAY)
                float3 positionWS : TEXCOORD4;
                float3 viewDirWS : TEXCOORD5;
                #endif
            };

            void InitializeSurfaceData(half3 color, half alpha, half3 normalTS, out SurfaceData surfaceData)
            {
                surfaceData = (SurfaceData)0;
                surfaceData.albedo = color;
                surfaceData.alpha = alpha;
                surfaceData.emission = half3(0, 0, 0);
                surfaceData.metallic = 0;
                surfaceData.occlusion = 1;
                surfaceData.smoothness = 1;
                surfaceData.specular = half3(0, 0, 0);
                surfaceData.clearCoatMask = 0;
                surfaceData.clearCoatSmoothness = 1;
                surfaceData.normalTS = normalTS;
            }

            void InitializeInputData(Varyings input, half3 normalTS, out InputData inputData)
            {
                inputData = (InputData)0;
                #if defined(DEBUG_DISPLAY)
                inputData.positionWS = input.positionWS;
                inputData.viewDirectionWS = input.viewDirWS;
                #else
                inputData.positionWS = float3(0, 0, 0);
                inputData.viewDirectionWS = half3(0, 0, 1);
                #endif
                #if defined(_NORMALMAP)
                float sgn = input.tangentWS.w;      // should be either +1 or -1
                float3 bitangent = sgn * cross(input.normalWS.xyz, input.tangentWS.xyz);
                inputData.tangentToWorld = half3x3(input.tangentWS.xyz, bitangent.xyz, input.normalWS.xyz);
                inputData.normalWS = TransformTangentToWorld(normalTS, inputData.tangentToWorld);
                #else
                inputData.normalWS = input.normalWS;
                #endif
                inputData.shadowCoord = float4(0, 0, 0, 0);
                inputData.fogCoord = input.uv0AndFogCoord.z;
                inputData.vertexLighting = half3(0, 0, 0);
                inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.vertexSH, inputData.normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.shadowMask = half4(1, 1, 1, 1);
                #if defined(DEBUG_DISPLAY)
                #if defined(LIGHTMAP_ON)
                inputData.staticLightmapUV = input.staticLightmapUV;
                #else
                inputData.vertexSH = input.vertexSH;
                #endif
                #endif
            }

            float3 ApplySway(float3 vertexWorldPos)
            {
                float swayStrength = _SwayStrength; // define the strength of the sway animation
                float swaySpeed = _SwaySpeed; // define the speed of the sway animation
                float swayNoiseStrength = _SwayNoiseStrength; // define the strength of the noise effect
                float swayNoiseScale = _SwayNoiseScale; // define the scale of the noise effect
                float vertexHeight = vertexWorldPos.y;
                float2 playerPos2D = float2(_PlayerPos.x, _PlayerPos.z);
                float2 vertexPos2D = float2(vertexWorldPos.x, vertexWorldPos.z);
                float2 toPlayer = normalize(playerPos2D - vertexPos2D);
                float distToPlayer = length(playerPos2D - vertexPos2D);
                float deflectAmount = 1.0 - saturate(
                    (distToPlayer - _DeflectHeight) / (_DeflectDistance - _DeflectHeight));
                //float deflectAmount = 1.0 - saturate((distToPlayer - _DeflectHeight) / (_DeflectDistance - _DeflectHeight));
                // Calculate the deflection vector
                float deflectWeight = saturate((vertexHeight - _DeflectHeight) / (_DeflectHeight + 0.0001));
                //float deflectWeight = sin(vertexHeight);
                float2 deflectVec = toPlayer * deflectAmount * _DeflectStrength * deflectWeight;
                // Calculate the weight of the sway animation based on vertexHeight
                float swayWeight = saturate((vertexHeight - _SwayHeight) / (_SwayHeight + 0.0001));
                // Calculate the noise offsets based on time and vertex position
                float2 noiseOffset = float2(_Time.y * swaySpeed, 0);
                float2 noiseOffset2 = float2(0, _Time.y * swaySpeed);
                // Sample the noise texture at the vertex position and add it to the sway offset
                float2 noiseUV = float2(vertexWorldPos.x * swayNoiseScale, vertexWorldPos.z * swayNoiseScale) +
                    noiseOffset;
                float noiseOffsetX = (SAMPLE_TEXTURE2D_LOD(_SwayNoiseTex, sampler_SwayNoiseTex, noiseUV, 0).r - 0.5) *
                    swayNoiseStrength;
                float2 noiseUV2 = float2((vertexWorldPos.x + 123.4) * swayNoiseScale,
                                         (vertexWorldPos.z + 567.8) * swayNoiseScale) + noiseOffset2;
                float noiseOffsetZ = (SAMPLE_TEXTURE2D_LOD(_SwayNoiseTex, sampler_SwayNoiseTex, noiseUV2, 0).r - 0.5) *
                    swayNoiseStrength;
                // Apply the sway offset to the vertex position
                float3 swayOffset = float3(noiseOffsetX, 0, noiseOffsetZ) * swayStrength * swayWeight;
                vertexWorldPos += swayOffset - float3(deflectVec.x, 0, deflectVec.y);
                return vertexWorldPos;
            }

            VertexPositionInputs GetVertexPositionInputs2(float3 positionOS)
            {
                VertexPositionInputs input;
                input.positionWS = TransformObjectToWorld(positionOS);
                input.positionVS = TransformWorldToView(input.positionWS);
                input.positionCS = TransformWorldToHClip(input.positionWS);
                float4 ndc = input.positionCS * 0.5f;
                input.positionNDC.xy = float2(ndc.x, ndc.y * _ProjectionParams.x) + ndc.w;
                input.positionNDC.zw = input.positionCS.zw;
                return input;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                // Modified from GetVertexPositionInputs
                float3 positionOS = input.positionOS.xyz;
                VertexPositionInputs vertexInput;
                vertexInput.positionWS = ApplySway(TransformObjectToWorld(positionOS));
                vertexInput.positionVS = TransformWorldToView(vertexInput.positionWS);
                vertexInput.positionCS = TransformWorldToHClip(vertexInput.positionWS);
                float4 ndc = vertexInput.positionCS * 0.5f;
                vertexInput.positionNDC.xy = float2(ndc.x, ndc.y * _ProjectionParams.x) + ndc.w;
                vertexInput.positionNDC.zw = vertexInput.positionCS.zw;

                output.positionCS = vertexInput.positionCS;
                output.uv0AndFogCoord.xy = TRANSFORM_TEX(input.uv, _BaseMap);
                #if defined(_FOG_FRAGMENT)
                output.uv0AndFogCoord.z = vertexInput.positionVS.z;
                #else
                output.uv0AndFogCoord.z = ComputeFogFactor(vertexInput.positionCS.z);
                #endif
                // normalWS and tangentWS already normalize.
                // this is required to avoid skewing the direction during interpolation
                // also required for per-vertex SH evaluation
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                output.normalWS = normalInput.normalWS;
                #if defined(_NORMALMAP)
                real sign = input.tangentOS.w * GetOddNegativeScale();
                output.tangentWS = half4(normalInput.tangentWS.xyz, sign);
                #endif
                OUTPUT_LIGHTMAP_UV(input.staticLightmapUV, unity_LightmapST, output.staticLightmapUV);
                OUTPUT_SH(output.normalWS, output.vertexSH);
                #if defined(DEBUG_DISPLAY)
                output.positionWS = vertexInput.positionWS;
                output.viewDirWS = GetWorldSpaceViewDir(vertexInput.positionWS);
                #endif
                return output;
            }

            inline float circle(in float2 st, in float radius)
            {
                return step(distance(st, float2(0.5, 0.5)), radius / 2.0);
            }

            half4 frag(Varyings input) : SV_Target0
            {
                half2 uv = input.uv0AndFogCoord.xy;
                InputData inputData;
                InitializeInputData(input, half3(0, 0, 1), inputData);
                SETUP_DEBUG_TEXTURE_DATA(inputData, input.uv0AndFogCoord.xy, _BaseMap);
                half4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv);
                half3 color = texColor.rgb * _BaseColor.rgb;
                half alpha = texColor.a * _BaseColor.a;
                alpha = AlphaDiscard(alpha, _Cutoff);
                color = AlphaModulate(color, alpha);
                #ifdef _DBUFFER
                ApplyDecalToBaseColorAndNormal(input.positionCS, color, inputData.normalWS);
                #endif
                SurfaceData surfaceData;
                InitializeSurfaceData(color, alpha, half3(0, 0, 1), surfaceData);
                half4 finalColor = UniversalFragmentBakedLit(inputData, surfaceData);
                finalColor.a = OutputAlpha(finalColor.a, _Surface);
                return finalColor;
            }
            ENDHLSL
        }
        Pass
        {
            Tags
            {
                "LightMode" = "DepthOnly"
            }
            // -------------------------------------
            // Render State Commands
            ZWrite On
            ColorMask R
            HLSLPROGRAM
            #pragma target 2.0
            // -------------------------------------
            // Shader Stages
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            // -------------------------------------
            // Includes
            #include "Packages/com.unity.render-pipelines.universal/Shaders/BakedLitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }
        // This pass is used when drawing to a _CameraNormalsTexture texture with the forward renderer or the depthNormal prepass with the deferred renderer.
        Pass
        {
            Name "DepthNormalsOnly"
            Tags
            {
                "LightMode" = "DepthNormalsOnly"
            }
            // -------------------------------------
            // Render State Commands
            ZWrite On
            Cull[_Cull]
            HLSLPROGRAM
            #pragma target 2.0
            // -------------------------------------
            // Shader Stages
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment
            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _ _NORMALMAP
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            // -------------------------------------
            // Universal Pipeline keywords
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT // forward-only variant
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            // -------------------------------------
            // Includes
            #include "Packages/com.unity.render-pipelines.universal/Shaders/BakedLitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/BakedLitDepthNormalsPass.hlsl"
            ENDHLSL
        }
        // Same as DepthNormals pass, but used for deferred renderer and forwardOnly materials.
        Pass
        {
            Name "DepthNormalsOnly"
            Tags
            {
                "LightMode" = "DepthNormalsOnly"
            }
            // -------------------------------------
            // Render State Commands
            ZWrite On
            Cull[_Cull]
            HLSLPROGRAM
            #pragma target 2.0
            // -------------------------------------
            // Shader Stages
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment
            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _ _NORMALMAP
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            // -------------------------------------
            // Universal Pipeline keywords
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT // forward-only variant
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            //--------------------------------------
            // Defines
            #define BUMP_SCALE_NOT_SUPPORTED 1
            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            // -------------------------------------
            // Includes
            #include "Packages/com.unity.render-pipelines.universal/Shaders/BakedLitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthNormalsPass.hlsl"
            ENDHLSL
        }
        // This pass it not used during regular rendering, only for lightmap baking.
        Pass
        {
            Name "Meta"
            Tags
            {
                "LightMode" = "Meta"
            }
            // -------------------------------------
            // Render State Commands
            Cull Off
            HLSLPROGRAM
            #pragma target 2.0
            // -------------------------------------
            // Shader Stages
            #pragma vertex UniversalVertexMeta
            #pragma fragment UniversalFragmentMetaUnlit
            // -------------------------------------
            // Unity defined keywords
            #pragma shader_feature EDITOR_VISUALIZATION
            // -------------------------------------
            // Includes
            #include "Packages/com.unity.render-pipelines.universal/Shaders/BakedLitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/BakedLitMetaPass.hlsl"
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Unlit"
}