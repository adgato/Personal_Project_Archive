using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class Rand
{
    [System.Serializable]
    public struct Seed
    {
        [SerializeField] private int _ID;
        public int ID => _ID;
        private static bool unityRandomInit = false;

        public static Seed RandomSeed()
        {
            Seed seed;
            if (!unityRandomInit)
            {
                Random.InitState(System.Environment.TickCount);
                unityRandomInit = true;
            }
            seed._ID = Random.Range(int.MinValue, int.MaxValue);
            return seed;
        }
        public static Seed NewSeed(System.Random prng)
        {
            Seed seed;
            seed._ID = prng.Next(int.MinValue, int.MaxValue);
            return seed;
        }
    }
    public struct ValueBuffer
    {
        private float[] values;
        private int pointer;

        public float Next() => values[pointer = (pointer + 1) % values.Length];
        public float[] GetArray() => values;

        public static ValueBuffer NewValueBuffer(int length, System.Random prng)
        {
            ValueBuffer valueBuffer = new ValueBuffer()
            {
                values = new float[length],
                pointer = 0
            };
            for (int i = 0; i < length; i++)
                valueBuffer.values[i] = (float)prng.NextDouble();
            return valueBuffer;
        }
    }

    public static Rand stream
    {
        get
        {
            if (_stream == null)
                _stream = new Rand(Seed.RandomSeed());
            return _stream;
        }
    }
    private static Rand _stream;

    private System.Random prng;

    public Rand() => prng = new System.Random(Seed.RandomSeed().ID);
    public Rand(Seed seed) => prng = new System.Random(seed.ID);

    public float value => (float)prng.NextDouble();
    public Vector3 insideUnitCube => new Vector3(value, value, value);
    public Vector3 normal => OnUnitSphere(-1, 1, 0, 2 * Mathf.PI);
    /// <summary>
    /// NOT uniform.
    /// </summary>
    public Quaternion quaternion => new Quaternion(Range(-1, 1f), Range(-1, 1f), Range(-1, 1f), Range(-1, 1f)).normalized;


    public bool Chance(float probability01) => value < probability01;

    public float Range(float min, float max) => Mathf.Lerp(min, max, value);
    public float Range(Vector2 range) => Range(range.x, range.y);
    public float Range(float min, float max, System.Func<float, float> Distribution01) => Mathf.Lerp(min, max, Distribution01(value));

    /// <summary>Yes, max is exclusive.</summary>
    /// <param name="min">Inclusive.</param>
    /// <param name="max">Exclusive.</param>
    public int Range(int min, int max) => prng.Next(min, max);
    public Seed PsuedoNewSeed() => Seed.NewSeed(prng);
    public ValueBuffer GetValueBuffer(int length) => ValueBuffer.NewValueBuffer(length, prng);

    /// <summary>
    /// Generates a uniformly distributed normal in the region between two cross sections of the unit sphere.
    /// </summary>
    /// <param name="minZ">Min z coordinate, use Mathf.Cos(phi) if phi is known.</param>
    /// <param name="maxZ">Max z coordinate, use Mathf.Cos(phi) if phi is known.</param>
    /// <param name="minTheta">Min angle around the z axis of the normal.</param>
    /// <param name="maxTheta">Max angle around the z axis of the normal.</param>
    /// https://math.stackexchange.com/questions/56784/generate-a-random-direction-within-a-cone/205589#205589
    public Vector3 OnUnitSphere(float minZ, float maxZ, float minTheta, float maxTheta)
    {
        float z = Range(minZ, maxZ);
        float a = Range(minTheta, maxTheta);
        return new Vector3(Mathf.Sqrt(1 - z * z) * Mathf.Cos(a), Mathf.Sqrt(1 - z * z) * Mathf.Sin(a), z);
    }

    public Color ColourHSV(float minHue = 0, float maxHue = 1, float minSat = 0, float maxSat = 1, float minVal = 0, float maxVal = 1)
    {
        return Color.HSVToRGB(Range(minHue, maxHue), Range(minSat, maxSat), Range(minVal, maxVal));
    }

    //Fisher-Yates shuffle
    public void Shuffle(System.Array array)
    {
        for (int i = array.Length - 1; i > 0; i--)
        {
            int randomIndex = Range(0, i + 1);
            object temp = array.GetValue(i);
            array.SetValue(array.GetValue(randomIndex), i);
            array.SetValue(temp, randomIndex);
        }
    }
    public void Shuffle<T>(T[] array)
    {
        for (int i = array.Length - 1; i > 0; i--)
        {
            int randomIndex = Range(0, i + 1);
            T temp = array[i];
            array[i] = array[randomIndex];
            array[randomIndex] = temp;
        }
    }
    public void Shuffle<T>(List<T> array)
    {
        for (int i = array.Count - 1; i > 0; i--)
        {
            int randomIndex = Range(0, i + 1);
            T temp = array[i];
            array[i] = array[randomIndex];
            array[randomIndex] = temp;
        }
    }
}
