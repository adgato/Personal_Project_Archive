//Note, this shader is similar to the Atmosphere.shader script in many ways, but the way the atmosphere is generated is completely different


Shader "Custom/Clouds"
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

			#include "UnityCG.cginc"
			#include "Math.cginc"
			#include "PerlinNoise.cginc"
			//

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
				float width = _ScreenParams.x;
				float height =_ScreenParams.y;
				//float minDim = min(width, height);
				float scale = 1000;
				float x = uv.x * width;
				float y = uv.y * height;
				return float2 (x/scale, y/scale);
			}

            sampler2D _MainTex;
            sampler2D _CameraDepthTexture;

            sampler2D fogColourRings;
			sampler2D BlueNoise;

            float3 sunCentre;
			float3 planetCentre;
            float planetRadius;
            float oceanRadius;
            float fogRange;

            float atmosphereRadius;
            float cloudLyrDepth;

            float noiseFreq;
			float noiseAmp;

            float ditherScale;
			float ditherStrength;

			float lightIntensity;
            float numInScatteringPoints;

            float windSpeed;

            sampler2D waveNormalA;
			sampler2D waveNormalB;
			float4 oceanColour;
			float visibleDepth;
			float waveSpeed;
			float waveStrength;
			float waveNormalScale;
			float smoothness;

			float roughBed;

			float reflection01(float3 normal, float3 position)
			{
				return dot(normal, normalize(sunCentre - position)) * 2 - 1;
			}

			float4 GenerateOcean(float4 originalCol, float3 clipPlanePos, float3 rayDir, float sceneDepth, float dstToOcean, float3 oceanPos, float oceanNoise, float4 fogColour)
			{
				if (dstToOcean < sceneDepth)
				{
					oceanColour = normalize(oceanColour) * 0.1;

					waveSpeed *= windSpeed;

					float dstAboveWater = length(clipPlanePos - planetCentre) - oceanRadius;

					float4 finalColour = lerp(originalCol, oceanColour, clamp((sceneDepth - dstToOcean) / 10, 0, 1));
					finalColour = lerp(oceanColour, finalColour, clamp((sceneDepth - dstToOcean) / (5 - oceanNoise), 0, 1));
					
					float3 oceanSphereNormal = normalize(oceanPos);

					float2 waveOffsetA = float2(_Time.x * waveSpeed, _Time.x * waveSpeed * 0.8);
					float2 waveOffsetB = float2(_Time.x * waveSpeed * -0.8, _Time.x * waveSpeed * -0.3);
					float3 waveNormal = triplanarNormal(oceanPos, oceanSphereNormal, waveNormalScale / planetRadius, waveOffsetA, waveNormalA);
					waveNormal = triplanarNormal(oceanPos, waveNormal, 1 / waveNormalScale, waveOffsetB, waveNormalB);
					waveNormal = normalize(lerp(oceanSphereNormal, waveNormal, waveStrength));
					//return float4(oceanNormal * .5 + .5,1);
					float diffuseLighting = saturate(dot(oceanSphereNormal, normalize(sunCentre - planetCentre)));
					float specularAngle = lerp(waveNormal, acos(dot(normalize(normalize(sunCentre - planetCentre) - rayDir), waveNormal)), 0.2);
					float specularExponent = specularAngle / (1 - smoothness);
					float specularHighlight = exp(-specularExponent * specularExponent);
				
					finalColour *= diffuseLighting;
					finalColour += specularHighlight * lerp(finalColour, float4(1,1,1,1), 0.1);
					
					finalColour = finalColour * (0.75 + reflection01(waveNormal, oceanPos) * 0.25);

					finalColour = float4(min(finalColour.r, 1), min(finalColour.g, 1), min(finalColour.b, 1), 1);

					return finalColour;
				}
				return originalCol;
			}
			float opticalDepth(float3 samplePoint, float spread1, float spread2)
			{
				return remap01(0, 2 * planetRadius * spread1, raySphere(planetCentre, planetRadius * spread2, samplePoint, normalize(sunCentre - samplePoint)).y);
			}
			void lightInDir(float3 enterPoint, float3 stepDir, float stepSize, float blueNoise)
			{
				lightIntensity = 0;
				float enterIntensity = 0;

				for (int i = 0; i < numInScatteringPoints; i++) 
				{
					float3 samplePoint = enterPoint + stepDir * stepSize * i;

					lightIntensity += opticalDepth(samplePoint, 1, 1.25) + blueNoise;
					if (i == 0)
						enterIntensity = lightIntensity;
				}
				lightIntensity = remap01(0, numInScatteringPoints, lightIntensity);
				lightIntensity = lerp(enterIntensity, lightIntensity, abs(lightIntensity - enterIntensity));
			}

            float4 frag (v2f i) : SV_Target
			{
                float p = 0.01;
                numInScatteringPoints = 20;

				float4 originalCol = tex2D(_MainTex, i.uv);
				float sceneDepthNonLinear = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv);
				float sceneDepth = LinearEyeDepth(sceneDepthNonLinear) * length(i.viewVector);
											
				float3 rayOrigin = _WorldSpaceCameraPos;
				float3 rayDir = normalize(i.viewVector);

				float dstToDefault = raySphere(planetCentre, oceanRadius, rayOrigin, rayDir);//
				float3 oceanPos = rayOrigin + rayDir * dstToDefault - planetCentre;
				
				float oceanNoise = 0;
				if (dstToDefault < 100 && roughBed == 0)
					oceanNoise = LayeredNoise(oceanPos, _Time.x * 5 * windSpeed, 10, 1);
				
				oceanRadius += oceanNoise * remap01(100, 90, dstToDefault);

				float2 oceanInfo = raySphere(planetCentre, oceanRadius, rayOrigin, rayDir);
				float dstToOcean = oceanInfo.x;
				float dstThroughOcean = oceanInfo.y;

				float dstToSurface = min(sceneDepth, dstToOcean);

				float3 clipPlanePos = rayOrigin + i.viewVector * _ProjectionParams.y;

                float2 btmHitInfo = raySphere(planetCentre, atmosphereRadius - cloudLyrDepth, rayOrigin, rayDir);
				float dstToBtmCloudLyr = btmHitInfo.x;
                float dstThroughBtmCloudLyr = min(btmHitInfo.y, (dstToSurface - dstToBtmCloudLyr));

                float2 topHitInfo = raySphere(planetCentre, atmosphereRadius, rayOrigin, rayDir);
				float dstToTopCloudLyr = topHitInfo.x;
                float dstThroughTopCloudLyr = min(topHitInfo.y, (dstToSurface - dstToTopCloudLyr));



				
				oceanRadius += oceanNoise * remap01(100, 90, dstToDefault);

                if (dstThroughTopCloudLyr > 0) //&& (dstToCloudLyr == 0 && dstToSurface > dstThroughCloudLyr || dstToCloudLyr != 0))
                {
					//2 if the the player is between the top and bottom layers, 1 if the player is within the bottom layer, 0 if the player is out of the atmosphere
                    int playerAlt = dstToTopCloudLyr > 0 ? 2 : dstToBtmCloudLyr > 0 ? 1 : 0;
                    float playerAltLerp = remap01(atmosphereRadius - cloudLyrDepth, atmosphereRadius, length(rayOrigin - planetCentre));

					//Calculate the entry and exit points of the ray through the cloud layers (they match if playerAlt is 0)
                    float3 enter1;
                    if (playerAlt == 2)
                        enter1 = rayOrigin + rayDir * dstToTopCloudLyr;
                    else if (playerAlt == 1)
                        enter1 = rayOrigin;
                    else
                        enter1 = rayOrigin + rayDir * dstThroughBtmCloudLyr;

                    float3 exit1;
                    if (playerAlt == 0)
                        exit1 = rayOrigin + rayDir * dstThroughTopCloudLyr;
                    else if (dstThroughBtmCloudLyr > 0)
                        exit1 = rayOrigin + rayDir * dstToBtmCloudLyr;
                    else if (dstThroughBtmCloudLyr == 0)
                        exit1 = rayOrigin + rayDir * (dstToTopCloudLyr + dstThroughTopCloudLyr);
                    
                    float3 enter2;
                    if (playerAlt != 0 && dstThroughBtmCloudLyr > 0 && (dstToBtmCloudLyr + dstThroughBtmCloudLyr) < dstToSurface)
                        enter2 = rayOrigin + rayDir * (dstToBtmCloudLyr + dstThroughBtmCloudLyr);
                    else
                        enter2 = enter1;
                    
                    float3 exit2;
                    if (playerAlt != 0 && dstThroughBtmCloudLyr > 0 && (dstToBtmCloudLyr + dstThroughBtmCloudLyr) < dstToSurface)
                        exit2 = rayOrigin + rayDir * (dstToTopCloudLyr + dstThroughTopCloudLyr);
                    else
                        exit2 = enter1;
                    
                    float blueNoise = 0;
					if (sceneDepth > 9999)
					{
						blueNoise = tex2Dlod(BlueNoise, float4(squareUV(i.uv) * ditherScale,0,0));
						blueNoise = (blueNoise - 0.5) * ditherStrength * p;
					}

                    float dstThroughCloudLyr = max(length(enter1 - exit1), length(enter2 - exit2));
                    float cloudStrength = remap01(0, cloudLyrDepth, dstThroughCloudLyr);
                    float frenzel = lerp(remap01(0, atmosphereRadius, topHitInfo.y), 1, 1 - playerAltLerp);

					//Calculate the latitude of the entry and exit points of the ray through the cloud layers
                    float outLat = remap01(-atmosphereRadius - cloudLyrDepth, atmosphereRadius, enter1.y - planetCentre.y) + LayeredNoise(enter1 - planetCentre, _Time.x, noiseFreq, noiseAmp) + blueNoise;
                    float inLat = remap01(-atmosphereRadius - cloudLyrDepth, atmosphereRadius, enter2.y) + LayeredNoise(enter2 - planetCentre, _Time.x, noiseFreq, noiseAmp) + blueNoise;
                    float platerLat = remap01(-atmosphereRadius - cloudLyrDepth, atmosphereRadius, rayOrigin.y) + LayeredNoise(rayOrigin - planetCentre, _Time.x, noiseFreq, noiseAmp) + blueNoise;

					//Not interested in sunsets which are the y component of fogColourRings at atmosphere is opaque at a distance
                    float4 outColour = tex2D(fogColourRings, float2(clamp(outLat, p, 1 - p), p));
                    float4 inColour = lerp(tex2D(fogColourRings, float2(clamp(inLat, p, 1 - p), p)), LayeredNoise(enter2 - planetCentre, _Time.x * windSpeed, 5, 1), 0.05);
                    float4 cloudColour = lerp(outColour, inColour, 1 - playerAltLerp);

                    float fog = lerp(1, remap01(0, planetRadius, dstThroughTopCloudLyr), 1 - playerAltLerp);
                    float4 fogColour = tex2D(fogColourRings, float2(clamp(platerLat, p, 1 - p), p));

					if (roughBed == 0)
						originalCol = GenerateOcean(originalCol, clipPlanePos, rayDir, sceneDepth, dstToOcean, oceanPos, oceanNoise, fogColour);

                    if (playerAlt != 2)
                        cloudColour = lerp(cloudColour, fogColour, fog);

                    lightInDir(enter1, rayDir, dstThroughTopCloudLyr / numInScatteringPoints, blueNoise);

                    

                    float4 finalColour;
                    if (dstThroughBtmCloudLyr < dstToSurface && !(dstThroughBtmCloudLyr + dstToBtmCloudLyr == dstToSurface && playerAlt == 1))
                        finalColour = lerp(originalCol, cloudColour, cloudStrength * frenzel);
                    else
                        finalColour = lerp(originalCol, fogColour, fog);

                    finalColour *= clamp(1.2 - lightIntensity, 0, 1);
                    finalColour.a = 1;
                    return finalColour;
                }

                return originalCol;
            }

            ENDCG
        }
    }
}
