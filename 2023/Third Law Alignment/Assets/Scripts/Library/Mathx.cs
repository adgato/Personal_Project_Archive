using System.Collections;
using System.Collections.Generic;
using UnityEngine;

///<summary>More math functions.</summary>
public struct Mathx
{
    ///<summary>Mod but always non negative.</summary>
    public static float Mod(float x, float m) => (x % m + m) % m;

    /// <summary>Sign but x = 0 returns y.</summary>
    public static float Sign(float x, float y = 1) => x == 0 ? y : x > 0 ? 1 : -1;


    /// <returns>Parameter x multiplied by itself, equivalent but more efficient than Mathf.Pow(x, 2).</returns>
    public static float Square(float x) => x * x;
    public static int Square(int x) => x * x;

    public static float Tanh(float x) => (Mathf.Exp(2 * x) - 1) / (Mathf.Exp(2 * x) + 1);

    /// <summary>
    /// Inverse lerp followed by lerp.
    /// </summary>
    /// <param name="a">Zero output value.</param>
    /// <param name="b">One output value.</param>
    /// <param name="minValue">Start input value.</param>
    /// <param name="maxValue">End input value.</param>
    /// <param name="value">Value value.</param>
    /// <returns>Output between a and b for all value.</returns>
    public static float Remap(float a, float b, float minValue, float maxValue, float value) => Mathf.Lerp(a, b, Mathf.InverseLerp(minValue, maxValue, value));

    public static float MiddleCommon(float x) => 0.5f * Mathf.Pow(2 * x - 1, 3) + 0.5f;

    public static float Frac(float x) => x - Mathf.Floor(x);

    /// <summary>
    /// Project ray to and through sphere and get collision distances.
    /// </summary>
    /// <returns>(dstToSphere, dstThroughSphere)</returns>
    public static Vector2 RaySphere(Vector3 sphereCentre, float sphereRadius, Vector3 rayOrigin, Vector3 rayDir)
    {
        Vector3 offset = rayOrigin - sphereCentre;
        //float a = 1; // Set to dot(rayDir, rayDir) if rayDir might not be normalized
        float b = Vector3.Dot(offset, rayDir);
        float d = b * b + sphereRadius * sphereRadius - Vector3.Dot(offset, offset); // Discriminant from quadratic formula

        // Number of intersections: 0 when d < 0; 1 when d = 0; 2 when d > 0
        if (d > 0)
        {
            float s = Mathf.Sqrt(d);
            float dstToSphereNear = Mathf.Max(0, -b - s);
            float dstToSphereFar = -b + s;

            // Ignore intersections that occur behind the ray
            if (dstToSphereFar >= 0)
            {
                return new Vector2(dstToSphereNear, dstToSphereFar - dstToSphereNear);
            }
        }
        // Ray did not intersect sphere
        return new Vector2(float.MaxValue, 0);
    }
}

public static class MathEx
{
    /// <summary>
    /// Convert true/false to 1/0.
    /// </summary>
    public static int ToInt(this bool b) => b ? 1 : 0;
    /// <summary>
    /// v.sqrMagnitude > 1 ? v.normalized : v
    /// </summary>
    public static Vector3 Clamp01(this Vector3 v) => v.sqrMagnitude > 1 ? v.normalized : v;

}
public struct Vector3x
{
    Vector3 v;

    Vector3x(Vector3 v) => this.v = v;

    public static implicit operator Vector3x(Vector3 x) => new Vector3x(x);
    public static implicit operator Vector3x(Vector3Int x) => new Vector3x(x);

    public static (bool x, bool y, bool z) operator <(Vector3x a, Vector3x b) => (a.v.x < b.v.x, a.v.y < b.v.y, a.v.z < b.v.z);
    public static (bool x, bool y, bool z) operator >(Vector3x a, Vector3x b) => b < a;

    public static bool Any((bool x, bool y, bool z) v) => v.x || v.y || v.z;
    public static bool All((bool x, bool y, bool z) v) => v.x && v.y && v.z;

    public static Vector3 Lerp(Vector3 a, Vector3 b, Vector3 t) => a + Vector3.Scale(b - a, t);

    public static Vector3 Lerp(float a, float b, Vector3 t) => a * Vector3.one + (b - a) * t;

    public static Vector3Int FloorToInt(Vector3 v) => new Vector3Int(Mathf.FloorToInt(v.x), Mathf.FloorToInt(v.y), Mathf.FloorToInt(v.z));
    public static Vector3Int RoundToInt(Vector3 v) => new Vector3Int(Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.y), Mathf.RoundToInt(v.z));

}


//https://bottosson.github.io/posts/oklab/
public struct Oklab
{
    public float L;
    public float a;
    public float b;
    public float alpha;
    public Oklab(float L, float a, float b, float alpha = 1)
    {
        this.L = L;
        this.a = a;
        this.b = b;
        this.alpha = alpha;
    }

    public static Oklab Lerp(Oklab a, Oklab b, float t) => new Oklab(Mathf.Lerp(a.L, b.L, t), Mathf.Lerp(a.a, b.a, t), Mathf.Lerp(a.b, b.b, t), Mathf.Lerp(a.alpha, b.alpha, t));
    public static Color Lerp(Color a, Color b, float t) => Oklab2Color(Lerp(Color2Oklab(a), Color2Oklab(b), t));

    public static Oklab Color2Oklab(Color c)
    {
        float l = 0.4122214708f * c.r + 0.5363325363f * c.g + 0.0514459929f * c.b;
        float m = 0.2119034982f * c.r + 0.6806995451f * c.g + 0.1073969566f * c.b;
        float s = 0.0883024619f * c.r + 0.2817188376f * c.g + 0.6299787005f * c.b;

        float l_ = Mathf.Pow(l, 0.3333f);
        float m_ = Mathf.Pow(m, 0.3333f);
        float s_ = Mathf.Pow(s, 0.3333f);

        return new Oklab(
            0.2104542553f * l_ + 0.7936177850f * m_ - 0.0040720468f * s_,
        1.9779984951f * l_ - 2.4285922050f * m_ + 0.4505937099f * s_,
        0.0259040371f * l_ + 0.7827717662f * m_ - 0.8086757660f * s_, c.a);
    }
    public static Color Oklab2Color(Oklab c)
    {
        float l_ = c.L + 0.3963377774f * c.a + 0.2158037573f * c.b;
        float m_ = c.L - 0.1055613458f * c.a - 0.0638541728f * c.b;
        float s_ = c.L - 0.0894841775f * c.a - 1.2914855480f * c.b;

        float l = l_ * l_ * l_;
        float m = m_ * m_ * m_;
        float s = s_ * s_ * s_;

        return new Color(
            +4.0767416621f * l - 3.3077115913f * m + 0.2309699292f * s,
        -1.2684380046f * l + 2.6097574011f * m - 0.3413193965f * s,
        -0.0041960863f * l - 0.7034186147f * m + 1.7076147010f * s, c.alpha);
    }
}