struct appdata
{
    float4 vertex : POSITION;
    float2 uv : TEXCOORD0;
    float3 normal : NORMAL0;
};

struct v2f
{
    float4 vertex : SV_POSITION;
    float2 uv : TEXCOORD0;
    float3 normal : TEXCOORD1;
    float3 worldPos : TEXCOORD2;
};

sampler2D _MainTex;
float4 _MainTex_ST;
//float4 _Color;
//float4x4 cullMatrices[64];
//float cullMatricesCount;

float2 texelOffset;

v2f vert (appdata v)
{
    v2f o;

    o.vertex = mul(unity_ObjectToWorld, v.vertex);
    o.worldPos = o.vertex.xyz;

    o.vertex = mul(UNITY_MATRIX_VP, float4(o.vertex.xyz,1));
    o.vertex.xy += texelOffset * o.vertex.w;

    o.uv = TRANSFORM_TEX(v.uv, _MainTex);

    o.normal = normalize(mul((float3x3)unity_ObjectToWorld, v.normal));

    return o;
}

struct MRT
{
    float4 AlbedoAlpha : SV_Target0;
    float4 Normal : SV_Target1;
};

MRT frag (v2f IN)
{
    /*
    for(int i=0; i<cullMatricesCount; i++)
    {
        float3 w = mul(cullMatrices[i], float4(IN.worldPos,1)).xyz;
        w = abs(w * 2);
        bool outside = (max(max(w.x, w.y), w.z) > 1);
        if (!outside) discard;
    }
    */

    MRT mrt;
    mrt.AlbedoAlpha = tex2D(_MainTex, IN.uv);// * _Color;
#ifdef OPAQUE
    mrt.AlbedoAlpha.a = 1;
#else
    mrt.AlbedoAlpha.a = max(mrt.AlbedoAlpha.a, 1.0f / 255.0f); // 0 is reserved for projClip
#endif
    mrt.Normal = float4(IN.normal * 0.5 + 0.5, 1);

    return mrt;
}

