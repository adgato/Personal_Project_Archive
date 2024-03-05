using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CompassAligner : MonoBehaviour
{
    [SerializeField] private RobotWeight robot;
    [SerializeField] private ShipWeight ship;
    [SerializeField] private Material streaksMat;
    private Material compass;
    private float lerp;
    // Start is called before the first frame update
    void Start()
    {
        compass = new Material(GetComponent<MeshRenderer>().sharedMaterial);
        GetComponent<MeshRenderer>().sharedMaterial = compass;
    }

    // Update is called once per frame
    void Update()
    {

        compass.SetFloat("_turnOnLerp", lerp);
        compass.SetFloat("_engineOn", InventoryUI.shipEngineOn01);
        if (ShipWeight.hyperOn)
            compass.EnableKeyword("_HYPERON");
        else
            compass.DisableKeyword("_HYPERON");

        //Show the velocity meter when not close to a planet
        if (robot.sigWeight == null)
        {
            float shipVelocity = ship.velocity.magnitude;
            float speed0to1_5 = Mathf.InverseLerp(0, ship.maxVelocity, shipVelocity) + 0.5f * Mathf.InverseLerp(ship.maxVelocity, ship.maxHyperVelocity, shipVelocity);
            float revs01 = 1 - (1 - Mathf.InverseLerp(0, ship.maxHyperVelocity + 1, shipVelocity)) % 0.125f / 0.125f;
            compass.SetFloat("_speed0to1_5", speed0to1_5);
            compass.SetFloat("_revs01", revs01);
            if (streaksMat != null)
                streaksMat.SetFloat("_speed0to1_5", speed0to1_5);

            lerp = Mathf.Clamp01(lerp - Time.deltaTime * 3);
            return;
        }
        lerp = Mathf.Clamp01(lerp + Time.deltaTime * 3);

        Vector3 northPole = robot.sigWeight.position + robot.sigWeight.transform.up * robot.sigWeight.planet.planetValues.radius;
        Vector3 northNeedle = Vector3.ProjectOnPlane(northPole - transform.position, transform.up).normalized;
        Vector3 localNeedle = transform.InverseTransformDirection(northNeedle);

        compass.SetFloat("_rotationRad", Mathf.Atan2(localNeedle.z, localNeedle.x) + Mathf.PI / 2);

        Debug.DrawLine(transform.position, northPole);
        Debug.DrawRay(transform.position, northNeedle, Color.red);
        Debug.DrawRay(transform.position, transform.forward, Color.blue);
    }
}
