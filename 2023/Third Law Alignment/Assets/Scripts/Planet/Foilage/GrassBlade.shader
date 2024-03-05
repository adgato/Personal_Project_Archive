Shader"Custom/GrassBlade"
{
Properties
{
    _SimplexNoise ("Simplex Noise", 3D) = "" {}
    _WindSpeed ("Wind Speed", Float) = 0.1
    _WindScale ("Wind Scale", Float) = 0.25
    _WindDirSpeed ("Wind Dir Speed", Float) = 0.1
    _WindDirScale ("Wind Dir Scale", Float) = 0.25
    _WindStrength ("Wind Strength XY", Vector) = (0.1, 0.1, 0, 0)
    _OffsetStrength ("Offset Strength XY", Vector) = (0.5, 0.5, 0, 0)
    _FlattenStrength ("Flatten Strength XY", Vector) = (0.5, 0.5, 0, 0)
    _FlattenRange ("Flatten Range", Float) = 1
    _Colour1 ("Colour1", Color) = (0.5, 1, 0.5, 1)
    _Colour2 ("Colour2", Color) = (0.5, 1, 0.5, 1)
    _ColourScale ("Colour Scale", Float) = 0.1
    _AlphaDropoff ("Transparent Dropoff", Float) = 0.1
    _Alpha ("Transparency", Range (0, 1)) = 0.75
}

SubShader
{
Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}

Pass
{
Cull Off
ZWrite On
Blend SrcAlpha OneMinusSrcAlpha
LOD 100


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

sampler3D _SimplexNoise;
float _WindSpeed;
float _WindDirSpeed;
float _WindScale;
float _WindDirScale;
float2 _WindStrength;
float2 _OffsetStrength;
float2 _FlattenStrength;
float3 _FlattenPos;
float _FlattenRange;
float4 _Colour1;
float4 _Colour2;
float _ColourScale;
float _AlphaDropoff;
float _Alpha;
float3 sunDir;

float rand01(float seed)
{
    return frac(sin(seed) * 43758.5453);
}
float rand(float seed)
{
    return 2 * rand01(seed) - 1;
}
float snoise(float3 p)
{
    return tex3Dlod(_SimplexNoise, float4(0.2 * p, 1));
}

#define fourpi 12.5663706144
            
v2f vert(appdata_t v)
{
    sunDir = normalize(float3(1, 0, 0));
    
    v2f o;
    o.uv = v.uv;

    float3 up = normalize(v.vertex.xyz); //float3(0, 1, 0) // normalize(v.vertex.xyz)
    float3 fwd = normalize(cross(up.xyz, up.yzx)); //this causes issues when x = y = z
    float3 rht = normalize(cross(up, fwd));
    
    float windAngle = snoise(v.vertex.xyz * _WindDirScale + _Time.y * _WindDirSpeed) * fourpi;
    float norSeed = 5 * (v.normal.x + 2 * v.normal.y + 4 * v.normal.z);

    float3 windDir = cos(windAngle) * fwd + sin(windAngle) * rht;
    
    float windNoise = snoise(v.vertex.xyz * _WindScale + _Time.y * _WindSpeed);
    
    float windNoiseX = windNoise * _WindStrength.x * v.uv.y;
    float windNoiseY = -abs(windNoise) * _WindStrength.y * v.uv.y;
    
    float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
    float3 playerToGrass = worldPos - _FlattenPos;
    float dstToGrass = length(playerToGrass);
    float trampled01 = clamp(1 - saturate(dstToGrass / _FlattenRange), 0, 0.5); //works when player is at the origin
    float3 offsetNoiseX = lerp(rand(norSeed + 2) * fwd + rand(norSeed + 4) * rht, playerToGrass / dstToGrass, trampled01);
    float3 offsetNoiseY = lerp(rand01(norSeed + 6), 1, trampled01) * up;
    float2 offsetScale = lerp(_OffsetStrength, _FlattenStrength, trampled01);
    float3 plasticOffset = (offsetNoiseX * offsetScale.x + offsetNoiseY * offsetScale.y) * v.uv.y;

    float3 elasticOffset = windNoiseX * windDir + windNoiseY * up;

    float darkness = min(1, 0.25 + saturate(invlerp(-0.2, 0.0, dot(up, sunDir))));
    o.color = lerp(_Colour1, _Colour2, snoise(v.vertex.xyz * _ColourScale) * 0.5 + 0.5) * lerp(0.8, 1, rand01(norSeed)) * darkness;
    o.color.a = lerp(_Alpha, 1, saturate(dstToGrass * _AlphaDropoff));
    o.pos = UnityObjectToClipPos(v.vertex + float4(elasticOffset + plasticOffset, 0));

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