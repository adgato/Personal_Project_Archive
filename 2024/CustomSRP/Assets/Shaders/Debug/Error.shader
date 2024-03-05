Shader "Debug/Error"
{
Properties
{
    _MainTex ("Main tex", 2D) = "white" {}
    _ErrorMessageTex ("Error message", 2D) = "white" {}
}
SubShader
{
Cull Off ZWrite Off ZTest Always
Pass
{
// The value of the LightMode Pass tag must match the ShaderTagId in ScriptableRenderContext.DrawRenderers
Tags { "LightMode" = "ExampleLightModeTag"}

HLSLPROGRAM
#pragma vertex vert
#pragma fragment frag
            
#include "UnityCG.cginc"
            
sampler2D _MainTex;
sampler2D _ErrorMessageTex;

struct appdata
{
    float4 vertex : POSITION;
    float4 uv : TEXCOORD0;
};

struct v2f
{
    float4 pos : SV_POSITION;
    float2 uv : TEXCOORD0;
};

v2f vert (appdata i)
{
    v2f o;
    o.pos = UnityObjectToClipPos(i.vertex);
    o.uv = i.uv;
    return o;
}

float4 frag (v2f i) : SV_Target
{
    float4 ogCol = tex2D(_MainTex, i.uv);
    float4 errorCol = tex2D(_ErrorMessageTex, i.uv);

    return lerp(ogCol, errorCol, errorCol.r);
}
ENDHLSL
}}}