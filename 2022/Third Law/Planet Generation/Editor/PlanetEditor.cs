using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Planet))]
public class PlanetEditor : Editor
{
    public override void OnInspectorGUI()
    {
        Planet planet = (Planet)target;

        
        if (GUILayout.Button("New Noise"))
        {
            planet.planetSettings.RandomiseTerrainSeed();
            planet.planetSettings.SyncSettings();
        }
        else if (GUILayout.Button("New Gradient"))
        {
            planet.planetSettings.RandomiseColourSeed();
            planet.planetSettings.SyncSettings();
        }
        else if (GUILayout.Button("New Environment"))
        {
            planet.planetSettings.RandomiseEnvironmentSeed();
            planet.planetSettings.SyncSettings();
        }
        else if (GUILayout.Button("Generate"))
        {
            if (planet.setPlanetValues.masterSeed.Length < 12)
                Debug.LogError("Error: Master Seed length less than 12");
            else
                planet.Create(planet.transform.position, planet.GetComponent<Weight>().initialVelocity);
        }
        else if (GUILayout.Button("New Generate"))
        {
            if (planet.setPlanetValues.masterSeed.Length < 12)
                Debug.LogError("Error: Master Seed length less than 12");
            else
            {
                planet.planetSettings.RandomiseTerrainSeed();
                planet.planetSettings.RandomiseColourSeed();
                planet.planetSettings.RandomiseEnvironmentSeed();
                planet.planetSettings.SyncSettings();
                planet.Create(planet.transform.position, planet.GetComponent<Weight>().initialVelocity);
            }
        }
        else if (DrawDefaultInspector())
        {
            planet.planetValues.radius = Mathf.Max(planet.planetValues.radius, 1);
            if (planet.setPlanetValues.masterSeed.Length > 12)
                planet.setPlanetValues.masterSeed = planet.setPlanetValues.masterSeed.Substring(0, 12);
            //planet.planetSettings.GetSettings();
            //planet.planetSettings.SyncValues();
            //planet.UpdateTerrain();
        }
        
    }

}
