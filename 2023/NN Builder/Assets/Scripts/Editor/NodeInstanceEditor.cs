using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(NodeInstance)), CanEditMultipleObjects]
public class NodeInstanceEditor : Editor
{
    public override void OnInspectorGUI()
    {
        NodeInspector.DrawLayout();
        DrawDefaultInspector();
    }
}
