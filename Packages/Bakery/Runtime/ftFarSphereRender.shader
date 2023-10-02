// Used on the scene when capturing from BakerySectors
// Outputs albedo, normal and alpha

// Shader replacement logic will select subshaders by RenderType
// In SRP we'll choose a pass instead

Shader "Hidden/ftFarSphereRender"
{
    Properties
    {
        //_Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        // Used by SRP
        Tags { "RenderType"="NotReally" }
        Pass
        {
            // Opaque
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #define OPAQUE
            #include "ftFarSphereRenderBase.cginc"
            ENDCG
        }
        Pass
        {
            // Transparent
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "ftFarSphereRenderBase.cginc"
            ENDCG
        }
        Pass
        {
            // TransparentCutout
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "ftFarSphereRenderBase.cginc"
            ENDCG
        }
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #define OPAQUE
            #include "ftFarSphereRenderBase.cginc"
            ENDCG
        }
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "ftFarSphereRenderBase.cginc"
            ENDCG
        }
    }
    SubShader
    {
        Tags { "RenderType"="TransparentCutout" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "ftFarSphereRenderBase.cginc"
            ENDCG
        }
    }
}
