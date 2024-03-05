using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(BezierTree))]
public class BezierTreeEditor : Editor
{
    public override void OnInspectorGUI()
    {
        BezierTree tree = (BezierTree)target;

        if (GUILayout.Button("Regenerate"))
        {
            tree.GenerateTree(Rand.Seed.RandomSeed());
            tree.RenderTree();
        }
        if (GUILayout.Button("Generate"))
        {
            tree.GenerateTree();
            tree.RenderTree();
        }

        DrawDefaultInspector();
    }
}
