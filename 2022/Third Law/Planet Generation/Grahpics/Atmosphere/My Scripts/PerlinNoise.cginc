float Fade(float t)
{
    return t * t * t * (t * (t * 6 - 15) + 10);
}
float Lerp(float t, float a, float b)
{
    return a + t * (b - a);
}
float Grad(int hash, float x, float y, float z)
{
    const int h = hash & 15;
    const float u = h < 8 ? x : y;
    const float v = h < 4 ? y : (h == 12 || h == 14 ? x : z);
    return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
}

float PerlinNoise3D(float3 coord)
{
    const int perm[] =
    {
        151, 160, 137, 91, 90, 15,
        131, 13, 201, 95, 96, 53, 194, 233, 7, 225, 140, 36, 103, 30, 69, 142, 8, 99, 37, 240, 21, 10, 23,
        190, 6, 148, 247, 120, 234, 75, 0, 26, 197, 62, 94, 252, 219, 203, 117, 35, 11, 32, 57, 177, 33,
        88, 237, 149, 56, 87, 174, 20, 125, 136, 171, 168, 68, 175, 74, 165, 71, 134, 139, 48, 27, 166,
        77, 146, 158, 231, 83, 111, 229, 122, 60, 211, 133, 230, 220, 105, 92, 41, 55, 46, 245, 40, 244,
        102, 143, 54, 65, 25, 63, 161, 1, 216, 80, 73, 209, 76, 132, 187, 208, 89, 18, 169, 200, 196,
        135, 130, 116, 188, 159, 86, 164, 100, 109, 198, 173, 186, 3, 64, 52, 217, 226, 250, 124, 123,
        5, 202, 38, 147, 118, 126, 255, 82, 85, 212, 207, 206, 59, 227, 47, 16, 58, 17, 182, 189, 28, 42,
        223, 183, 170, 213, 119, 248, 152, 2, 44, 154, 163, 70, 221, 153, 101, 155, 167, 43, 172, 9,
        129, 22, 39, 253, 19, 98, 108, 110, 79, 113, 224, 232, 178, 185, 112, 104, 218, 246, 97, 228,
        251, 34, 242, 193, 238, 210, 144, 12, 191, 179, 162, 241, 81, 51, 145, 235, 249, 14, 239, 107,
        49, 192, 214, 31, 181, 199, 106, 157, 184, 84, 204, 176, 115, 121, 50, 45, 127, 4, 150, 254,
        138, 236, 205, 93, 222, 114, 67, 29, 24, 72, 243, 141, 128, 195, 78, 66, 215, 61, 156, 180,
        151
    };
    
    float x = coord.x;
    float y = coord.y;
    float z = coord.z;

    const int X = (int) floor(x) & 0xff;
    const int Y = (int) floor(y) & 0xff;
    const int Z = (int) floor(z) & 0xff;
    x -= floor(x);
    y -= floor(y);
    z -= floor(z);
    const float u = Fade(x);
    const float v = Fade(y);
    const float w = Fade(z);
    const int A = (perm[X] + Y) & 0xff;
    const int B = (perm[X + 1] + Y) & 0xff;
    const int AA = (perm[A] + Z) & 0xff;
    const int BA = (perm[B] + Z) & 0xff;
    const int AB = (perm[A + 1] + Z) & 0xff;
    const int BB = (perm[B + 1] + Z) & 0xff;
    return Lerp(w, Lerp(v, Lerp(u, Grad(perm[AA], x, y, z), Grad(perm[BA], x - 1, y, z)),
                               Lerp(u, Grad(perm[AB], x, y - 1, z), Grad(perm[BB], x - 1, y - 1, z))),
                       Lerp(v, Lerp(u, Grad(perm[AA + 1], x, y, z - 1), Grad(perm[BA + 1], x - 1, y, z - 1)),
                               Lerp(u, Grad(perm[AB + 1], x, y - 1, z - 1), Grad(perm[BB + 1], x - 1, y - 1, z - 1))));
}
float Smooth(float a, float b, float k)
{
    float h = max(0, min(1, (b - a + k) / (2 * k)));
    return a * h + b * (1 - h) - k * h * (1 - h);
}

float PerlinNoise1D(float x, float seed) //untested
{
    float randstep0 = frac(999 * sin(floor(x) + seed));
    float randstep1 = frac(999 * sin(floor(x - 1) + seed));

    float smoothsaw = frac(x) * frac(x) * (1 - 2 * (frac(x) - 1));
				
    return randstep0 * smoothsaw + randstep1 * (1 - smoothsaw);
}

float LayeredNoise(float3 samplePoint, float offsetTime, float noiseFreq, float noiseAmp)
{
    float lac = 1;
    float per = 1;
    float h = 0;
    for (uint i = 0; i < 3; i++)
    {
        float3 offset = float3(1, 1, 1) * (i % 2 == 0 ? 1 : -1) * offsetTime;

        h += PerlinNoise3D(normalize(samplePoint) * lac + offset) * per;
        lac *= noiseFreq;
        per *= noiseAmp;
    }
    return h / 3;
}

float2 unity_gradientNoise_dir(float2 p)
{
    p = p % 289;
    float x = (34 * p.x + 1) * p.x % 289 + p.y;
    x = (34 * x + 1) * x % 289;
    x = frac(x / 41) * 2 - 1;
    return normalize(float2(x - floor(x + 0.5), abs(x) - 0.5));
}

float unity_gradientNoise(float2 p)
{
    float2 ip = floor(p);
    float2 fp = frac(p);
    float d00 = dot(unity_gradientNoise_dir(ip), fp);
    float d01 = dot(unity_gradientNoise_dir(ip + float2(0, 1)), fp - float2(0, 1));
    float d10 = dot(unity_gradientNoise_dir(ip + float2(1, 0)), fp - float2(1, 0));
    float d11 = dot(unity_gradientNoise_dir(ip + float2(1, 1)), fp - float2(1, 1));
    fp = fp * fp * fp * (fp * (fp * 6 - 15) + 10);
    return lerp(lerp(d00, d01, fp.y), lerp(d10, d11, fp.y), fp.x);
}

float Unity_GradientNoise_float(float2 UV, float Scale)
{
    return unity_gradientNoise(UV * Scale) + 0.5;
}
