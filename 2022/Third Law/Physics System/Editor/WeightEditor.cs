using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Weight))]
public class WeightEditor : Editor
{
    bool changed = false;

    public override void OnInspectorGUI()
    {
        changed = DrawDefaultInspector();
    }

    public void OnSceneGUI()
    {

        Weight weight = (Weight)target;
        if (weight.transform.localScale != Vector3.one)
        {
            weight.initialVelocity += weight.transform.localScale - Vector3.one;
            weight.transform.localScale = Vector3.one;
        }
        if (!changed)
            weight.Simulate();
    }
}
