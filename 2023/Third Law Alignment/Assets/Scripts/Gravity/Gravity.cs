using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gravity : MonoBehaviour
{
    [SerializeField] private float G = 0.1f;

    private List<Weight> weights = new List<Weight>();
    private List<ZeroWeight> zeroWeights = new List<ZeroWeight>();
    private Vector3[] weightAccelerations;

    public List<Weight> GetWeights() => weights;
    public List<ZeroWeight> GetZeroWeights() => zeroWeights;

    public void AddWeight(Weight weight)
    {
        if (weights.Contains(weight))
        {
            Debug.LogWarning("Warning: this weight has already been added so will be ignored");
            return;
        }
        weights.Add(weight);
        if (weight.IsZeroWeight) //weight.GetType() == typeof(ZeroWeight) will work also
            zeroWeights.Add((ZeroWeight)weight);
    }

    public void FixedUpdate()
    {
        if (ControlSaver.GamePaused)
            return;

        for (int i = 0; i < weights.Count; i++)
            weights[i].PreUpdate();

        GetAccDueToGravity();

        for (int i = 0; i < weights.Count; i++)
            weights[i].Accelerate(weightAccelerations[i]);

        for (int i = 0; i < zeroWeights.Count; i++)
            zeroWeights[i].UpdateClosest();

        Vector3 origin = (PlayerRobotWeight.Player.Closest == null ? PlayerRobotWeight.Player : PlayerRobotWeight.Player.Closest).Position;
        for (int i = 0; i < weights.Count; i++)
            weights[i].Interpolate(-origin);

        for (int i = 0; i < weights.Count; i++)
            weights[i].UpdateColliders();

        for (int i = 0; i < weights.Count; i++)
            weights[i].UpdatePosition();


        for (int i = 0; i < weights.Count; i++)
            weights[i].PostUpdate();
    }

    void GetAccDueToGravity()
    {
        weightAccelerations = new Vector3[weights.Count];
        for (int i = 0; i < weights.Count - 1; i++)
            for (int j = i + 1; j < weights.Count; j++)
            {
                //Vector3 displacement = weights[j].Position - weights[i].Position;
                //float sqrDist = displacement.sqrMagnitude;
                //Vector3 directionInfluence = G / sqrDist * displacement; //normalize vector for inverse square relationship
                //weightAccelerations[i] += directionInfluence * weights[j].Mass;
                //weightAccelerations[j] -= directionInfluence * weights[i].Mass;

                Vector3 displacement = weights[j].Position - weights[i].Position;
                float r = displacement.magnitude;
                float r2 = r * r;
                float r3 = r2 * r;

                Vector3 directionInfluence = G / r3 * displacement;
                weightAccelerations[i] += (r - weights[j].Mass) * weights[j].Mass * directionInfluence;
                weightAccelerations[j] -= (r - weights[i].Mass) * weights[i].Mass * directionInfluence;
            }
        for (int i = 0; i < weights.Count; i++)
            for (int j = 0; j < weights.Count; j++)
                if (weights[i].IsZeroWeight && ((ZeroWeight)weights[i]).EqualsClosest(weights[j]))
                {
                    Vector3 displacement = weights[j].Position - weights[i].Position;
                    float r = displacement.magnitude;
                    float r2 = r * r;
                    float r3 = r2 * r;

                    weightAccelerations[i] = weightAccelerations[j];
                    ((RobotWeight)weights[i]).FightingAccelerate(weights[j].Mass / r2 * displacement);
                }
    }

}
