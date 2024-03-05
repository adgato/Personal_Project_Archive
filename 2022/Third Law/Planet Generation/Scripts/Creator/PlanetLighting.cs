using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
[ExecuteAlways]
#endif
public class PlanetLighting : MonoBehaviour
{
    [SerializeField] private Transform sun;
    [SerializeField] private RobotWeight robotWeight;
    [SerializeField] private float lightIntensityDropoff;
    [SerializeField] private float maxIntensity;
    [SerializeField] private float minIntensity;
    [SerializeField] private float maxIntensityAtDistance;
    [SerializeField] private float minIntensityAtDistance;
    private float minAtSqrDist;
    private float maxAtSqrDist;
    private Light sunlight;
    private bool isPlanet;
    private PlanetEffect atmosphere;

    private void Start()
    {
        minAtSqrDist = minIntensityAtDistance * minIntensityAtDistance;
        maxAtSqrDist = maxIntensityAtDistance * maxIntensityAtDistance;

        sunlight = GetComponent<Light>();
        isPlanet = transform.parent.GetChild(2).TryGetComponent(out atmosphere);

    }

    //Only want to light this planet (and the player and ship if they are on it)
    private void Update()
    {
        sunlight.cullingMask = 0;

        bool lightingPlayer;

        if (isPlanet)
        {
            sunlight.cullingMask |= 1 << gameObject.layer;
            lightingPlayer = robotWeight.sigWeight != null && transform.IsChildOf(robotWeight.sigWeight.transform);
        }
        else if (robotWeight.sigWeight != null)
        {
            lightingPlayer = false;
            atmosphere = robotWeight.sigWeight.planet.atmosphere;
        }
        else
        {
            lightingPlayer = true;
            atmosphere = null;
        }

        //If lighting player then mask player and ship outside (layers 6 & 12)
        if (lightingPlayer)
            sunlight.cullingMask |= 1 << 6 | 1 << 12;

        lightIntensityDropoff = 1 - Mathf.InverseLerp(0.4f, 6, atmosphere == null ? 0.4f : atmosphere.density);

        transform.rotation = Quaternion.LookRotation(transform.position - sun.position);
        float t = Mathf.InverseLerp(maxAtSqrDist, minAtSqrDist, (transform.position - sun.position).sqrMagnitude);
        sunlight.intensity = lightIntensityDropoff * Mathf.Lerp(maxIntensity, minIntensity, t);
        sunlight.shadowStrength = lightIntensityDropoff;
    }

    private void OnValidate()
    {
        Start();
    }
}
