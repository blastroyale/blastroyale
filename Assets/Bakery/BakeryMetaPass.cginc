#ifndef BAKERY_META
#define BAKERY_META

Texture2D bestFitNormalMap;
float _IsFlipped;

struct BakeryMetaInput
{
    float2 uv0 : TEXCOORD0;
    float2 uv1 : TEXCOORD1;
    float3 normal : NORMAL;
#ifndef _TERRAIN_NORMAL_MAP
    float4 tangent : TANGENT;
#endif
};

struct v2f_bakeryMeta
{
    float4 pos      : SV_POSITION;
    float2 uv       : TEXCOORD0;
    float3 normal   : TEXCOORD1;
    float3 tangent  : TEXCOORD2;
    float3 binormal : TEXCOORD3;
};

v2f_bakeryMeta vert_bakerymt (BakeryMetaInput v)
{
    v2f_bakeryMeta o;
    o.pos = float4(((v.uv1.xy * unity_LightmapST.xy + unity_LightmapST.zw)*2-1) * float2(1,-1), 0.5, 1);
    o.uv = v.uv0;
    o.normal = normalize(mul((float3x3)unity_ObjectToWorld, v.normal).xyz);

#ifdef _TERRAIN_NORMAL_MAP
    o.tangent = cross(o.normal, float3(0,0,1));
    o.binormal = cross(o.normal, o.tangent) * -1;
#else
    o.tangent = normalize(mul((float3x3)unity_ObjectToWorld, v.tangent.xyz).xyz);
    o.binormal = cross(o.normal, o.tangent) * v.tangent.w * _IsFlipped;
#endif

    return o;
}

float3 EncodeNormalBestFit(float3 n)
{
    float3 nU = abs(n);
    float maxNAbs = max(nU.z, max(nU.x, nU.y));
    float2 TC = nU.z<maxNAbs? (nU.y<maxNAbs? nU.yz : nU.xz) : nU.xy;
    //if (TC.x != TC.y)
    //{
        TC = TC.x<TC.y? TC.yx : TC.xy;
        TC.y /= TC.x;

        n /= maxNAbs;
        float fittingScale = bestFitNormalMap.Load(int3(TC.x*1023, TC.y*1023, 0)).a;
        n *= fittingScale;
    //}
    return n*0.5+0.5;
}

float3 TransformNormalMapToWorld(v2f_bakeryMeta i, float3 tangentNormalMap)
{
    float3x3 TBN = float3x3(normalize(i.tangent), normalize(i.binormal), normalize(i.normal));
    return mul(tangentNormalMap, TBN);
}

#define BakeryEncodeNormal EncodeNormalBestFit

#endif
