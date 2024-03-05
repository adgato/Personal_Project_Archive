using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct MeshData
{
    public NodePack[] nodes;
    public int[] tris;
}
public struct NodePack
{
    public Vector3 vert;
    public Vector2 uv;
}

public struct Craters
{
    public Vector3 point;
    public float radius;
    public float floorHeight;
}

public struct PlanetValues
{
    public int terrainSeed;
    public int colourSeed;
    public int environmentSeed;
    public Dictionary<float, Gradient> biomeGradients;
    public float groundLevel;
    public float seabedLevel;
    public bool roughBed;
    public bool crackedGround;
    public float radius;
    public float temperature;
    public float windSpeed;
}
public struct CraterValues
{
    public int seed;
    public int numCraters;
    public float smoothness;
    public float rimWidth;
    public float rimSteepness;
}
public struct ContinentNoiseValues
{
    public int seed;
    public int octaves;
    public float scale;
    public float persistance;
    public float lacunarity;
    public float dropoff;
}
public struct MountainNoiseValues
{
    public int seed;
    public int octaves;
    public float scale;
    public float persistance;
    public float lacunarity;
    public float dropoff;
    public int initialExtraOctaves;
}
public struct RoughNoiseValues
{
    public int seed;
    public int octaves;
    public float scale;
    public float persistance;
    public float lacunarity;
    public float startingOctave;
}
public struct WarpNoiseValues
{
    public int seed;
    public int octaves;
    public float scale;
    public float persistance;
    public float lacunarity;
}
