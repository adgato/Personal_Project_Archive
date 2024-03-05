Shader"Custom/GrassBladeSimple"
{
Properties
{
    _SimplexNoise ("Simplex Noise", 3D) = "" {}
    _Colour1_5 ("Colour1", Color) = (0.5, 1, 0.5, 1)
    _OffsetStrength ("Offset Strength XY", Vector) = (0.5, 0.5, 0, 0)
}

SubShader
{
Tags {"RenderType"="Opaque"}

Pass
{
Cull Off


CGPROGRAM
#pragma vertex vert
#pragma fragment frag

#include "C:\\Program Files\\Unity 2022.3.3f1\\Editor\\Data\\CGIncludes\\UnityCG.cginc" //comment out before compile
#include "UnityCG.cginc"
#include "../Math.cginc"

struct appdata_t
{
    float2 uv : TEXCOORD0;
    float4 vertex : POSITION;
    float3 normal : NORMAL;
    float4 color : COLOR;
};

struct v2f
{
    float2 uv : TEXCOORD0;
    float4 pos : SV_POSITION;
    float4 color : COLOR;
};

float4 _Colour1_5;
float2 _OffsetStrength;
float3 sunDir;

float rand01(float seed)
{
    return frac(sin(seed) * 43758.5453);
}
float rand(float seed)
{
    return 2 * rand01(seed) - 1;
}

v2f vert(appdata_t v)
{
    sunDir = normalize(float3(1, 0, 0));
    
    v2f o;
    o.uv = v.uv;
    
    float3 up = normalize(v.vertex.xyz); //float3(0, 1, 0) // normalize(v.vertex.xyz)
    float3 fwd = normalize(cross(up.xyz, up.yxz));
    float3 rht = normalize(cross(up, fwd));
    
    float norSeed = 5 * (v.normal.x + 2 * v.normal.y + 4 * v.normal.z);
    
    float3 offsetNoiseX = rand(norSeed + 2) * fwd + rand(norSeed + 4) * rht;
    float3 offsetNoiseY = rand01(norSeed + 6) * up;
    float3 plasticOffset = (offsetNoiseX * _OffsetStrength.x + offsetNoiseY * _OffsetStrength.y) * v.uv.y;

    float darkness = min(1, 0.25 + saturate(invlerp(-0.2, 0.0, dot(up, sunDir))));
    o.color = _Colour1_5 * lerp(0.8, 1, rand01(norSeed)) * darkness;
    o.pos = UnityObjectToClipPos(v.vertex + float4(plasticOffset, 0));

    return o;
}

half4 frag(v2f i) : SV_Target
{
    return i.color;
}

ENDCG
}
}
}