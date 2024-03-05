using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TowerGen))]
public class TowerEditor : Editor
{
    private int seed;

    public override void OnInspectorGUI()
    {
        TowerGen tower = (TowerGen)target;

        DrawDefaultInspector();
        if (GUILayout.Button("Clear"))
        {
            if (tower.transform.childCount == 1)
            {
                tower.transform.GetChild(0).gameObject.SetActive(false);
                DestroyImmediate(tower.transform.GetChild(0).gameObject);
            }
        }
        else if (GUILayout.Button("New Placeholder"))
        {
            tower.debugMake = false;
            seed = Random.Range(-9999, 9999);
            tower.Init(seed);
        }
        else if (GUILayout.Button("Placeholder"))
        {
            tower.debugMake = false;
            tower.Init(seed);
        }
        else if (GUILayout.Button("Generate"))
        {
            tower.debugMake = true;
            tower.Init(seed);
        }
        else if (GUILayout.Button("New Generate"))
        {
            tower.debugMake = true;
            seed = Random.Range(-9999, 9999);
            tower.Init(seed);
        }
        else if (GUILayout.Button("Realign"))
        {
            tower.DebugRealign();
        }
    }
}
