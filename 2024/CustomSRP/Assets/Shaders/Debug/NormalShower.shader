Shader "Hidden/NormalShower"
{
Properties
{
    _MainTex ("Texture", 2D) = "white" {}
    [HideInInspector] _Layer ("Layer", Int) = 0
    [HideInInspector] _Mask ("Mask", Int) = 0
}
SubShader
{
Cull Off ZWrite Off ZTest Always
Pass
{

HLSLPROGRAM
#pragma vertex vert
#pragma fragment frag
            
#include "Assets/ShaderIncludes/UnityShaderUtilities.cginc"
#include "Assets/ShaderIncludes/Helpers/CRPHelper.cginc"

uniform sampler2D _MainTex;
uniform float4 _BlitScaleBias;
float4 _Filter;

struct Attributes
{
    float4 vertex : POSITION;
    float2 uv : TEXCOORD0;
};

struct Varyings
{
    float2 uv : TEXCOORD0;
    float4 vertex : SV_POSITION;
};

Varyings vert(Attributes v)
{
    Varyings o;
    o.vertex = UnityObjectToClipPos(v.vertex);
    o.uv = v.uv;
    return o;
}

float4 frag(Varyings i) : SV_Target
{
    //convert to view space for best viewing (note that we are hoping the correct matrix has been set somewhere previously...)
    return (float4(SampleNormalTex(_MainTex, i.uv), 0) * 0.5 + 0.5) * _Filter;
}

ENDHLSL
}}}
