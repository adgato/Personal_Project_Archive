#include "../Math.cginc"

float GradientNoise(float2 UV, float Scale, sampler2D noiseTexture)
{
    return tex2D(noiseTexture, UV * Scale);
}

struct Disc
{
    float4 colour;
    float3 normal;
    float2 iris;
};

float4 DrawRings(float3 rayDir, float3 sunDir, Disc disc, float rings, float distance01, float distanceToRing, sampler2D noiseTexture)
{
				//Calculate the overall opacity of the ring based on the two noise values that determine the ring spacing
    float coarseNoise = GradientNoise(float2(distance01, distance01), rings, noiseTexture);
    float fineNoise = GradientNoise(float2(distance01, distance01), rings, noiseTexture);
    float alpha = clamp(pow(sin(1.571429 * coarseNoise), 5) * fineNoise, 0, 1);
    alpha *= saturate(invlerp(5, 100, distanceToRing));

    float4 ringColour = disc.colour * max(0.5, square(dot(disc.normal, rayDir)) * 2); //creates shine on ring
    ringColour.a = alpha * disc.colour.a;
    return ringColour;
}

//old code, it works, could be simpler but its efficient, and is even commented!
uint GenerateRings(float3 planetCentre, float3 sunDir, float3 rayOrigin, float3 rayDir, float planetRadius, float surfaceDist, float dstToAtmosphere, float numDiscs, Disc disc1, Disc disc2, float rings, sampler2D noiseTexture, out float4 discColour)
{
    discColour = float4(0, 0, 0, 0);

    float3 intersectPoint;
    float distanceToRing = surfaceDist + 1;
    float ringDistToCentre = 0;
    float distance01 = -1;

    if (numDiscs > 0)
    {
					//Calculate the intersection time with the first disc
        float intersectTime = dot(disc1.normal, planetCentre - rayOrigin) / dot(disc1.normal, rayDir);
					
					//If the intersection is in front of the camera, calculate the intersect point and ring colour to draw
        if (intersectTime > 0)
        {
            intersectPoint = rayOrigin + rayDir * intersectTime;

            ringDistToCentre = length(intersectPoint - planetCentre);

            distance01 = ringDistToCentre / (planetRadius * 4);

            if (distance01 > disc1.iris.x && distance01 < disc1.iris.y)
            {
                distanceToRing = length(intersectPoint - rayOrigin);
                if (distanceToRing < surfaceDist)
                    discColour = DrawRings(rayDir, sunDir, disc1, rings, distance01, distanceToRing, noiseTexture);
            }
        }
    }
    if (numDiscs > 1)
    {
        float intersectTime = dot(disc2.normal, planetCentre - rayOrigin) / dot(disc2.normal, rayDir);

        if (intersectTime > 0)
        {
            float3 newIntersectPoint = rayOrigin + rayDir * intersectTime;
            float newRingDist = length(newIntersectPoint - planetCentre);
            float newDist = length(newIntersectPoint - rayOrigin);
            float newdist01 = newRingDist / (planetRadius * 4);

            if (newDist < surfaceDist && newdist01 > disc2.iris.x && newdist01 < disc2.iris.y)
            {
                float4 newDiscColour = DrawRings(rayDir, sunDir, disc2, rings, newdist01, newDist, noiseTexture);

							//If the disc is closer than the previously drawn disc or there are no discs drawn yet, draw the new disc
                if (discColour.a == 0 || distanceToRing > dstToAtmosphere)
                    discColour = newDiscColour;
							// Otherwise, blend the two discs together
                else if (newDist < dstToAtmosphere || dstToAtmosphere == 0)
                {
                    float geoMean = sqrt(newDiscColour.a * discColour.a);
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
    discColour.a = sqrt(discColour.a); //looks better
				//Return 0 if no discs were drawn, 1 if discs were only drawn behind the atmosphere and 2 if a disc was drawn in front of the atmosphere 
    if (discColour.a == 0)
        return 0;
    else if (distanceToRing > dstToAtmosphere)
        return 1;
    return 2;
}


float4 GenerateOcean(
    float time, float4 originalCol, float4 oceanColour, float3 rayDir, float3 sunDir, 
    float planetRadius, float sceneDepth, float2 oceanInfo, float dstAboveWater, float3 oceanPos, float oceanNoise,
    float waveStrength, float smoothness, float waveNormalScale, 
    sampler2D waveNormalA, sampler2D waveNormalB)
{
    if (oceanInfo.x > sceneDepth || oceanInfo.y <= 0)
        return originalCol;
    waveStrength *= saturate(1 - exp(-oceanInfo.x));
    smoothness *= saturate(1 - exp(-oceanInfo.x));

    //oceanColour = normalize(oceanColour) * 0.1;

				//Calculate the distance above water by subtracting the ocean radius from the distance between the clip plane position and the planet center
    //float dstAboveWater = length(clipPlanePos - planetCentre) - oceanRadius;
    //float time =  _Time.x * wasveSpeed

    float4 finalColour = lerp(oceanColour, originalCol, exp(-0.5 * (sceneDepth - oceanInfo.x)));
				//This adds our lip lining shore at depths of 5 units or less
    finalColour = lerp(finalColour, 1, exp(-2 * (1 - 0.5 * square(oceanNoise)) * (sceneDepth - oceanInfo.x)));

    float3 oceanSphereNormal = normalize(oceanPos);

				//Calculate two wave offsets based on the time and wave speed, and use them to calculate the normal vector of the waves using triplanar mapping
    float2 waveOffsetA = float2(time, time * 0.5);
    float2 waveOffsetB = float2(time * 0.5, time * -1);
    float3 waveNormal = triplanarNormal(oceanPos, oceanSphereNormal, waveNormalScale, waveOffsetA, waveNormalA);
    waveNormal = triplanarNormal(oceanPos, waveNormal, waveNormalScale * 2, waveOffsetB, waveNormalB);
    waveNormal = normalize(lerp(oceanSphereNormal, waveNormal, waveStrength));
				//return float4(oceanNormal * .5 + .5,1);
    float diffuseLighting = saturate(dot(oceanSphereNormal, sunDir) + 1);
    float specularAngle = acos(dot(normalize(sunDir - rayDir), waveNormal));
    float specularExponent = abs(specularAngle) / smoothness;
    float specularHighlight = exp(-specularExponent * specularExponent);
			
    finalColour *= diffuseLighting;
    finalColour += specularHighlight * lerp(finalColour, oceanColour, 0.5);
				
    finalColour *= dot(waveNormal, sunDir) * 0.5 + 0.5; //0.75 + reflection01(waveNormal, oceanPos) * 0.25;

    return saturate(finalColour);
}
