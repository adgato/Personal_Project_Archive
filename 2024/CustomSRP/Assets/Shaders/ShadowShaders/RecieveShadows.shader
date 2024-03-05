// This defines a simple unlit Shader object that is compatible with a custom Scriptable Render Pipeline.
// It applies a hardcoded color, and demonstrates the use of the LightMode Pass tag.
// It is not compatible with SRP Batcher.

Shader "Shadow/RecieveShadowVolume"
{
Properties
{
    _Colour ("Colour", Color) = (0.5, 1, 0.5, 1)
}
SubShader
{
HLSLINCLUDE
#include "Assets/ShaderIncludes/UnityShaderUtilities.cginc"
#include "Assets/ShaderIncludes/Helpers/CRPHelper.cginc"

ENDHLSL

Pass
{

// The value of the LightMode Pass tag must match the ShaderTagId in ScriptableRenderContext.DrawRenderers
Tags { "LightMode" = "ExampleLightModeTag" "Layers" = "DeferredTest" }
            
HLSLPROGRAM

#pragma vertex vert
#pragma fragment frag

//float4x4 unity_MatrixVP;
//float4x4 unity_ObjectToWorld;
CBUFFER_START(UnityPerMaterial)
             
uniform float4 _Colour;

CBUFFER_END

struct PixelData
{
    float4 colour : CRP_Target_COLOUR;
    float2 normal : CRP_Target_NORMAL;
    float layer : CRP_Target_LAYER;
};

struct Attributes
{
    float4 vertex : POSITION;
    float4 normal : NORMAL;
};

struct Varyings
{
    float4 pos : SV_POSITION;
    float3 normal : TEXCOORD0;
};


Varyings vert (Attributes v)
{
    Varyings o;
    o.pos = UnityObjectToClipPos(v.vertex);
    o.normal = COMPUTE_VIEW_NORMAL(v.normal);
    return o;
}

PixelData frag (Varyings i)
{
    PixelData o;
    o.colour = _Colour;
    o.normal = EncodeNormal(normalize(i.normal));; //float4(i.normal, 0);
    uint layer = Layer_DeferredTest;
    o.layer = EncodeUintToFloat(layer); 
    return o;
}

ENDHLSL
}
Pass
{
ZWrite Off

Stencil
{
    Ref 0
    Comp NotEqual
}


Tags { "LightMode" = "ShadowWrite" "Layers" = "DeferredShadow" }
            
HLSLPROGRAM

#pragma vertex vert
#pragma fragment frag

struct PixelData2
{
    float4 layer : CRP_Target_LAYER;
};

struct Attributes2
{
    float4 vertex : POSITION;
};

struct Varyings2
{
    float4 pos : SV_POSITION;
};


Varyings2 vert(Attributes2 v)
{
    Varyings2 o;
    o.pos = UnityObjectToClipPos(v.vertex);
    return o;
}

PixelData2 frag(Varyings2 i)
{
    PixelData2 o;
    uint layer = Layer_DeferredShadow;
    o.layer = EncodeUintToFloat(layer);
    return o;
}

ENDHLSL
}
}}