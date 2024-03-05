using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(TransformChangeDetector))]
public class CDirectionalLight : MonoWatchable<CDirectionalLight.LightData>
{
    private LightData lightData;
    public override LightData GetData() => lightData;

    [SerializeField] private Color colour;
    [SerializeField] private float intensity;
    public Color Colour => colour;

    /// <summary>
    /// Should match LightStructs.cginc
    /// </summary>
    [Serializable]
    public struct LightData : IEquatable<LightData>
    {
        public Color colour;
        public float intensity;
        public Vector3 direction;

        public bool Equals(LightData other) => intensity == other.intensity && direction == other.direction;
    }

    private void Awake()
    {
        lightData.intensity = intensity;
        lightData.colour = colour;
        OnChange();
    }

    public void OnRotationChanged()
    {
        lightData.direction = transform.forward;
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
        foreach (CDirectionalLight objs in FindObjectsOfType<CDirectionalLight>())
            objs.OnChange();
    }
#endif
}
