#include "PerlinNoise.cginc"

float3 GenerateWarpMap(float3 vert, float persistance, float lacunarity, float scale, int octaves, StructuredBuffer<float3> offsets)
{
    float height = 0.0f;
    float amp = 1.0f;
    float freq = 1.0f;
    for (int i = 0; i < octaves; i++)
    {
        float3 samplePoint = vert * freq + offsets[i];

        float perlinValue = PerlinNoise3D(samplePoint * scale);
        
        height += perlinValue * amp;
        
        //Higher octaves have higher frequency and smaller amplitude (finer noise)
        amp *= persistance;
        freq *= lacunarity;
    }
    return height * vert;
}

float GenerateContinents(float3 vert, float persistance, float lacunarity, float scale, int octaves, StructuredBuffer<float3> offsets, float dropoff, float groundLevel, float seabedLevel, float3 warpMap)
{
    float height = 0.0f;
    float amp = 1.0f;
    float freq = 1.0f;
    for (int i = 0; i < octaves; i++)
    {
        //Offset by warp map for more interesting (warped) noise
        float3 samplePoint = vert * freq + offsets[i] + warpMap;
        float perlinValue = 1 + PerlinNoise3D(samplePoint * scale);
        height += perlinValue * amp;
        
        amp *= persistance;
        freq *= lacunarity;
    }
    if (height > groundLevel)
        height = lerp(groundLevel, height, dropoff);
    
    return height;
}

float GenerateMountains(float3 vert, float persistance, float lacunarity, float scale, int octaves, StructuredBuffer<float3> offsets, float dropoff, float initialExtraOctaves, float groundLevel, float seabedLevel)
{
    float height = 0.0f;
    float amp = 1.0f;
    float freq = 1.0f;
    
    for (int i = 0; i < octaves; i++)
    {
        float3 samplePoint = vert * freq + offsets[i];
        float perlinValue = 1 + PerlinNoise3D(samplePoint * scale);

        //Run first octave multiple times to generate lots of mountains
        if (i == 0)
        {
            for (int j = octaves; j < initialExtraOctaves; j++)
            {
                samplePoint = vert * freq + offsets[j];
                perlinValue = max(perlinValue, 1 + PerlinNoise3D(samplePoint * scale));
            }
        }

        height += perlinValue * amp;

        amp *= persistance;
        freq *= lacunarity;
    }
    if (height > groundLevel)
        height = height * height * dropoff;
    
    return height;
}

float GenerateRoughTerrain(float3 vert, float persistance, float lacunarity, int octaves, StructuredBuffer<float3> offsets, float startingOctave)
{
    float height = 0.0f;
    float amp = pow(abs(persistance), startingOctave);
    float freq = pow(abs(lacunarity), startingOctave);
    
    for (int i = 0; i < octaves; i++)
    {
        float3 samplePoint = vert * freq + offsets[i];
        float perlinValue = 1 + PerlinNoise3D(samplePoint);
        height += perlinValue * amp;
        
        amp *= persistance;
        freq *= lacunarity;
    }
    height /= 10;
    return 1 + height;
}

struct Craters
{
    float3 position;
    float radius;
    float floorHeight;
};

///START OF NOT MY CODE

float GenerateCraters(float3 vert, float rimWidth, float rimSteepness, float smoothness, int numCraters, StructuredBuffer<Craters> craters, float radius)
{
    float craterHeight = 0;
    for (int i = 0; i < numCraters; i++)
    {

        float x = length(vert - craters[i].position) / craters[i].radius;

        float cavity = x * x - 1;
        float rimX = min(0, x - 1 - rimWidth);
        float rim = rimSteepness * rimX * rimX;

        float craterShape = Smooth(cavity, craters[i].floorHeight, -smoothness);
        craterShape = Smooth(craterShape, rim, smoothness);
        craterHeight += craterShape * craters[i].radius;
    }
    return max(-radius / 2, craterHeight);
}

///END OF NOT MY CODE