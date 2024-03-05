using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PlanetData))]
public class PlanetDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        PlanetData planetData = (PlanetData)target;
        if (Application.isPlaying && GUILayout.Button("New"))
            planetData.Randomise();
        if (Application.isPlaying && GUILayout.Button("Generate"))
            planetData.Initialise();
        base.OnInspectorGUI();
    }
}
