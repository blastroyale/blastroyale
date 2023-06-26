Shader "Effect/PPX_FX/PPX_common_shader"
{
	Properties
	{
		[Enum(UnityEngine.Rendering.CompareFunction)] _Ztest("Ztest", Float) = 4
		[Enum(ON,0,Off,1)]_Zwrite("Zwrite", Float) = 0
		[Enum(UnityEngine.Rendering.CullMode)]_Cull("Cull", Float) = 0
		_add_or_blend("add_or_blend", Range(0 , 1)) = 1
		[NoScaleOffset]_base_texture("base_texture", 2D) = "white" {}
		[Toggle(_USE_CUSTOM2_XYZW_CONTROL_BASE_UV_ON)] _use_custom2_xyzw_control_base_uv("use_custom2_xyzw_control_base_uv", Float) = 0
		_base_uv("base_uv", Vector) = (1,1,0,0)
		_base_speed("base_speed", Vector) = (1,0,1,0)
		[Toggle(_USE_CHANGE_COLOR_ON)] _use_change_color("use_change_color", Float) = 0
		_HUE("HUE", Range(0 , 1)) = 0
		_Saturation("Saturation", Range(-1 , 1)) = 0
		_Value("Value", Range(-1 , 1)) = 0
		_alpha_power("alpha_power", Range(0 , 3)) = 1
		_Fade_distance("Fade_distance", Range(0 , 3)) = 1
		[Toggle(_USE_DOUBEL_PASS_ON)] _use_doubel_pass("use_doubel_pass", Float) = 0
		[HDR]_base_front_color("base_front_color", Color) = (1,1,1,0)
		_base_front_power("base_front_power", Range(0 , 3)) = 1
		[HDR]_base_back_color("base_back_color", Color) = (1,0,0,0)
		_base_back_power("base_back_power", Range(0 , 3)) = 1
		[Toggle(_USE_EMISSIVE_ON)] _use_emissive("use_emissive", Float) = 0
		[NoScaleOffset]_emissive_tex("emissive_tex", 2D) = "white" {}
		[Toggle(_CONNECT_BASE_ALONE_ON)] _connect_base_alone("connect_base_alone", Float) = 0
		_emissive_uv("emissive_uv", Vector) = (1,1,0,0)
		_emissive_speed("emissive_speed", Vector) = (1,0,1,0)
		[HDR]_Emissive_color("Emissive_color", Color) = (1,1,1,0)
		_Emissive_power("Emissive_power", Range(0 , 3)) = 1
		[Toggle(_USE_SECOND_TEX_ON)] _use_second_tex("use_second_tex", Float) = 0
		[Toggle(_USE_DISSOLVE_OR_MUL_ON)] _use_dissolve_or_mul("use_dissolve_or_mul", Float) = 1
		[NoScaleOffset]_dissolve_texture("dissolve_texture", 2D) = "white" {}
		_dissolve_uv("dissolve_uv", Vector) = (1,1,0,0)
		_dissolve_speed("dissolve_speed", Vector) = (1,0,1,0)
		_edge_hardness("edge_hardness", Range(0 , 22)) = 1
		_dissolve("dissolve", Range(0 , 1)) = 1
		[Toggle(_USE_CUSTOM1_X_DISSOLVE_ON)] _use_custom1_x_dissolve("use_custom1_x_dissolve", Float) = 0
		[Toggle(_USE_DISTORT_ON)] _use_distort("use_distort", Float) = 0
		_Distort_tex("Distort_tex", 2D) = "white" {}
		_Distort_mask("Distort_mask", 2D) = "white" {}
		_distort_uv("distort_uv", Vector) = (1,1,0,0)
		_distort_speed("distort_speed", Vector) = (1,0,1,0)
		_distort_power("distort_power", Range(0 , 1)) = 0.2029095
		[Toggle(_USE_CUSTOM1_Z_DISTORT_ON)] _use_custom1_z_distort("use_custom1_z_distort", Float) = 0
		[Toggle(_USE_COLOR_TEX_ON)] _use_color_tex("use_color_tex", Float) = 0
		_color_Tex("color_Tex", 2D) = "white" {}
		_color_uv("color_uv", Vector) = (1,1,0,0)
		_color_speed("color_speed", Vector) = (1,0,1,0)
		_reduce_color("reduce_color", Range(0 , 1)) = 0
		[Toggle(_USE_DISPLACE_ON)] _use_displace("use_displace", Float) = 0
		[NoScaleOffset]_displace_tex("displace_tex", 2D) = "white" {}
		_displace_uv("displace_uv", Vector) = (1,1,0,0)
		_displace_speed("displace_speed", Vector) = (0,0,1,0)
		_displace_power("displace_power", Range(-4 , 4)) = 1
		[NoScaleOffset]_displace_mask_tex("displace_mask_tex", 2D) = "white" {}
		_displace_mask_uv("displace_mask_uv", Vector) = (1,1,0,0)
		_displace_mask_speed("displace_mask_speed", Vector) = (0,0,1,0)
		[Toggle(_USE_CUSTOM1_W_DISPLACE_ON)] _use_custom1_w_displace("use_custom1_w_displace", Float) = 0
		[HideInInspector] _texcoord("", 2D) = "white" {}

	}

		SubShader
	{


		 Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline"}
	LOD 100

		HLSLINCLUDE
		#pragma target 3.0
		ENDHLSL
		Blend One OneMinusSrcAlpha
		AlphaToMask Off
		Cull[_Cull]
		ColorMask RGBA
		ZWrite[_Zwrite]
		ZTest[_Ztest]
		Offset 0 , 0



		Pass
		{
			Name "Unlit"
			Tags { "LightMode" = "UniversalForward" }
			HLSLPROGRAM



			#ifndef UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX
		//only defining to not throw compilation error over Unity 5.5
		#define UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input)
		#endif
		#pragma vertex vert
		#pragma fragment frag
		#pragma multi_compile_instancing
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderVariablesFunctions.hlsl"
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
		#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
		#define ASE_NEEDS_FRAG_COLOR
		#define ASE_NEEDS_VERT_POSITION
		#pragma shader_feature_local _USE_DISPLACE_ON
		#pragma shader_feature_local _USE_CUSTOM1_W_DISPLACE_ON
		#pragma shader_feature_local _USE_COLOR_TEX_ON
		#pragma shader_feature _USE_DOUBEL_PASS_ON
		#pragma shader_feature_local _USE_CHANGE_COLOR_ON
		#pragma shader_feature_local _USE_CUSTOM2_XYZW_CONTROL_BASE_UV_ON
		#pragma shader_feature_local _USE_DISTORT_ON
		#pragma shader_feature _USE_CUSTOM1_Z_DISTORT_ON
		#pragma shader_feature_local _USE_EMISSIVE_ON
		#pragma shader_feature_local _CONNECT_BASE_ALONE_ON
		#pragma shader_feature _USE_SECOND_TEX_ON
		#pragma shader_feature _USE_DISSOLVE_OR_MUL_ON
		#pragma shader_feature _USE_CUSTOM1_X_DISSOLVE_ON


		struct appdata
		{
			float4 vertex : POSITION;
			float4 color : COLOR;
			float3 ase_normal : NORMAL;
			float4 ase_texcoord : TEXCOORD0;
			float4 ase_texcoord1 : TEXCOORD1;
			float4 ase_texcoord2 : TEXCOORD2;
			UNITY_VERTEX_INPUT_INSTANCE_ID
		};

		struct v2f
		{
			float4 vertex : SV_POSITION;
			#ifdef ASE_NEEDS_FRAG_WORLD_POSITION
			float3 worldPos : TEXCOORD0;
			#endif
			float4 ase_color : COLOR;
			float4 ase_texcoord1 : TEXCOORD1;
			float4 ase_texcoord2 : TEXCOORD2;
			float4 ase_texcoord3 : TEXCOORD3;
			float4 ase_texcoord4 : TEXCOORD4;
			UNITY_VERTEX_INPUT_INSTANCE_ID
			UNITY_VERTEX_OUTPUT_STEREO
		};

		uniform float _Cull;
		uniform float _Ztest;
		uniform float _Zwrite;
		uniform sampler2D _displace_tex;
		uniform float4 _displace_speed;
		uniform float4 _displace_uv;
		uniform sampler2D _displace_mask_tex;
		uniform float4 _displace_mask_speed;
		uniform float4 _displace_mask_uv;
		uniform float _displace_power;
		uniform sampler2D _base_texture;
		uniform float4 _base_speed;
		uniform float4 _base_uv;
		uniform sampler2D _Distort_tex;
		uniform float4 _distort_speed;
		uniform float4 _distort_uv;
		uniform float _distort_power;
		uniform sampler2D _Distort_mask;
		uniform float4 _Distort_mask_ST;
		uniform float _HUE;
		uniform float _Saturation;
		uniform float _Value;
		uniform float4 _base_front_color;
		uniform float _base_front_power;
		uniform sampler2D _emissive_tex;
		uniform float4 _emissive_speed;
		uniform float4 _emissive_uv;
		uniform float4 _Emissive_color;
		uniform float _Emissive_power;
		uniform float _alpha_power;
		uniform sampler2D _dissolve_texture;
		uniform float4 _dissolve_speed;
		uniform float4 _dissolve_uv;
		uniform float _edge_hardness;
		uniform float _dissolve;
		uniform float4 _base_back_color;
		uniform float _base_back_power;
		uniform sampler2D _color_Tex;
		uniform float4 _color_speed;
		uniform float4 _color_uv;
		uniform float _reduce_color;
		uniform float4 _CameraDepthTexture_TexelSize;
		uniform float _Fade_distance;
		uniform float _add_or_blend;
		float3 HSVToRGB(float3 c)
		{
			float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
			float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
			return c.z * lerp(K.xxx, saturate(p - K.xxx), c.y);
		}

		float3 RGBToHSV(float3 c)
		{
			float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
			float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
			float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));
			float d = q.x - min(q.w, q.y);
			float e = 1.0e-10;
			return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
		}


		v2f vert(appdata v)
		{
			v2f o;
			UNITY_SETUP_INSTANCE_ID(v);
			UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
			UNITY_TRANSFER_INSTANCE_ID(v, o);

			float3 temp_cast_0 = (0.0).xxx;
			float4 break11_g105 = _displace_speed;
			float mulTime5_g105 = _Time.y * break11_g105.z;
			float2 appendResult6_g105 = (float2(break11_g105.x , break11_g105.y));
			float4 break10_g105 = _displace_uv;
			float2 appendResult2_g105 = (float2(break10_g105.x , break10_g105.y));
			float2 appendResult3_g105 = (float2(break10_g105.z , break10_g105.w));
			float2 texCoord4_g105 = v.ase_texcoord.xy * appendResult2_g105 + appendResult3_g105;
			float2 panner1_g105 = (mulTime5_g105 * appendResult6_g105 + texCoord4_g105);
			float4 break11_g104 = _displace_mask_speed;
			float mulTime5_g104 = _Time.y * break11_g104.z;
			float2 appendResult6_g104 = (float2(break11_g104.x , break11_g104.y));
			float4 break10_g104 = _displace_mask_uv;
			float2 appendResult2_g104 = (float2(break10_g104.x , break10_g104.y));
			float2 appendResult3_g104 = (float2(break10_g104.z , break10_g104.w));
			float2 texCoord4_g104 = v.ase_texcoord.xy * appendResult2_g104 + appendResult3_g104;
			float2 panner1_g104 = (mulTime5_g104 * appendResult6_g104 + texCoord4_g104);
			float4 texCoord514 = v.ase_texcoord1;
			texCoord514.xy = v.ase_texcoord1.xy * float2(1,1) + float2(0,0);
			#ifdef _USE_CUSTOM1_W_DISPLACE_ON
			float staticSwitch515 = texCoord514.y;
			#else
			float staticSwitch515 = _displace_power;
			#endif
			float3 displace516 = ((v.ase_normal * (tex2Dlod(_displace_tex, float4(panner1_g105, 0, 0.0)).r * tex2Dlod(_displace_mask_tex, float4(panner1_g104, 0, 0.0)).r)) * staticSwitch515);
			#ifdef _USE_DISPLACE_ON
			float3 staticSwitch518 = displace516;
			#else
			float3 staticSwitch518 = temp_cast_0;
			#endif

			float3 vertexPos500 = v.vertex.xyz;
			float4 ase_clipPos500 = TransformObjectToHClip(vertexPos500);
			float4 screenPos500 = ComputeScreenPos(ase_clipPos500);
			o.ase_texcoord4 = screenPos500;

			o.ase_color = v.color;
			o.ase_texcoord1 = v.ase_texcoord;
			o.ase_texcoord2 = v.ase_texcoord1;
			o.ase_texcoord3 = v.ase_texcoord2;
			float3 vertexValue = float3(0, 0, 0);
			#if ASE_ABSOLUTE_VERTEX_POS
			vertexValue = v.vertex.xyz;
			#endif
			vertexValue = staticSwitch518;
			#if ASE_ABSOLUTE_VERTEX_POS
			v.vertex.xyz = vertexValue;
			#else
			v.vertex.xyz += vertexValue;
			#endif
			o.vertex = TransformObjectToHClip(v.vertex.xyz);

			#ifdef ASE_NEEDS_FRAG_WORLD_POSITION
			o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
			#endif
			return o;
		}

		float4 frag(v2f i , half ase_vface : VFACE) : SV_Target
		{
			UNITY_SETUP_INSTANCE_ID(i);
			UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
			float4 finalColor;
			#ifdef ASE_NEEDS_FRAG_WORLD_POSITION
			float3 WorldPosition = i.worldPos;
			#endif
			float4 break11_g96 = _base_speed;
			float mulTime5_g96 = _Time.y * break11_g96.z;
			float2 appendResult6_g96 = (float2(break11_g96.x , break11_g96.y));
			float4 break10_g96 = _base_uv;
			float2 appendResult2_g96 = (float2(break10_g96.x , break10_g96.y));
			float2 appendResult3_g96 = (float2(break10_g96.z , break10_g96.w));
			float2 texCoord4_g96 = i.ase_texcoord1.xy * appendResult2_g96 + appendResult3_g96;
			float2 panner1_g96 = (mulTime5_g96 * appendResult6_g96 + texCoord4_g96);
			float4 texCoord541 = i.ase_texcoord2;
			texCoord541.xy = i.ase_texcoord2.xy * float2(1,1) + float2(0,0);
			float2 appendResult536 = (float2(texCoord541.z , texCoord541.w));
			float4 texCoord542 = i.ase_texcoord3;
			texCoord542.xy = i.ase_texcoord3.xy * float2(1,1) + float2(0,0);
			float2 appendResult537 = (float2(texCoord542.x , texCoord542.y));
			float2 texCoord538 = i.ase_texcoord1.xy * appendResult536 + appendResult537;
			#ifdef _USE_CUSTOM2_XYZW_CONTROL_BASE_UV_ON
			float2 staticSwitch535 = texCoord538;
			#else
			float2 staticSwitch535 = panner1_g96;
			#endif
			float4 break11_g93 = _distort_speed;
			float mulTime5_g93 = _Time.y * break11_g93.z;
			float2 appendResult6_g93 = (float2(break11_g93.x , break11_g93.y));
			float4 break10_g93 = _distort_uv;
			float2 appendResult2_g93 = (float2(break10_g93.x , break10_g93.y));
			float2 appendResult3_g93 = (float2(break10_g93.z , break10_g93.w));
			float2 texCoord4_g93 = i.ase_texcoord1.xy * appendResult2_g93 + appendResult3_g93;
			float2 panner1_g93 = (mulTime5_g93 * appendResult6_g93 + texCoord4_g93);
			float4 tex2DNode402 = tex2D(_Distort_tex, panner1_g93);
			float2 appendResult417 = (float2(tex2DNode402.r , tex2DNode402.g));
			float4 texCoord443 = i.ase_texcoord2;
			texCoord443.xy = i.ase_texcoord2.xy * float2(1,1) + float2(0,0);
			#ifdef _USE_CUSTOM1_Z_DISTORT_ON
			float staticSwitch445 = texCoord443.x;
			#else
			float staticSwitch445 = _distort_power;
			#endif
			float2 uv_Distort_mask = i.ase_texcoord1.xy * _Distort_mask_ST.xy + _Distort_mask_ST.zw;
			#ifdef _USE_DISTORT_ON
			float2 staticSwitch543 = (appendResult417 * staticSwitch445 * tex2D(_Distort_mask, uv_Distort_mask).r);
			#else
			float2 staticSwitch543 = float2(0,0);
			#endif
			float2 temp_output_420_0 = (staticSwitch535 + staticSwitch543);
			float4 tex2DNode71 = tex2D(_base_texture, temp_output_420_0);
			float3 hsvTorgb2_g99 = RGBToHSV((tex2DNode71).xyz);
			float3 appendResult5_g99 = (float3(_HUE , _Saturation , _Value));
			float3 break6_g99 = appendResult5_g99;
			float3 hsvTorgb1_g99 = HSVToRGB(float3((hsvTorgb2_g99.x + break6_g99.x),(hsvTorgb2_g99.y + break6_g99.y),(hsvTorgb2_g99.z + break6_g99.z)));
			#ifdef _USE_CHANGE_COLOR_ON
			float4 staticSwitch520 = float4(hsvTorgb1_g99 , 0.0);
			#else
			float4 staticSwitch520 = tex2DNode71;
			#endif
			float4 temp_output_127_0 = (float4((i.ase_color).rgb , 0.0) * staticSwitch520);
			float3 temp_cast_3 = (0.0).xxx;
			float4 break11_g108 = _emissive_speed;
			float mulTime5_g108 = _Time.y * break11_g108.z;
			float2 appendResult6_g108 = (float2(break11_g108.x , break11_g108.y));
			float4 break10_g108 = _emissive_uv;
			float2 appendResult2_g108 = (float2(break10_g108.x , break10_g108.y));
			float2 appendResult3_g108 = (float2(break10_g108.z , break10_g108.w));
			float2 texCoord4_g108 = i.ase_texcoord1.xy * appendResult2_g108 + appendResult3_g108;
			float2 panner1_g108 = (mulTime5_g108 * appendResult6_g108 + texCoord4_g108);
			#ifdef _CONNECT_BASE_ALONE_ON
			float2 staticSwitch554 = (panner1_g108 + staticSwitch543);
			#else
			float2 staticSwitch554 = temp_output_420_0;
			#endif
			float4 texCoord398 = i.ase_texcoord1;
			texCoord398.xy = i.ase_texcoord1.xy * float2(1,1) + float2(0,0);
			float3 emissive462 = (((tex2D(_emissive_tex, staticSwitch554).r * _Emissive_color) * (texCoord398.w * _Emissive_power))).rgb;
			#ifdef _USE_EMISSIVE_ON
			float3 staticSwitch545 = emissive462;
			#else
			float3 staticSwitch545 = temp_cast_3;
			#endif
			float temp_output_130_0 = (i.ase_color.a * saturate((_alpha_power * tex2DNode71.a)));
			float4 break11_g97 = _dissolve_speed;
			float mulTime5_g97 = _Time.y * break11_g97.z;
			float2 appendResult6_g97 = (float2(break11_g97.x , break11_g97.y));
			float4 break10_g97 = _dissolve_uv;
			float2 appendResult2_g97 = (float2(break10_g97.x , break10_g97.y));
			float2 appendResult3_g97 = (float2(break10_g97.z , break10_g97.w));
			float2 texCoord4_g97 = i.ase_texcoord1.xy * appendResult2_g97 + appendResult3_g97;
			float2 panner1_g97 = (mulTime5_g97 * appendResult6_g97 + texCoord4_g97);
			float4 tex2DNode138 = tex2D(_dissolve_texture, panner1_g97);
			float mul449 = tex2DNode138.r;
			float temp_output_7_0_g98 = _edge_hardness;
			float3 texCoord320 = i.ase_texcoord1.xyz;
			texCoord320.xy = i.ase_texcoord1.xyz.xy * float2(1,1) + float2(0,0);
			#ifdef _USE_CUSTOM1_X_DISSOLVE_ON
			float staticSwitch212 = texCoord320.z;
			#else
			float staticSwitch212 = _dissolve;
			#endif
			float lerpResult2_g98 = lerp(temp_output_7_0_g98 , -1.0 , staticSwitch212);
			float dissolve448 = saturate(saturate(((tex2DNode138.r * temp_output_7_0_g98) - lerpResult2_g98)));
			#ifdef _USE_DISSOLVE_OR_MUL_ON
			float staticSwitch266 = (temp_output_130_0 * dissolve448);
			#else
			float staticSwitch266 = (temp_output_130_0 * mul449);
			#endif
			#ifdef _USE_SECOND_TEX_ON
			float staticSwitch447 = staticSwitch266;
			#else
			float staticSwitch447 = temp_output_130_0;
			#endif
			float final_alpha466 = staticSwitch447;
			float4 appendResult465 = (float4((((temp_output_127_0 * _base_front_color * _base_front_power) + float4(staticSwitch545 , 0.0))).rgb , final_alpha466));
			float4 appendResult475 = (float4((((temp_output_127_0 * _base_back_color * _base_back_power) + float4(staticSwitch545 , 0.0))).rgb , final_alpha466));
			float4 switchResult456 = (((ase_vface > 0) ? (appendResult465) : (appendResult475)));
			#ifdef _USE_DOUBEL_PASS_ON
			float4 staticSwitch460 = switchResult456;
			#else
			float4 staticSwitch460 = appendResult465;
			#endif
			float3 temp_output_471_0 = (staticSwitch460).xyz;
			float4 break11_g106 = _color_speed;
			float mulTime5_g106 = _Time.y * break11_g106.z;
			float2 appendResult6_g106 = (float2(break11_g106.x , break11_g106.y));
			float4 break10_g106 = _color_uv;
			float2 appendResult2_g106 = (float2(break10_g106.x , break10_g106.y));
			float2 appendResult3_g106 = (float2(break10_g106.z , break10_g106.w));
			float2 texCoord4_g106 = i.ase_texcoord1.xy * appendResult2_g106 + appendResult3_g106;
			float2 panner1_g106 = (mulTime5_g106 * appendResult6_g106 + texCoord4_g106);
			float3 desaturateInitialColor547 = tex2D(_color_Tex, panner1_g106).rgb;
			float desaturateDot547 = dot(desaturateInitialColor547, float3(0.299, 0.587, 0.114));
			float3 desaturateVar547 = lerp(desaturateInitialColor547, desaturateDot547.xxx, _reduce_color);
			#ifdef _USE_COLOR_TEX_ON
			float3 staticSwitch497 = ((desaturateVar547).xyz * temp_output_471_0);
			#else
			float3 staticSwitch497 = temp_output_471_0;
			#endif
			float4 screenPos500 = i.ase_texcoord4;
			float4 ase_screenPosNorm500 = screenPos500 / screenPos500.w;
			ase_screenPosNorm500.z = (UNITY_NEAR_CLIP_VALUE >= 0) ? ase_screenPosNorm500.z : ase_screenPosNorm500.z * 0.5 + 0.5;
			float screenDepth500 = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, ase_screenPosNorm500.xy), _ZBufferParams);
			float distanceDepth500 = saturate(abs((screenDepth500 - LinearEyeDepth(ase_screenPosNorm500.z, _ZBufferParams)) / (_Fade_distance)));
			float temp_output_9_0_g107 = saturate(((staticSwitch460).w * distanceDepth500));
			float4 appendResult2_g107 = (float4((staticSwitch497 * temp_output_9_0_g107) , temp_output_9_0_g107));
			float4 appendResult6_g107 = (float4(appendResult2_g107.xyz , (temp_output_9_0_g107 * _add_or_blend)));


			finalColor = appendResult6_g107;
			return finalColor;
		}
		ENDHLSL
	}
	}
		CustomEditor "PPX_Common_shader_GUI"


}
