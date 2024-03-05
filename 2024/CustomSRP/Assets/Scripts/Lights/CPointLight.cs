using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(TransformChangeDetector))]
public class CPointLight : MonoWatchable<CPointLight.LightData>
{
    private LightData lightData;
    public override LightData GetData() => lightData;

    [SerializeField] private Color colour;
    [SerializeField] private float intensity; 

    /// <summary>
    /// Should match LightStructs.cginc
    /// </summary>
    [Serializable]
    public struct LightData : IEquatable<LightData>
    {
        public Color colour;
        public float intensity;
        public Vector3 worldPos;

        public bool Equals(LightData other) => intensity == other.intensity && worldPos == other.worldPos;
    }
    public void OnPositionChanged()
    {
        lightData.worldPos = transform.position;
        OnChange();
    }

    private void Awake()
    {
        lightData.intensity = intensity;
        lightData.colour = colour;
        OnChange();
    }
    private void OnValidate()
    {
        lightData.intensity = intensity;
        lightData.colour = colour;
        OnChange();
    }
    private void OnDrawGizmos()
    {
        Gizmos.DrawIcon(transform.position, "d_DirectionalLight Icon.png", true);
    }

#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadMethod]
    private static void OnLoadMethod()
    {
        foreach (CPointLight objs in FindObjectsOfType<CPointLight>())
            objs.OnChange();
    }
#endif
}
