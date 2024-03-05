#include "LAYERMASKS.cginc"

#define CRP_Target_COLOUR SV_TARGET0
#define CRP_Target_NORMAL SV_TARGET1
#define CRP_Target_LAYER SV_TARGET2

#define EncodeUintToFloat(u) (asfloat(u | 0x40000000))
#define DecodeFloatToUint(f) (asuint(f) & 0xBFFFFFFF)

#define COMPUTE_VIEW_NORMAL(n) mul(UNITY_MATRIX_IT_MV, n);

float2 OctWrap(float2 v)
{
    return (1.0 - abs(v.yx)) * (v.xy >= 0.0 ? 1.0 : -1.0);
}

float2 EncodeNormal(float3 n)
{
    n /= (abs(n.x) + abs(n.y) + abs(n.z));
    n.xy = n.z >= 0.0 ? n.xy : OctWrap(n.xy);
    return n.xy;
}

float3 DecodeNormal(float2 encN)
{
    float3 n;
    n.z = 1.0 - abs(encN.x) - abs(encN.y);
    n.xy = n.z >= 0.0 ? encN.xy : OctWrap(encN.xy);
    n = normalize(n);
    return n;
}

uint SampleLayerTex(sampler2D _LayerTex, float2 uv)
{
    float layer = tex2D(_LayerTex, uv).r;
    return DecodeFloatToUint(layer);
}

float3 SampleNormalTex(sampler2D _NormalTex, float2 uv)
{
    return DecodeNormal(tex2D(_NormalTex, uv).rg);
}