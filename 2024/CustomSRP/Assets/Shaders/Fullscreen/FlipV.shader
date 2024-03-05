Shader "Hidden/FlipV"
{
SubShader
{

Pass
{
ZWrite Off ZTest Always Blend Off Cull Off

HLSLPROGRAM

#pragma vertex Vert
#pragma fragment FragNearest
#include "Assets/ShaderIncludes/Helpers/BlitHelper.cginc"

sampler2D _MainTex;

SamplerState sampler_PointClamp;
SamplerState sampler_LinearClamp;
uniform float4 _BlitScaleBias;
uniform float _BlitMipLevel;


struct Attributes
{
    uint vertexID : SV_VertexID;
};

struct Varyings
{
    
    float4 positionCS : SV_POSITION;
    float2 texcoord : TEXCOORD0;
};

Varyings Vert(Attributes input)
{
    Varyings output;
    output.positionCS = FullScreenTriangleVertexPosition(input.vertexID);
    output.texcoord = FullScreenTriangleTexCoord(input.vertexID) * _BlitScaleBias.xy + _BlitScaleBias.zw;
    output.texcoord.y = 1 - output.texcoord.y;

    return output;
}

float4 FragNearest(Varyings input) : SV_Target
{

    return tex2D(_MainTex, input.texcoord.xy);
}

ENDHLSL
}


}

Fallback Off
}