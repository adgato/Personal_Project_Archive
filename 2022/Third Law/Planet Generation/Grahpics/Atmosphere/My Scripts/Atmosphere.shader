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
				return uv * _ScreenParams.xy / 1920;
			}

            sampler2D _MainTex;
            sampler2D _CameraDepthTexture;
			sampler2D _CameraDepthNormalsTexture;

			sampler2D fogColourRings;
			sampler2D BlueNoise;

			float3 sunCentre;
			float3 planetCentre;
			float atmosphereRadius;
			float oceanRadius;
			float planetRadius;

            float fogRange;

			float noiseFreq;
			float noiseAmp;

			float windSpeed;

			float ditherScale;
			float ditherStrength;

			float dispersionPower;
			float dispersionScale;
			float density;

			sampler2D waveNormalA;
			sampler2D waveNormalB;
			float4 oceanColour;
			float visibleDepth;
			float waveSpeed;
			float waveStrength;
			float waveNormalScale;
			float smoothness;

			float roughBed;

			float numDiscs;
			float rings;
			float2 iris1;
			float2 iris2;
			float3 disc1Normal;
			float3 disc2Normal;
			float4 disc1Colour;
			float4 disc2Colour;

			float4 discColour;

            float numInScatteringPoints;
			float atmosphereDensity;
			float opticalDensity;
			float lightIntensity;

			float4 DrawRings(float4 discNColour, float distance01, float distanceToRing, float3 intersectPoint)
			{
				//Calculate the overall opacity of the ring based on the two noise values that determine the ring spacing
				float coarseNoise = Unity_GradientNoise_float(float2(distance01, distance01), rings / 10);
				float fineNoise = Unity_GradientNoise_float(float2(distance01, distance01), rings);
				float alpha = clamp(pow(sin(1.571429 * coarseNoise), 5) * fineNoise, 0, 1);
				alpha = alpha * clamp(remap01(5, 100, distanceToRing), 0, 1);

				float4 ringColour = discNColour * remap01(1, -1, dot(normalize(intersectPoint - planetCentre), normalize(intersectPoint - sunCentre)));
				ringColour.a = alpha;
				return ringColour;
			}

			float GenerateRings(float3 rayOrigin, float3 rayDir, float surfaceDist, float dstToAtmosphere)
			{
				discColour = float4(0, 0, 0, 0);

				float3 intersectPoint;
				float distanceToRing = surfaceDist + 1;
				float ringDistToCentre = 0;
				float distance01 = -1;

				if (numDiscs > 0)
				{
					//Calculate the intersection time with the first disc
					float intersectTime = dot(disc1Normal, planetCentre - rayOrigin) / dot(disc1Normal, rayDir);
					
					//If the intersection is in front of the camera, calculate the intersect point and ring colour to draw
					if (intersectTime > 0)
					{
						intersectPoint = rayOrigin + rayDir * intersectTime;

						ringDistToCentre = length(intersectPoint - planetCentre);

						distance01 = ringDistToCentre / (planetRadius * 5);

						if (distance01 > iris1.x && distance01 < iris1.y)
						{
							distanceToRing = length(intersectPoint - rayOrigin);
							if (distanceToRing < surfaceDist)
								discColour = DrawRings(disc1Colour, distance01, distanceToRing, intersectPoint);
						}
					}
				}
				if (numDiscs > 1)
				{
					float intersectTime = dot(disc2Normal, planetCentre - rayOrigin) / dot(disc2Normal, rayDir);

					if (intersectTime > 0)
					{
						float3 newIntersectPoint = rayOrigin + rayDir * intersectTime;
						float newRingDist = length(newIntersectPoint - planetCentre);
						float newDist = length(newIntersectPoint - rayOrigin);
						float newdist01 = newRingDist / (planetRadius * 5);

						if (newDist < surfaceDist && newdist01 > iris2.x && newdist01 < iris2.y)
						{
							float4 newDiscColour = DrawRings(disc2Colour, newdist01, newDist, newIntersectPoint);

							//If the disc is closer than the previously drawn disc or there are no discs drawn yet, draw the new disc
							if (discColour.a == 0 || distanceToRing > dstToAtmosphere)
								discColour = newDiscColour;
							// Otherwise, blend the two discs together
							else if (newDist < dstToAtmosphere || dstToAtmosphere == 0)
							{
								float geoMean = pow(newDiscColour.a * discColour.a, 0.5);
								float t = 0.5;
								if (newDiscColour.a != discColour.a)
									t = abs((newDiscColour.a - geoMean) / (newDiscColour.a - discColour.a));

								discColour = lerp(discColour, newDiscColour, t);
								discColour.a = max(newDiscColour.a, discColour.a);
							}

							if (newDist < distanceToRing)
							{
								intersectPoint = newIntersectPoint;
								ringDistToCentre = newRingDist;
								distanceToRing = newDist;

								distance01 = ringDistToCentre / (planetRadius * 5);
							}
						}
					}

				}
				//Return 0 if no discs were drawn, 1 if a disc was drawn in front of the atmosphere and 2 is discs were only drawn behind the atmosphere
				if (discColour.a == 0)
					return 0;
				else if (distanceToRing > dstToAtmosphere)
					return 1;
				return 2;
			}

			float reflection01(float3 normal, float3 position)
			{
				return dot(normal, normalize(sunCentre - position)) * 2 - 1;
			}

			float4 GenerateOcean(float4 originalCol, float3 clipPlanePos, float3 rayDir, float sceneDepth, float dstToOcean, float dstThroughOcean, float3 oceanPos, float oceanNoise)
			{
				if (dstToOcean > sceneDepth || dstThroughOcean <= 0)
					return originalCol;

				oceanColour = normalize(oceanColour) * 0.1;

				//Calculate the distance above water by subtracting the ocean radius from the distance between the clip plane position and the planet center
				float dstAboveWater = length(clipPlanePos - planetCentre) - oceanRadius;

				float4 finalColour = lerp(originalCol, oceanColour, clamp((sceneDepth - dstToOcean) / 10, 0, 1));
				//This adds our lip lining shore at depths of 5 units or less
				finalColour = lerp(oceanColour, finalColour, clamp((sceneDepth - dstToOcean) / (5 - oceanNoise), 0, 1));

				float3 oceanSphereNormal = normalize(oceanPos);

				//Calculate two wave offsets based on the time and wave speed, and use them to calculate the normal vector of the waves using triplanar mapping
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
				finalColour += specularHighlight * lerp(finalColour, oceanColour, 0.5);
				
				finalColour *= dot(waveNormal, normalize(sunCentre - oceanPos)) * 2 - 1; //0.75 + reflection01(waveNormal, oceanPos) * 0.25;

				return saturate(finalColour);
			}

            float densityAtPoint(float3 samplePoint) 
            {
                return remap01(0, atmosphereRadius - planetRadius, length(samplePoint - planetCentre) - planetRadius);
            }
			float opticalDepth(float3 samplePoint, float spread1, float spread2)
			{
				return remap01(0, 2 * planetRadius * spread1, raySphere(planetCentre, planetRadius * spread2, samplePoint, normalize(sunCentre - samplePoint)).y);
			}

			//This function calculates the density and optical density of the atmosphere in a given direction,
			//starting from a given point and stepping in that direction at a given step size, with some added noise.
            void densityInDir(float3 enterPoint, float3 stepDir, float stepSize, float blueNoise)
			{
				float angle = 0;
				opticalDensity = 0;
				atmosphereDensity = 0;

				//Loop over a set number of scattering points to calculate the density and optical density.
				for (int i = 0; i < numInScatteringPoints; i++) 
                {
					float3 samplePoint = enterPoint + stepDir * stepSize * i;
					float3 sampleNormal = normalize(samplePoint - sunCentre);

					//Calculate the atmosphere density at the current scattering point
                    atmosphereDensity += pow(densityAtPoint(samplePoint), density) + blueNoise;

					angle -= dot(stepDir, sampleNormal);
					opticalDensity += lerp(0, planetRadius * 3.54, opticalDepth(samplePoint, 1.77, 1.77)) + blueNoise;
                }

				//Rescale variables to be between 0 and 1

				angle = remap01(-numInScatteringPoints, numInScatteringPoints, angle);

				opticalDensity *= stepSize;
				opticalDensity = remap01(0, atmosphereRadius * atmosphereRadius, opticalDensity) * angle * min(1, 2 / density);
				opticalDensity = clamp(pow(opticalDensity, dispersionPower) * dispersionScale, 0, 1);

                atmosphereDensity = 1 - remap01(0, numInScatteringPoints, atmosphereDensity);
                atmosphereDensity = clamp(atmosphereDensity + density / 100, 0, 1);
            }
			void lightInDir(float3 enterPoint, float3 stepDir, float stepSize, float blueNoise)
			{
				lightIntensity = 0;
				float enterIntensity = 0;

				for (int i = 0; i < numInScatteringPoints; i++) 
				{
					float3 samplePoint = enterPoint + stepDir * stepSize * i;

					lightIntensity += opticalDepth(samplePoint, 1, 1.77) + blueNoise;
					if (i == 0)
						enterIntensity = lightIntensity;
				}
				lightIntensity = remap01(0, numInScatteringPoints, lightIntensity);
				lightIntensity = lerp(enterIntensity, lightIntensity, abs(lightIntensity - enterIntensity));
			}

            float4 frag (v2f i) : SV_Target
			{
				float p = 0.01;

				density = pow(density, 2);
                numInScatteringPoints = 20;

				float4 originalCol = tex2D(_MainTex, i.uv);
				float sceneDepthNonLinear = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv);
				float sceneDepth = LinearEyeDepth(sceneDepthNonLinear) * length(i.viewVector);
					
				float3 rayOrigin = _WorldSpaceCameraPos;
				float3 rayDir = normalize(i.viewVector);

				//Calculate the distance to the default ocean level and the position of the point on the ocean surface
				float dstToDefault = raySphere(planetCentre, oceanRadius, rayOrigin, rayDir);
				float3 oceanPos = rayOrigin + rayDir * dstToDefault - planetCentre;
				
				float oceanNoise = 0;
				if (dstToDefault < 100 && roughBed == 0)
					oceanNoise = LayeredNoise(oceanPos, _Time.x * 5 * windSpeed, 10, 1);
				
				//Increase the radius of the ocean to make waves
				oceanRadius += oceanNoise * remap01(100, 90, dstToDefault);


				//Calculate the distance to and through the updated ocean surface
                float2 oceanInfo = raySphere(planetCentre, oceanRadius, rayOrigin, rayDir);
				float dstToOcean = oceanInfo.x;
				float dstThroughOcean = oceanInfo.y;

				float dstToSurface = min(sceneDepth, dstToOcean);

				float3 clipPlanePos = rayOrigin + i.viewVector * _ProjectionParams.y;
				
                float2 atmosInfo = raySphere(planetCentre, atmosphereRadius, rayOrigin, rayDir);
				float dstToAtmosphere = atmosInfo.x;

				float dstThroughAtmosphere = min(dstToSurface - dstToAtmosphere, atmosInfo.y);

				float2 discInfo = raySphere(planetCentre, planetRadius * 5, rayOrigin, rayDir);

				float drawRingMode = 0;
				if (min(dstToSurface - discInfo.x, discInfo.y) > 0)
					drawRingMode = GenerateRings(rayOrigin, rayDir, dstToSurface, dstToAtmosphere);
				//If a ring is to be drawn in front of the atmosphere
				if (drawRingMode == 1)
					originalCol = lerp(originalCol, discColour, discColour.a);

                if (dstThroughAtmosphere > 0)
                {
					float3 enterPoint = rayOrigin + rayDir * dstToAtmosphere;
                    float3 cEnterPoint;

                    float correctedAtmDst = dstThroughAtmosphere;
					//If inside atmosphere
                    if (dstToAtmosphere == 0)
                    {
						//Get distance behind player out of atmosphere
                        float newDist = raySphere(planetCentre, atmosphereRadius, rayOrigin, -rayDir).y; 
                        if (newDist < atmosphereRadius - planetRadius && dot(rayDir, planetCentre - rayOrigin) < 0)
                        {
                            cEnterPoint = rayOrigin - rayDir * newDist; //actually exit point
                            correctedAtmDst += newDist; //add distance behind player
                        }
                    }
                    else
                        cEnterPoint = enterPoint;

					float blueNoise = 0;
					if (sceneDepth > 9999)
					{
						blueNoise = tex2Dlod(BlueNoise, float4(squareUV(i.uv) * ditherScale,0,0));
						blueNoise = (blueNoise - 0.5) * ditherStrength * p;
					}

                    densityInDir(cEnterPoint, rayDir, correctedAtmDst / numInScatteringPoints, blueNoise);
					lightInDir(enterPoint, rayDir, dstThroughAtmosphere / numInScatteringPoints, blueNoise);

					float latitude = remap01(-atmosphereRadius, atmosphereRadius, enterPoint.y - planetCentre.y) + LayeredNoise(enterPoint - planetCentre, _Time.x * windSpeed, noiseFreq, noiseAmp) + blueNoise;

					//Get the color of the atmosphere based on the latitude (colour bands) and density (thinner means sunset colours are used) at the given point
					float4 fogColour = tex2D(fogColourRings, float2(clamp(latitude, p, 1 - p), clamp(opticalDensity, p, 1 - p)));
					fogColour *= 1 - lightIntensity;

					originalCol = GenerateOcean(originalCol, clipPlanePos, rayDir, sceneDepth, dstToOcean, dstThroughOcean, oceanPos, oceanNoise, fogColour);

                    float4 atmosphereColour = lerp(originalCol, fogColour, atmosphereDensity);

					//Anything closer than the fogRange should be visible
                    if (dstThroughAtmosphere < fogRange)
                        atmosphereColour = lerp(originalCol, atmosphereColour, remap01(0, fogRange, dstThroughAtmosphere));
					
					//If a ring is to be drawn behind the the atmosphere
					if (drawRingMode == 2)
						atmosphereColour = lerp(atmosphereColour, discColour, discColour.a);

                    return atmosphereColour;
                }
				//If a ring is to be drawn
				if (drawRingMode != 0)
					originalCol = lerp(originalCol, discColour, discColour.a);

				return originalCol;
            }

            ENDCG
        }
    }
}
