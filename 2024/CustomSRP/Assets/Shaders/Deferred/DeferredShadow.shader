Shader"Deferred/ShadowVolumeRenderer"
{
Properties
{
    _MainTex ("Texture", 2D) = "white" {}
    _NormalTex ("Texture", 2D) = "white" {}
    _DepthTex ("Texture", 2D) = "white" {}
    _LayerTex ("Texture", 2D) = "white" {}
    _Colour ("Colour", Color) = (1,0,0,0)
    _Dots ("Dots", 2D) = "white" {}
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
#include "Assets/ShaderIncludes/Helpers/BlitHelper.cginc"
#include "Assets/ShaderIncludes/LightStructs.cginc"
            

uniform sampler2D _MainTex;
uniform sampler2D _NormalTex;
uniform sampler2D _DepthTex;
uniform sampler2D _LayerTex;

StructuredBuffer<DirectionalLightData> _DirectionalLights;
int _DirectionalLightsCount;
            
uniform float4 _BlitScaleBias;
            
uniform uint _Layer;

float4 _Colour;

sampler2D _Dots;

struct Attributes
{
    uint vertexID : SV_VertexID;
};

struct Varyings
{
    float4 pos : SV_POSITION;
    float2 uv : TEXCOORD0;
    float4 viewDir : TEXCOORD1;
};

Varyings vert(Attributes v)
{
    Varyings o;
    o.pos = FullScreenTriangleVertexPosition(v.vertexID);
    o.uv = FullScreenTriangleTexCoord(v.vertexID) * _BlitScaleBias.xy + _BlitScaleBias.zw;
    o.viewDir = mul(unity_CameraInvProjection, float4(o.uv * 2.0 - 1.0, 1.0, 1.0));
    return o;
}

float4 frag(Varyings i) : SV_Target
{

    float4 colour = tex2D(_MainTex, i.uv);
                
    uint layer = SampleLayerTex(_LayerTex, i.uv);
    if ((layer & Mask_DeferredShadow ^ Layer_DeferredShadow) > 0u)
        return colour;

    float3 normal = SampleNormalTex(_NormalTex, i.uv);
    float depth01 = Linear01Depth(tex2D(_DepthTex, i.uv).r);
    float3 viewPos = (i.viewDir.xyz / i.viewDir.w) * depth01;
    float sceneDepth = length(viewPos);
    
    float dots = tex2D(_Dots, i.uv * 4).r;
    
        
    float diffuse = 0;
    
    for (int i = 0; i < _DirectionalLightsCount; i++)
        diffuse += saturate(dot(normal, -_DirectionalLights[i].direction)) * _DirectionalLights[i].intensity;

    //convert the lightDirection to view space also. should probably do this on the cpu side?
    //lightDir = normalize(mul(UNITY_MATRIX_IT_MV, float4(lightDir, 0)).xyz);
    
    //return colour;
    //return float4(normal, 0);
    //return sceneDepth * 0.1;
    //return lerp(float4(1, 0, 0, 0), 0.5 * colour * (0.1 + saturate(dot(normal, -lightDir))), abs(_SinTime.w));
    return lerp(lerp(colour * saturate(0.3 + round(2 * pow(diffuse, 0.5)) / 2) * saturate(lerp(dots, 0.5, 0.5) * 0.5), _Colour, 0.1), 1, sceneDepth / 100);
}
ENDHLSL
}}}