using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SunGenSystem))]
public class SunGenSystemEditor : Editor
{

    public override void OnInspectorGUI()
    {
        SunGenSystem sun = (SunGenSystem)target;
        if (GUILayout.Button("Generate"))
        {
            sun.Generate(sun.lordSeed, sun.transform.position, 5000);
        }
        if (GUILayout.Button("New Generate"))
        {
            sun.Generate(Random.Range(0, 999999), sun.transform.position, 5000);
        }
        DrawDefaultInspector();
    }
}
