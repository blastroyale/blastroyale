Shader "SG_ItemCard2D_Maskable"
{
	Properties
	{
		[NoScaleOffset]_Frame("Frame", 2D) = "white" {}
		[NoScaleOffset]_FrameShapeMask("FrameShapeMask", 2D) = "white" {}
		[NoScaleOffset]_NameTag("NameTag", 2D) = "white" {}
		[NoScaleOffset]_AdjectivePattern("AdjectivePattern", 2D) = "white" {}
		_Adjective_Amount("Adjective Amount", Float) = 0.5
		[ToggleUI]_Plus_Indicator("Plus Indicator", Float) = 0
		[NoScaleOffset]_Plus_Image("Plus Image", 2D) = "white" {}
		_Plus_Amount("Plus Amount", Float) = 5.53
		[NoScaleOffset]_GradeTag("GradeTag", 2D) = "white" {}
		_Grade_Amount("Grade Amount", Float) = -0.5
		[NoScaleOffset]_MainTex("_MainTex", 2D) = "white" {}
		_FX_Scale("FX Scale", Vector) = (0, 0, 0, 0)
		_FX_Offset("FX Offset", Vector) = (0, 0, 0, 0)
		[NonModifiableTextureData][NoScaleOffset]_Texture2DAsset_0e3260f7159a476b8fe0d283c3c6f5b7_Out_0("Texture2D", 2D) = "white" {}
		[HideInInspector][NoScaleOffset]unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
		[HideInInspector][NoScaleOffset]unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {}
		[HideInInspector][NoScaleOffset]unity_ShadowMasks("unity_ShadowMasks", 2DArray) = "" {}

		_Stencil("Stencil ID", Float) = 0
		_StencilComp("StencilComp", Float) = 8
		_StencilOp("StencilOp", Float) = 0
		_StencilReadMask("StencilReadMask", Float) = 255
		_StencilWriteMask("StencilWriteMask", Float) = 255
		_ColorMask("ColorMask", Float) = 15
	}
	SubShader
	{
		Tags
		{
			"RenderPipeline"="UniversalPipeline"
			"RenderType"="Transparent"
			"UniversalMaterialType" = "Unlit"
			"Queue"="Transparent"
			"ShaderGraphShader"="true"
			"ShaderGraphTargetId"=""
		}
		Pass
		{
			Name "Sprite Unlit"
			Tags
			{
				"LightMode" = "Universal2D"
			}

			// Render State
			Cull Off
			Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
			ZTest LEqual
			ZWrite Off

			Stencil
			{
				Ref [_Stencil]
				Comp [_StencilComp]
				Pass [_StencilOp]
				ReadMask [_StencilReadMask]
				WriteMask [_StencilWriteMask]
			}
			ColorMask [_ColorMask]

			// Debug
			// <None>

			// --------------------------------------------------
			// Pass

			HLSLPROGRAM
			// Pragmas
			#pragma target 2.0
			#pragma exclude_renderers d3d11_9x
			#pragma vertex vert
			#pragma fragment frag

			// DotsInstancingOptions: <None>
			// HybridV1InjectedBuiltinProperties: <None>

			// Keywords
			#pragma multi_compile_fragment _ DEBUG_DISPLAY
			// GraphKeywords: <None>

			// Defines
			#define _SURFACE_TYPE_TRANSPARENT 1
			#define ATTRIBUTES_NEED_NORMAL
			#define ATTRIBUTES_NEED_TANGENT
			#define ATTRIBUTES_NEED_TEXCOORD0
			#define ATTRIBUTES_NEED_COLOR
			#define VARYINGS_NEED_POSITION_WS
			#define VARYINGS_NEED_TEXCOORD0
			#define VARYINGS_NEED_COLOR
			#define FEATURES_GRAPH_VERTEX
			/* WARNING: $splice Could not find named fragment 'PassInstancing' */
			#define SHADERPASS SHADERPASS_SPRITEUNLIT
			/* WARNING: $splice Could not find named fragment 'DotsInstancingVars' */

			// Includes
			/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreInclude' */

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			// --------------------------------------------------
			// Structs and Packing

			/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */

			struct Attributes
			{
				float3 positionOS : POSITION;
				float3 normalOS : NORMAL;
				float4 tangentOS : TANGENT;
				float4 uv0 : TEXCOORD0;
				float4 color : COLOR;
				#if UNITY_ANY_INSTANCING_ENABLED
                     uint instanceID : INSTANCEID_SEMANTIC;
				#endif
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float3 positionWS;
				float4 texCoord0;
				float4 color;
				#if UNITY_ANY_INSTANCING_ENABLED
                     uint instanceID : CUSTOM_INSTANCE_ID;
				#endif
				#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                     uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
				#endif
				#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                     uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
				#endif
				#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                     FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
				#endif
			};

			struct SurfaceDescriptionInputs
			{
				float4 uv0;
			};

			struct VertexDescriptionInputs
			{
				float3 ObjectSpaceNormal;
				float3 ObjectSpaceTangent;
				float3 ObjectSpacePosition;
			};

			struct PackedVaryings
			{
				float4 positionCS : SV_POSITION;
				float3 interp0 : INTERP0;
				float4 interp1 : INTERP1;
				float4 interp2 : INTERP2;
				#if UNITY_ANY_INSTANCING_ENABLED
                     uint instanceID : CUSTOM_INSTANCE_ID;
				#endif
				#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                     uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
				#endif
				#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                     uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
				#endif
				#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                     FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
				#endif
			};

			PackedVaryings PackVaryings(Varyings input)
			{
				PackedVaryings output;
				ZERO_INITIALIZE(PackedVaryings, output);
				output.positionCS = input.positionCS;
				output.interp0.xyz = input.positionWS;
				output.interp1.xyzw = input.texCoord0;
				output.interp2.xyzw = input.color;
				#if UNITY_ANY_INSTANCING_ENABLED
                    output.instanceID = input.instanceID;
				#endif
				#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                    output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
				#endif
				#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                    output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
				#endif
				#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                    output.cullFace = input.cullFace;
				#endif
				return output;
			}

			Varyings UnpackVaryings(PackedVaryings input)
			{
				Varyings output;
				output.positionCS = input.positionCS;
				output.positionWS = input.interp0.xyz;
				output.texCoord0 = input.interp1.xyzw;
				output.color = input.interp2.xyzw;
				#if UNITY_ANY_INSTANCING_ENABLED
                    output.instanceID = input.instanceID;
				#endif
				#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                    output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
				#endif
				#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                    output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
				#endif
				#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                    output.cullFace = input.cullFace;
				#endif
				return output;
			}


			// --------------------------------------------------
			// Graph

			// Graph Properties
			CBUFFER_START(UnityPerMaterial)
			float4 _Texture2DAsset_0e3260f7159a476b8fe0d283c3c6f5b7_Out_0_TexelSize;
			float4 _Frame_TexelSize;
			float4 _NameTag_TexelSize;
			float4 _AdjectivePattern_TexelSize;
			float4 _FrameShapeMask_TexelSize;
			float4 _Plus_Image_TexelSize;
			float4 _GradeTag_TexelSize;
			float _Plus_Amount;
			float _Adjective_Amount;
			float _Grade_Amount;
			float _Plus_Indicator;
			float4 _MainTex_TexelSize;
			float2 _FX_Scale;
			float2 _FX_Offset;
			CBUFFER_END

			// Object and Global properties
			SAMPLER(SamplerState_Linear_Repeat);
			TEXTURE2D(_Texture2DAsset_0e3260f7159a476b8fe0d283c3c6f5b7_Out_0);
			SAMPLER(sampler_Texture2DAsset_0e3260f7159a476b8fe0d283c3c6f5b7_Out_0);
			TEXTURE2D(_Frame);
			SAMPLER(sampler_Frame);
			TEXTURE2D(_NameTag);
			SAMPLER(sampler_NameTag);
			TEXTURE2D(_AdjectivePattern);
			SAMPLER(sampler_AdjectivePattern);
			TEXTURE2D(_FrameShapeMask);
			SAMPLER(sampler_FrameShapeMask);
			TEXTURE2D(_Plus_Image);
			SAMPLER(sampler_Plus_Image);
			TEXTURE2D(_GradeTag);
			SAMPLER(sampler_GradeTag);
			TEXTURE2D(_MainTex);
			SAMPLER(sampler_MainTex);

			// Graph Includes
			// GraphIncludes: <None>

			// -- Property used by ScenePickingPass
			#ifdef SCENEPICKINGPASS
                float4 _SelectionID;
			#endif

			// -- Properties used by SceneSelectionPass
			#ifdef SCENESELECTIONPASS
                int _ObjectId;
                int _PassValue;
			#endif

			// Graph Functions

			void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
			{
				Out = lerp(A, B, T);
			}

			void Unity_Multiply_float_float(float A, float B, out float Out)
			{
				Out = A * B;
			}

			void Unity_Blend_Overlay_float4(float4 Base, float4 Blend, out float4 Out, float Opacity)
			{
				float4 result1 = 1.0 - 2.0 * (1.0 - Base) * (1.0 - Blend);
				float4 result2 = 2.0 * Base * Blend;
				float4 zeroOrOne = step(Base, 0.5);
				Out = result2 * zeroOrOne + (1 - zeroOrOne) * result1;
				Out = lerp(Base, Out, Opacity);
			}

			void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
			{
				Out = UV * Tiling + Offset;
			}

			void Unity_Branch_float4(float Predicate, float4 True, float4 False, out float4 Out)
			{
				Out = Predicate ? True : False;
			}

			void Unity_Add_float4(float4 A, float4 B, out float4 Out)
			{
				Out = A + B;
			}

			/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */

			// Graph Vertex
			struct VertexDescription
			{
				float3 Position;
				float3 Normal;
				float3 Tangent;
			};

			VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
			{
				VertexDescription description = (VertexDescription)0;
				description.Position = IN.ObjectSpacePosition;
				description.Normal = IN.ObjectSpaceNormal;
				description.Tangent = IN.ObjectSpaceTangent;
				return description;
			}

			#ifdef FEATURES_GRAPH_VERTEX
			Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
			{
				return output;
			}

			#define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
			#endif

			// Graph Pixel
			struct SurfaceDescription
			{
				float3 BaseColor;
				float Alpha;
			};

			SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
			{
				SurfaceDescription surface = (SurfaceDescription)0;
				float _Property_6c605c7282b547b9b456952c497ed278_Out_0 = _Plus_Indicator;
				float4 _SampleTexture2D_48cadbb9084f4d1d9e22a8129e005c38_RGBA_0 = SAMPLE_TEXTURE2D(
					UnityBuildTexture2DStructNoScale(_Texture2DAsset_0e3260f7159a476b8fe0d283c3c6f5b7_Out_0).tex,
					UnityBuildTexture2DStructNoScale(_Texture2DAsset_0e3260f7159a476b8fe0d283c3c6f5b7_Out_0).
					samplerstate,
					UnityBuildTexture2DStructNoScale(_Texture2DAsset_0e3260f7159a476b8fe0d283c3c6f5b7_Out_0).
					GetTransformedUV(IN.uv0.xy));
				float _SampleTexture2D_48cadbb9084f4d1d9e22a8129e005c38_R_4 =
					_SampleTexture2D_48cadbb9084f4d1d9e22a8129e005c38_RGBA_0.r;
				float _SampleTexture2D_48cadbb9084f4d1d9e22a8129e005c38_G_5 =
					_SampleTexture2D_48cadbb9084f4d1d9e22a8129e005c38_RGBA_0.g;
				float _SampleTexture2D_48cadbb9084f4d1d9e22a8129e005c38_B_6 =
					_SampleTexture2D_48cadbb9084f4d1d9e22a8129e005c38_RGBA_0.b;
				float _SampleTexture2D_48cadbb9084f4d1d9e22a8129e005c38_A_7 =
					_SampleTexture2D_48cadbb9084f4d1d9e22a8129e005c38_RGBA_0.a;
				UnityTexture2D _Property_07a4a1af592d47dca5c43a77ef085fa4_Out_0 = UnityBuildTexture2DStructNoScale(
					_Frame);
				float4 _SampleTexture2D_cccaafae547644f886264237218c117d_RGBA_0 = SAMPLE_TEXTURE2D(
					_Property_07a4a1af592d47dca5c43a77ef085fa4_Out_0.tex,
					_Property_07a4a1af592d47dca5c43a77ef085fa4_Out_0.samplerstate,
					_Property_07a4a1af592d47dca5c43a77ef085fa4_Out_0.GetTransformedUV(IN.uv0.xy));
				float _SampleTexture2D_cccaafae547644f886264237218c117d_R_4 =
					_SampleTexture2D_cccaafae547644f886264237218c117d_RGBA_0.r;
				float _SampleTexture2D_cccaafae547644f886264237218c117d_G_5 =
					_SampleTexture2D_cccaafae547644f886264237218c117d_RGBA_0.g;
				float _SampleTexture2D_cccaafae547644f886264237218c117d_B_6 =
					_SampleTexture2D_cccaafae547644f886264237218c117d_RGBA_0.b;
				float _SampleTexture2D_cccaafae547644f886264237218c117d_A_7 =
					_SampleTexture2D_cccaafae547644f886264237218c117d_RGBA_0.a;
				float4 _Lerp_ab02372f6d574e1bb5f4fa3042c3d3ef_Out_3;
				Unity_Lerp_float4(_SampleTexture2D_48cadbb9084f4d1d9e22a8129e005c38_RGBA_0,
				                  _SampleTexture2D_cccaafae547644f886264237218c117d_RGBA_0,
				                  (_SampleTexture2D_cccaafae547644f886264237218c117d_A_7.xxxx),
				                  _Lerp_ab02372f6d574e1bb5f4fa3042c3d3ef_Out_3);
				UnityTexture2D _Property_ab38279b3cd940748ca4dd1475149aa5_Out_0 = UnityBuildTexture2DStructNoScale(
					_NameTag);
				float4 _SampleTexture2D_a66c2c8c7e0745de8a3863f7344a711d_RGBA_0 = SAMPLE_TEXTURE2D(
					_Property_ab38279b3cd940748ca4dd1475149aa5_Out_0.tex,
					_Property_ab38279b3cd940748ca4dd1475149aa5_Out_0.samplerstate,
					_Property_ab38279b3cd940748ca4dd1475149aa5_Out_0.GetTransformedUV(IN.uv0.xy));
				float _SampleTexture2D_a66c2c8c7e0745de8a3863f7344a711d_R_4 =
					_SampleTexture2D_a66c2c8c7e0745de8a3863f7344a711d_RGBA_0.r;
				float _SampleTexture2D_a66c2c8c7e0745de8a3863f7344a711d_G_5 =
					_SampleTexture2D_a66c2c8c7e0745de8a3863f7344a711d_RGBA_0.g;
				float _SampleTexture2D_a66c2c8c7e0745de8a3863f7344a711d_B_6 =
					_SampleTexture2D_a66c2c8c7e0745de8a3863f7344a711d_RGBA_0.b;
				float _SampleTexture2D_a66c2c8c7e0745de8a3863f7344a711d_A_7 =
					_SampleTexture2D_a66c2c8c7e0745de8a3863f7344a711d_RGBA_0.a;
				float4 _Lerp_e4228b01a251412fa8221310994cfa20_Out_3;
				Unity_Lerp_float4(_Lerp_ab02372f6d574e1bb5f4fa3042c3d3ef_Out_3,
				                  _SampleTexture2D_a66c2c8c7e0745de8a3863f7344a711d_RGBA_0,
				                  (_SampleTexture2D_a66c2c8c7e0745de8a3863f7344a711d_A_7.xxxx),
				                  _Lerp_e4228b01a251412fa8221310994cfa20_Out_3);
				UnityTexture2D _Property_c60a2f30295b407084b75ccc59c22fa8_Out_0 = UnityBuildTexture2DStructNoScale(
					_AdjectivePattern);
				float4 _SampleTexture2D_161fad2c37a948679d86596bb185f422_RGBA_0 = SAMPLE_TEXTURE2D(
					_Property_c60a2f30295b407084b75ccc59c22fa8_Out_0.tex,
					_Property_c60a2f30295b407084b75ccc59c22fa8_Out_0.samplerstate,
					_Property_c60a2f30295b407084b75ccc59c22fa8_Out_0.GetTransformedUV(IN.uv0.xy));
				float _SampleTexture2D_161fad2c37a948679d86596bb185f422_R_4 =
					_SampleTexture2D_161fad2c37a948679d86596bb185f422_RGBA_0.r;
				float _SampleTexture2D_161fad2c37a948679d86596bb185f422_G_5 =
					_SampleTexture2D_161fad2c37a948679d86596bb185f422_RGBA_0.g;
				float _SampleTexture2D_161fad2c37a948679d86596bb185f422_B_6 =
					_SampleTexture2D_161fad2c37a948679d86596bb185f422_RGBA_0.b;
				float _SampleTexture2D_161fad2c37a948679d86596bb185f422_A_7 =
					_SampleTexture2D_161fad2c37a948679d86596bb185f422_RGBA_0.a;
				UnityTexture2D _Property_b0d8535f94d748cfbf54f7b3a287e751_Out_0 = UnityBuildTexture2DStructNoScale(
					_FrameShapeMask);
				float4 _SampleTexture2D_13c02885e0f047a798551f250463dbb5_RGBA_0 = SAMPLE_TEXTURE2D(
					_Property_b0d8535f94d748cfbf54f7b3a287e751_Out_0.tex,
					_Property_b0d8535f94d748cfbf54f7b3a287e751_Out_0.samplerstate,
					_Property_b0d8535f94d748cfbf54f7b3a287e751_Out_0.GetTransformedUV(IN.uv0.xy));
				float _SampleTexture2D_13c02885e0f047a798551f250463dbb5_R_4 =
					_SampleTexture2D_13c02885e0f047a798551f250463dbb5_RGBA_0.r;
				float _SampleTexture2D_13c02885e0f047a798551f250463dbb5_G_5 =
					_SampleTexture2D_13c02885e0f047a798551f250463dbb5_RGBA_0.g;
				float _SampleTexture2D_13c02885e0f047a798551f250463dbb5_B_6 =
					_SampleTexture2D_13c02885e0f047a798551f250463dbb5_RGBA_0.b;
				float _SampleTexture2D_13c02885e0f047a798551f250463dbb5_A_7 =
					_SampleTexture2D_13c02885e0f047a798551f250463dbb5_RGBA_0.a;
				float _Multiply_687192210f1a4cbf81c78436bc291c7f_Out_2;
				Unity_Multiply_float_float(_SampleTexture2D_161fad2c37a948679d86596bb185f422_A_7,
				                           _SampleTexture2D_13c02885e0f047a798551f250463dbb5_A_7,
				                           _Multiply_687192210f1a4cbf81c78436bc291c7f_Out_2);
				float _Property_ab766b752fbf48f9b5236ea91d222c0d_Out_0 = _Adjective_Amount;
				float _Multiply_d3730c24842142beb2df4ae6bb5c1ac8_Out_2;
				Unity_Multiply_float_float(_Multiply_687192210f1a4cbf81c78436bc291c7f_Out_2,
				                           _Property_ab766b752fbf48f9b5236ea91d222c0d_Out_0,
				                           _Multiply_d3730c24842142beb2df4ae6bb5c1ac8_Out_2);
				float4 _Blend_3213240eae8840c99465ac6e19db00e9_Out_2;
				Unity_Blend_Overlay_float4(_Lerp_e4228b01a251412fa8221310994cfa20_Out_3,
				                           _SampleTexture2D_161fad2c37a948679d86596bb185f422_RGBA_0,
				                           _Blend_3213240eae8840c99465ac6e19db00e9_Out_2,
				                           _Multiply_d3730c24842142beb2df4ae6bb5c1ac8_Out_2);
				float4 Color_615e32773a2240fe9124cdb46ef3d90a = IsGammaSpace()
					                                                ? float4(1, 1, 1, 0)
					                                                : float4(SRGBToLinear(float3(1, 1, 1)), 0);
				UnityTexture2D _Property_cc255972cd844a1cbc7eeb9e7c4da77a_Out_0 = UnityBuildTexture2DStructNoScale(
					_Plus_Image);
				float2 _Property_39ae1007383e4909a2b790a1f8e2dbce_Out_0 = _FX_Scale;
				float2 _Property_746e380bb83f42f6aa7f71c4694dda62_Out_0 = _FX_Offset;
				float2 _TilingAndOffset_c836f6ac64514dff9324963f05decbb7_Out_3;
				Unity_TilingAndOffset_float(IN.uv0.xy, _Property_39ae1007383e4909a2b790a1f8e2dbce_Out_0,
				                            _Property_746e380bb83f42f6aa7f71c4694dda62_Out_0,
				                            _TilingAndOffset_c836f6ac64514dff9324963f05decbb7_Out_3);
				float4 _SampleTexture2D_a7d08c4034114bb3a2198cd36e767173_RGBA_0 = SAMPLE_TEXTURE2D(
					_Property_cc255972cd844a1cbc7eeb9e7c4da77a_Out_0.tex,
					_Property_cc255972cd844a1cbc7eeb9e7c4da77a_Out_0.samplerstate,
					_Property_cc255972cd844a1cbc7eeb9e7c4da77a_Out_0.GetTransformedUV(
						_TilingAndOffset_c836f6ac64514dff9324963f05decbb7_Out_3));
				float _SampleTexture2D_a7d08c4034114bb3a2198cd36e767173_R_4 =
					_SampleTexture2D_a7d08c4034114bb3a2198cd36e767173_RGBA_0.r;
				float _SampleTexture2D_a7d08c4034114bb3a2198cd36e767173_G_5 =
					_SampleTexture2D_a7d08c4034114bb3a2198cd36e767173_RGBA_0.g;
				float _SampleTexture2D_a7d08c4034114bb3a2198cd36e767173_B_6 =
					_SampleTexture2D_a7d08c4034114bb3a2198cd36e767173_RGBA_0.b;
				float _SampleTexture2D_a7d08c4034114bb3a2198cd36e767173_A_7 =
					_SampleTexture2D_a7d08c4034114bb3a2198cd36e767173_RGBA_0.a;
				float _Multiply_52a77a4627a44944926a07dfcd54df6d_Out_2;
				Unity_Multiply_float_float(_SampleTexture2D_13c02885e0f047a798551f250463dbb5_A_7,
				                           _SampleTexture2D_a7d08c4034114bb3a2198cd36e767173_A_7,
				                           _Multiply_52a77a4627a44944926a07dfcd54df6d_Out_2);
				float _Property_8120342a1bc842a8bece2f112b4a24ad_Out_0 = _Plus_Amount;
				float _Multiply_2df54a93e6204b67adb342bf2fa9025b_Out_2;
				Unity_Multiply_float_float(_Multiply_52a77a4627a44944926a07dfcd54df6d_Out_2,
				                           _Property_8120342a1bc842a8bece2f112b4a24ad_Out_0,
				                           _Multiply_2df54a93e6204b67adb342bf2fa9025b_Out_2);
				float4 _Blend_4fdd1316f4604b8a957e5a3c232f58f0_Out_2;
				Unity_Blend_Overlay_float4(_Blend_3213240eae8840c99465ac6e19db00e9_Out_2,
				                           Color_615e32773a2240fe9124cdb46ef3d90a,
				                           _Blend_4fdd1316f4604b8a957e5a3c232f58f0_Out_2,
				                           _Multiply_2df54a93e6204b67adb342bf2fa9025b_Out_2);
				float4 _Branch_d58bdd5e773540cba057d8eeabdc8418_Out_3;
				Unity_Branch_float4(_Property_6c605c7282b547b9b456952c497ed278_Out_0,
				                    _Blend_4fdd1316f4604b8a957e5a3c232f58f0_Out_2,
				                    _Blend_3213240eae8840c99465ac6e19db00e9_Out_2,
				                    _Branch_d58bdd5e773540cba057d8eeabdc8418_Out_3);
				UnityTexture2D _Property_812ec6fee4c247598b56fedde9fe8d82_Out_0 = UnityBuildTexture2DStructNoScale(
					_GradeTag);
				float2 _TilingAndOffset_7cd239011c3445d0bb0db853afbdd446_Out_3;
				Unity_TilingAndOffset_float(IN.uv0.xy, float2(1, 1), float2(0.02, 0),
				                            _TilingAndOffset_7cd239011c3445d0bb0db853afbdd446_Out_3);
				float4 _SampleTexture2D_69edc517709d48e19a56fb154356b81e_RGBA_0 = SAMPLE_TEXTURE2D(
					_Property_812ec6fee4c247598b56fedde9fe8d82_Out_0.tex,
					_Property_812ec6fee4c247598b56fedde9fe8d82_Out_0.samplerstate,
					_Property_812ec6fee4c247598b56fedde9fe8d82_Out_0.GetTransformedUV(
						_TilingAndOffset_7cd239011c3445d0bb0db853afbdd446_Out_3));
				float _SampleTexture2D_69edc517709d48e19a56fb154356b81e_R_4 =
					_SampleTexture2D_69edc517709d48e19a56fb154356b81e_RGBA_0.r;
				float _SampleTexture2D_69edc517709d48e19a56fb154356b81e_G_5 =
					_SampleTexture2D_69edc517709d48e19a56fb154356b81e_RGBA_0.g;
				float _SampleTexture2D_69edc517709d48e19a56fb154356b81e_B_6 =
					_SampleTexture2D_69edc517709d48e19a56fb154356b81e_RGBA_0.b;
				float _SampleTexture2D_69edc517709d48e19a56fb154356b81e_A_7 =
					_SampleTexture2D_69edc517709d48e19a56fb154356b81e_RGBA_0.a;
				float _Property_06cf96a72f4343b98a89f7355722c8e4_Out_0 = _Grade_Amount;
				float _Multiply_6ad75cd7329f4ab2bb0e5a0125a67d4e_Out_2;
				Unity_Multiply_float_float(_SampleTexture2D_69edc517709d48e19a56fb154356b81e_A_7,
				                           _Property_06cf96a72f4343b98a89f7355722c8e4_Out_0,
				                           _Multiply_6ad75cd7329f4ab2bb0e5a0125a67d4e_Out_2);
				float4 _Add_678bd7b7e0d9438e8ab8ce493727c798_Out_2;
				Unity_Add_float4(_Branch_d58bdd5e773540cba057d8eeabdc8418_Out_3,
				                 (_Multiply_6ad75cd7329f4ab2bb0e5a0125a67d4e_Out_2.xxxx),
				                 _Add_678bd7b7e0d9438e8ab8ce493727c798_Out_2);
				float _Split_49267e3c5aec45e082f03597de8ee5bf_R_1 = _Add_678bd7b7e0d9438e8ab8ce493727c798_Out_2[0];
				float _Split_49267e3c5aec45e082f03597de8ee5bf_G_2 = _Add_678bd7b7e0d9438e8ab8ce493727c798_Out_2[1];
				float _Split_49267e3c5aec45e082f03597de8ee5bf_B_3 = _Add_678bd7b7e0d9438e8ab8ce493727c798_Out_2[2];
				float _Split_49267e3c5aec45e082f03597de8ee5bf_A_4 = _Add_678bd7b7e0d9438e8ab8ce493727c798_Out_2[3];
				surface.BaseColor = (_Add_678bd7b7e0d9438e8ab8ce493727c798_Out_2.xyz);
				surface.Alpha = _Split_49267e3c5aec45e082f03597de8ee5bf_A_4;
				return surface;
			}

			// --------------------------------------------------
			// Build Graph Inputs

			VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
			{
				VertexDescriptionInputs output;
				ZERO_INITIALIZE(VertexDescriptionInputs, output);

				output.ObjectSpaceNormal = input.normalOS;
				output.ObjectSpaceTangent = input.tangentOS.xyz;
				output.ObjectSpacePosition = input.positionOS;

				return output;
			}

			SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
			{
				SurfaceDescriptionInputs output;
				ZERO_INITIALIZE(SurfaceDescriptionInputs, output);


				output.uv0 = input.texCoord0;
				#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN                output.FaceSign =                                   IS_FRONT_VFACE(input.cullFace, true, false);
				#else
				#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
				#endif
				#undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

				return output;
			}


			// --------------------------------------------------
			// Main

			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/2D/ShaderGraph/Includes/SpriteUnlitPass.hlsl"
			ENDHLSL
		}
		Pass
		{
			Name "SceneSelectionPass"
			Tags
			{
				"LightMode" = "SceneSelectionPass"
			}

			// Render State
			Cull Off

			Stencil
			{
				Ref [_Stencil]
				Comp [_StencilComp]
				Pass [_StencilOp]
				ReadMask [_StencilReadMask]
				WriteMask [_StencilWriteMask]
			}
			ColorMask [_ColorMask]

			// Debug
			// <None>

			// --------------------------------------------------
			// Pass

			HLSLPROGRAM
			// Pragmas
			#pragma target 2.0
			#pragma exclude_renderers d3d11_9x
			#pragma vertex vert
			#pragma fragment frag

			// DotsInstancingOptions: <None>
			// HybridV1InjectedBuiltinProperties: <None>

			// Keywords
			// PassKeywords: <None>
			// GraphKeywords: <None>

			// Defines
			#define _SURFACE_TYPE_TRANSPARENT 1
			#define ATTRIBUTES_NEED_NORMAL
			#define ATTRIBUTES_NEED_TANGENT
			#define ATTRIBUTES_NEED_TEXCOORD0
			#define VARYINGS_NEED_TEXCOORD0
			#define FEATURES_GRAPH_VERTEX
			/* WARNING: $splice Could not find named fragment 'PassInstancing' */
			#define SHADERPASS SHADERPASS_DEPTHONLY
			#define SCENESELECTIONPASS 1

			/* WARNING: $splice Could not find named fragment 'DotsInstancingVars' */

			// Includes
			/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreInclude' */

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			// --------------------------------------------------
			// Structs and Packing

			/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */

			struct Attributes
			{
				float3 positionOS : POSITION;
				float3 normalOS : NORMAL;
				float4 tangentOS : TANGENT;
				float4 uv0 : TEXCOORD0;
				#if UNITY_ANY_INSTANCING_ENABLED
                     uint instanceID : INSTANCEID_SEMANTIC;
				#endif
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float4 texCoord0;
				#if UNITY_ANY_INSTANCING_ENABLED
                     uint instanceID : CUSTOM_INSTANCE_ID;
				#endif
				#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                     uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
				#endif
				#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                     uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
				#endif
				#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                     FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
				#endif
			};

			struct SurfaceDescriptionInputs
			{
				float4 uv0;
			};

			struct VertexDescriptionInputs
			{
				float3 ObjectSpaceNormal;
				float3 ObjectSpaceTangent;
				float3 ObjectSpacePosition;
			};

			struct PackedVaryings
			{
				float4 positionCS : SV_POSITION;
				float4 interp0 : INTERP0;
				#if UNITY_ANY_INSTANCING_ENABLED
                     uint instanceID : CUSTOM_INSTANCE_ID;
				#endif
				#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                     uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
				#endif
				#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                     uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
				#endif
				#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                     FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
				#endif
			};

			PackedVaryings PackVaryings(Varyings input)
			{
				PackedVaryings output;
				ZERO_INITIALIZE(PackedVaryings, output);
				output.positionCS = input.positionCS;
				output.interp0.xyzw = input.texCoord0;
				#if UNITY_ANY_INSTANCING_ENABLED
                    output.instanceID = input.instanceID;
				#endif
				#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                    output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
				#endif
				#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                    output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
				#endif
				#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                    output.cullFace = input.cullFace;
				#endif
				return output;
			}

			Varyings UnpackVaryings(PackedVaryings input)
			{
				Varyings output;
				output.positionCS = input.positionCS;
				output.texCoord0 = input.interp0.xyzw;
				#if UNITY_ANY_INSTANCING_ENABLED
                    output.instanceID = input.instanceID;
				#endif
				#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                    output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
				#endif
				#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                    output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
				#endif
				#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                    output.cullFace = input.cullFace;
				#endif
				return output;
			}


			// --------------------------------------------------
			// Graph

			// Graph Properties
			CBUFFER_START(UnityPerMaterial)
			float4 _Texture2DAsset_0e3260f7159a476b8fe0d283c3c6f5b7_Out_0_TexelSize;
			float4 _Frame_TexelSize;
			float4 _NameTag_TexelSize;
			float4 _AdjectivePattern_TexelSize;
			float4 _FrameShapeMask_TexelSize;
			float4 _Plus_Image_TexelSize;
			float4 _GradeTag_TexelSize;
			float _Plus_Amount;
			float _Adjective_Amount;
			float _Grade_Amount;
			float _Plus_Indicator;
			float4 _MainTex_TexelSize;
			float2 _FX_Scale;
			float2 _FX_Offset;
			CBUFFER_END

			// Object and Global properties
			SAMPLER(SamplerState_Linear_Repeat);
			TEXTURE2D(_Texture2DAsset_0e3260f7159a476b8fe0d283c3c6f5b7_Out_0);
			SAMPLER(sampler_Texture2DAsset_0e3260f7159a476b8fe0d283c3c6f5b7_Out_0);
			TEXTURE2D(_Frame);
			SAMPLER(sampler_Frame);
			TEXTURE2D(_NameTag);
			SAMPLER(sampler_NameTag);
			TEXTURE2D(_AdjectivePattern);
			SAMPLER(sampler_AdjectivePattern);
			TEXTURE2D(_FrameShapeMask);
			SAMPLER(sampler_FrameShapeMask);
			TEXTURE2D(_Plus_Image);
			SAMPLER(sampler_Plus_Image);
			TEXTURE2D(_GradeTag);
			SAMPLER(sampler_GradeTag);
			TEXTURE2D(_MainTex);
			SAMPLER(sampler_MainTex);

			// Graph Includes
			// GraphIncludes: <None>

			// -- Property used by ScenePickingPass
			#ifdef SCENEPICKINGPASS
                float4 _SelectionID;
			#endif

			// -- Properties used by SceneSelectionPass
			#ifdef SCENESELECTIONPASS
			int _ObjectId;
			int _PassValue;
			#endif

			// Graph Functions

			void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
			{
				Out = lerp(A, B, T);
			}

			void Unity_Multiply_float_float(float A, float B, out float Out)
			{
				Out = A * B;
			}

			void Unity_Blend_Overlay_float4(float4 Base, float4 Blend, out float4 Out, float Opacity)
			{
				float4 result1 = 1.0 - 2.0 * (1.0 - Base) * (1.0 - Blend);
				float4 result2 = 2.0 * Base * Blend;
				float4 zeroOrOne = step(Base, 0.5);
				Out = result2 * zeroOrOne + (1 - zeroOrOne) * result1;
				Out = lerp(Base, Out, Opacity);
			}

			void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
			{
				Out = UV * Tiling + Offset;
			}

			void Unity_Branch_float4(float Predicate, float4 True, float4 False, out float4 Out)
			{
				Out = Predicate ? True : False;
			}

			void Unity_Add_float4(float4 A, float4 B, out float4 Out)
			{
				Out = A + B;
			}

			/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */

			// Graph Vertex
			struct VertexDescription
			{
				float3 Position;
				float3 Normal;
				float3 Tangent;
			};

			VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
			{
				VertexDescription description = (VertexDescription)0;
				description.Position = IN.ObjectSpacePosition;
				description.Normal = IN.ObjectSpaceNormal;
				description.Tangent = IN.ObjectSpaceTangent;
				return description;
			}

			#ifdef FEATURES_GRAPH_VERTEX
			Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
			{
				return output;
			}

			#define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
			#endif

			// Graph Pixel
			struct SurfaceDescription
			{
				float Alpha;
			};

			SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
			{
				SurfaceDescription surface = (SurfaceDescription)0;
				float _Property_6c605c7282b547b9b456952c497ed278_Out_0 = _Plus_Indicator;
				float4 _SampleTexture2D_48cadbb9084f4d1d9e22a8129e005c38_RGBA_0 = SAMPLE_TEXTURE2D(
					UnityBuildTexture2DStructNoScale(_Texture2DAsset_0e3260f7159a476b8fe0d283c3c6f5b7_Out_0).tex,
					UnityBuildTexture2DStructNoScale(_Texture2DAsset_0e3260f7159a476b8fe0d283c3c6f5b7_Out_0).
					samplerstate,
					UnityBuildTexture2DStructNoScale(_Texture2DAsset_0e3260f7159a476b8fe0d283c3c6f5b7_Out_0).
					GetTransformedUV(IN.uv0.xy));
				float _SampleTexture2D_48cadbb9084f4d1d9e22a8129e005c38_R_4 =
					_SampleTexture2D_48cadbb9084f4d1d9e22a8129e005c38_RGBA_0.r;
				float _SampleTexture2D_48cadbb9084f4d1d9e22a8129e005c38_G_5 =
					_SampleTexture2D_48cadbb9084f4d1d9e22a8129e005c38_RGBA_0.g;
				float _SampleTexture2D_48cadbb9084f4d1d9e22a8129e005c38_B_6 =
					_SampleTexture2D_48cadbb9084f4d1d9e22a8129e005c38_RGBA_0.b;
				float _SampleTexture2D_48cadbb9084f4d1d9e22a8129e005c38_A_7 =
					_SampleTexture2D_48cadbb9084f4d1d9e22a8129e005c38_RGBA_0.a;
				UnityTexture2D _Property_07a4a1af592d47dca5c43a77ef085fa4_Out_0 = UnityBuildTexture2DStructNoScale(
					_Frame);
				float4 _SampleTexture2D_cccaafae547644f886264237218c117d_RGBA_0 = SAMPLE_TEXTURE2D(
					_Property_07a4a1af592d47dca5c43a77ef085fa4_Out_0.tex,
					_Property_07a4a1af592d47dca5c43a77ef085fa4_Out_0.samplerstate,
					_Property_07a4a1af592d47dca5c43a77ef085fa4_Out_0.GetTransformedUV(IN.uv0.xy));
				float _SampleTexture2D_cccaafae547644f886264237218c117d_R_4 =
					_SampleTexture2D_cccaafae547644f886264237218c117d_RGBA_0.r;
				float _SampleTexture2D_cccaafae547644f886264237218c117d_G_5 =
					_SampleTexture2D_cccaafae547644f886264237218c117d_RGBA_0.g;
				float _SampleTexture2D_cccaafae547644f886264237218c117d_B_6 =
					_SampleTexture2D_cccaafae547644f886264237218c117d_RGBA_0.b;
				float _SampleTexture2D_cccaafae547644f886264237218c117d_A_7 =
					_SampleTexture2D_cccaafae547644f886264237218c117d_RGBA_0.a;
				float4 _Lerp_ab02372f6d574e1bb5f4fa3042c3d3ef_Out_3;
				Unity_Lerp_float4(_SampleTexture2D_48cadbb9084f4d1d9e22a8129e005c38_RGBA_0,
				                  _SampleTexture2D_cccaafae547644f886264237218c117d_RGBA_0,
				                  (_SampleTexture2D_cccaafae547644f886264237218c117d_A_7.xxxx),
				                  _Lerp_ab02372f6d574e1bb5f4fa3042c3d3ef_Out_3);
				UnityTexture2D _Property_ab38279b3cd940748ca4dd1475149aa5_Out_0 = UnityBuildTexture2DStructNoScale(
					_NameTag);
				float4 _SampleTexture2D_a66c2c8c7e0745de8a3863f7344a711d_RGBA_0 = SAMPLE_TEXTURE2D(
					_Property_ab38279b3cd940748ca4dd1475149aa5_Out_0.tex,
					_Property_ab38279b3cd940748ca4dd1475149aa5_Out_0.samplerstate,
					_Property_ab38279b3cd940748ca4dd1475149aa5_Out_0.GetTransformedUV(IN.uv0.xy));
				float _SampleTexture2D_a66c2c8c7e0745de8a3863f7344a711d_R_4 =
					_SampleTexture2D_a66c2c8c7e0745de8a3863f7344a711d_RGBA_0.r;
				float _SampleTexture2D_a66c2c8c7e0745de8a3863f7344a711d_G_5 =
					_SampleTexture2D_a66c2c8c7e0745de8a3863f7344a711d_RGBA_0.g;
				float _SampleTexture2D_a66c2c8c7e0745de8a3863f7344a711d_B_6 =
					_SampleTexture2D_a66c2c8c7e0745de8a3863f7344a711d_RGBA_0.b;
				float _SampleTexture2D_a66c2c8c7e0745de8a3863f7344a711d_A_7 =
					_SampleTexture2D_a66c2c8c7e0745de8a3863f7344a711d_RGBA_0.a;
				float4 _Lerp_e4228b01a251412fa8221310994cfa20_Out_3;
				Unity_Lerp_float4(_Lerp_ab02372f6d574e1bb5f4fa3042c3d3ef_Out_3,
				                  _SampleTexture2D_a66c2c8c7e0745de8a3863f7344a711d_RGBA_0,
				                  (_SampleTexture2D_a66c2c8c7e0745de8a3863f7344a711d_A_7.xxxx),
				                  _Lerp_e4228b01a251412fa8221310994cfa20_Out_3);
				UnityTexture2D _Property_c60a2f30295b407084b75ccc59c22fa8_Out_0 = UnityBuildTexture2DStructNoScale(
					_AdjectivePattern);
				float4 _SampleTexture2D_161fad2c37a948679d86596bb185f422_RGBA_0 = SAMPLE_TEXTURE2D(
					_Property_c60a2f30295b407084b75ccc59c22fa8_Out_0.tex,
					_Property_c60a2f30295b407084b75ccc59c22fa8_Out_0.samplerstate,
					_Property_c60a2f30295b407084b75ccc59c22fa8_Out_0.GetTransformedUV(IN.uv0.xy));
				float _SampleTexture2D_161fad2c37a948679d86596bb185f422_R_4 =
					_SampleTexture2D_161fad2c37a948679d86596bb185f422_RGBA_0.r;
				float _SampleTexture2D_161fad2c37a948679d86596bb185f422_G_5 =
					_SampleTexture2D_161fad2c37a948679d86596bb185f422_RGBA_0.g;
				float _SampleTexture2D_161fad2c37a948679d86596bb185f422_B_6 =
					_SampleTexture2D_161fad2c37a948679d86596bb185f422_RGBA_0.b;
				float _SampleTexture2D_161fad2c37a948679d86596bb185f422_A_7 =
					_SampleTexture2D_161fad2c37a948679d86596bb185f422_RGBA_0.a;
				UnityTexture2D _Property_b0d8535f94d748cfbf54f7b3a287e751_Out_0 = UnityBuildTexture2DStructNoScale(
					_FrameShapeMask);
				float4 _SampleTexture2D_13c02885e0f047a798551f250463dbb5_RGBA_0 = SAMPLE_TEXTURE2D(
					_Property_b0d8535f94d748cfbf54f7b3a287e751_Out_0.tex,
					_Property_b0d8535f94d748cfbf54f7b3a287e751_Out_0.samplerstate,
					_Property_b0d8535f94d748cfbf54f7b3a287e751_Out_0.GetTransformedUV(IN.uv0.xy));
				float _SampleTexture2D_13c02885e0f047a798551f250463dbb5_R_4 =
					_SampleTexture2D_13c02885e0f047a798551f250463dbb5_RGBA_0.r;
				float _SampleTexture2D_13c02885e0f047a798551f250463dbb5_G_5 =
					_SampleTexture2D_13c02885e0f047a798551f250463dbb5_RGBA_0.g;
				float _SampleTexture2D_13c02885e0f047a798551f250463dbb5_B_6 =
					_SampleTexture2D_13c02885e0f047a798551f250463dbb5_RGBA_0.b;
				float _SampleTexture2D_13c02885e0f047a798551f250463dbb5_A_7 =
					_SampleTexture2D_13c02885e0f047a798551f250463dbb5_RGBA_0.a;
				float _Multiply_687192210f1a4cbf81c78436bc291c7f_Out_2;
				Unity_Multiply_float_float(_SampleTexture2D_161fad2c37a948679d86596bb185f422_A_7,
				                           _SampleTexture2D_13c02885e0f047a798551f250463dbb5_A_7,
				                           _Multiply_687192210f1a4cbf81c78436bc291c7f_Out_2);
				float _Property_ab766b752fbf48f9b5236ea91d222c0d_Out_0 = _Adjective_Amount;
				float _Multiply_d3730c24842142beb2df4ae6bb5c1ac8_Out_2;
				Unity_Multiply_float_float(_Multiply_687192210f1a4cbf81c78436bc291c7f_Out_2,
				                           _Property_ab766b752fbf48f9b5236ea91d222c0d_Out_0,
				                           _Multiply_d3730c24842142beb2df4ae6bb5c1ac8_Out_2);
				float4 _Blend_3213240eae8840c99465ac6e19db00e9_Out_2;
				Unity_Blend_Overlay_float4(_Lerp_e4228b01a251412fa8221310994cfa20_Out_3,
				                           _SampleTexture2D_161fad2c37a948679d86596bb185f422_RGBA_0,
				                           _Blend_3213240eae8840c99465ac6e19db00e9_Out_2,
				                           _Multiply_d3730c24842142beb2df4ae6bb5c1ac8_Out_2);
				float4 Color_615e32773a2240fe9124cdb46ef3d90a = IsGammaSpace()
					                                                ? float4(1, 1, 1, 0)
					                                                : float4(SRGBToLinear(float3(1, 1, 1)), 0);
				UnityTexture2D _Property_cc255972cd844a1cbc7eeb9e7c4da77a_Out_0 = UnityBuildTexture2DStructNoScale(
					_Plus_Image);
				float2 _Property_39ae1007383e4909a2b790a1f8e2dbce_Out_0 = _FX_Scale;
				float2 _Property_746e380bb83f42f6aa7f71c4694dda62_Out_0 = _FX_Offset;
				float2 _TilingAndOffset_c836f6ac64514dff9324963f05decbb7_Out_3;
				Unity_TilingAndOffset_float(IN.uv0.xy, _Property_39ae1007383e4909a2b790a1f8e2dbce_Out_0,
				                            _Property_746e380bb83f42f6aa7f71c4694dda62_Out_0,
				                            _TilingAndOffset_c836f6ac64514dff9324963f05decbb7_Out_3);
				float4 _SampleTexture2D_a7d08c4034114bb3a2198cd36e767173_RGBA_0 = SAMPLE_TEXTURE2D(
					_Property_cc255972cd844a1cbc7eeb9e7c4da77a_Out_0.tex,
					_Property_cc255972cd844a1cbc7eeb9e7c4da77a_Out_0.samplerstate,
					_Property_cc255972cd844a1cbc7eeb9e7c4da77a_Out_0.GetTransformedUV(
						_TilingAndOffset_c836f6ac64514dff9324963f05decbb7_Out_3));
				float _SampleTexture2D_a7d08c4034114bb3a2198cd36e767173_R_4 =
					_SampleTexture2D_a7d08c4034114bb3a2198cd36e767173_RGBA_0.r;
				float _SampleTexture2D_a7d08c4034114bb3a2198cd36e767173_G_5 =
					_SampleTexture2D_a7d08c4034114bb3a2198cd36e767173_RGBA_0.g;
				float _SampleTexture2D_a7d08c4034114bb3a2198cd36e767173_B_6 =
					_SampleTexture2D_a7d08c4034114bb3a2198cd36e767173_RGBA_0.b;
				float _SampleTexture2D_a7d08c4034114bb3a2198cd36e767173_A_7 =
					_SampleTexture2D_a7d08c4034114bb3a2198cd36e767173_RGBA_0.a;
				float _Multiply_52a77a4627a44944926a07dfcd54df6d_Out_2;
				Unity_Multiply_float_float(_SampleTexture2D_13c02885e0f047a798551f250463dbb5_A_7,
				                           _SampleTexture2D_a7d08c4034114bb3a2198cd36e767173_A_7,
				                           _Multiply_52a77a4627a44944926a07dfcd54df6d_Out_2);
				float _Property_8120342a1bc842a8bece2f112b4a24ad_Out_0 = _Plus_Amount;
				float _Multiply_2df54a93e6204b67adb342bf2fa9025b_Out_2;
				Unity_Multiply_float_float(_Multiply_52a77a4627a44944926a07dfcd54df6d_Out_2,
				                           _Property_8120342a1bc842a8bece2f112b4a24ad_Out_0,
				                           _Multiply_2df54a93e6204b67adb342bf2fa9025b_Out_2);
				float4 _Blend_4fdd1316f4604b8a957e5a3c232f58f0_Out_2;
				Unity_Blend_Overlay_float4(_Blend_3213240eae8840c99465ac6e19db00e9_Out_2,
				                           Color_615e32773a2240fe9124cdb46ef3d90a,
				                           _Blend_4fdd1316f4604b8a957e5a3c232f58f0_Out_2,
				                           _Multiply_2df54a93e6204b67adb342bf2fa9025b_Out_2);
				float4 _Branch_d58bdd5e773540cba057d8eeabdc8418_Out_3;
				Unity_Branch_float4(_Property_6c605c7282b547b9b456952c497ed278_Out_0,
				                    _Blend_4fdd1316f4604b8a957e5a3c232f58f0_Out_2,
				                    _Blend_3213240eae8840c99465ac6e19db00e9_Out_2,
				                    _Branch_d58bdd5e773540cba057d8eeabdc8418_Out_3);
				UnityTexture2D _Property_812ec6fee4c247598b56fedde9fe8d82_Out_0 = UnityBuildTexture2DStructNoScale(
					_GradeTag);
				float2 _TilingAndOffset_7cd239011c3445d0bb0db853afbdd446_Out_3;
				Unity_TilingAndOffset_float(IN.uv0.xy, float2(1, 1), float2(0.02, 0),
				                            _TilingAndOffset_7cd239011c3445d0bb0db853afbdd446_Out_3);
				float4 _SampleTexture2D_69edc517709d48e19a56fb154356b81e_RGBA_0 = SAMPLE_TEXTURE2D(
					_Property_812ec6fee4c247598b56fedde9fe8d82_Out_0.tex,
					_Property_812ec6fee4c247598b56fedde9fe8d82_Out_0.samplerstate,
					_Property_812ec6fee4c247598b56fedde9fe8d82_Out_0.GetTransformedUV(
						_TilingAndOffset_7cd239011c3445d0bb0db853afbdd446_Out_3));
				float _SampleTexture2D_69edc517709d48e19a56fb154356b81e_R_4 =
					_SampleTexture2D_69edc517709d48e19a56fb154356b81e_RGBA_0.r;
				float _SampleTexture2D_69edc517709d48e19a56fb154356b81e_G_5 =
					_SampleTexture2D_69edc517709d48e19a56fb154356b81e_RGBA_0.g;
				float _SampleTexture2D_69edc517709d48e19a56fb154356b81e_B_6 =
					_SampleTexture2D_69edc517709d48e19a56fb154356b81e_RGBA_0.b;
				float _SampleTexture2D_69edc517709d48e19a56fb154356b81e_A_7 =
					_SampleTexture2D_69edc517709d48e19a56fb154356b81e_RGBA_0.a;
				float _Property_06cf96a72f4343b98a89f7355722c8e4_Out_0 = _Grade_Amount;
				float _Multiply_6ad75cd7329f4ab2bb0e5a0125a67d4e_Out_2;
				Unity_Multiply_float_float(_SampleTexture2D_69edc517709d48e19a56fb154356b81e_A_7,
				                           _Property_06cf96a72f4343b98a89f7355722c8e4_Out_0,
				                           _Multiply_6ad75cd7329f4ab2bb0e5a0125a67d4e_Out_2);
				float4 _Add_678bd7b7e0d9438e8ab8ce493727c798_Out_2;
				Unity_Add_float4(_Branch_d58bdd5e773540cba057d8eeabdc8418_Out_3,
				                 (_Multiply_6ad75cd7329f4ab2bb0e5a0125a67d4e_Out_2.xxxx),
				                 _Add_678bd7b7e0d9438e8ab8ce493727c798_Out_2);
				float _Split_49267e3c5aec45e082f03597de8ee5bf_R_1 = _Add_678bd7b7e0d9438e8ab8ce493727c798_Out_2[0];
				float _Split_49267e3c5aec45e082f03597de8ee5bf_G_2 = _Add_678bd7b7e0d9438e8ab8ce493727c798_Out_2[1];
				float _Split_49267e3c5aec45e082f03597de8ee5bf_B_3 = _Add_678bd7b7e0d9438e8ab8ce493727c798_Out_2[2];
				float _Split_49267e3c5aec45e082f03597de8ee5bf_A_4 = _Add_678bd7b7e0d9438e8ab8ce493727c798_Out_2[3];
				surface.Alpha = _Split_49267e3c5aec45e082f03597de8ee5bf_A_4;
				return surface;
			}

			// --------------------------------------------------
			// Build Graph Inputs

			VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
			{
				VertexDescriptionInputs output;
				ZERO_INITIALIZE(VertexDescriptionInputs, output);

				output.ObjectSpaceNormal = input.normalOS;
				output.ObjectSpaceTangent = input.tangentOS.xyz;
				output.ObjectSpacePosition = input.positionOS;

				return output;
			}

			SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
			{
				SurfaceDescriptionInputs output;
				ZERO_INITIALIZE(SurfaceDescriptionInputs, output);


				output.uv0 = input.texCoord0;
				#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN                output.FaceSign =                                   IS_FRONT_VFACE(input.cullFace, true, false);
				#else
				#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
				#endif
				#undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

				return output;
			}


			// --------------------------------------------------
			// Main

			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/SelectionPickingPass.hlsl"
			ENDHLSL
		}
		Pass
		{
			Name "ScenePickingPass"
			Tags
			{
				"LightMode" = "Picking"
			}

			// Render State
			Cull Back

			Stencil
			{
				Ref [_Stencil]
				Comp [_StencilComp]
				Pass [_StencilOp]
				ReadMask [_StencilReadMask]
				WriteMask [_StencilWriteMask]
			}
			ColorMask [_ColorMask]

			// Debug
			// <None>

			// --------------------------------------------------
			// Pass

			HLSLPROGRAM
			// Pragmas
			#pragma target 2.0
			#pragma exclude_renderers d3d11_9x
			#pragma vertex vert
			#pragma fragment frag

			// DotsInstancingOptions: <None>
			// HybridV1InjectedBuiltinProperties: <None>

			// Keywords
			// PassKeywords: <None>
			// GraphKeywords: <None>

			// Defines
			#define _SURFACE_TYPE_TRANSPARENT 1
			#define ATTRIBUTES_NEED_NORMAL
			#define ATTRIBUTES_NEED_TANGENT
			#define ATTRIBUTES_NEED_TEXCOORD0
			#define VARYINGS_NEED_TEXCOORD0
			#define FEATURES_GRAPH_VERTEX
			/* WARNING: $splice Could not find named fragment 'PassInstancing' */
			#define SHADERPASS SHADERPASS_DEPTHONLY
			#define SCENEPICKINGPASS 1

			/* WARNING: $splice Could not find named fragment 'DotsInstancingVars' */

			// Includes
			/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreInclude' */

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			// --------------------------------------------------
			// Structs and Packing

			/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */

			struct Attributes
			{
				float3 positionOS : POSITION;
				float3 normalOS : NORMAL;
				float4 tangentOS : TANGENT;
				float4 uv0 : TEXCOORD0;
				#if UNITY_ANY_INSTANCING_ENABLED
                     uint instanceID : INSTANCEID_SEMANTIC;
				#endif
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float4 texCoord0;
				#if UNITY_ANY_INSTANCING_ENABLED
                     uint instanceID : CUSTOM_INSTANCE_ID;
				#endif
				#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                     uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
				#endif
				#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                     uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
				#endif
				#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                     FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
				#endif
			};

			struct SurfaceDescriptionInputs
			{
				float4 uv0;
			};

			struct VertexDescriptionInputs
			{
				float3 ObjectSpaceNormal;
				float3 ObjectSpaceTangent;
				float3 ObjectSpacePosition;
			};

			struct PackedVaryings
			{
				float4 positionCS : SV_POSITION;
				float4 interp0 : INTERP0;
				#if UNITY_ANY_INSTANCING_ENABLED
                     uint instanceID : CUSTOM_INSTANCE_ID;
				#endif
				#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                     uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
				#endif
				#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                     uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
				#endif
				#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                     FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
				#endif
			};

			PackedVaryings PackVaryings(Varyings input)
			{
				PackedVaryings output;
				ZERO_INITIALIZE(PackedVaryings, output);
				output.positionCS = input.positionCS;
				output.interp0.xyzw = input.texCoord0;
				#if UNITY_ANY_INSTANCING_ENABLED
                    output.instanceID = input.instanceID;
				#endif
				#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                    output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
				#endif
				#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                    output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
				#endif
				#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                    output.cullFace = input.cullFace;
				#endif
				return output;
			}

			Varyings UnpackVaryings(PackedVaryings input)
			{
				Varyings output;
				output.positionCS = input.positionCS;
				output.texCoord0 = input.interp0.xyzw;
				#if UNITY_ANY_INSTANCING_ENABLED
                    output.instanceID = input.instanceID;
				#endif
				#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                    output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
				#endif
				#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                    output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
				#endif
				#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                    output.cullFace = input.cullFace;
				#endif
				return output;
			}


			// --------------------------------------------------
			// Graph

			// Graph Properties
			CBUFFER_START(UnityPerMaterial)
			float4 _Texture2DAsset_0e3260f7159a476b8fe0d283c3c6f5b7_Out_0_TexelSize;
			float4 _Frame_TexelSize;
			float4 _NameTag_TexelSize;
			float4 _AdjectivePattern_TexelSize;
			float4 _FrameShapeMask_TexelSize;
			float4 _Plus_Image_TexelSize;
			float4 _GradeTag_TexelSize;
			float _Plus_Amount;
			float _Adjective_Amount;
			float _Grade_Amount;
			float _Plus_Indicator;
			float4 _MainTex_TexelSize;
			float2 _FX_Scale;
			float2 _FX_Offset;
			CBUFFER_END

			// Object and Global properties
			SAMPLER(SamplerState_Linear_Repeat);
			TEXTURE2D(_Texture2DAsset_0e3260f7159a476b8fe0d283c3c6f5b7_Out_0);
			SAMPLER(sampler_Texture2DAsset_0e3260f7159a476b8fe0d283c3c6f5b7_Out_0);
			TEXTURE2D(_Frame);
			SAMPLER(sampler_Frame);
			TEXTURE2D(_NameTag);
			SAMPLER(sampler_NameTag);
			TEXTURE2D(_AdjectivePattern);
			SAMPLER(sampler_AdjectivePattern);
			TEXTURE2D(_FrameShapeMask);
			SAMPLER(sampler_FrameShapeMask);
			TEXTURE2D(_Plus_Image);
			SAMPLER(sampler_Plus_Image);
			TEXTURE2D(_GradeTag);
			SAMPLER(sampler_GradeTag);
			TEXTURE2D(_MainTex);
			SAMPLER(sampler_MainTex);

			// Graph Includes
			// GraphIncludes: <None>

			// -- Property used by ScenePickingPass
			#ifdef SCENEPICKINGPASS
			float4 _SelectionID;
			#endif

			// -- Properties used by SceneSelectionPass
			#ifdef SCENESELECTIONPASS
                int _ObjectId;
                int _PassValue;
			#endif

			// Graph Functions

			void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
			{
				Out = lerp(A, B, T);
			}

			void Unity_Multiply_float_float(float A, float B, out float Out)
			{
				Out = A * B;
			}

			void Unity_Blend_Overlay_float4(float4 Base, float4 Blend, out float4 Out, float Opacity)
			{
				float4 result1 = 1.0 - 2.0 * (1.0 - Base) * (1.0 - Blend);
				float4 result2 = 2.0 * Base * Blend;
				float4 zeroOrOne = step(Base, 0.5);
				Out = result2 * zeroOrOne + (1 - zeroOrOne) * result1;
				Out = lerp(Base, Out, Opacity);
			}

			void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
			{
				Out = UV * Tiling + Offset;
			}

			void Unity_Branch_float4(float Predicate, float4 True, float4 False, out float4 Out)
			{
				Out = Predicate ? True : False;
			}

			void Unity_Add_float4(float4 A, float4 B, out float4 Out)
			{
				Out = A + B;
			}

			/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */

			// Graph Vertex
			struct VertexDescription
			{
				float3 Position;
				float3 Normal;
				float3 Tangent;
			};

			VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
			{
				VertexDescription description = (VertexDescription)0;
				description.Position = IN.ObjectSpacePosition;
				description.Normal = IN.ObjectSpaceNormal;
				description.Tangent = IN.ObjectSpaceTangent;
				return description;
			}

			#ifdef FEATURES_GRAPH_VERTEX
			Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
			{
				return output;
			}

			#define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
			#endif

			// Graph Pixel
			struct SurfaceDescription
			{
				float Alpha;
			};

			SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
			{
				SurfaceDescription surface = (SurfaceDescription)0;
				float _Property_6c605c7282b547b9b456952c497ed278_Out_0 = _Plus_Indicator;
				float4 _SampleTexture2D_48cadbb9084f4d1d9e22a8129e005c38_RGBA_0 = SAMPLE_TEXTURE2D(
					UnityBuildTexture2DStructNoScale(_Texture2DAsset_0e3260f7159a476b8fe0d283c3c6f5b7_Out_0).tex,
					UnityBuildTexture2DStructNoScale(_Texture2DAsset_0e3260f7159a476b8fe0d283c3c6f5b7_Out_0).
					samplerstate,
					UnityBuildTexture2DStructNoScale(_Texture2DAsset_0e3260f7159a476b8fe0d283c3c6f5b7_Out_0).
					GetTransformedUV(IN.uv0.xy));
				float _SampleTexture2D_48cadbb9084f4d1d9e22a8129e005c38_R_4 =
					_SampleTexture2D_48cadbb9084f4d1d9e22a8129e005c38_RGBA_0.r;
				float _SampleTexture2D_48cadbb9084f4d1d9e22a8129e005c38_G_5 =
					_SampleTexture2D_48cadbb9084f4d1d9e22a8129e005c38_RGBA_0.g;
				float _SampleTexture2D_48cadbb9084f4d1d9e22a8129e005c38_B_6 =
					_SampleTexture2D_48cadbb9084f4d1d9e22a8129e005c38_RGBA_0.b;
				float _SampleTexture2D_48cadbb9084f4d1d9e22a8129e005c38_A_7 =
					_SampleTexture2D_48cadbb9084f4d1d9e22a8129e005c38_RGBA_0.a;
				UnityTexture2D _Property_07a4a1af592d47dca5c43a77ef085fa4_Out_0 = UnityBuildTexture2DStructNoScale(
					_Frame);
				float4 _SampleTexture2D_cccaafae547644f886264237218c117d_RGBA_0 = SAMPLE_TEXTURE2D(
					_Property_07a4a1af592d47dca5c43a77ef085fa4_Out_0.tex,
					_Property_07a4a1af592d47dca5c43a77ef085fa4_Out_0.samplerstate,
					_Property_07a4a1af592d47dca5c43a77ef085fa4_Out_0.GetTransformedUV(IN.uv0.xy));
				float _SampleTexture2D_cccaafae547644f886264237218c117d_R_4 =
					_SampleTexture2D_cccaafae547644f886264237218c117d_RGBA_0.r;
				float _SampleTexture2D_cccaafae547644f886264237218c117d_G_5 =
					_SampleTexture2D_cccaafae547644f886264237218c117d_RGBA_0.g;
				float _SampleTexture2D_cccaafae547644f886264237218c117d_B_6 =
					_SampleTexture2D_cccaafae547644f886264237218c117d_RGBA_0.b;
				float _SampleTexture2D_cccaafae547644f886264237218c117d_A_7 =
					_SampleTexture2D_cccaafae547644f886264237218c117d_RGBA_0.a;
				float4 _Lerp_ab02372f6d574e1bb5f4fa3042c3d3ef_Out_3;
				Unity_Lerp_float4(_SampleTexture2D_48cadbb9084f4d1d9e22a8129e005c38_RGBA_0,
				                  _SampleTexture2D_cccaafae547644f886264237218c117d_RGBA_0,
				                  (_SampleTexture2D_cccaafae547644f886264237218c117d_A_7.xxxx),
				                  _Lerp_ab02372f6d574e1bb5f4fa3042c3d3ef_Out_3);
				UnityTexture2D _Property_ab38279b3cd940748ca4dd1475149aa5_Out_0 = UnityBuildTexture2DStructNoScale(
					_NameTag);
				float4 _SampleTexture2D_a66c2c8c7e0745de8a3863f7344a711d_RGBA_0 = SAMPLE_TEXTURE2D(
					_Property_ab38279b3cd940748ca4dd1475149aa5_Out_0.tex,
					_Property_ab38279b3cd940748ca4dd1475149aa5_Out_0.samplerstate,
					_Property_ab38279b3cd940748ca4dd1475149aa5_Out_0.GetTransformedUV(IN.uv0.xy));
				float _SampleTexture2D_a66c2c8c7e0745de8a3863f7344a711d_R_4 =
					_SampleTexture2D_a66c2c8c7e0745de8a3863f7344a711d_RGBA_0.r;
				float _SampleTexture2D_a66c2c8c7e0745de8a3863f7344a711d_G_5 =
					_SampleTexture2D_a66c2c8c7e0745de8a3863f7344a711d_RGBA_0.g;
				float _SampleTexture2D_a66c2c8c7e0745de8a3863f7344a711d_B_6 =
					_SampleTexture2D_a66c2c8c7e0745de8a3863f7344a711d_RGBA_0.b;
				float _SampleTexture2D_a66c2c8c7e0745de8a3863f7344a711d_A_7 =
					_SampleTexture2D_a66c2c8c7e0745de8a3863f7344a711d_RGBA_0.a;
				float4 _Lerp_e4228b01a251412fa8221310994cfa20_Out_3;
				Unity_Lerp_float4(_Lerp_ab02372f6d574e1bb5f4fa3042c3d3ef_Out_3,
				                  _SampleTexture2D_a66c2c8c7e0745de8a3863f7344a711d_RGBA_0,
				                  (_SampleTexture2D_a66c2c8c7e0745de8a3863f7344a711d_A_7.xxxx),
				                  _Lerp_e4228b01a251412fa8221310994cfa20_Out_3);
				UnityTexture2D _Property_c60a2f30295b407084b75ccc59c22fa8_Out_0 = UnityBuildTexture2DStructNoScale(
					_AdjectivePattern);
				float4 _SampleTexture2D_161fad2c37a948679d86596bb185f422_RGBA_0 = SAMPLE_TEXTURE2D(
					_Property_c60a2f30295b407084b75ccc59c22fa8_Out_0.tex,
					_Property_c60a2f30295b407084b75ccc59c22fa8_Out_0.samplerstate,
					_Property_c60a2f30295b407084b75ccc59c22fa8_Out_0.GetTransformedUV(IN.uv0.xy));
				float _SampleTexture2D_161fad2c37a948679d86596bb185f422_R_4 =
					_SampleTexture2D_161fad2c37a948679d86596bb185f422_RGBA_0.r;
				float _SampleTexture2D_161fad2c37a948679d86596bb185f422_G_5 =
					_SampleTexture2D_161fad2c37a948679d86596bb185f422_RGBA_0.g;
				float _SampleTexture2D_161fad2c37a948679d86596bb185f422_B_6 =
					_SampleTexture2D_161fad2c37a948679d86596bb185f422_RGBA_0.b;
				float _SampleTexture2D_161fad2c37a948679d86596bb185f422_A_7 =
					_SampleTexture2D_161fad2c37a948679d86596bb185f422_RGBA_0.a;
				UnityTexture2D _Property_b0d8535f94d748cfbf54f7b3a287e751_Out_0 = UnityBuildTexture2DStructNoScale(
					_FrameShapeMask);
				float4 _SampleTexture2D_13c02885e0f047a798551f250463dbb5_RGBA_0 = SAMPLE_TEXTURE2D(
					_Property_b0d8535f94d748cfbf54f7b3a287e751_Out_0.tex,
					_Property_b0d8535f94d748cfbf54f7b3a287e751_Out_0.samplerstate,
					_Property_b0d8535f94d748cfbf54f7b3a287e751_Out_0.GetTransformedUV(IN.uv0.xy));
				float _SampleTexture2D_13c02885e0f047a798551f250463dbb5_R_4 =
					_SampleTexture2D_13c02885e0f047a798551f250463dbb5_RGBA_0.r;
				float _SampleTexture2D_13c02885e0f047a798551f250463dbb5_G_5 =
					_SampleTexture2D_13c02885e0f047a798551f250463dbb5_RGBA_0.g;
				float _SampleTexture2D_13c02885e0f047a798551f250463dbb5_B_6 =
					_SampleTexture2D_13c02885e0f047a798551f250463dbb5_RGBA_0.b;
				float _SampleTexture2D_13c02885e0f047a798551f250463dbb5_A_7 =
					_SampleTexture2D_13c02885e0f047a798551f250463dbb5_RGBA_0.a;
				float _Multiply_687192210f1a4cbf81c78436bc291c7f_Out_2;
				Unity_Multiply_float_float(_SampleTexture2D_161fad2c37a948679d86596bb185f422_A_7,
				                           _SampleTexture2D_13c02885e0f047a798551f250463dbb5_A_7,
				                           _Multiply_687192210f1a4cbf81c78436bc291c7f_Out_2);
				float _Property_ab766b752fbf48f9b5236ea91d222c0d_Out_0 = _Adjective_Amount;
				float _Multiply_d3730c24842142beb2df4ae6bb5c1ac8_Out_2;
				Unity_Multiply_float_float(_Multiply_687192210f1a4cbf81c78436bc291c7f_Out_2,
				                           _Property_ab766b752fbf48f9b5236ea91d222c0d_Out_0,
				                           _Multiply_d3730c24842142beb2df4ae6bb5c1ac8_Out_2);
				float4 _Blend_3213240eae8840c99465ac6e19db00e9_Out_2;
				Unity_Blend_Overlay_float4(_Lerp_e4228b01a251412fa8221310994cfa20_Out_3,
				                           _SampleTexture2D_161fad2c37a948679d86596bb185f422_RGBA_0,
				                           _Blend_3213240eae8840c99465ac6e19db00e9_Out_2,
				                           _Multiply_d3730c24842142beb2df4ae6bb5c1ac8_Out_2);
				float4 Color_615e32773a2240fe9124cdb46ef3d90a = IsGammaSpace()
					                                                ? float4(1, 1, 1, 0)
					                                                : float4(SRGBToLinear(float3(1, 1, 1)), 0);
				UnityTexture2D _Property_cc255972cd844a1cbc7eeb9e7c4da77a_Out_0 = UnityBuildTexture2DStructNoScale(
					_Plus_Image);
				float2 _Property_39ae1007383e4909a2b790a1f8e2dbce_Out_0 = _FX_Scale;
				float2 _Property_746e380bb83f42f6aa7f71c4694dda62_Out_0 = _FX_Offset;
				float2 _TilingAndOffset_c836f6ac64514dff9324963f05decbb7_Out_3;
				Unity_TilingAndOffset_float(IN.uv0.xy, _Property_39ae1007383e4909a2b790a1f8e2dbce_Out_0,
				                            _Property_746e380bb83f42f6aa7f71c4694dda62_Out_0,
				                            _TilingAndOffset_c836f6ac64514dff9324963f05decbb7_Out_3);
				float4 _SampleTexture2D_a7d08c4034114bb3a2198cd36e767173_RGBA_0 = SAMPLE_TEXTURE2D(
					_Property_cc255972cd844a1cbc7eeb9e7c4da77a_Out_0.tex,
					_Property_cc255972cd844a1cbc7eeb9e7c4da77a_Out_0.samplerstate,
					_Property_cc255972cd844a1cbc7eeb9e7c4da77a_Out_0.GetTransformedUV(
						_TilingAndOffset_c836f6ac64514dff9324963f05decbb7_Out_3));
				float _SampleTexture2D_a7d08c4034114bb3a2198cd36e767173_R_4 =
					_SampleTexture2D_a7d08c4034114bb3a2198cd36e767173_RGBA_0.r;
				float _SampleTexture2D_a7d08c4034114bb3a2198cd36e767173_G_5 =
					_SampleTexture2D_a7d08c4034114bb3a2198cd36e767173_RGBA_0.g;
				float _SampleTexture2D_a7d08c4034114bb3a2198cd36e767173_B_6 =
					_SampleTexture2D_a7d08c4034114bb3a2198cd36e767173_RGBA_0.b;
				float _SampleTexture2D_a7d08c4034114bb3a2198cd36e767173_A_7 =
					_SampleTexture2D_a7d08c4034114bb3a2198cd36e767173_RGBA_0.a;
				float _Multiply_52a77a4627a44944926a07dfcd54df6d_Out_2;
				Unity_Multiply_float_float(_SampleTexture2D_13c02885e0f047a798551f250463dbb5_A_7,
				                           _SampleTexture2D_a7d08c4034114bb3a2198cd36e767173_A_7,
				                           _Multiply_52a77a4627a44944926a07dfcd54df6d_Out_2);
				float _Property_8120342a1bc842a8bece2f112b4a24ad_Out_0 = _Plus_Amount;
				float _Multiply_2df54a93e6204b67adb342bf2fa9025b_Out_2;
				Unity_Multiply_float_float(_Multiply_52a77a4627a44944926a07dfcd54df6d_Out_2,
				                           _Property_8120342a1bc842a8bece2f112b4a24ad_Out_0,
				                           _Multiply_2df54a93e6204b67adb342bf2fa9025b_Out_2);
				float4 _Blend_4fdd1316f4604b8a957e5a3c232f58f0_Out_2;
				Unity_Blend_Overlay_float4(_Blend_3213240eae8840c99465ac6e19db00e9_Out_2,
				                           Color_615e32773a2240fe9124cdb46ef3d90a,
				                           _Blend_4fdd1316f4604b8a957e5a3c232f58f0_Out_2,
				                           _Multiply_2df54a93e6204b67adb342bf2fa9025b_Out_2);
				float4 _Branch_d58bdd5e773540cba057d8eeabdc8418_Out_3;
				Unity_Branch_float4(_Property_6c605c7282b547b9b456952c497ed278_Out_0,
				                    _Blend_4fdd1316f4604b8a957e5a3c232f58f0_Out_2,
				                    _Blend_3213240eae8840c99465ac6e19db00e9_Out_2,
				                    _Branch_d58bdd5e773540cba057d8eeabdc8418_Out_3);
				UnityTexture2D _Property_812ec6fee4c247598b56fedde9fe8d82_Out_0 = UnityBuildTexture2DStructNoScale(
					_GradeTag);
				float2 _TilingAndOffset_7cd239011c3445d0bb0db853afbdd446_Out_3;
				Unity_TilingAndOffset_float(IN.uv0.xy, float2(1, 1), float2(0.02, 0),
				                            _TilingAndOffset_7cd239011c3445d0bb0db853afbdd446_Out_3);
				float4 _SampleTexture2D_69edc517709d48e19a56fb154356b81e_RGBA_0 = SAMPLE_TEXTURE2D(
					_Property_812ec6fee4c247598b56fedde9fe8d82_Out_0.tex,
					_Property_812ec6fee4c247598b56fedde9fe8d82_Out_0.samplerstate,
					_Property_812ec6fee4c247598b56fedde9fe8d82_Out_0.GetTransformedUV(
						_TilingAndOffset_7cd239011c3445d0bb0db853afbdd446_Out_3));
				float _SampleTexture2D_69edc517709d48e19a56fb154356b81e_R_4 =
					_SampleTexture2D_69edc517709d48e19a56fb154356b81e_RGBA_0.r;
				float _SampleTexture2D_69edc517709d48e19a56fb154356b81e_G_5 =
					_SampleTexture2D_69edc517709d48e19a56fb154356b81e_RGBA_0.g;
				float _SampleTexture2D_69edc517709d48e19a56fb154356b81e_B_6 =
					_SampleTexture2D_69edc517709d48e19a56fb154356b81e_RGBA_0.b;
				float _SampleTexture2D_69edc517709d48e19a56fb154356b81e_A_7 =
					_SampleTexture2D_69edc517709d48e19a56fb154356b81e_RGBA_0.a;
				float _Property_06cf96a72f4343b98a89f7355722c8e4_Out_0 = _Grade_Amount;
				float _Multiply_6ad75cd7329f4ab2bb0e5a0125a67d4e_Out_2;
				Unity_Multiply_float_float(_SampleTexture2D_69edc517709d48e19a56fb154356b81e_A_7,
				                           _Property_06cf96a72f4343b98a89f7355722c8e4_Out_0,
				                           _Multiply_6ad75cd7329f4ab2bb0e5a0125a67d4e_Out_2);
				float4 _Add_678bd7b7e0d9438e8ab8ce493727c798_Out_2;
				Unity_Add_float4(_Branch_d58bdd5e773540cba057d8eeabdc8418_Out_3,
				                 (_Multiply_6ad75cd7329f4ab2bb0e5a0125a67d4e_Out_2.xxxx),
				                 _Add_678bd7b7e0d9438e8ab8ce493727c798_Out_2);
				float _Split_49267e3c5aec45e082f03597de8ee5bf_R_1 = _Add_678bd7b7e0d9438e8ab8ce493727c798_Out_2[0];
				float _Split_49267e3c5aec45e082f03597de8ee5bf_G_2 = _Add_678bd7b7e0d9438e8ab8ce493727c798_Out_2[1];
				float _Split_49267e3c5aec45e082f03597de8ee5bf_B_3 = _Add_678bd7b7e0d9438e8ab8ce493727c798_Out_2[2];
				float _Split_49267e3c5aec45e082f03597de8ee5bf_A_4 = _Add_678bd7b7e0d9438e8ab8ce493727c798_Out_2[3];
				surface.Alpha = _Split_49267e3c5aec45e082f03597de8ee5bf_A_4;
				return surface;
			}

			// --------------------------------------------------
			// Build Graph Inputs

			VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
			{
				VertexDescriptionInputs output;
				ZERO_INITIALIZE(VertexDescriptionInputs, output);

				output.ObjectSpaceNormal = input.normalOS;
				output.ObjectSpaceTangent = input.tangentOS.xyz;
				output.ObjectSpacePosition = input.positionOS;

				return output;
			}

			SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
			{
				SurfaceDescriptionInputs output;
				ZERO_INITIALIZE(SurfaceDescriptionInputs, output);


				output.uv0 = input.texCoord0;
				#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN                output.FaceSign =                                   IS_FRONT_VFACE(input.cullFace, true, false);
				#else
				#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
				#endif
				#undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

				return output;
			}


			// --------------------------------------------------
			// Main

			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/SelectionPickingPass.hlsl"
			ENDHLSL
		}
		Pass
		{
			Name "Sprite Unlit"
			Tags
			{
				"LightMode" = "UniversalForward"
			}

			// Render State
			Cull Off
			Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
			ZTest LEqual
			ZWrite Off

			Stencil
			{
				Ref [_Stencil]
				Comp [_StencilComp]
				Pass [_StencilOp]
				ReadMask [_StencilReadMask]
				WriteMask [_StencilWriteMask]
			}
			ColorMask [_ColorMask]

			// Debug
			// <None>

			// --------------------------------------------------
			// Pass

			HLSLPROGRAM
			// Pragmas
			#pragma target 2.0
			#pragma exclude_renderers d3d11_9x
			#pragma vertex vert
			#pragma fragment frag

			// DotsInstancingOptions: <None>
			// HybridV1InjectedBuiltinProperties: <None>

			// Keywords
			#pragma multi_compile_fragment _ DEBUG_DISPLAY
			// GraphKeywords: <None>

			// Defines
			#define _SURFACE_TYPE_TRANSPARENT 1
			#define ATTRIBUTES_NEED_NORMAL
			#define ATTRIBUTES_NEED_TANGENT
			#define ATTRIBUTES_NEED_TEXCOORD0
			#define ATTRIBUTES_NEED_COLOR
			#define VARYINGS_NEED_POSITION_WS
			#define VARYINGS_NEED_TEXCOORD0
			#define VARYINGS_NEED_COLOR
			#define FEATURES_GRAPH_VERTEX
			/* WARNING: $splice Could not find named fragment 'PassInstancing' */
			#define SHADERPASS SHADERPASS_SPRITEFORWARD
			/* WARNING: $splice Could not find named fragment 'DotsInstancingVars' */

			// Includes
			/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreInclude' */

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			// --------------------------------------------------
			// Structs and Packing

			/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */

			struct Attributes
			{
				float3 positionOS : POSITION;
				float3 normalOS : NORMAL;
				float4 tangentOS : TANGENT;
				float4 uv0 : TEXCOORD0;
				float4 color : COLOR;
				#if UNITY_ANY_INSTANCING_ENABLED
                     uint instanceID : INSTANCEID_SEMANTIC;
				#endif
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float3 positionWS;
				float4 texCoord0;
				float4 color;
				#if UNITY_ANY_INSTANCING_ENABLED
                     uint instanceID : CUSTOM_INSTANCE_ID;
				#endif
				#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                     uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
				#endif
				#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                     uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
				#endif
				#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                     FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
				#endif
			};

			struct SurfaceDescriptionInputs
			{
				float4 uv0;
			};

			struct VertexDescriptionInputs
			{
				float3 ObjectSpaceNormal;
				float3 ObjectSpaceTangent;
				float3 ObjectSpacePosition;
			};

			struct PackedVaryings
			{
				float4 positionCS : SV_POSITION;
				float3 interp0 : INTERP0;
				float4 interp1 : INTERP1;
				float4 interp2 : INTERP2;
				#if UNITY_ANY_INSTANCING_ENABLED
                     uint instanceID : CUSTOM_INSTANCE_ID;
				#endif
				#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                     uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
				#endif
				#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                     uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
				#endif
				#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                     FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
				#endif
			};

			PackedVaryings PackVaryings(Varyings input)
			{
				PackedVaryings output;
				ZERO_INITIALIZE(PackedVaryings, output);
				output.positionCS = input.positionCS;
				output.interp0.xyz = input.positionWS;
				output.interp1.xyzw = input.texCoord0;
				output.interp2.xyzw = input.color;
				#if UNITY_ANY_INSTANCING_ENABLED
                    output.instanceID = input.instanceID;
				#endif
				#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                    output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
				#endif
				#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                    output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
				#endif
				#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                    output.cullFace = input.cullFace;
				#endif
				return output;
			}

			Varyings UnpackVaryings(PackedVaryings input)
			{
				Varyings output;
				output.positionCS = input.positionCS;
				output.positionWS = input.interp0.xyz;
				output.texCoord0 = input.interp1.xyzw;
				output.color = input.interp2.xyzw;
				#if UNITY_ANY_INSTANCING_ENABLED
                    output.instanceID = input.instanceID;
				#endif
				#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                    output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
				#endif
				#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                    output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
				#endif
				#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                    output.cullFace = input.cullFace;
				#endif
				return output;
			}


			// --------------------------------------------------
			// Graph

			// Graph Properties
			CBUFFER_START(UnityPerMaterial)
			float4 _Texture2DAsset_0e3260f7159a476b8fe0d283c3c6f5b7_Out_0_TexelSize;
			float4 _Frame_TexelSize;
			float4 _NameTag_TexelSize;
			float4 _AdjectivePattern_TexelSize;
			float4 _FrameShapeMask_TexelSize;
			float4 _Plus_Image_TexelSize;
			float4 _GradeTag_TexelSize;
			float _Plus_Amount;
			float _Adjective_Amount;
			float _Grade_Amount;
			float _Plus_Indicator;
			float4 _MainTex_TexelSize;
			float2 _FX_Scale;
			float2 _FX_Offset;
			CBUFFER_END

			// Object and Global properties
			SAMPLER(SamplerState_Linear_Repeat);
			TEXTURE2D(_Texture2DAsset_0e3260f7159a476b8fe0d283c3c6f5b7_Out_0);
			SAMPLER(sampler_Texture2DAsset_0e3260f7159a476b8fe0d283c3c6f5b7_Out_0);
			TEXTURE2D(_Frame);
			SAMPLER(sampler_Frame);
			TEXTURE2D(_NameTag);
			SAMPLER(sampler_NameTag);
			TEXTURE2D(_AdjectivePattern);
			SAMPLER(sampler_AdjectivePattern);
			TEXTURE2D(_FrameShapeMask);
			SAMPLER(sampler_FrameShapeMask);
			TEXTURE2D(_Plus_Image);
			SAMPLER(sampler_Plus_Image);
			TEXTURE2D(_GradeTag);
			SAMPLER(sampler_GradeTag);
			TEXTURE2D(_MainTex);
			SAMPLER(sampler_MainTex);

			// Graph Includes
			// GraphIncludes: <None>

			// -- Property used by ScenePickingPass
			#ifdef SCENEPICKINGPASS
                float4 _SelectionID;
			#endif

			// -- Properties used by SceneSelectionPass
			#ifdef SCENESELECTIONPASS
                int _ObjectId;
                int _PassValue;
			#endif

			// Graph Functions

			void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
			{
				Out = lerp(A, B, T);
			}

			void Unity_Multiply_float_float(float A, float B, out float Out)
			{
				Out = A * B;
			}

			void Unity_Blend_Overlay_float4(float4 Base, float4 Blend, out float4 Out, float Opacity)
			{
				float4 result1 = 1.0 - 2.0 * (1.0 - Base) * (1.0 - Blend);
				float4 result2 = 2.0 * Base * Blend;
				float4 zeroOrOne = step(Base, 0.5);
				Out = result2 * zeroOrOne + (1 - zeroOrOne) * result1;
				Out = lerp(Base, Out, Opacity);
			}

			void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
			{
				Out = UV * Tiling + Offset;
			}

			void Unity_Branch_float4(float Predicate, float4 True, float4 False, out float4 Out)
			{
				Out = Predicate ? True : False;
			}

			void Unity_Add_float4(float4 A, float4 B, out float4 Out)
			{
				Out = A + B;
			}

			/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */

			// Graph Vertex
			struct VertexDescription
			{
				float3 Position;
				float3 Normal;
				float3 Tangent;
			};

			VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
			{
				VertexDescription description = (VertexDescription)0;
				description.Position = IN.ObjectSpacePosition;
				description.Normal = IN.ObjectSpaceNormal;
				description.Tangent = IN.ObjectSpaceTangent;
				return description;
			}

			#ifdef FEATURES_GRAPH_VERTEX
			Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
			{
				return output;
			}

			#define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
			#endif

			// Graph Pixel
			struct SurfaceDescription
			{
				float3 BaseColor;
				float Alpha;
			};

			SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
			{
				SurfaceDescription surface = (SurfaceDescription)0;
				float _Property_6c605c7282b547b9b456952c497ed278_Out_0 = _Plus_Indicator;
				float4 _SampleTexture2D_48cadbb9084f4d1d9e22a8129e005c38_RGBA_0 = SAMPLE_TEXTURE2D(
					UnityBuildTexture2DStructNoScale(_Texture2DAsset_0e3260f7159a476b8fe0d283c3c6f5b7_Out_0).tex,
					UnityBuildTexture2DStructNoScale(_Texture2DAsset_0e3260f7159a476b8fe0d283c3c6f5b7_Out_0).
					samplerstate,
					UnityBuildTexture2DStructNoScale(_Texture2DAsset_0e3260f7159a476b8fe0d283c3c6f5b7_Out_0).
					GetTransformedUV(IN.uv0.xy));
				float _SampleTexture2D_48cadbb9084f4d1d9e22a8129e005c38_R_4 =
					_SampleTexture2D_48cadbb9084f4d1d9e22a8129e005c38_RGBA_0.r;
				float _SampleTexture2D_48cadbb9084f4d1d9e22a8129e005c38_G_5 =
					_SampleTexture2D_48cadbb9084f4d1d9e22a8129e005c38_RGBA_0.g;
				float _SampleTexture2D_48cadbb9084f4d1d9e22a8129e005c38_B_6 =
					_SampleTexture2D_48cadbb9084f4d1d9e22a8129e005c38_RGBA_0.b;
				float _SampleTexture2D_48cadbb9084f4d1d9e22a8129e005c38_A_7 =
					_SampleTexture2D_48cadbb9084f4d1d9e22a8129e005c38_RGBA_0.a;
				UnityTexture2D _Property_07a4a1af592d47dca5c43a77ef085fa4_Out_0 = UnityBuildTexture2DStructNoScale(
					_Frame);
				float4 _SampleTexture2D_cccaafae547644f886264237218c117d_RGBA_0 = SAMPLE_TEXTURE2D(
					_Property_07a4a1af592d47dca5c43a77ef085fa4_Out_0.tex,
					_Property_07a4a1af592d47dca5c43a77ef085fa4_Out_0.samplerstate,
					_Property_07a4a1af592d47dca5c43a77ef085fa4_Out_0.GetTransformedUV(IN.uv0.xy));
				float _SampleTexture2D_cccaafae547644f886264237218c117d_R_4 =
					_SampleTexture2D_cccaafae547644f886264237218c117d_RGBA_0.r;
				float _SampleTexture2D_cccaafae547644f886264237218c117d_G_5 =
					_SampleTexture2D_cccaafae547644f886264237218c117d_RGBA_0.g;
				float _SampleTexture2D_cccaafae547644f886264237218c117d_B_6 =
					_SampleTexture2D_cccaafae547644f886264237218c117d_RGBA_0.b;
				float _SampleTexture2D_cccaafae547644f886264237218c117d_A_7 =
					_SampleTexture2D_cccaafae547644f886264237218c117d_RGBA_0.a;
				float4 _Lerp_ab02372f6d574e1bb5f4fa3042c3d3ef_Out_3;
				Unity_Lerp_float4(_SampleTexture2D_48cadbb9084f4d1d9e22a8129e005c38_RGBA_0,
				                  _SampleTexture2D_cccaafae547644f886264237218c117d_RGBA_0,
				                  (_SampleTexture2D_cccaafae547644f886264237218c117d_A_7.xxxx),
				                  _Lerp_ab02372f6d574e1bb5f4fa3042c3d3ef_Out_3);
				UnityTexture2D _Property_ab38279b3cd940748ca4dd1475149aa5_Out_0 = UnityBuildTexture2DStructNoScale(
					_NameTag);
				float4 _SampleTexture2D_a66c2c8c7e0745de8a3863f7344a711d_RGBA_0 = SAMPLE_TEXTURE2D(
					_Property_ab38279b3cd940748ca4dd1475149aa5_Out_0.tex,
					_Property_ab38279b3cd940748ca4dd1475149aa5_Out_0.samplerstate,
					_Property_ab38279b3cd940748ca4dd1475149aa5_Out_0.GetTransformedUV(IN.uv0.xy));
				float _SampleTexture2D_a66c2c8c7e0745de8a3863f7344a711d_R_4 =
					_SampleTexture2D_a66c2c8c7e0745de8a3863f7344a711d_RGBA_0.r;
				float _SampleTexture2D_a66c2c8c7e0745de8a3863f7344a711d_G_5 =
					_SampleTexture2D_a66c2c8c7e0745de8a3863f7344a711d_RGBA_0.g;
				float _SampleTexture2D_a66c2c8c7e0745de8a3863f7344a711d_B_6 =
					_SampleTexture2D_a66c2c8c7e0745de8a3863f7344a711d_RGBA_0.b;
				float _SampleTexture2D_a66c2c8c7e0745de8a3863f7344a711d_A_7 =
					_SampleTexture2D_a66c2c8c7e0745de8a3863f7344a711d_RGBA_0.a;
				float4 _Lerp_e4228b01a251412fa8221310994cfa20_Out_3;
				Unity_Lerp_float4(_Lerp_ab02372f6d574e1bb5f4fa3042c3d3ef_Out_3,
				                  _SampleTexture2D_a66c2c8c7e0745de8a3863f7344a711d_RGBA_0,
				                  (_SampleTexture2D_a66c2c8c7e0745de8a3863f7344a711d_A_7.xxxx),
				                  _Lerp_e4228b01a251412fa8221310994cfa20_Out_3);
				UnityTexture2D _Property_c60a2f30295b407084b75ccc59c22fa8_Out_0 = UnityBuildTexture2DStructNoScale(
					_AdjectivePattern);
				float4 _SampleTexture2D_161fad2c37a948679d86596bb185f422_RGBA_0 = SAMPLE_TEXTURE2D(
					_Property_c60a2f30295b407084b75ccc59c22fa8_Out_0.tex,
					_Property_c60a2f30295b407084b75ccc59c22fa8_Out_0.samplerstate,
					_Property_c60a2f30295b407084b75ccc59c22fa8_Out_0.GetTransformedUV(IN.uv0.xy));
				float _SampleTexture2D_161fad2c37a948679d86596bb185f422_R_4 =
					_SampleTexture2D_161fad2c37a948679d86596bb185f422_RGBA_0.r;
				float _SampleTexture2D_161fad2c37a948679d86596bb185f422_G_5 =
					_SampleTexture2D_161fad2c37a948679d86596bb185f422_RGBA_0.g;
				float _SampleTexture2D_161fad2c37a948679d86596bb185f422_B_6 =
					_SampleTexture2D_161fad2c37a948679d86596bb185f422_RGBA_0.b;
				float _SampleTexture2D_161fad2c37a948679d86596bb185f422_A_7 =
					_SampleTexture2D_161fad2c37a948679d86596bb185f422_RGBA_0.a;
				UnityTexture2D _Property_b0d8535f94d748cfbf54f7b3a287e751_Out_0 = UnityBuildTexture2DStructNoScale(
					_FrameShapeMask);
				float4 _SampleTexture2D_13c02885e0f047a798551f250463dbb5_RGBA_0 = SAMPLE_TEXTURE2D(
					_Property_b0d8535f94d748cfbf54f7b3a287e751_Out_0.tex,
					_Property_b0d8535f94d748cfbf54f7b3a287e751_Out_0.samplerstate,
					_Property_b0d8535f94d748cfbf54f7b3a287e751_Out_0.GetTransformedUV(IN.uv0.xy));
				float _SampleTexture2D_13c02885e0f047a798551f250463dbb5_R_4 =
					_SampleTexture2D_13c02885e0f047a798551f250463dbb5_RGBA_0.r;
				float _SampleTexture2D_13c02885e0f047a798551f250463dbb5_G_5 =
					_SampleTexture2D_13c02885e0f047a798551f250463dbb5_RGBA_0.g;
				float _SampleTexture2D_13c02885e0f047a798551f250463dbb5_B_6 =
					_SampleTexture2D_13c02885e0f047a798551f250463dbb5_RGBA_0.b;
				float _SampleTexture2D_13c02885e0f047a798551f250463dbb5_A_7 =
					_SampleTexture2D_13c02885e0f047a798551f250463dbb5_RGBA_0.a;
				float _Multiply_687192210f1a4cbf81c78436bc291c7f_Out_2;
				Unity_Multiply_float_float(_SampleTexture2D_161fad2c37a948679d86596bb185f422_A_7,
				                           _SampleTexture2D_13c02885e0f047a798551f250463dbb5_A_7,
				                           _Multiply_687192210f1a4cbf81c78436bc291c7f_Out_2);
				float _Property_ab766b752fbf48f9b5236ea91d222c0d_Out_0 = _Adjective_Amount;
				float _Multiply_d3730c24842142beb2df4ae6bb5c1ac8_Out_2;
				Unity_Multiply_float_float(_Multiply_687192210f1a4cbf81c78436bc291c7f_Out_2,
				                           _Property_ab766b752fbf48f9b5236ea91d222c0d_Out_0,
				                           _Multiply_d3730c24842142beb2df4ae6bb5c1ac8_Out_2);
				float4 _Blend_3213240eae8840c99465ac6e19db00e9_Out_2;
				Unity_Blend_Overlay_float4(_Lerp_e4228b01a251412fa8221310994cfa20_Out_3,
				                           _SampleTexture2D_161fad2c37a948679d86596bb185f422_RGBA_0,
				                           _Blend_3213240eae8840c99465ac6e19db00e9_Out_2,
				                           _Multiply_d3730c24842142beb2df4ae6bb5c1ac8_Out_2);
				float4 Color_615e32773a2240fe9124cdb46ef3d90a = IsGammaSpace()
					                                                ? float4(1, 1, 1, 0)
					                                                : float4(SRGBToLinear(float3(1, 1, 1)), 0);
				UnityTexture2D _Property_cc255972cd844a1cbc7eeb9e7c4da77a_Out_0 = UnityBuildTexture2DStructNoScale(
					_Plus_Image);
				float2 _Property_39ae1007383e4909a2b790a1f8e2dbce_Out_0 = _FX_Scale;
				float2 _Property_746e380bb83f42f6aa7f71c4694dda62_Out_0 = _FX_Offset;
				float2 _TilingAndOffset_c836f6ac64514dff9324963f05decbb7_Out_3;
				Unity_TilingAndOffset_float(IN.uv0.xy, _Property_39ae1007383e4909a2b790a1f8e2dbce_Out_0,
				                            _Property_746e380bb83f42f6aa7f71c4694dda62_Out_0,
				                            _TilingAndOffset_c836f6ac64514dff9324963f05decbb7_Out_3);
				float4 _SampleTexture2D_a7d08c4034114bb3a2198cd36e767173_RGBA_0 = SAMPLE_TEXTURE2D(
					_Property_cc255972cd844a1cbc7eeb9e7c4da77a_Out_0.tex,
					_Property_cc255972cd844a1cbc7eeb9e7c4da77a_Out_0.samplerstate,
					_Property_cc255972cd844a1cbc7eeb9e7c4da77a_Out_0.GetTransformedUV(
						_TilingAndOffset_c836f6ac64514dff9324963f05decbb7_Out_3));
				float _SampleTexture2D_a7d08c4034114bb3a2198cd36e767173_R_4 =
					_SampleTexture2D_a7d08c4034114bb3a2198cd36e767173_RGBA_0.r;
				float _SampleTexture2D_a7d08c4034114bb3a2198cd36e767173_G_5 =
					_SampleTexture2D_a7d08c4034114bb3a2198cd36e767173_RGBA_0.g;
				float _SampleTexture2D_a7d08c4034114bb3a2198cd36e767173_B_6 =
					_SampleTexture2D_a7d08c4034114bb3a2198cd36e767173_RGBA_0.b;
				float _SampleTexture2D_a7d08c4034114bb3a2198cd36e767173_A_7 =
					_SampleTexture2D_a7d08c4034114bb3a2198cd36e767173_RGBA_0.a;
				float _Multiply_52a77a4627a44944926a07dfcd54df6d_Out_2;
				Unity_Multiply_float_float(_SampleTexture2D_13c02885e0f047a798551f250463dbb5_A_7,
				                           _SampleTexture2D_a7d08c4034114bb3a2198cd36e767173_A_7,
				                           _Multiply_52a77a4627a44944926a07dfcd54df6d_Out_2);
				float _Property_8120342a1bc842a8bece2f112b4a24ad_Out_0 = _Plus_Amount;
				float _Multiply_2df54a93e6204b67adb342bf2fa9025b_Out_2;
				Unity_Multiply_float_float(_Multiply_52a77a4627a44944926a07dfcd54df6d_Out_2,
				                           _Property_8120342a1bc842a8bece2f112b4a24ad_Out_0,
				                           _Multiply_2df54a93e6204b67adb342bf2fa9025b_Out_2);
				float4 _Blend_4fdd1316f4604b8a957e5a3c232f58f0_Out_2;
				Unity_Blend_Overlay_float4(_Blend_3213240eae8840c99465ac6e19db00e9_Out_2,
				                           Color_615e32773a2240fe9124cdb46ef3d90a,
				                           _Blend_4fdd1316f4604b8a957e5a3c232f58f0_Out_2,
				                           _Multiply_2df54a93e6204b67adb342bf2fa9025b_Out_2);
				float4 _Branch_d58bdd5e773540cba057d8eeabdc8418_Out_3;
				Unity_Branch_float4(_Property_6c605c7282b547b9b456952c497ed278_Out_0,
				                    _Blend_4fdd1316f4604b8a957e5a3c232f58f0_Out_2,
				                    _Blend_3213240eae8840c99465ac6e19db00e9_Out_2,
				                    _Branch_d58bdd5e773540cba057d8eeabdc8418_Out_3);
				UnityTexture2D _Property_812ec6fee4c247598b56fedde9fe8d82_Out_0 = UnityBuildTexture2DStructNoScale(
					_GradeTag);
				float2 _TilingAndOffset_7cd239011c3445d0bb0db853afbdd446_Out_3;
				Unity_TilingAndOffset_float(IN.uv0.xy, float2(1, 1), float2(0.02, 0),
				                            _TilingAndOffset_7cd239011c3445d0bb0db853afbdd446_Out_3);
				float4 _SampleTexture2D_69edc517709d48e19a56fb154356b81e_RGBA_0 = SAMPLE_TEXTURE2D(
					_Property_812ec6fee4c247598b56fedde9fe8d82_Out_0.tex,
					_Property_812ec6fee4c247598b56fedde9fe8d82_Out_0.samplerstate,
					_Property_812ec6fee4c247598b56fedde9fe8d82_Out_0.GetTransformedUV(
						_TilingAndOffset_7cd239011c3445d0bb0db853afbdd446_Out_3));
				float _SampleTexture2D_69edc517709d48e19a56fb154356b81e_R_4 =
					_SampleTexture2D_69edc517709d48e19a56fb154356b81e_RGBA_0.r;
				float _SampleTexture2D_69edc517709d48e19a56fb154356b81e_G_5 =
					_SampleTexture2D_69edc517709d48e19a56fb154356b81e_RGBA_0.g;
				float _SampleTexture2D_69edc517709d48e19a56fb154356b81e_B_6 =
					_SampleTexture2D_69edc517709d48e19a56fb154356b81e_RGBA_0.b;
				float _SampleTexture2D_69edc517709d48e19a56fb154356b81e_A_7 =
					_SampleTexture2D_69edc517709d48e19a56fb154356b81e_RGBA_0.a;
				float _Property_06cf96a72f4343b98a89f7355722c8e4_Out_0 = _Grade_Amount;
				float _Multiply_6ad75cd7329f4ab2bb0e5a0125a67d4e_Out_2;
				Unity_Multiply_float_float(_SampleTexture2D_69edc517709d48e19a56fb154356b81e_A_7,
				                           _Property_06cf96a72f4343b98a89f7355722c8e4_Out_0,
				                           _Multiply_6ad75cd7329f4ab2bb0e5a0125a67d4e_Out_2);
				float4 _Add_678bd7b7e0d9438e8ab8ce493727c798_Out_2;
				Unity_Add_float4(_Branch_d58bdd5e773540cba057d8eeabdc8418_Out_3,
				                 (_Multiply_6ad75cd7329f4ab2bb0e5a0125a67d4e_Out_2.xxxx),
				                 _Add_678bd7b7e0d9438e8ab8ce493727c798_Out_2);
				float _Split_49267e3c5aec45e082f03597de8ee5bf_R_1 = _Add_678bd7b7e0d9438e8ab8ce493727c798_Out_2[0];
				float _Split_49267e3c5aec45e082f03597de8ee5bf_G_2 = _Add_678bd7b7e0d9438e8ab8ce493727c798_Out_2[1];
				float _Split_49267e3c5aec45e082f03597de8ee5bf_B_3 = _Add_678bd7b7e0d9438e8ab8ce493727c798_Out_2[2];
				float _Split_49267e3c5aec45e082f03597de8ee5bf_A_4 = _Add_678bd7b7e0d9438e8ab8ce493727c798_Out_2[3];
				surface.BaseColor = (_Add_678bd7b7e0d9438e8ab8ce493727c798_Out_2.xyz);
				surface.Alpha = _Split_49267e3c5aec45e082f03597de8ee5bf_A_4;
				return surface;
			}

			// --------------------------------------------------
			// Build Graph Inputs

			VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
			{
				VertexDescriptionInputs output;
				ZERO_INITIALIZE(VertexDescriptionInputs, output);

				output.ObjectSpaceNormal = input.normalOS;
				output.ObjectSpaceTangent = input.tangentOS.xyz;
				output.ObjectSpacePosition = input.positionOS;

				return output;
			}

			SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
			{
				SurfaceDescriptionInputs output;
				ZERO_INITIALIZE(SurfaceDescriptionInputs, output);


				output.uv0 = input.texCoord0;
				#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN                output.FaceSign =                                   IS_FRONT_VFACE(input.cullFace, true, false);
				#else
				#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
				#endif
				#undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

				return output;
			}


			// --------------------------------------------------
			// Main

			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/2D/ShaderGraph/Includes/SpriteUnlitPass.hlsl"
			ENDHLSL
		}
	}
	CustomEditor "UnityEditor.ShaderGraph.GenericShaderGraphMaterialGUI"
	FallBack "Hidden/Shader Graph/FallbackError"
}