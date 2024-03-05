using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TreeGen))]
public class TreeEditor : Editor
{
    public override void OnInspectorGUI()
    {
        TreeGen tree = (TreeGen)target;

        DrawDefaultInspector();
        if (GUILayout.Button("Regenerate"))
        {
            tree.Init(0, Random.Range(-9999, 9999));
            tree.RenderTree();
        }
        
    }
}
