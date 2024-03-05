using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CDirectionalLight))]
public class CustomDirectionalLightEditor : Editor
{
    void OnSceneGUI()
    {
        CDirectionalLight light = (CDirectionalLight)target;


        Vector3 pos = light.transform.position;
        Vector3 dir = light.transform.forward;
        float size = HandleUtility.GetHandleSize(pos);
        float scale = 0.2f * size;

        // Draw the direction in the scene view
        Color solidColour = light.Colour;
        solidColour.a = 0.5f;
        Handles.color = solidColour;

        Vector3 di1 = (light.transform.right + light.transform.up).normalized * scale;
        Vector3 di2 = (light.transform.right - light.transform.up).normalized * scale;
        Vector3 up = light.transform.up * scale;
        Vector3 right = light.transform.right * scale;
        Vector3 fwd = dir * size;
        
        Handles.DrawWireDisc(pos, dir, scale);
        Handles.DrawLine(pos + up, pos + up + fwd, scale);
        Handles.DrawLine(pos - up, pos - up + fwd, scale);
        Handles.DrawLine(pos + right, pos + right + fwd, scale);
        Handles.DrawLine(pos - right, pos - right + fwd, scale);
        Handles.DrawLine(pos + di1, pos + di1 + fwd, scale);
        Handles.DrawLine(pos - di1, pos - di1 + fwd, scale);
        Handles.DrawLine(pos + di2, pos + di2 + fwd, scale);
        Handles.DrawLine(pos - di2, pos - di2 + fwd, scale);

        // Update scene view to reflect changes
        if (GUI.changed)
            EditorUtility.SetDirty(target);
    }
}
