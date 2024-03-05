using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(HiveGen))]
public class HiveEditor : Editor
{
    public override void OnInspectorGUI()
    {
        HiveGen hive = (HiveGen)target;

        if (GUILayout.Button("Regenerate"))
        {
            hive.Start();
        }
        else if (GUILayout.Button("Clear"))
        {
            hive.Clear();
        }

        DrawDefaultInspector();
    }
}
