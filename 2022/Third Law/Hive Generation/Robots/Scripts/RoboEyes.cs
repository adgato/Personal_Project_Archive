using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoboEyes
{
    [SerializeField] private Transform[] Eyes;
    [SerializeField] private Light[] lights;
    public Color colour { get; private set; }

    public RoboEyes(RoboVision _roboVision)
    {
        Eyes = _roboVision.Eyes;
        lights = _roboVision.GetComponentsInChildren<Light>();
        Eyes[0].GetChild(0).GetComponent<MeshRenderer>().sharedMaterial = new Material(Eyes[0].GetChild(0).GetComponent<MeshRenderer>().sharedMaterial);
        Eyes[1].GetChild(0).GetComponent<MeshRenderer>().sharedMaterial = Eyes[0].GetChild(0).GetComponent<MeshRenderer>().sharedMaterial;
    }
    public RoboEyes(AndroidVision _roboVision)
    {
        Eyes = _roboVision.Eyes;
        lights = _roboVision.GetComponentsInChildren<Light>();
        Eyes[0].GetChild(0).GetComponent<MeshRenderer>().sharedMaterial = new Material(Eyes[0].GetChild(0).GetComponent<MeshRenderer>().sharedMaterial);
        Eyes[1].GetChild(0).GetComponent<MeshRenderer>().sharedMaterial = Eyes[0].GetChild(0).GetComponent<MeshRenderer>().sharedMaterial;
    }

    public void SetEyeColour(Color eyeColour)
    {
        colour = eyeColour;
        Eyes[0].GetChild(0).GetComponent<MeshRenderer>().sharedMaterial.SetColor("_Color", eyeColour);
        Eyes[1].GetChild(0).GetComponent<MeshRenderer>().sharedMaterial.SetColor("_Color", eyeColour);
        foreach (Light light in lights)
            light.color = eyeColour;
    }
}
