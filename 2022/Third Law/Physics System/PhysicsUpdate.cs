using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PhysicsUpdate : MonoBehaviour
{
    public static List<Weight> weights;
    public static ZeroWeight[] zeroWeights;
    public static ShipWeight shipWeight;
    public static RobotWeight robotWeight;
    public List<Weight> readonlyWeights;

    private void Start()
    {
        Reinit();
    }
    public static void Reinit()
    {
        weights = new List<Weight>();
        zeroWeights = FindObjectsOfType<ZeroWeight>();
        foreach (ZeroWeight zeroWeight in zeroWeights)
            AddWeight(zeroWeight);

        shipWeight = FindObjectOfType<ShipWeight>();
        robotWeight = FindObjectOfType<RobotWeight>();
    }
    public static void AddWeight(Weight weight)
    {
        if (weight != null && weights != null)
        {
            weights.Add(weight);
            weight.Init();
        }
    }
    public static void RemoveWeight(Weight weight)
    {
        if (weights.Contains(weight))
            weights.Remove(weight);
    }
    private void FixedUpdate()
    {
        if (CameraState.isPaused)
            return;

        readonlyWeights = weights;

        Vector3[] displacements = new Vector3[weights.Count];
        Vector3[] zeroDisplacements = new Vector3[zeroWeights.Length];
        int k = 0;
        int i = 0;

        
        //Get all displacements due to gravity
        foreach (Weight weight in weights)
            weight.GetGravity(out displacements[k++]);

        //Apply these displacements to weights with mass, and apply displacements of the most proximate mass to weights without mass
        k = 0;
        foreach (Weight weight in weights)
        {
            if (weight.mass == 0)
            {
                if (weight.sigWeight != null)
                {
                    int j = weights.IndexOf(weight.sigWeight);
                    weight.Teleport(displacements[j]);
                    zeroDisplacements[i] = displacements[k] - displacements[j];
                }
                else
                    zeroDisplacements[i] = displacements[k];
                i++;
                k++;
            }
            else
                weight.Teleport(displacements[k++]);
        }

        //Collsion Accounted for with Relative Displacement (CARD), collision detection algorithm works properly for a zeroWeight iff the displacement paramater of MoveRelative is relative to every collider significant
        //Apply CARD for Ship first (so it dosen't clip through Planets)
        i = System.Array.IndexOf(zeroWeights, shipWeight);
        Vector3 shipSafeDisplacement = shipWeight.MoveRelative(zeroDisplacements[i]);

        //Apply CARD on Robot relative to Ship if necessary (shipSafeDisplacement accounts for robot displacement relative to planet, since the robot is in the ship and shipSafeDisplacement is relative to the planet) 
        i = System.Array.IndexOf(zeroWeights, robotWeight);
        if (CameraState.inShip)
        {
            robotWeight.Teleport(shipSafeDisplacement);
            if (CameraState.InLockState(CameraState.LockState.unlocked))
                robotWeight.MoveRelative(zeroDisplacements[i] - shipSafeDisplacement);
        }
        else
            robotWeight.MoveRelative(zeroDisplacements[i]);

        //Apply CARD to the other zero weights last
        i = 0;
        foreach (ZeroWeight weight in zeroWeights)
        {
            if (weight != robotWeight && weight != shipWeight)
                weight.MoveRelative(zeroDisplacements[i]);
            i++;
        }

        robotWeight.UpdateForceMeter();
        //Camera close to origin minimises floating point error in camera position (stops jittering)
        Vector3 origin = robotWeight.position;

        StarGenSystem.galaxyCentre -= origin;
        foreach (Weight weight in weights)
            weight.Teleport(-origin, true);
    }
}
