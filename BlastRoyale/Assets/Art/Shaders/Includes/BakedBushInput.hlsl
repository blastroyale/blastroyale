#ifndef UNIVERSAL_BAKEDLIT_INPUT_INCLUDED
#define UNIVERSAL_BAKEDLIT_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

TEXTURE2D(_SwayNoiseTex);
SAMPLER(sampler_SwayNoiseTex);

CBUFFER_START(UnityPerMaterial)
float4 _BaseMap_ST;
half4 _BaseColor;
half _Cutoff;
half _Glossiness;
half _Metallic;
half _Surface;

half _SwayStrength;
half _SwaySpeed;
half _SwayHeight;
half _SwayNoiseStrength;
half _SwayNoiseScale;
half4 _PlayerPos;
half _DeflectDistance;
half _DeflectStrength;
half _DeflectHeight;
CBUFFER_END

#ifdef UNITY_DOTS_INSTANCING_ENABLED
    UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
        UNITY_DOTS_INSTANCED_PROP(float4, _BaseColor)
        UNITY_DOTS_INSTANCED_PROP(float , _Cutoff)
        UNITY_DOTS_INSTANCED_PROP(float , _Glossiness)
        UNITY_DOTS_INSTANCED_PROP(float , _Metallic)
        UNITY_DOTS_INSTANCED_PROP(float , _Surface)
    UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)

    #define _BaseColor          UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4 , _BaseColor)
    #define _Cutoff             UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float  , _Cutoff)
    #define _Glossiness         UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float  , _Glossiness)
    #define _Metallic           UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float  , _Metallic)
    #define _Surface            UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float  , _Surface)
#endif

#endif
