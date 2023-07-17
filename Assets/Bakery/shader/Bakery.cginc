#ifndef BAKERY_INCLUDED
#define BAKERY_INCLUDED

float bakeryLightmapMode;
//float2 bakeryLightmapSize;
#define BAKERYMODE_DEFAULT 0
#define BAKERYMODE_VERTEXLM 1.0f
#define BAKERYMODE_RNM 2.0f
#define BAKERYMODE_SH 3.0f

//#define BAKERY_SSBUMP

//#define BAKERY_COMPRESSED_VOLUME_RGBM

// can't fit vertexLM SH to sm3_0 interpolators
#if defined(SHADER_API_GLCORE) || defined(SHADER_API_GLES) || defined(SHADER_API_D3D11_9X)
    #undef BAKERY_VERTEXLMSH
#endif

// can't do stuff on sm2_0 due to standard shader alrady taking up all instructions
#if SHADER_TARGET < 30
    #undef BAKERY_BICUBIC
    #undef BAKERY_LMSPEC

    #undef BAKERY_RNM
    #undef BAKERY_SH
    #undef BAKERY_MONOSH
    #undef BAKERY_VERTEXLM
#endif

#ifndef _NORMALMAP
    #undef BAKERY_RNM
    //#undef BAKERY_SH
#endif

#ifndef UNITY_SHOULD_SAMPLE_SH
    #undef BAKERY_PROBESHNONLINEAR
#endif

#if defined(BAKERY_RNM) && defined(BAKERY_LMSPEC)
#define BAKERY_RNMSPEC
#endif

#ifndef BAKERY_VERTEXLM
    #undef BAKERY_VERTEXLMDIR
    #undef BAKERY_VERTEXLMSH
    #undef BAKERY_VERTEXLMMASK
#endif

#define lumaConv float3(0.2125f, 0.7154f, 0.0721f)

#if defined(BAKERY_SH) || defined(BAKERY_MONOSH) || defined(BAKERY_VERTEXLMSH) || defined(BAKERY_PROBESHNONLINEAR) || defined(BAKERY_VOLUME)
float shEvaluateDiffuseL1Geomerics(float L0, float3 L1, float3 n)
{
    // average energy
    float R0 = L0;

    // avg direction of incoming light
    float3 R1 = 0.5f * L1;

    // directional brightness
    float lenR1 = length(R1);

    // linear angle between normal and direction 0-1
    //float q = 0.5f * (1.0f + dot(R1 / lenR1, n));
    //float q = dot(R1 / lenR1, n) * 0.5 + 0.5;
    float q = dot(normalize(R1), n) * 0.5 + 0.5;

    // power for q
    // lerps from 1 (linear) to 3 (cubic) based on directionality
    float p = 1.0f + 2.0f * lenR1 / R0;

    // dynamic range constant
    // should vary between 4 (highly directional) and 0 (ambient)
    float a = (1.0f - lenR1 / R0) / (1.0f + lenR1 / R0);

    return R0 * (a + (1.0f - a) * (p + 1.0f) * pow(q, p));
}
#endif

#ifdef BAKERY_VERTEXLM
    float4 unpack4NFloats(float src) {
        //return fmod(float4(src / 262144.0, src / 4096.0, src / 64.0, src), 64.0)/64.0;
        return frac(float4(src / (262144.0*64), src / (4096.0*64), src / (64.0*64), src));
    }
    float3 unpack3NFloats(float src) {
        float r = frac(src);
        float g = frac(src * 256.0);
        float b = frac(src * 65536.0);
        return float3(r, g, b);
    }
#if defined(BAKERY_VERTEXLMDIR)

#ifdef BAKERY_MONOSH
    void BakeryVertexLMMonoSH(inout float3 diffuseColor, inout float3 specularColor, float3 nL1, float3 normalWorld, float3 viewDir, float smoothness)
    {
        nL1 = nL1;
        float3 L0 = diffuseColor;
        float3 L1x = nL1.x * L0 * 2;
        float3 L1y = nL1.y * L0 * 2;
        float3 L1z = nL1.z * L0 * 2;

        float3 sh;
    #if BAKERY_SHNONLINEAR
        //sh.r = shEvaluateDiffuseL1Geomerics(L0.r, float3(L1x.r, L1y.r, L1z.r), normalWorld);
        //sh.g = shEvaluateDiffuseL1Geomerics(L0.g, float3(L1x.g, L1y.g, L1z.g), normalWorld);
        //sh.b = shEvaluateDiffuseL1Geomerics(L0.b, float3(L1x.b, L1y.b, L1z.b), normalWorld);

        float lumaL0 = dot(L0, 1);
        float lumaL1x = dot(L1x, 1);
        float lumaL1y = dot(L1y, 1);
        float lumaL1z = dot(L1z, 1);
        float lumaSH = shEvaluateDiffuseL1Geomerics(lumaL0, float3(lumaL1x, lumaL1y, lumaL1z), normalWorld);

        sh = L0 + normalWorld.x * L1x + normalWorld.y * L1y + normalWorld.z * L1z;
        float regularLumaSH = dot(sh, 1);
        //sh *= regularLumaSH < 0.001 ? 1 : (lumaSH / regularLumaSH);
        sh *= lerp(1, lumaSH / regularLumaSH, saturate(regularLumaSH*16));

    #else
        sh = L0 + normalWorld.x * L1x + normalWorld.y * L1y + normalWorld.z * L1z;
    #endif

        diffuseColor = max(sh, 0.0);

        #ifdef BAKERY_LMSPEC
            float3 dominantDir = nL1;
            float focus = saturate(length(dominantDir));
            half3 halfDir = Unity_SafeNormalize(normalize(dominantDir) - viewDir);
            half nh = saturate(dot(normalWorld, halfDir));
            half perceptualRoughness = SmoothnessToPerceptualRoughness(smoothness );//* sqrt(focus));
            half roughness = PerceptualRoughnessToRoughness(perceptualRoughness);
            half spec = GGXTerm(nh, roughness);
            specularColor = max(spec * sh, 0.0);
        #endif
    }
#endif

    void BakeryVertexLMDirection(inout float3 diffuseColor, inout float3 specularColor, float3 lightDirection, float3 vertexNormalWorld, float3 normalWorld, float3 viewDir, float smoothness)
    {
        float3 dominantDir = Unity_SafeNormalize(lightDirection);
        half halfLambert = dot(normalWorld, dominantDir) * 0.5 + 0.5;
        half flatNormalHalfLambert = dot(vertexNormalWorld, dominantDir) * 0.5 + 0.5;

        #ifdef BAKERY_LMSPEC
            half3 halfDir = Unity_SafeNormalize(normalize(dominantDir) - viewDir);
            half nh = saturate(dot(normalWorld, halfDir));
            half perceptualRoughness = SmoothnessToPerceptualRoughness(smoothness);
            half roughness = PerceptualRoughnessToRoughness(perceptualRoughness);
            half spec = GGXTerm(nh, roughness);
            specularColor = spec * diffuseColor;
        #endif

        diffuseColor *= halfLambert / max(1e-4h, flatNormalHalfLambert);
    }
#elif defined(BAKERY_VERTEXLMSH)
    void BakeryVertexLMSH(inout float3 diffuseColor, inout float3 specularColor, float3 shL1x, float3 shL1y, float3 shL1z, float3 normalWorld, float3 viewDir, float smoothness)
    {
        float3 L0 = diffuseColor;
        float3 nL1x = shL1x;
        float3 nL1y = shL1y;
        float3 nL1z = shL1z;
        float3 L1x = nL1x * L0 * 2;
        float3 L1y = nL1y * L0 * 2;
        float3 L1z = nL1z * L0 * 2;

        float3 sh;
    #if BAKERY_SHNONLINEAR
        //sh.r = shEvaluateDiffuseL1Geomerics(L0.r, float3(L1x.r, L1y.r, L1z.r), normalWorld);
        //sh.g = shEvaluateDiffuseL1Geomerics(L0.g, float3(L1x.g, L1y.g, L1z.g), normalWorld);
        //sh.b = shEvaluateDiffuseL1Geomerics(L0.b, float3(L1x.b, L1y.b, L1z.b), normalWorld);

        float lumaL0 = dot(L0, 1);
        float lumaL1x = dot(L1x, 1);
        float lumaL1y = dot(L1y, 1);
        float lumaL1z = dot(L1z, 1);
        float lumaSH = shEvaluateDiffuseL1Geomerics(lumaL0, float3(lumaL1x, lumaL1y, lumaL1z), normalWorld);

        sh = L0 + normalWorld.x * L1x + normalWorld.y * L1y + normalWorld.z * L1z;
        float regularLumaSH = dot(sh, 1);
        //sh *= regularLumaSH < 0.001 ? 1 : (lumaSH / regularLumaSH);
        sh *= lerp(1, lumaSH / regularLumaSH, saturate(regularLumaSH*16));

    #else
        sh = L0 + normalWorld.x * L1x + normalWorld.y * L1y + normalWorld.z * L1z;
    #endif

        diffuseColor = max(sh, 0.0);

        #ifdef BAKERY_LMSPEC
            float3 dominantDir = float3(dot(nL1x, lumaConv), dot(nL1y, lumaConv), dot(nL1z, lumaConv));
            float focus = saturate(length(dominantDir));
            half3 halfDir = Unity_SafeNormalize(normalize(dominantDir) - viewDir);
            half nh = saturate(dot(normalWorld, halfDir));
            half perceptualRoughness = SmoothnessToPerceptualRoughness(smoothness );//* sqrt(focus));
            half roughness = PerceptualRoughnessToRoughness(perceptualRoughness);
            half spec = GGXTerm(nh, roughness);
            specularColor = max(spec * sh, 0.0);
        #endif
    }
#endif
#endif

#ifdef BAKERY_BICUBIC
float BakeryBicubic_w0(float a)
{
    return (1.0f/6.0f)*(a*(a*(-a + 3.0f) - 3.0f) + 1.0f);
}

float BakeryBicubic_w1(float a)
{
    return (1.0f/6.0f)*(a*a*(3.0f*a - 6.0f) + 4.0f);
}

float BakeryBicubic_w2(float a)
{
    return (1.0f/6.0f)*(a*(a*(-3.0f*a + 3.0f) + 3.0f) + 1.0f);
}

float BakeryBicubic_w3(float a)
{
    return (1.0f/6.0f)*(a*a*a);
}

float BakeryBicubic_g0(float a)
{
    return BakeryBicubic_w0(a) + BakeryBicubic_w1(a);
}

float BakeryBicubic_g1(float a)
{
    return BakeryBicubic_w2(a) + BakeryBicubic_w3(a);
}

float BakeryBicubic_h0(float a)
{
    return -1.0f + BakeryBicubic_w1(a) / (BakeryBicubic_w0(a) + BakeryBicubic_w1(a)) + 0.5f;
}

float BakeryBicubic_h1(float a)
{
    return 1.0f + BakeryBicubic_w3(a) / (BakeryBicubic_w2(a) + BakeryBicubic_w3(a)) + 0.5f;
}
#endif

struct BakeryVertexInput
{
    float4 vertex   : POSITION;
#ifdef BAKERY_VERTEXLM
    fixed4 color : COLOR;
    #ifdef BAKERY_VERTEXLMSH
        float2 uv3      : TEXCOORD3;
    #endif
#endif
    half3 normal    : NORMAL;
    float2 uv0      : TEXCOORD0;
    float2 uv1      : TEXCOORD1;
#if defined(DYNAMICLIGHTMAP_ON) || defined(UNITY_PASS_META)
    float2 uv2      : TEXCOORD2;
#endif
#if defined(_TANGENT_TO_WORLD) || defined(BAKERY_RNMSPEC)
    half4 tangent   : TANGENT;
#endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

float4 BakeryTexCoords(BakeryVertexInput v)
{
    float4 texcoord;
    texcoord.xy = TRANSFORM_TEX(v.uv0, _MainTex); // Always source from uv0
    texcoord.zw = TRANSFORM_TEX(((_UVSec == 0) ? v.uv0 : v.uv1), _DetailAlbedoMap);
    return texcoord;
}

inline half4 BakeryVertexGIForward(BakeryVertexInput v, float3 posWorld, half3 normalWorld)
{
    half4 ambientOrLightmapUV = 0;
    // Static lightmaps
#ifndef LIGHTMAP_OFF
    ambientOrLightmapUV.xy = v.uv1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
    ambientOrLightmapUV.zw = 0;
    // Sample light probe for Dynamic objects only (no static or dynamic lightmaps)
#elif UNITY_SHOULD_SAMPLE_SH
#ifdef VERTEXLIGHT_ON
    // Approximated illumination from non-important point lights
    ambientOrLightmapUV.rgb = Shade4PointLights(
    unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,
    unity_LightColor[0].rgb, unity_LightColor[1].rgb, unity_LightColor[2].rgb, unity_LightColor[3].rgb,
    unity_4LightAtten0, posWorld, normalWorld);
#endif

    ambientOrLightmapUV.rgb = ShadeSHPerVertex(normalWorld, ambientOrLightmapUV.rgb);
#endif

#ifdef DYNAMICLIGHTMAP_ON
    ambientOrLightmapUV.zw = v.uv2.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
#endif

#ifdef BAKERY_VERTEXLM
    if (bakeryLightmapMode == BAKERYMODE_VERTEXLM)
    {
        #ifdef BAKERY_VERTEXLMMASK
            ambientOrLightmapUV = unpack4NFloats(v.uv1.x);
        #endif
    }
#endif

    return ambientOrLightmapUV;
}

//Forward Pass
struct BakeryVertexOutputForwardBase
{
    float4 pos                          : SV_POSITION;
    float4 tex                          : TEXCOORD0;
    half3 eyeVec                        : TEXCOORD1;

#if UNITY_VERSION >= 201740
    float4 tangentToWorldAndPackedData[3]    : TEXCOORD2;    // [3x3:tangentToWorld | 1x3:viewDirForParallax]
#else
    half4 tangentToWorldAndPackedData[3]    : TEXCOORD2;    // [3x3:tangentToWorld | 1x3:viewDirForParallax]
#endif

#if defined(BAKERY_RNMSPEC)
    half3 viewDirForParallax            : TEXCOORD13;
#endif

    half4 ambientOrLightmapUV           : TEXCOORD5;    // SH or Lightmap UV
    UNITY_SHADOW_COORDS(6)
    UNITY_FOG_COORDS(7)

#ifdef BAKERY_VERTEXLM
    float4 color : COLOR_centroid;
    #if defined(BAKERY_VERTEXLMDIR)
        float3 lightDirection : TEXCOORD10_centroid; // is this even legal
    #elif defined(BAKERY_VERTEXLMSH)
        float3 shL1x : TEXCOORD10_centroid;
        float3 shL1y : TEXCOORD11_centroid;
        float3 shL1z : TEXCOORD12_centroid;
    #endif
#endif

    // next ones would not fit into SM2.0 limits, but they are always for SM3.0+
#if UNITY_SPECCUBE_BOX_PROJECTION || UNITY_LIGHT_PROBE_PROXY_VOLUME || (UNITY_REQUIRE_FRAG_WORLDPOS && !UNITY_PACK_WORLDPOS_WITH_TANGENT)
    float3 posWorld                 : TEXCOORD8;
#endif

#if UNITY_OPTIMIZE_TEXCUBELOD
    #if UNITY_SPECCUBE_BOX_PROJECTION
        half3 reflUVW               : TEXCOORD9;
    #else
        half3 reflUVW               : TEXCOORD8;
    #endif
#endif

    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

BakeryVertexOutputForwardBase bakeryVertForwardBase(BakeryVertexInput v)
{
    UNITY_SETUP_INSTANCE_ID(v);
    BakeryVertexOutputForwardBase o;
    UNITY_INITIALIZE_OUTPUT(BakeryVertexOutputForwardBase, o);
    UNITY_TRANSFER_INSTANCE_ID(v, o);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    float4 posWorld = mul(unity_ObjectToWorld, v.vertex);
    #if UNITY_REQUIRE_FRAG_WORLDPOS
        #if UNITY_PACK_WORLDPOS_WITH_TANGENT
            o.tangentToWorldAndPackedData[0].w = posWorld.x;
            o.tangentToWorldAndPackedData[1].w = posWorld.y;
            o.tangentToWorldAndPackedData[2].w = posWorld.z;
        #else
            o.posWorld = posWorld.xyz;
        #endif
    #endif
    o.pos = UnityObjectToClipPos(v.vertex);

    float3 normalWorld = UnityObjectToWorldNormal(v.normal);
    o.eyeVec = NormalizePerVertexNormal(posWorld.xyz - _WorldSpaceCameraPos);

    o.tex = BakeryTexCoords(v);
#ifdef _TANGENT_TO_WORLD
    float4 tangentWorld = float4(UnityObjectToWorldDir(v.tangent.xyz), v.tangent.w);

    float3x3 tangentToWorld = CreateTangentToWorldPerVertex(normalWorld, tangentWorld.xyz, tangentWorld.w);
    o.tangentToWorldAndPackedData[0].xyz = tangentToWorld[0];
    o.tangentToWorldAndPackedData[1].xyz = tangentToWorld[1];
    o.tangentToWorldAndPackedData[2].xyz = tangentToWorld[2];
#else
    o.tangentToWorldAndPackedData[0].xyz = 0;
    o.tangentToWorldAndPackedData[1].xyz = 0;
    o.tangentToWorldAndPackedData[2].xyz = normalWorld;
#endif
    //We need this for shadow receving
    UNITY_TRANSFER_SHADOW(o, v.uv1);

    o.ambientOrLightmapUV = BakeryVertexGIForward(v, posWorld, normalWorld);

#if defined(_PARALLAXMAP) || defined(BAKERY_RNMSPEC)
    TANGENT_SPACE_ROTATION;
#endif

#if defined(_PARALLAXMAP)
    half3 viewDirForParallax = mul(rotation, ObjSpaceViewDir(v.vertex));
    o.tangentToWorldAndPackedData[0].w = viewDirForParallax.x;
    o.tangentToWorldAndPackedData[1].w = viewDirForParallax.y;
    o.tangentToWorldAndPackedData[2].w = viewDirForParallax.z;
#endif

#if defined(BAKERY_RNMSPEC)
    o.viewDirForParallax = mul(rotation, ObjSpaceViewDir(v.vertex));
#endif

#if UNITY_OPTIMIZE_TEXCUBELOD
    o.reflUVW = reflect(o.eyeVec, normalWorld);
#endif

#ifdef BAKERY_VERTEXLM
    // Unpack from RGBM
    o.color = v.color;
    o.color.rgb *= o.color.a * 8.0f;
    o.color.rgb *= o.color.rgb;

    #if defined(BAKERY_VERTEXLMDIR)
        o.lightDirection = unpack3NFloats(v.uv1.y) * 2 - 1;
    #elif defined(BAKERY_VERTEXLMSH)
        o.shL1x = unpack3NFloats(v.uv1.y) * 2 - 1;
        o.shL1y = unpack3NFloats(v.uv3.x) * 2 - 1;
        o.shL1z = unpack3NFloats(v.uv3.y) * 2 - 1;
    #endif
#endif

    UNITY_TRANSFER_FOG(o, o.pos);
    return o;
}

/*
inline UnityGI BakeryFragmentGI (FragmentCommonData s, half occlusion, half4 i_ambientOrLightmapUV, half atten, UnityLight light, bool reflections)
{
    UnityGIInput d;
    d.light = light;
    d.worldPos = s.posWorld;
    d.worldViewDir = -s.eyeVec;
    d.atten = atten;
    #if defined(LIGHTMAP_ON) || defined(DYNAMICLIGHTMAP_ON)
        d.ambient = 0;
        d.lightmapUV = i_ambientOrLightmapUV;
    #else
        d.ambient = i_ambientOrLightmapUV.rgb;
        d.lightmapUV = 0;
    #endif

    d.probeHDR[0] = unity_SpecCube0_HDR;
    d.probeHDR[1] = unity_SpecCube1_HDR;
    #if defined(UNITY_SPECCUBE_BLENDING) || defined(UNITY_SPECCUBE_BOX_PROJECTION)
      d.boxMin[0] = unity_SpecCube0_BoxMin; // .w holds lerp value for blending
    #endif
    #ifdef UNITY_SPECCUBE_BOX_PROJECTION
      d.boxMax[0] = unity_SpecCube0_BoxMax;
      d.probePosition[0] = unity_SpecCube0_ProbePosition;
      d.boxMax[1] = unity_SpecCube1_BoxMax;
      d.boxMin[1] = unity_SpecCube1_BoxMin;
      d.probePosition[1] = unity_SpecCube1_ProbePosition;
    #endif

    if(reflections)
    {
        Unity_GlossyEnvironmentData g = UnityGlossyEnvironmentSetup(s.smoothness, -s.eyeVec, s.normalWorld, s.specColor);
        // Replace the reflUVW if it has been compute in Vertex shader. Note: the compiler will optimize the calcul in UnityGlossyEnvironmentSetup itself
        #if UNITY_STANDARD_SIMPLE
            g.reflUVW = s.reflUVW;
        #endif

        return UnityGlobalIllumination (d, occlusion, s.normalWorld, g);
    }
    else
    {
        return UnityGlobalIllumination (d, occlusion, s.normalWorld);
    }
}
*/

#if defined(BAKERY_RNM) || defined(BAKERY_SH)
sampler2D _RNM0, _RNM1, _RNM2;
float4 _RNM0_TexelSize;
#endif

#if UNITY_VERSION >= 201740
    SamplerState bakery_trilinear_clamp_sampler;
#endif

#ifdef BAKERY_VOLUME
Texture3D _Volume0, _Volume1, _Volume2, _VolumeMask;
SamplerState sampler_Volume0;
float3 _VolumeMin, _VolumeInvSize;
float3 _GlobalVolumeMin, _GlobalVolumeInvSize;
    #ifdef BAKERY_COMPRESSED_VOLUME
        Texture3D _Volume3;
    #endif
    #ifdef BAKERY_VOLROTATION
        float4x4 _GlobalVolumeMatrix, _VolumeMatrix;
    #endif
#endif

#ifdef BAKERY_BICUBIC
    // Bicubic
    float4 BakeryTex2D(sampler2D tex, float2 uv, float4 texelSize)
    {
        float x = uv.x * texelSize.z;
        float y = uv.y * texelSize.z;

        x -= 0.5f;
        y -= 0.5f;

        float px = floor(x);
        float py = floor(y);

        float fx = x - px;
        float fy = y - py;

        float g0x = BakeryBicubic_g0(fx);
        float g1x = BakeryBicubic_g1(fx);
        float h0x = BakeryBicubic_h0(fx);
        float h1x = BakeryBicubic_h1(fx);
        float h0y = BakeryBicubic_h0(fy);
        float h1y = BakeryBicubic_h1(fy);

        return     BakeryBicubic_g0(fy) * ( g0x * tex2D(tex, (float2(px + h0x, py + h0y) * texelSize.x))   +
                              g1x * tex2D(tex, (float2(px + h1x, py + h0y) * texelSize.x))) +

                   BakeryBicubic_g1(fy) * ( g0x * tex2D(tex, (float2(px + h0x, py + h1y) * texelSize.x))   +
                              g1x * tex2D(tex, (float2(px + h1x, py + h1y) * texelSize.x)));
    }
    float4 BakeryTex2D(Texture2D tex, SamplerState s, float2 uv, float4 texelSize)
    {
        float x = uv.x * texelSize.z;
        float y = uv.y * texelSize.z;

        x -= 0.5f;
        y -= 0.5f;

        float px = floor(x);
        float py = floor(y);

        float fx = x - px;
        float fy = y - py;

        float g0x = BakeryBicubic_g0(fx);
        float g1x = BakeryBicubic_g1(fx);
        float h0x = BakeryBicubic_h0(fx);
        float h1x = BakeryBicubic_h1(fx);
        float h0y = BakeryBicubic_h0(fy);
        float h1y = BakeryBicubic_h1(fy);

        return     BakeryBicubic_g0(fy) * ( g0x * tex.Sample(s, (float2(px + h0x, py + h0y) * texelSize.x))   +
                              g1x * tex.Sample(s, (float2(px + h1x, py + h0y) * texelSize.x))) +

                   BakeryBicubic_g1(fy) * ( g0x * tex.Sample(s, (float2(px + h0x, py + h1y) * texelSize.x))   +
                              g1x * tex.Sample(s, (float2(px + h1x, py + h1y) * texelSize.x)));
    }
#else
    // Bilinear
    float4 BakeryTex2D(sampler2D tex, float2 uv, float4 texelSize)
    {
        return tex2D(tex, uv);
    }
    float4 BakeryTex2D(Texture2D tex, SamplerState s, float2 uv, float4 texelSize)
    {
        return tex.Sample(s, uv);
    }
#endif

#ifdef DIRLIGHTMAP_COMBINED
#ifdef BAKERY_LMSPEC
float BakeryDirectionalLightmapSpecular(float2 lmUV, float3 normalWorld, float3 viewDir, float smoothness)
{
#if UNITY_VERSION >= 201740
    float3 dominantDir = unity_LightmapInd.Sample(bakery_trilinear_clamp_sampler, lmUV).xyz * 2 - 1;
#else
    float3 dominantDir = UNITY_SAMPLE_TEX2D_SAMPLER(unity_LightmapInd, unity_Lightmap, lmUV).xyz * 2 - 1;
#endif
    half3 halfDir = Unity_SafeNormalize(normalize(dominantDir) - viewDir);
    half nh = saturate(dot(normalWorld, halfDir));
    half perceptualRoughness = SmoothnessToPerceptualRoughness(smoothness);
    half roughness = PerceptualRoughnessToRoughness(perceptualRoughness);
    half spec = GGXTerm(nh, roughness);
    return spec;
}
#endif
#endif

#ifdef BAKERY_RNM
void BakeryRNM(inout float3 diffuseColor, inout float3 specularColor, float2 lmUV, float3 normalMap, float smoothness, float3 viewDirT)
{
    const float3 rnmBasis0 = float3(0.816496580927726f, 0, 0.5773502691896258f);
    const float3 rnmBasis1 = float3(-0.4082482904638631f, 0.7071067811865475f, 0.5773502691896258f);
    const float3 rnmBasis2 = float3(-0.4082482904638631f, -0.7071067811865475f, 0.5773502691896258f);

    float3 rnm0 = DecodeLightmap(BakeryTex2D(_RNM0, lmUV, _RNM0_TexelSize));
    float3 rnm1 = DecodeLightmap(BakeryTex2D(_RNM1, lmUV, _RNM0_TexelSize));
    float3 rnm2 = DecodeLightmap(BakeryTex2D(_RNM2, lmUV, _RNM0_TexelSize));

    #ifdef BAKERY_SSBUMP
        diffuseColor = normalMap.x * rnm0
                     + normalMap.z * rnm1
                     + normalMap.y * rnm2;
         diffuseColor *= 2;
    #else
        diffuseColor = saturate(dot(rnmBasis0, normalMap)) * rnm0
                     + saturate(dot(rnmBasis1, normalMap)) * rnm1
                     + saturate(dot(rnmBasis2, normalMap)) * rnm2;
    #endif

    #ifdef BAKERY_LMSPEC
        float3 dominantDirT = rnmBasis0 * dot(rnm0, lumaConv) +
                              rnmBasis1 * dot(rnm1, lumaConv) +
                              rnmBasis2 * dot(rnm2, lumaConv);

        float3 dominantDirTN = NormalizePerPixelNormal(dominantDirT);
        float3 specColor = saturate(dot(rnmBasis0, dominantDirTN)) * rnm0 +
                           saturate(dot(rnmBasis1, dominantDirTN)) * rnm1 +
                           saturate(dot(rnmBasis2, dominantDirTN)) * rnm2;

        half3 halfDir = Unity_SafeNormalize(dominantDirTN - viewDirT);
        half nh = saturate(dot(normalMap, halfDir));
        half perceptualRoughness = SmoothnessToPerceptualRoughness(smoothness);
        half roughness = PerceptualRoughnessToRoughness(perceptualRoughness);
        half spec = GGXTerm(nh, roughness);
        specularColor = spec * specColor;
    #endif
}
#endif

#ifdef BAKERY_SH
void BakerySH(inout float3 diffuseColor, inout float3 specularColor, float2 lmUV, float3 normalWorld, float3 viewDir, float smoothness)
{

    /*
#ifdef SHADER_API_D3D11
    #if UNITY_VERSION >= 201740
        float3 L0 = DecodeLightmap(BakeryTex2D(unity_Lightmap, bakery_trilinear_clamp_sampler, lmUV, _RNM0_TexelSize));
    #else
        float3 L0 = DecodeLightmap(BakeryTex2D(unity_Lightmap, samplerunity_Lightmap, lmUV, _RNM0_TexelSize));
    #endif
#else
    float3 L0 = DecodeLightmap(UNITY_SAMPLE_TEX2D(unity_Lightmap, lmUV));
#endif
    */

    // already sampled
    float3 L0 = diffuseColor;

    float3 nL1x = BakeryTex2D(_RNM0, lmUV, _RNM0_TexelSize) * 2 - 1;
    float3 nL1y = BakeryTex2D(_RNM1, lmUV, _RNM0_TexelSize) * 2 - 1;
    float3 nL1z = BakeryTex2D(_RNM2, lmUV, _RNM0_TexelSize) * 2 - 1;
    float3 L1x = nL1x * L0 * 2;
    float3 L1y = nL1y * L0 * 2;
    float3 L1z = nL1z * L0 * 2;

    float3 sh;
#if BAKERY_SHNONLINEAR
    float lumaL0 = dot(L0, 1);
    float lumaL1x = dot(L1x, 1);
    float lumaL1y = dot(L1y, 1);
    float lumaL1z = dot(L1z, 1);
    float lumaSH = shEvaluateDiffuseL1Geomerics(lumaL0, float3(lumaL1x, lumaL1y, lumaL1z), normalWorld);

    sh = L0 + normalWorld.x * L1x + normalWorld.y * L1y + normalWorld.z * L1z;
    float regularLumaSH = dot(sh, 1);
    //sh *= regularLumaSH < 0.001 ? 1 : (lumaSH / regularLumaSH);
    sh *= lerp(1, lumaSH / regularLumaSH, saturate(regularLumaSH*16));

    //sh.r = shEvaluateDiffuseL1Geomerics(L0.r, float3(L1x.r, L1y.r, L1z.r), normalWorld);
    //sh.g = shEvaluateDiffuseL1Geomerics(L0.g, float3(L1x.g, L1y.g, L1z.g), normalWorld);
    //sh.b = shEvaluateDiffuseL1Geomerics(L0.b, float3(L1x.b, L1y.b, L1z.b), normalWorld);

#else
    sh = L0 + normalWorld.x * L1x + normalWorld.y * L1y + normalWorld.z * L1z;
#endif

    diffuseColor = max(sh, 0.0);

    specularColor = 0;
    #ifdef BAKERY_LMSPEC
        float3 dominantDir = float3(dot(nL1x, lumaConv), dot(nL1y, lumaConv), dot(nL1z, lumaConv));
        float focus = saturate(length(dominantDir));
        half3 halfDir = Unity_SafeNormalize(normalize(dominantDir) - viewDir);
        half nh = saturate(dot(normalWorld, halfDir));
        half perceptualRoughness = SmoothnessToPerceptualRoughness(smoothness );//* sqrt(focus));
        half roughness = PerceptualRoughnessToRoughness(perceptualRoughness);
        half spec = GGXTerm(nh, roughness);

        sh = L0 + dominantDir.x * L1x + dominantDir.y * L1y + dominantDir.z * L1z;

        specularColor = max(spec * sh, 0.0);
    #endif
}
#endif

#ifdef BAKERY_MONOSH
void BakeryMonoSH(inout float3 diffuseColor, inout float3 specularColor, float2 lmUV, float3 normalWorld, float3 viewDir, float smoothness)
{

#if UNITY_VERSION >= 201740
    float3 dominantDir = unity_LightmapInd.Sample(bakery_trilinear_clamp_sampler, lmUV).xyz;
    float3 L0 = DecodeLightmap(unity_Lightmap.Sample(bakery_trilinear_clamp_sampler, lmUV));
#else
    float3 dominantDir = UNITY_SAMPLE_TEX2D_SAMPLER(unity_LightmapInd, unity_Lightmap, lmUV).xyz;
    float3 L0 = DecodeLightmap(UNITY_SAMPLE_TEX2D_SAMPLER(unity_Lightmap, unity_Lightmap, lmUV));
#endif

    float3 nL1 = dominantDir * 2 - 1;
    float3 L1x = nL1.x * L0 * 2;
    float3 L1y = nL1.y * L0 * 2;
    float3 L1z = nL1.z * L0 * 2;

    float3 sh;
#if BAKERY_SHNONLINEAR
    float lumaL0 = dot(L0, 1);
    float lumaL1x = dot(L1x, 1);
    float lumaL1y = dot(L1y, 1);
    float lumaL1z = dot(L1z, 1);
    float lumaSH = shEvaluateDiffuseL1Geomerics(lumaL0, float3(lumaL1x, lumaL1y, lumaL1z), normalWorld);

    sh = L0 + normalWorld.x * L1x + normalWorld.y * L1y + normalWorld.z * L1z;
    float regularLumaSH = dot(sh, 1);
    //sh *= regularLumaSH < 0.001 ? 1 : (lumaSH / regularLumaSH);
    sh *= lerp(1, lumaSH / regularLumaSH, saturate(regularLumaSH*16));

    //sh.r = shEvaluateDiffuseL1Geomerics(L0.r, float3(L1x.r, L1y.r, L1z.r), normalWorld);
    //sh.g = shEvaluateDiffuseL1Geomerics(L0.g, float3(L1x.g, L1y.g, L1z.g), normalWorld);
    //sh.b = shEvaluateDiffuseL1Geomerics(L0.b, float3(L1x.b, L1y.b, L1z.b), normalWorld);

#else
    sh = L0 + normalWorld.x * L1x + normalWorld.y * L1y + normalWorld.z * L1z;
#endif

    diffuseColor = max(sh, 0.0);

    specularColor = 0;
    #ifdef BAKERY_LMSPEC
        dominantDir = nL1;
        float focus = saturate(length(dominantDir));
        half3 halfDir = Unity_SafeNormalize(normalize(dominantDir) - viewDir);
        half nh = saturate(dot(normalWorld, halfDir));
        half perceptualRoughness = SmoothnessToPerceptualRoughness(smoothness );//* sqrt(focus));
        half roughness = PerceptualRoughnessToRoughness(perceptualRoughness);
        half spec = GGXTerm(nh, roughness);

        sh = L0 + dominantDir.x * L1x + dominantDir.y * L1y + dominantDir.z * L1z;

        specularColor = max(spec * sh, 0.0);
    #endif
}
#endif

half4 bakeryFragForwardBase(BakeryVertexOutputForwardBase i) : SV_Target
{
    FRAGMENT_SETUP(s)
#if UNITY_OPTIMIZE_TEXCUBELOD
    s.reflUVW = i.reflUVW;
#endif

    UnityLight mainLight = MainLight ();
    UNITY_LIGHT_ATTENUATION(atten, i, s.posWorld);

#ifdef BAKERY_VOLUME
    bool isGlobal = _VolumeInvSize.x > 1000000; // ~inf
    float3 volViewDir = s.eyeVec;
    #ifdef BAKERY_VOLROTATION
        float4x4 volMatrix = (isGlobal ? _GlobalVolumeMatrix : _VolumeMatrix);
        float3 volInvSize = (isGlobal ? _GlobalVolumeInvSize : _VolumeInvSize);
        float3 lpUV = mul(volMatrix, float4(s.posWorld,1)).xyz * volInvSize + 0.5f;
        float3 volNormal = mul((float3x3)volMatrix, s.normalWorld);
        #ifdef BAKERY_LMSPEC
            volViewDir = mul((float3x3)volMatrix, volViewDir);
        #endif
    #else
        float3 lpUV = (s.posWorld - (isGlobal ? _GlobalVolumeMin : _VolumeMin)) * (isGlobal ? _GlobalVolumeInvSize : _VolumeInvSize);
        float3 volNormal = s.normalWorld;
    #endif
#endif

#ifdef BAKERY_VOLUME
    mainLight.color *= saturate(dot(_VolumeMask.Sample(sampler_Volume0, lpUV), unity_OcclusionMaskSelector));
#elif BAKERY_VERTEXLMMASK
    if (bakeryLightmapMode == BAKERYMODE_VERTEXLM)
    {
        mainLight.color *= saturate(dot(i.ambientOrLightmapUV, unity_OcclusionMaskSelector));
    }
#endif

    half occlusion = Occlusion(i.tex.xy);
    UnityGI gi = FragmentGI(s, occlusion, i.ambientOrLightmapUV, atten, mainLight);

#ifdef BAKERY_VOLUME

    #ifdef BAKERY_COMPRESSED_VOLUME
        float4 tex0, tex1, tex2, tex3;
        float3 L0, L1x, L1y, L1z;
        tex0 = _Volume0.Sample(sampler_Volume0, lpUV);
        tex1 = _Volume1.Sample(sampler_Volume0, lpUV) * 2 - 1;
        tex2 = _Volume2.Sample(sampler_Volume0, lpUV) * 2 - 1;
        tex3 = _Volume3.Sample(sampler_Volume0, lpUV) * 2 - 1;
        #ifdef BAKERY_COMPRESSED_VOLUME_RGBM
            L0 = tex0.xyz * (tex0.w * 8.0f);
            L0 *= L0;
        #else
            L0 = tex0.xyz;
        #endif
        L1x = tex1.xyz * L0;
        L1y = tex2.xyz * L0;
        L1z = tex3.xyz * L0;
    #else
        float4 tex0, tex1, tex2;
        float3 L0, L1x, L1y, L1z;
        tex0 = _Volume0.Sample(sampler_Volume0, lpUV);
        tex1 = _Volume1.Sample(sampler_Volume0, lpUV);
        tex2 = _Volume2.Sample(sampler_Volume0, lpUV);
        L0 = tex0.xyz;
        L1x = tex1.xyz;
        L1y = tex2.xyz;
        L1z = float3(tex0.w, tex1.w, tex2.w);
    #endif
    gi.indirect.diffuse.r = shEvaluateDiffuseL1Geomerics(L0.r, float3(L1x.r, L1y.r, L1z.r), volNormal);
    gi.indirect.diffuse.g = shEvaluateDiffuseL1Geomerics(L0.g, float3(L1x.g, L1y.g, L1z.g), volNormal);
    gi.indirect.diffuse.b = shEvaluateDiffuseL1Geomerics(L0.b, float3(L1x.b, L1y.b, L1z.b), volNormal);

    #ifdef UNITY_COLORSPACE_GAMMA
        gi.indirect.diffuse = pow(gi.indirect.diffuse, 1.0f / 2.2f);
    #endif

    #ifdef BAKERY_LMSPEC
        float3 nL1x = L1x / L0;
        float3 nL1y = L1y / L0;
        float3 nL1z = L1z / L0;
        float3 dominantDir = float3(dot(nL1x, lumaConv), dot(nL1y, lumaConv), dot(nL1z, lumaConv));
        half3 halfDir = Unity_SafeNormalize(normalize(dominantDir) - volViewDir);
        half nh = saturate(dot(volNormal, halfDir));
        half perceptualRoughness = SmoothnessToPerceptualRoughness(s.smoothness);
        half roughness = PerceptualRoughnessToRoughness(perceptualRoughness);
        half spec = GGXTerm(nh, roughness);
        float3 sh = L0 + dominantDir.x * L1x + dominantDir.y * L1y + dominantDir.z * L1z;
        gi.indirect.specular += max(spec * sh, 0.0);
    #endif

#elif BAKERY_PROBESHNONLINEAR
    float3 L0 = float3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w);
    gi.indirect.diffuse.r = shEvaluateDiffuseL1Geomerics(L0.r, unity_SHAr.xyz, s.normalWorld);
    gi.indirect.diffuse.g = shEvaluateDiffuseL1Geomerics(L0.g, unity_SHAg.xyz, s.normalWorld);
    gi.indirect.diffuse.b = shEvaluateDiffuseL1Geomerics(L0.b, unity_SHAb.xyz, s.normalWorld);
#endif

#ifdef DIRLIGHTMAP_COMBINED
#ifdef BAKERY_LMSPEC
#ifndef BAKERY_MONOSH
    if (bakeryLightmapMode == BAKERYMODE_DEFAULT)
    {
        gi.indirect.specular += BakeryDirectionalLightmapSpecular(i.ambientOrLightmapUV.xy, s.normalWorld, s.eyeVec, s.smoothness) * gi.indirect.diffuse;
    }
#endif
#endif
#endif

#ifdef BAKERY_VERTEXLM
    if (bakeryLightmapMode == BAKERYMODE_VERTEXLM)
    {
        gi.indirect.diffuse = i.color.rgb;
        float3 prevSpec = gi.indirect.specular;

        #if defined(BAKERY_VERTEXLMDIR)
            #ifdef BAKERY_MONOSH
                BakeryVertexLMMonoSH(gi.indirect.diffuse, gi.indirect.specular, i.lightDirection, s.normalWorld, s.eyeVec, s.smoothness);
            #else
                BakeryVertexLMDirection(gi.indirect.diffuse, gi.indirect.specular, i.lightDirection, i.tangentToWorldAndPackedData[2].xyz, s.normalWorld, s.eyeVec, s.smoothness);
            #endif
            gi.indirect.specular += prevSpec;
        #elif defined (BAKERY_VERTEXLMSH)
            BakeryVertexLMSH(gi.indirect.diffuse, gi.indirect.specular, i.shL1x, i.shL1y, i.shL1z, s.normalWorld, s.eyeVec, s.smoothness);
            gi.indirect.specular += prevSpec;
        #endif
    }
#endif

#ifdef BAKERY_RNM
    if (bakeryLightmapMode == BAKERYMODE_RNM)
    {
        #ifdef BAKERY_SSBUMP
            float3 normalMap = tex2D(_BumpMap, i.tex.xy).xyz;
        #else
            float3 normalMap = NormalInTangentSpace(i.tex);
        #endif

        float3 eyeVecT = 0;
        #ifdef BAKERY_LMSPEC
            eyeVecT = -NormalizePerPixelNormal(i.viewDirForParallax);
        #endif

        float3 prevSpec = gi.indirect.specular;
        BakeryRNM(gi.indirect.diffuse, gi.indirect.specular, i.ambientOrLightmapUV.xy, normalMap, s.smoothness, eyeVecT);
        gi.indirect.specular += prevSpec;
    }
#endif

#ifdef BAKERY_SH
    #if SHADER_TARGET >= 30
    if (bakeryLightmapMode == BAKERYMODE_SH)
    #endif
    {
        float3 prevSpec = gi.indirect.specular;
        BakerySH(gi.indirect.diffuse, gi.indirect.specular, i.ambientOrLightmapUV.xy, s.normalWorld, s.eyeVec, s.smoothness);
        gi.indirect.specular += prevSpec;
    }
#endif

#ifdef DIRLIGHTMAP_COMBINED
#ifdef BAKERY_MONOSH
    if (bakeryLightmapMode != BAKERYMODE_VERTEXLM)
    {
        float3 prevSpec = gi.indirect.specular;
        BakeryMonoSH(gi.indirect.diffuse, gi.indirect.specular, i.ambientOrLightmapUV.xy, s.normalWorld, s.eyeVec, s.smoothness);
        gi.indirect.specular += prevSpec;
    }
#endif
#endif

    half4 c = UNITY_BRDF_PBS(s.diffColor, s.specColor, s.oneMinusReflectivity, s.smoothness, s.normalWorld, -s.eyeVec, gi.light, gi.indirect);

    c.rgb += UNITY_BRDF_GI(s.diffColor, s.specColor, s.oneMinusReflectivity, s.smoothness, s.normalWorld, -s.eyeVec, occlusion, gi);
    c.rgb += Emission(i.tex.xy);

    UNITY_APPLY_FOG(i.fogCoord, c.rgb);

    return OutputForward(c, s.alpha);
}


//  Additive forward pass (one light per pass)
struct BakeryVertexOutputForwardAdd
{
    float4 pos                          : SV_POSITION;
    float4 tex                          : TEXCOORD0;
    half3 eyeVec                        : TEXCOORD1;
#if UNITY_VERSION >= 201740
    float4 tangentToWorldAndLightDir[3]    : TEXCOORD2;    // [3x3:tangentToWorld | 1x3:viewDirForParallax]
#else
    half4 tangentToWorldAndLightDir[3]    : TEXCOORD2;    // [3x3:tangentToWorld | 1x3:viewDirForParallax]
#endif
    float3 posWorld                     : TEXCOORD5;
    UNITY_SHADOW_COORDS(6)
    UNITY_FOG_COORDS(7)

        // next ones would not fit into SM2.0 limits, but they are always for SM3.0+
#if defined(_PARALLAXMAP)
    half3 viewDirForParallax            : TEXCOORD8;
#endif

#ifdef BAKERY_VERTEXLMMASK
    fixed4 shadowMask : COLOR;
#endif

    UNITY_VERTEX_OUTPUT_STEREO
};

BakeryVertexOutputForwardAdd bakeryVertForwardAdd(BakeryVertexInput v)
{
    UNITY_SETUP_INSTANCE_ID(v);
    BakeryVertexOutputForwardAdd o;
    UNITY_INITIALIZE_OUTPUT(BakeryVertexOutputForwardAdd, o);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    float4 posWorld = mul(unity_ObjectToWorld, v.vertex);
    o.pos = UnityObjectToClipPos(v.vertex);

    o.tex = BakeryTexCoords(v);
    o.eyeVec = NormalizePerVertexNormal(posWorld.xyz - _WorldSpaceCameraPos);
    o.posWorld = posWorld.xyz;
    float3 normalWorld = UnityObjectToWorldNormal(v.normal);
#ifdef _TANGENT_TO_WORLD
    float4 tangentWorld = float4(UnityObjectToWorldDir(v.tangent.xyz), v.tangent.w);

    float3x3 tangentToWorld = CreateTangentToWorldPerVertex(normalWorld, tangentWorld.xyz, tangentWorld.w);
    o.tangentToWorldAndLightDir[0].xyz = tangentToWorld[0];
    o.tangentToWorldAndLightDir[1].xyz = tangentToWorld[1];
    o.tangentToWorldAndLightDir[2].xyz = tangentToWorld[2];
#else
    o.tangentToWorldAndLightDir[0].xyz = 0;
    o.tangentToWorldAndLightDir[1].xyz = 0;
    o.tangentToWorldAndLightDir[2].xyz = normalWorld;
#endif
    //We need this for shadow receving
    UNITY_TRANSFER_SHADOW(o, v.uv1);

    float3 lightDir = _WorldSpaceLightPos0.xyz - posWorld.xyz * _WorldSpaceLightPos0.w;
#ifndef USING_DIRECTIONAL_LIGHT
    lightDir = NormalizePerVertexNormal(lightDir);
#endif
    o.tangentToWorldAndLightDir[0].w = lightDir.x;
    o.tangentToWorldAndLightDir[1].w = lightDir.y;
    o.tangentToWorldAndLightDir[2].w = lightDir.z;

#ifdef _PARALLAXMAP
    TANGENT_SPACE_ROTATION;
    o.viewDirForParallax = mul(rotation, ObjSpaceViewDir(v.vertex));
#endif

#ifdef BAKERY_VERTEXLMMASK
    o.shadowMask = unpack4NFloats(v.uv1.x);
#endif

    UNITY_TRANSFER_FOG(o, o.pos);
    return o;
}

half4 bakeryFragForwardAdd(BakeryVertexOutputForwardAdd i) : SV_Target
{
    FRAGMENT_SETUP_FWDADD(s)

    UNITY_LIGHT_ATTENUATION(atten, i, s.posWorld)
    UnityLight light = AdditiveLight (IN_LIGHTDIR_FWDADD(i), atten);
    UnityIndirect noIndirect = ZeroIndirect ();

    half4 c = UNITY_BRDF_PBS(s.diffColor, s.specColor, s.oneMinusReflectivity, s.smoothness, s.normalWorld, -s.eyeVec, light, noIndirect);

#ifdef BAKERY_VOLUME
    bool isGlobal = _VolumeInvSize.x > 1000000; // ~inf

    #ifdef BAKERY_VOLROTATION
        float4x4 volMatrix = (isGlobal ? _GlobalVolumeMatrix : _VolumeMatrix);
        float3 volInvSize = (isGlobal ? _GlobalVolumeInvSize : _VolumeInvSize);
        float3 lpUV = mul(volMatrix, float4(s.posWorld,1)).xyz * volInvSize + 0.5f;
    #else
        float3 lpUV = (s.posWorld - (isGlobal ? _GlobalVolumeMin : _VolumeMin)) * (isGlobal ? _GlobalVolumeInvSize : _VolumeInvSize);
    #endif

    c *= saturate(dot(_VolumeMask.Sample(sampler_Volume0, lpUV), unity_OcclusionMaskSelector));

#elif BAKERY_VERTEXLMMASK
    if (bakeryLightmapMode == BAKERYMODE_VERTEXLM)
    {
        c *= saturate(dot(i.shadowMask, unity_OcclusionMaskSelector));
    }
#endif

    UNITY_APPLY_FOG_COLOR(i.fogCoord, c.rgb, half4(0,0,0,0)); // fog towards black in additive pass

    return OutputForward(c, s.alpha);
}


//Deferred Pass
struct BakeryVertexOutputDeferred
{
    float4 pos                          : SV_POSITION;
    float4 tex                          : TEXCOORD0;
    half3 eyeVec                        : TEXCOORD1;

#if UNITY_VERSION >= 201740
    float4 tangentToWorldAndPackedData[3]    : TEXCOORD2;    // [3x3:tangentToWorld | 1x3:viewDirForParallax]
#else
    half4 tangentToWorldAndPackedData[3]    : TEXCOORD2;    // [3x3:tangentToWorld | 1x3:viewDirForParallax]
#endif

#if defined(BAKERY_RNMSPEC)
    half3 viewDirForParallax            : TEXCOORD9;
#endif

    half4 ambientOrLightmapUV           : TEXCOORD5;    // SH or Lightmap UVs

#ifdef BAKERY_VERTEXLM
    fixed4 color : COLOR;
    #if defined(BAKERY_VERTEXLMDIR)
        float3 lightDirection : TEXCOORD8;
    #elif defined(BAKERY_VERTEXLMSH)
        float3 shL1x : TEXCOORD8_centroid;
        float3 shL1y : TEXCOORD10_centroid;
        float3 shL1z : TEXCOORD11_centroid;
    #endif
#endif

#if UNITY_SPECCUBE_BOX_PROJECTION || UNITY_LIGHT_PROBE_PROXY_VOLUME || (UNITY_REQUIRE_FRAG_WORLDPOS && !UNITY_PACK_WORLDPOS_WITH_TANGENT) || BAKERY_VOLUME
    float3 posWorld                     : TEXCOORD6;
#endif

#if UNITY_OPTIMIZE_TEXCUBELOD
#if UNITY_SPECCUBE_BOX_PROJECTION
    half3 reflUVW               : TEXCOORD7;
#else
    half3 reflUVW               : TEXCOORD6;
#endif
#endif

    UNITY_VERTEX_OUTPUT_STEREO
};

BakeryVertexOutputDeferred bakeryVertDeferred(BakeryVertexInput v)
{
    UNITY_SETUP_INSTANCE_ID(v);
    BakeryVertexOutputDeferred o;
    UNITY_INITIALIZE_OUTPUT(BakeryVertexOutputDeferred, o);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    float4 posWorld = mul(unity_ObjectToWorld, v.vertex);
#if UNITY_SPECCUBE_BOX_PROJECTION || UNITY_LIGHT_PROBE_PROXY_VOLUME || BAKERY_VOLUME
    o.posWorld = posWorld;
#endif
    o.pos = UnityObjectToClipPos(v.vertex);

    o.tex = BakeryTexCoords(v);
    o.eyeVec = NormalizePerVertexNormal(posWorld.xyz - _WorldSpaceCameraPos);
    float3 normalWorld = UnityObjectToWorldNormal(v.normal);
#ifdef _TANGENT_TO_WORLD
    float4 tangentWorld = float4(UnityObjectToWorldDir(v.tangent.xyz), v.tangent.w);

    float3x3 tangentToWorld = CreateTangentToWorldPerVertex(normalWorld, tangentWorld.xyz, tangentWorld.w);
    o.tangentToWorldAndPackedData[0].xyz = tangentToWorld[0];
    o.tangentToWorldAndPackedData[1].xyz = tangentToWorld[1];
    o.tangentToWorldAndPackedData[2].xyz = tangentToWorld[2];
#else
    o.tangentToWorldAndPackedData[0].xyz = 0;
    o.tangentToWorldAndPackedData[1].xyz = 0;
    o.tangentToWorldAndPackedData[2].xyz = normalWorld;
#endif

    o.ambientOrLightmapUV = 0;

#ifndef LIGHTMAP_OFF
    o.ambientOrLightmapUV.xy = v.uv1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
#elif UNITY_SHOULD_SAMPLE_SH
    o.ambientOrLightmapUV.rgb = ShadeSHPerVertex(normalWorld, o.ambientOrLightmapUV.rgb);
#endif
#ifdef DYNAMICLIGHTMAP_ON
    o.ambientOrLightmapUV.zw = v.uv2.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
#endif

#ifdef BAKERY_VERTEXLMMASK
    if (bakeryLightmapMode == BAKERYMODE_VERTEXLM)
    {
        o.ambientOrLightmapUV = unpack4NFloats(v.uv1);
    }
#endif

#if defined(_PARALLAXMAP) || defined(BAKERY_RNMSPEC)
    TANGENT_SPACE_ROTATION;
#endif

#if defined(_PARALLAXMAP)
    half3 viewDirForParallax = mul(rotation, ObjSpaceViewDir(v.vertex));
    o.tangentToWorldAndPackedData[0].w = viewDirForParallax.x;
    o.tangentToWorldAndPackedData[1].w = viewDirForParallax.y;
    o.tangentToWorldAndPackedData[2].w = viewDirForParallax.z;
#endif

#if defined(BAKERY_RNMSPEC)
    o.viewDirForParallax = mul(rotation, ObjSpaceViewDir(v.vertex));
#endif

#ifdef BAKERY_VERTEXLM
    // Unpack from RGBM
    o.color = v.color;
    o.color.rgb *= o.color.a * 8.0f;
    o.color.rgb *= o.color.rgb;

    #if defined(BAKERY_VERTEXLMDIR)
        o.lightDirection = unpack3NFloats(v.uv1.y) * 2 - 1;
    #elif defined(BAKERY_VERTEXLMSH)
        o.shL1x = unpack3NFloats(v.uv1.y) * 2 - 1;
        o.shL1y = unpack3NFloats(v.uv3.x) * 2 - 1;
        o.shL1z = unpack3NFloats(v.uv3.y) * 2 - 1;
    #endif
#endif

#if UNITY_OPTIMIZE_TEXCUBELOD
    o.reflUVW = reflect(o.eyeVec, normalWorld);
#endif

    return o;
}

void bakeryFragDeferred(
    BakeryVertexOutputDeferred i,
    out half4 outDiffuse : SV_Target0,          // RT0: diffuse color (rgb), occlusion (a)
    out half4 outSpecSmoothness : SV_Target1,   // RT1: spec color (rgb), smoothness (a)
    out half4 outNormal : SV_Target2,           // RT2: normal (rgb), --unused, very low precision-- (a)
    out half4 outEmission : SV_Target3          // RT3: emission (rgb), --unused-- (a)
#if defined(SHADOWS_SHADOWMASK) && (UNITY_ALLOWED_MRT_COUNT > 4)
    ,out half4 outShadowMask : SV_Target4       // RT4: shadowmask (rgba)
#endif
)
{
#if (SHADER_TARGET < 30)
    outDiffuse = 1;
    outSpecSmoothness = 1;
    outNormal = 0;
    outEmission = 0;
    #if defined(SHADOWS_SHADOWMASK) && (UNITY_ALLOWED_MRT_COUNT > 4)
        outShadowMask = 1;
    #endif
    return;
#endif

    FRAGMENT_SETUP(s)
#if UNITY_OPTIMIZE_TEXCUBELOD
        s.reflUVW = i.reflUVW;
#endif

    // no analytic lights in this pass
    UnityLight dummyLight = DummyLight();
    half atten = 1;

    // only GI
    half occlusion = Occlusion(i.tex.xy);
#if UNITY_ENABLE_REFLECTION_BUFFERS
    bool sampleReflectionsInDeferred = false;
#else
    bool sampleReflectionsInDeferred = true;
#endif

    UnityGI gi = FragmentGI(s, occlusion, i.ambientOrLightmapUV, atten, dummyLight, sampleReflectionsInDeferred);

#ifdef BAKERY_VOLUME
    bool isGlobal = _VolumeInvSize.x > 1000000; // ~inf
    float3 volViewDir = s.eyeVec;

    #ifdef BAKERY_VOLROTATION
        float4x4 volMatrix = (isGlobal ? _GlobalVolumeMatrix : _VolumeMatrix);
        float3 volInvSize = (isGlobal ? _GlobalVolumeInvSize : _VolumeInvSize);
        float3 lpUV = mul(volMatrix, float4(i.posWorld,1)).xyz * volInvSize + 0.5f;
        float3 volNormal = mul((float3x3)volMatrix, s.normalWorld);
        #ifdef BAKERY_LMSPEC
            volViewDir = mul((float3x3)volMatrix, volViewDir);
        #endif
    #else
        float3 lpUV = (i.posWorld - (isGlobal ? _GlobalVolumeMin : _VolumeMin)) * (isGlobal ? _GlobalVolumeInvSize : _VolumeInvSize);
        float3 volNormal = s.normalWorld;
    #endif

    #ifdef BAKERY_COMPRESSED_VOLUME
        float4 tex0, tex1, tex2, tex3;
        float3 L0, L1x, L1y, L1z;
        tex0 = _Volume0.Sample(sampler_Volume0, lpUV);
        tex1 = _Volume1.Sample(sampler_Volume0, lpUV) * 2 - 1;
        tex2 = _Volume2.Sample(sampler_Volume0, lpUV) * 2 - 1;
        tex3 = _Volume3.Sample(sampler_Volume0, lpUV) * 2 - 1;
        #ifdef BAKERY_COMPRESSED_VOLUME_RGBM
            L0 = tex0.xyz * (tex0.w * 8.0f);
            L0 *= L0;
        #else
            L0 = tex0.xyz;
        #endif
        L1x = tex1.xyz * L0;
        L1y = tex2.xyz * L0;
        L1z = tex3.xyz * L0;
    #else
        float4 tex0, tex1, tex2;
        float3 L0, L1x, L1y, L1z;
        tex0 = _Volume0.Sample(sampler_Volume0, lpUV);
        tex1 = _Volume1.Sample(sampler_Volume0, lpUV);
        tex2 = _Volume2.Sample(sampler_Volume0, lpUV);
        L0 = tex0.xyz;
        L1x = tex1.xyz;
        L1y = tex2.xyz;
        L1z = float3(tex0.w, tex1.w, tex2.w);
    #endif

    gi.indirect.diffuse.r = shEvaluateDiffuseL1Geomerics(L0.r, float3(L1x.r, L1y.r, L1z.r), volNormal);
    gi.indirect.diffuse.g = shEvaluateDiffuseL1Geomerics(L0.g, float3(L1x.g, L1y.g, L1z.g), volNormal);
    gi.indirect.diffuse.b = shEvaluateDiffuseL1Geomerics(L0.b, float3(L1x.b, L1y.b, L1z.b), volNormal);
    #ifdef UNITY_COLORSPACE_GAMMA
        gi.indirect.diffuse = pow(gi.indirect.diffuse, 1.0f / 2.2f);
    #endif

    #ifdef BAKERY_LMSPEC
        float3 nL1x = L1x / L0;
        float3 nL1y = L1y / L0;
        float3 nL1z = L1z / L0;
        float3 dominantDir = float3(dot(nL1x, lumaConv), dot(nL1y, lumaConv), dot(nL1z, lumaConv));
        half3 halfDir = Unity_SafeNormalize(normalize(dominantDir) - volViewDir);
        half nh = saturate(dot(volNormal, halfDir));
        half perceptualRoughness = SmoothnessToPerceptualRoughness(s.smoothness);
        half roughness = PerceptualRoughnessToRoughness(perceptualRoughness);
        half spec = GGXTerm(nh, roughness);
        float3 sh = L0 + dominantDir.x * L1x + dominantDir.y * L1y + dominantDir.z * L1z;
        gi.indirect.specular += max(spec * sh, 0.0);
    #endif

#elif BAKERY_PROBESHNONLINEAR
    float3 L0 = float3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w);
    gi.indirect.diffuse.r = shEvaluateDiffuseL1Geomerics(L0.r, unity_SHAr.xyz, s.normalWorld);
    gi.indirect.diffuse.g = shEvaluateDiffuseL1Geomerics(L0.g, unity_SHAg.xyz, s.normalWorld);
    gi.indirect.diffuse.b = shEvaluateDiffuseL1Geomerics(L0.b, unity_SHAb.xyz, s.normalWorld);
#endif

#ifdef DIRLIGHTMAP_COMBINED
#ifdef BAKERY_LMSPEC
#ifndef BAKERY_MONOSH
    if (bakeryLightmapMode == BAKERYMODE_DEFAULT)
    {
        gi.indirect.specular += BakeryDirectionalLightmapSpecular(i.ambientOrLightmapUV.xy, s.normalWorld, s.eyeVec, s.smoothness) * gi.indirect.diffuse;
    }
#endif
#endif
#endif

#ifdef BAKERY_VERTEXLM
    if (bakeryLightmapMode == BAKERYMODE_VERTEXLM)
    {
        gi.indirect.diffuse = i.color.rgb;
        float3 prevSpec = gi.indirect.specular;

        #if defined(BAKERY_VERTEXLMDIR)
            #ifdef BAKERY_MONOSH
                BakeryVertexLMMonoSH(gi.indirect.diffuse, gi.indirect.specular, i.lightDirection, s.normalWorld, s.eyeVec, s.smoothness);
            #else
                BakeryVertexLMDirection(gi.indirect.diffuse, gi.indirect.specular, i.lightDirection, i.tangentToWorldAndPackedData[2].xyz, s.normalWorld, s.eyeVec, s.smoothness);
            #endif
            gi.indirect.specular += prevSpec;
        #elif defined (BAKERY_VERTEXLMSH)
            BakeryVertexLMSH(gi.indirect.diffuse, gi.indirect.specular, i.shL1x, i.shL1y, i.shL1z, s.normalWorld, s.eyeVec, s.smoothness);
            gi.indirect.specular += prevSpec;
        #endif
    }
#endif

#ifdef BAKERY_RNM
    if (bakeryLightmapMode == BAKERYMODE_RNM)
    {
        #ifdef BAKERY_SSBUMP
            float3 normalMap = tex2D(_BumpMap, i.tex.xy).xyz;
        #else
            float3 normalMap = NormalInTangentSpace(i.tex);
        #endif

        float3 eyeVecT = 0;
        #ifdef BAKERY_LMSPEC
            eyeVecT = -NormalizePerPixelNormal(i.viewDirForParallax);
        #endif

        float3 prevSpec = gi.indirect.specular;
        BakeryRNM(gi.indirect.diffuse, gi.indirect.specular, i.ambientOrLightmapUV.xy, normalMap, s.smoothness, eyeVecT);
        gi.indirect.specular += prevSpec;
    }
#endif

#ifdef BAKERY_SH
    #if SHADER_TARGET >= 30
    if (bakeryLightmapMode == BAKERYMODE_SH)
    #endif
    {
        float3 prevSpec = gi.indirect.specular;
        BakerySH(gi.indirect.diffuse, gi.indirect.specular, i.ambientOrLightmapUV.xy, s.normalWorld, s.eyeVec, s.smoothness);
        gi.indirect.specular += prevSpec;
    }
#endif

#ifdef DIRLIGHTMAP_COMBINED
#ifdef BAKERY_MONOSH
    if (bakeryLightmapMode != BAKERYMODE_VERTEXLM)
    {
        float3 prevSpec = gi.indirect.specular;
        BakeryMonoSH(gi.indirect.diffuse, gi.indirect.specular, i.ambientOrLightmapUV.xy, s.normalWorld, s.eyeVec, s.smoothness);
        gi.indirect.specular += prevSpec;
    }
#endif
#endif

    half3 color = UNITY_BRDF_PBS(s.diffColor, s.specColor, s.oneMinusReflectivity, s.smoothness, s.normalWorld, -s.eyeVec, gi.light, gi.indirect).rgb;

    color += UNITY_BRDF_GI(s.diffColor, s.specColor, s.oneMinusReflectivity, s.smoothness, s.normalWorld, -s.eyeVec, occlusion, gi);

#ifdef _EMISSION
    color += Emission(i.tex.xy);
#endif

#ifndef UNITY_HDR_ON
    color.rgb = exp2(-color.rgb);
#endif

    outDiffuse = half4(s.diffColor, occlusion);
    outSpecSmoothness = half4(s.specColor, s.smoothness);
    outNormal = half4(s.normalWorld*0.5 + 0.5, 1);
    outEmission = half4(color, 1);

// Baked direct lighting occlusion if any
#if defined(SHADOWS_SHADOWMASK) && (UNITY_ALLOWED_MRT_COUNT > 4)
    #ifdef BAKERY_VOLUME
        outShadowMask = _VolumeMask.Sample(sampler_Volume0, lpUV);
    #elif BAKERY_VERTEXLMMASK
        outShadowMask = i.ambientOrLightmapUV;
    #else
        outShadowMask = UnityGetRawBakedOcclusions(i.ambientOrLightmapUV.xy, IN_WORLDPOS(i));
    #endif
#endif
}

#endif
