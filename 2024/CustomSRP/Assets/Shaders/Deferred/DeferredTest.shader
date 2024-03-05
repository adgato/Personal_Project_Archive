Shader"Deferred/Test"
{
Properties
{
    _MainTex ("Texture", 2D) = "white" {}
    _NormalTex ("Texture", 2D) = "white" {}
    _DepthTex ("Texture", 2D) = "white" {}
    _LayerTex ("Texture", 2D) = "white" {}
    SimplexNoise ("SimplexNoise", 3D) = "white" {}
    _Colour ("Colour", Color) = (1,0,0,0)
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
StructuredBuffer<PointLightData> _PointLights;
int _PointLightsCount;
            
uniform float4 _BlitScaleBias;
            
uniform uint _Layer;

float4 _Colour;
sampler3D SimplexNoise;


float snoise(float3 p)
{
    return 2 * tex3D(SimplexNoise, 0.2 * p) - 1;
}


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

    uint layer = SampleLayerTex(_LayerTex, i.uv);
    if ((layer & Mask_DeferredTest ^ Layer_DeferredTest) > 0u)
        return tex2D(_MainTex, i.uv);
    
    float snoise1 = snoise(float3(3 * i.uv, _Time.y));
    float2 temperatureUV = i.uv + 0.001 * float2(snoise(float3(3 * i.uv, _Time.y)), 0.2 * snoise(float3(3 * i.uv, -1 - _Time.y)));
    
    float4 colour = tex2D(_MainTex, temperatureUV);

    float3 normal = SampleNormalTex(_NormalTex, temperatureUV);
    float depth01 = Linear01Depth(tex2D(_DepthTex, temperatureUV).r);
    float3 viewPos = (i.viewDir.xyz / i.viewDir.w) * depth01;
    //float3 worldPos = mul(unity_MatrixInvV, float4(viewPos, 1.0)).xyz;
    float sceneDepth = length(viewPos);
    
    float diffuse = 0;
    
    for (int i = 0; i < _DirectionalLightsCount; i++)
        diffuse += saturate(dot(normal, -_DirectionalLights[i].direction)) * _DirectionalLights[i].intensity;
    
    //for (i = 0; i < _PointLightsCount; i++)
    //    diffuse += saturate(dot(normal, normalize(_PointLights[i].worldPos - worldPos))) * _PointLights[i].intensity;


    //convert the lightDirection to view space also. should probably do this on the cpu side?
    //lightDir = normalize(mul(UNITY_MATRIX_IT_MV, float4(lightDir, 0)).xyz);
    
    //return colour;
    //return float4(normal, 0);
    //return sceneDepth * 0.1;
    return lerp(colour * saturate(0.3 + round(2 * pow(diffuse, 0.5)) / 2), _Colour, saturate(0.25 * saturate(abs(snoise1) * 0.3) + sceneDepth / 100));
}
ENDHLSL
}}}