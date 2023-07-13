#ifdef LIGHTMAP_RGBM_SCALE

#ifndef FTRACE_INCLUDED
#define FTRACE_INCLUDED

// Bicubic interpolation

float ftBicubic_w0(float a)
{
    return (1.0f/6.0f)*(a*(a*(-a + 3.0f) - 3.0f) + 1.0f);
}

float ftBicubic_w1(float a)
{
    return (1.0f/6.0f)*(a*a*(3.0f*a - 6.0f) + 4.0f);
}

float ftBicubic_w2(float a)
{
    return (1.0f/6.0f)*(a*(a*(-3.0f*a + 3.0f) + 3.0f) + 1.0f);
}

float ftBicubic_w3(float a)
{
    return (1.0f/6.0f)*(a*a*a);
}

float ftBicubic_g0(float a)
{
    return ftBicubic_w0(a) + ftBicubic_w1(a);
}

float ftBicubic_g1(float a)
{
    return ftBicubic_w2(a) + ftBicubic_w3(a);
}

float ftBicubic_h0(float a)
{
    return -1.0f + ftBicubic_w1(a) / (ftBicubic_w0(a) + ftBicubic_w1(a)) + 0.5f;
}

float ftBicubic_h1(float a)
{
    return 1.0f + ftBicubic_w3(a) / (ftBicubic_w2(a) + ftBicubic_w3(a)) + 0.5f;
}

#ifdef SHADER_API_D3D11
#if defined (SHADOWS_SHADOWMASK)

float4 ftBicubicSampleShadow3( Texture2D tex, SamplerState ftShadowSampler, float2 uv )
{
    float width, height;
    tex.GetDimensions(width, height);

    float x = uv.x * width;
    float y = uv.y * height;

    x -= 0.5f;
    y -= 0.5f;

    float px = floor(x);
    float py = floor(y);

    float fx = x - px;
    float fy = y - py;

    float g0x = ftBicubic_g0(fx);
    float g1x = ftBicubic_g1(fx);
    float h0x = ftBicubic_h0(fx);
    float h1x = ftBicubic_h1(fx);
    float h0y = ftBicubic_h0(fy);
    float h1y = ftBicubic_h1(fy);

    float4 r = ftBicubic_g0(fy) * ( g0x * tex.Sample(ftShadowSampler, (float2(px + h0x, py + h0y) * 1.0f/width))   +
                          g1x * tex.Sample(ftShadowSampler, (float2(px + h1x, py + h0y) * 1.0f/width))) +

               ftBicubic_g1(fy) * ( g0x * tex.Sample(ftShadowSampler, (float2(px + h0x, py + h1y) * 1.0f/width))   +
                          g1x * tex.Sample(ftShadowSampler, (float2(px + h1x, py + h1y) * 1.0f/width)));
    return r;
}

#if UNITY_VERSION >= 201740
    SamplerState bakery_trilinear_clamp_sampler2;
#endif

float4 ftBicubicSampleShadow( Texture2D tex, float2 uv )
{
    #if UNITY_VERSION >= 201740
        SamplerState samplerMask = bakery_trilinear_clamp_sampler2;
    #else
        #if defined(LIGHTMAP_ON)
        SamplerState samplerMask = samplerunity_Lightmap;
        #else
        SamplerState samplerMask = samplerunity_ShadowMask;
        #endif
    #endif

    return ftBicubicSampleShadow3(tex, samplerMask, uv);
}

#define ftBicubicSampleShadow2(t,s,u) ftBicubicSampleShadow3(t, sampler##s, u)

#else
#define ftBicubicSampleShadow UNITY_SAMPLE_TEX2D
#define ftBicubicSampleShadow2 UNITY_SAMPLE_TEX2D_SAMPLER
#endif
#else
#define ftBicubicSampleShadow UNITY_SAMPLE_TEX2D
#define ftBicubicSampleShadow2 UNITY_SAMPLE_TEX2D_SAMPLER
#endif

float3 ftLightmapBicubic( float2 uv )
{
    #ifdef SHADER_API_D3D11
        float width, height;
        unity_Lightmap.GetDimensions(width, height);

        float x = uv.x * width;
        float y = uv.y * height;

        x -= 0.5f;
        y -= 0.5f;

        float px = floor(x);
        float py = floor(y);

        float fx = x - px;
        float fy = y - py;

        // note: we could store these functions in a lookup table texture, but maths is cheap
        float g0x = ftBicubic_g0(fx);
        float g1x = ftBicubic_g1(fx);
        float h0x = ftBicubic_h0(fx);
        float h1x = ftBicubic_h1(fx);
        float h0y = ftBicubic_h0(fy);
        float h1y = ftBicubic_h1(fy);

        float4 r = ftBicubic_g0(fy) * ( g0x * UNITY_SAMPLE_TEX2D(unity_Lightmap, (float2(px + h0x, py + h0y) * 1.0f/width))   +
                              g1x * UNITY_SAMPLE_TEX2D(unity_Lightmap, (float2(px + h1x, py + h0y) * 1.0f/width))) +

                   ftBicubic_g1(fy) * ( g0x * UNITY_SAMPLE_TEX2D(unity_Lightmap, (float2(px + h0x, py + h1y) * 1.0f/width))   +
                              g1x * UNITY_SAMPLE_TEX2D(unity_Lightmap, (float2(px + h1x, py + h1y) * 1.0f/width)));
        return DecodeLightmap(r);
    #else
        return DecodeLightmap(UNITY_SAMPLE_TEX2D(unity_Lightmap, uv));
    #endif
}


// Light falloff

float ftLightFalloff(float4x4 ftUnityLightMatrix, float3 worldPos)
{
    float3 lightCoord = mul(ftUnityLightMatrix, float4(worldPos, 1)).xyz / ftUnityLightMatrix._11;
    float distSq = dot(lightCoord, lightCoord);
    float falloff = saturate(1.0f - pow(sqrt(distSq) * ftUnityLightMatrix._11, 4.0f)) / (distSq + 1.0f);
    return falloff;
}

float ftLightFalloff(float4 lightPosRadius, float3 worldPos)
{
    float3 lightCoord = worldPos - lightPosRadius.xyz;
    float distSq = dot(lightCoord, lightCoord);
    float falloff = saturate(1.0f - pow(sqrt(distSq * lightPosRadius.w), 4.0f)) / (distSq + 1.0f);
    return falloff;
}

#endif

#endif
