using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class Weight : MonoBehaviour
{
    //The sigWeight is the PLANET closest (most significant) to this weight, this is important information for many scripts
    [HideInInspector] public Weight sigWeight = null;

    public static float gConst = 30f;
    public static int framesPerUpdate = 60;

    [HideInInspector] public Planet planet;
    protected Rigidbody rb;
    public Vector3 position { get { return rb.position; } set { rb.position = value; } }

    [SerializeField] protected float Mass;
    private void OnValidate() {  Mass = Mathf.Max(1, Mass); }

    public virtual float mass { get { return Mass; } }
    public Vector3 initialVelocity;

    public Vector3 velocity;
    private Vector3 acceleration;

    private int counter = -1;

    public bool stationary = false;

    [Space]
    [Header("Simulation Settings")]
    public Color colour;
    [Range(0, 300)]
    public float secondsAhead;
    [SerializeField] private Weight relativeTo;
    private Vector3 simVelocity;
    private Vector3 simAcceleration;
    [HideInInspector] public List<Vector3> positionOverTime;

    public static float MassForSurfaceG(float g, float radius)
    {
        return g * radius / gConst;
    }

    public void Init()
    {
        sigWeight = null;
        velocity = initialVelocity;
        ConfigRigidbody();
        TryGetComponent(out planet);
    }

    public void SetMass(float g, float radius)
    {
        Mass = MassForSurfaceG(g, radius);
    }

    private void ConfigRigidbody()
    {
        if (!TryGetComponent(out rb))
            rb = gameObject.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        //rb.freezeRotation = true;
    }

    public void GetGravity(out Vector3 displacement)
    {
        velocity += GetAcceleration() * Time.fixedDeltaTime;

        displacement = velocity * Time.fixedDeltaTime;
    }

    public void Teleport(Vector3 displacement, bool force = false)
    {
        if (stationary && !force)
            return;
        position += displacement;
    }

    private Vector3 GetAcceleration()
    {
        counter++;
        if (counter % framesPerUpdate != 0)
            return acceleration;

        acceleration = Vector3.zero;

        float minSqrDist = 25_000_000; //5000^2 (maximum distance of rings)

        sigWeight = null;

        //Add acceleration due to gravity towards each active weight
        foreach (Weight weight in PhysicsUpdate.weights)
        {
            if (weight == this || weight.mass == 0)
                continue;

            Vector3 acc = gConst * weight.mass / (weight.position - position).sqrMagnitude * (weight.position - position); //directly proportional to distance (since 1 normalizes vector)
            acceleration += acc;

            if (weight.planet != null && (weight.position - position).sqrMagnitude < minSqrDist)
            {
                minSqrDist = (weight.position - position).sqrMagnitude;
                sigWeight = weight;
            }
        }
        return acceleration;
    }

    public Vector3 GetAccelerationAhead(int frame, List<Weight> weights)
    {
        Vector3 acceleration = Vector3.zero;
        foreach (Weight weight in weights)
        {
            if (weight == this)
                continue;
            acceleration += gConst * weight.mass / (weight.positionOverTime[frame] - positionOverTime[frame]).magnitude * (weight.positionOverTime[frame] - positionOverTime[frame]).normalized;
        }
        return acceleration;
    }

    public void Simulate()
    {
        if (relativeTo == null)
            relativeTo = this;

        int frames = Mathf.RoundToInt(secondsAhead / Time.fixedDeltaTime);

        List<Weight> sigWeights = new List<Weight>();

        foreach (Weight weight in FindObjectsOfType<Weight>())
        {
            if (weight.mass == 0)
                continue;

            weight.positionOverTime = new List<Vector3>(frames);
            weight.positionOverTime.Add(weight.transform.position);
            weight.simVelocity = Application.isPlaying ? weight.velocity : weight.initialVelocity;

            sigWeights.Add(weight);
        }

        for (int i = 0; i < frames; i++)
        {
            foreach (Weight weight in sigWeights)
            {
                if (i % framesPerUpdate == 0)
                    weight.simAcceleration = weight.GetAccelerationAhead(i, sigWeights);
                weight.simVelocity += weight.simAcceleration * Time.fixedDeltaTime;
                weight.positionOverTime.Add(weight.positionOverTime[i] + (weight.stationary ? Vector3.zero : weight.simVelocity * Time.fixedDeltaTime));
            }
        }
#if UNITY_EDITOR
        if (Application.isEditor)
        {
            foreach (Weight weight in sigWeights)
            {
                List<Vector3> lines = new List<Vector3>();
                for (int i = 0; i < frames; i += framesPerUpdate)
                    lines.Add(relativeTo.transform.position - relativeTo.positionOverTime[i] + weight.positionOverTime[i]);
                Handles.color = weight.colour;
                Handles.DrawPolyLine(lines.ToArray());
            }
        }
#endif
    }
}
