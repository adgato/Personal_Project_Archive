using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.CustomRenderPipeline;

[CustomEditor(typeof(CRPAsset))]
public class CRPEditor : Editor
{
    public override void OnInspectorGUI()
    {
        CRPAsset viewer = (CRPAsset)target;

        if (GUILayout.Button("Reinitialise CRP"))
            viewer.Initialise();

        base.OnInspectorGUI();

        if (!viewer.FullyConfigured())
            EditorGUILayout.HelpBox("CRP has not been properly initialised.", MessageType.Info);
    }
}
