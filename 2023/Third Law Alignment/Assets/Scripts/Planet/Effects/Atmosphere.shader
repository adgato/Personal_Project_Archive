Shader "Custom/Atmosphere"
{

Properties
{
_MainTex ("Texture", 2D) = "white" {}

}

SubShader
{
// No culling or depth
Cull Off ZWrite Off ZTest Always

Pass
{
CGPROGRAM
#pragma vertex vert
#pragma fragment frag

#include "C:\\Program Files\\Unity\\Hub\\Editor\\2022.3.3f1\\Editor\\Data\\CGIncludes\\UnityCG.cginc" //comment out before compile
#include "UnityCG.cginc"

#include "Rings.cginc"

struct appdata {
	float4 vertex : POSITION;
	float4 uv : TEXCOORD0;
};

struct v2f {
	float4 pos : SV_POSITION;
					
	float2 uv : TEXCOORD0;
	float3 viewVector : TEXCOORD1;
};

v2f vert (appdata v) {
	v2f output;
	output.pos = UnityObjectToClipPos(v.vertex);

	output.uv = v.uv;
	// Camera space matches OpenGL convention where cam forward is -z. In unity forward is positive z.
	// (https://docs.unity3d.com/ScriptReference/Camera-cameraToWorldMatrix.html)
    float3 viewVector = mul(unity_CameraInvProjection, float4(v.uv.xy * 2 - 1, 0, -1));
	output.viewVector = mul(unity_CameraToWorld, float4(viewVector,0));
	return output;
}


float2 squareUV(float2 uv) {
    return uv * _ScreenParams.xy / 1920;
}

sampler2D _MainTex;
sampler2D _CameraDepthTexture;
sampler2D _CameraDepthNormalsTexture;

sampler2D BlueNoise;
sampler3D SimplexNoise;
sampler2D PerlinNoise;


float3 planetCentre;
float3 sunDir;
float sunIntensity;
float atmosphereRadius; //smaller improves accuracy
float fogRadius;
float planetRadius;
float oceanRadius;
float k;
float rayleighHeight;
float fogHeight;
float fogRange;
float fogOpacity;

float numDiscs;
float rings;
float4 disc1colour;
float3 disc1normal;
float2 disc1iris;
float4 disc2colour;
float3 disc2normal;
float2 disc2iris;

float waveSpeed;
float waveStrength;
float waveSmoothness;
float waveNormalScale;
sampler2D waveNormalA;
sampler2D waveNormalB;

float4 fogColour1;
float4 fogColour2;
float4x4 atmosphereColours;
float3 wavelengths;

int samples;

float snoise(float3 p)
{
    return 2 * tex3D(SimplexNoise, 0.2 * p) - 1;
}

float opticalDepth(float3 P)
{
    return exp(-(length(P - planetCentre) - planetRadius) / rayleighHeight);
}

float4 frag (v2f i) : SV_Target
{
    samples = 5;
	
    
    float4 bgCol = tex2D(_MainTex, i.uv);
    float4 blueNoise = 0.015 * saturate(1.04508196721 * (tex2D(BlueNoise, squareUV(i.uv) * 4).r - 0.043137254902));
	
    float sceneDepthNonLinear = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv);
	float sceneDepth = LinearEyeDepth(sceneDepthNonLinear) * length(i.viewVector);

	float3 rayOrigin = _WorldSpaceCameraPos;
	float3 rayDir = normalize(i.viewVector);

    float2 oceanInfo = raySphere(planetCentre, oceanRadius, rayOrigin, rayDir);
    float dstToOcean = oceanInfo.x;
    float dstThroughOcean = oceanInfo.y;
    
    float waveTime = _Time.x * waveSpeed;

    float dstToSurface = min(sceneDepth, dstToOcean);
    
    float3 surfacePoint = dstToSurface * rayDir + rayOrigin;
    //float3 normal = normalize(cross(ddx(surfacePoint), ddy(surfacePoint)));
    
    
    float2 atmosInfo = raySphere(planetCentre, atmosphereRadius, rayOrigin, rayDir);
	float dstToAtm = atmosInfo.x;
    float dstThroughAtm = min(dstToSurface - dstToAtm, atmosInfo.y);
    
    float dstThroughAtmBlind = min(dstToOcean - dstToAtm, atmosInfo.y);
    
    Disc disc1;
    {
        disc1.colour = disc1colour;
        disc1.normal = disc1normal;
        disc1.iris = disc1iris;
    }
    Disc disc2;
    {
        disc2.colour = disc2colour;
        disc2.normal = disc2normal;
        disc2.iris = disc2iris;
    }
    
    float2 discInfo = raySphere(planetCentre, planetRadius * 4, rayOrigin, rayDir);
    float4 discColour;
    uint drawRingMode = 0;

    if (min(dstToSurface - discInfo.x, discInfo.y) > 0)
        drawRingMode = GenerateRings(planetCentre, sunDir, rayOrigin, rayDir, planetRadius, dstToSurface, dstToAtm, numDiscs, disc1, disc2, rings, PerlinNoise, discColour);

    
    if (drawRingMode == 1)
        bgCol = lerp(bgCol, discColour, discColour.a);
    
    
    if (dstToOcean < sceneDepth + 2)
    {
        float3 oceanPos = rayOrigin + rayDir * dstToOcean - planetCentre;
        float oceanNoise = 0;
        if (dstToOcean < 100) // && roughBed == 0
            oceanNoise = invlerp(-1, 1, snoise(oceanPos * 0.1 + waveTime * 5));
				
	        //Increase the radius of the ocean to make waves, and update values
        oceanRadius += oceanNoise * saturate(invlerp(50, 40, dstToOcean));
        oceanInfo = raySphere(planetCentre, oceanRadius, rayOrigin, rayDir);
        dstToOcean = oceanInfo.x;
        dstThroughOcean = oceanInfo.y;
        dstToSurface = min(sceneDepth, dstToOcean);
        dstThroughAtm = min(dstToSurface - dstToAtm, atmosInfo.y);
				
        float dstAboveWater = length(rayOrigin + i.viewVector * _ProjectionParams.y - planetCentre) - oceanRadius;
        bgCol = GenerateOcean(waveTime, bgCol, fogColour1, rayDir, sunDir, planetRadius, sceneDepth, oceanInfo, dstAboveWater, oceanPos, oceanNoise,
                                    waveStrength, waveSmoothness, waveNormalScale, waveNormalA, waveNormalB);
    }

	if (dstThroughAtm > 0)
	{
        if (dstToSurface - dstToAtm <= dstThroughAtm)
        {
            float3 localSurface = surfacePoint - planetCentre;
            float darkness = saturate(invlerp(-0.1, 0.1, dot(normalize(localSurface), sunDir))) * saturate(dot(localSurface, localSurface) / square(planetRadius));

            bgCol = lerp(lerp(bgCol, 0.05, 0.5), bgCol, darkness); //fix to infinite colour bug here by saturating bgCol, however, this reduces bloom
        }
   
        
        float3 A = rayOrigin + rayDir * dstToAtm;
        float3 B = rayOrigin + rayDir * (dstToAtm + dstThroughAtm);

        float intensityScale = sunIntensity * (1 + square(dot(rayDir, sunDir)));
        float3 wlInfluence = k / pow(wavelengths, 4);
        
        float stepUnitLen = 1.0 / (samples + 1);
		
        float3 step1 = (B - A) * stepUnitLen;

        float3 stepFog = min(fogRadius * 0.25, raySphere(planetCentre, fogRadius, rayOrigin, rayDir).y) * stepUnitLen * rayDir; //this stops us from sampling too deep into the fog, as opposed to step1.
		
        float step1Len = dstThroughAtm * stepUnitLen;

        float3 P = A;
        float3 F = A;
		
        float3 intensity = 0;
        float depthAP = 0;
        float fogStrength = 0;
        float fogBrightness = 0;
        float3 fogColour = 0;

        //please forgive me the sin that is the spageh
        for (int i = 0; i < samples; i++)
        {
            P += step1;
            float depthP = opticalDepth(P);
            depthAP += depthP * step1Len;       
            float dstPC = raySphere(planetCentre, atmosphereRadius, P, sunDir).y;
            float3 step2 = stepUnitLen * dstPC * sunDir;
            float3 Q = P;
            float depthPC = 0;
            for (int j = 0; j < samples; j++)
            {
                Q += step2;
                depthPC += opticalDepth(Q);
            }
            depthPC *= stepUnitLen * dstPC;
            intensity += depthP * exp(-(depthAP + depthPC) * wlInfluence);
            
            float3 localF = F - planetCentre;
            F += stepFog;
            fogStrength += pow(depthP, rayleighHeight / fogHeight);;
            fogBrightness += raySphere(planetCentre, atmosphereRadius, F, sunDir).y;
            float fogNoise = invlerp(-1, 1, snoise(localF * 0.01 + waveTime * 2.5));
            fogColour += saturate(lerp(fogColour1.xyz, fogColour2.xyz, 
                                  0.5 * lerp(GradientNoise(float2(fogNoise * 0.2, sign(localF.y) * pow(abs(localF.y / fogRadius), 1.5)), 0.5, PerlinNoise), fogNoise, saturate(invlerp(50, 0, dstToAtm)))));
        }
        fogColour *= stepUnitLen;
        float fog = (1 - exp(-0.5 * fogStrength * step1Len)) * saturate(pow(dstThroughAtm / fogRange, 0.25)) * fogOpacity;
        float fogIntensity = exp(-fogBrightness * stepUnitLen / fogRadius);
        float fogBlend = blueNoise * saturate(fogIntensity * 100);

        intensity *= step1Len * intensityScale * wlInfluence;
        float4 atmosCol = mul(atmosphereColours, float4(intensity, 0));
        float atmosBlend = blueNoise * saturate(float4(intensity, 0) * 100);

        float4 finalCol = lerp(bgCol + atmosCol + atmosBlend, fogIntensity * float4(fogColour, 0) + fogBlend, fog);
        
        if (drawRingMode == 2)
            finalCol = lerp(finalCol, discColour, discColour.a);
        
        return finalCol;
    }

    if (drawRingMode != 0)
        return lerp(bgCol, discColour, discColour.a);

    return bgCol;
}

ENDCG
}
}
}
