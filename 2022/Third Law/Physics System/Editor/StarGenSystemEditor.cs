using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(StarGenSystem))]
public class StarGenSystemEditor : Editor
{

    public override void OnInspectorGUI()
    {
        StarGenSystem galaxy = (StarGenSystem)target;
        if (GUILayout.Button("Generate"))
        {
            galaxy.GenerateStars(StarGenSystem.GenPurpose.starting);
        }
        if (GUILayout.Button("New Generate"))
        {
            galaxy.GenerateStars(StarGenSystem.GenPurpose.wiping);
        }
        DrawDefaultInspector();
    }
}
