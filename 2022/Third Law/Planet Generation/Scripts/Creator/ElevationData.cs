using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElevationData
{
    public float Max { get; private set; }
    public float Min { get; private set; }

    public float Mean01 { get; private set; }
    public float lowerMean01 { get; private set; }
    public float upperMean01 { get; private set; }
    public float interMeanRange01 { get; private set; }
    private int meanCount;
    private int lowCount;
    private int upCount;
    
    private List<Vector3[]> vertSegments;
    
    public ElevationData()
    {
        Reset(float.MaxValue);
    }
    public void Reset(float _radius)
    {
        Min = _radius;
        Max = float.MinValue;
        Mean01 = 0;
        lowerMean01 = 0;
        upperMean01 = 0;
        meanCount = 0;
        lowCount = 0;
        upCount = 0;
        vertSegments = new List<Vector3[]>();
    }
    public void Add(float v)
    {
        Max = Mathf.Max(Max, v);
        if (v >= Min)
        {
            Mean01 += v;
            meanCount++;

            if (v < Mean01 / meanCount)
            {
                //Since the mean will change as adding values, this only approximately the mean of all the values below the mean
                lowerMean01 += v; 
                lowCount++;
            }
            else
            {
                //(See above) this method is better because it is significantly less expensive for only a slight reduction in accuracy.
                upperMean01 += v; 
                upCount++;
            }
        }
    }
    public void CalcStats()
    {
        if (Min == float.MaxValue)
            Debug.LogError("Error: Elevation Data needs to be reset");

        Mean01 = Mathf.InverseLerp(Min, Max, Mean01 / meanCount);
        lowerMean01 = Mathf.InverseLerp(Min, Max, lowerMean01 / lowCount);
        upperMean01 = Mathf.InverseLerp(Min, Max, upperMean01 / upCount);
        //If values are very similar the range can be negative so absolute value taken below 
        interMeanRange01 = Mathf.Abs(upperMean01 - lowerMean01); 

        //Debug.Log("Mean: " + Mean01 + "\nL: " + lowerMean01 + "\nU: " + upperMean01 + "\nI: " + interMeanRange01);
    }
    public void AddMap(Vector3[] verts)
    {
        vertSegments.Add(verts);
    }
    public Vector3[] GetMapTexture(int index)
    {
        return vertSegments[index];
    }
}
