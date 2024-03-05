Shader"Custom/ScreenEffects"
{

Properties
{
_MainTex ("Texture", 2D) = "white" {}

}

SubShader
{
// No culling or depth
Cull Off
ZWrite Off
ZTest Always

Pass
{
CGPROGRAM
#pragma vertex vert
#pragma fragment frag

#include "C:\\Program Files\\Unity 2022.3.3f1\\Editor\\Data\\CGIncludes\\UnityCG.cginc" //comment out before compile
#include "UnityCG.cginc"
#include "../Planet/Math.cginc"

struct appdata
{
    float4 vertex : POSITION;
    float4 uv : TEXCOORD0;
};

struct v2f
{
    float4 pos : SV_POSITION;
					
    float2 uv : TEXCOORD0;
    float3 viewVector : TEXCOORD1;
};

v2f vert(appdata v)
{
    v2f output;
    output.pos = UnityObjectToClipPos(v.vertex);

    output.uv = v.uv;
	// Camera space matches OpenGL convention where cam forward is -z. In unity forward is positive z.
	// (https://docs.unity3d.com/ScriptReference/Camera-cameraToWorldMatrix.html)
    float3 viewVector = mul(unity_CameraInvProjection, float4(v.uv.xy * 2 - 1, 0, -1));
    output.viewVector = mul(unity_CameraToWorld, float4(viewVector, 0));
    return output;
}

float2 squareUV(float2 uv)
{
    return float2(uv.x * _ScreenParams.x / 1920, uv.y * _ScreenParams.y / 1920);
}

sampler2D _MainTex;
sampler2D _CameraDepthTexture;
sampler2D _CameraDepthNormalsTexture;

int TonemappingEnabled;

sampler3D SimplexNoise;

float tonemappingValue;
float temperature01; //1 is hot, 0 is cold
float darkness01;

float snoise(float3 p)
{
    return 2 * tex3D(SimplexNoise, 0.2 * p) - 1;
}

//https://www.desmos.com/calculator/rolgelnpfb
float3 ACESFilm(float3 x)
{
    x = linear_srgb_to_oklab(x);
    float maxR = x.r;
    x = lerp(x, x * x + float3(0.2, 0, 0), darkness01);
    x.r = min(x.r, maxR);
    float expo = exp(tonemappingValue * x.r);
    x.r *= saturate(expo / (expo + tonemappingValue));
    return oklab_to_linear_srgb(x);
}

float4 frag(v2f i) : SV_Target
{
    float3 sundir = float3(1, 0, 0);
    
    float2 temperatureUV;
    //1 is hot, 0 is cold, offset UV based to evoke temperature (heat waves & shaking for hot & cold)
    if (temperature01 > 0.5) 
        temperatureUV = i.uv + 0.001 * pow(saturate(invlerp(0.5, 1, temperature01)), 3) * float2(snoise(float3(3 * i.uv, _Time.y)), 0.2 * snoise(float3(3 * i.uv, -1 - _Time.y)));
    else
        temperatureUV = i.uv + 0.00025 * pow(saturate(invlerp(0.5, 0, temperature01)), 3) * float2(floor(_Time.y * unity_DeltaTime.y) % 2 * 2 - 1, 0.2 * (floor(_Time.y * unity_DeltaTime.y) % 2 * 2 - 1));
                    
    temperatureUV = saturate(temperatureUV);

    float3 bgCol = tex2D(_MainTex, temperatureUV);

    return TonemappingEnabled == 1 ? float4(ACESFilm(bgCol), 0) : float4(bgCol, 0);
}

ENDCG
}
}
}
