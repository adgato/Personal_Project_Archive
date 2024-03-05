using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ShipRouter : MonoBehaviour
{
    public GameObject Map;
    private Material globeMap;
    public Transform[] Legs;
    private float[] old_extensions;
    private float[] extensions;
    private float lerp = 0;

    public float shipFuelBurnRate;
    public float shipFuelRemaining;

    [SerializeField] private ShipDoorOpen outerDoor;
    public Vector3 centre = new Vector3(0, 0.3f, 0.8f);
    public Vector3 size = new Vector3(12, 5.6f, 14.5f);

    public void Start()
    {
        globeMap = Map.GetComponent<MeshRenderer>().sharedMaterial;
        old_extensions = new float[Legs.Length];
        extensions = new float[Legs.Length];
        for (int i = 0; i < extensions.Length; i++)
        {
            old_extensions[i] = 1;
            extensions[i] = 1;
        }

        Update();
    }
    public void Update()
    {
        shipFuelBurnRate = (InventoryUI.shipFuelRemaining - shipFuelRemaining) / Time.deltaTime;
        shipFuelRemaining = InventoryUI.shipFuelRemaining;

        globeMap.SetFloat("_engineOn", InventoryUI.shipEngineOn01);
        RecheckCameraInShip();
        UpdateLegs();
    }
    public bool RecheckCameraInShip()
    {
        Vector3 p = transform.InverseTransformDirection(Camera.main.transform.position - transform.position);
        Vector3 ceil = centre + size / 2;
        Vector3 floor = centre - size / 2;
        bool check = floor.x < p.x && p.x < ceil.x;
        if (check)
            check &= floor.y < p.y && p.y < ceil.y; //kinda the same as check &&=
        if (check)
            check &= floor.z < p.z && p.z < ceil.z;

        CameraState.withinShip = check;
        //In ship if within ship and (outer door is closed or the player has just died meaning the outer door may still be closing)
        CameraState.inShip = CameraState.withinShip && (outerDoor.lerp <= 0 || Time.realtimeSinceStartup < StarGenSystem.genTime + 5);
        
        return CameraState.inShip;
    }
    public void CastLegs()
    {
        lerp = 0;
    }

    public void ResetLegs()
    {
        if (extensions[0] == 1)
            return;

        //Set taget of legs to their withdrawn length
        for (int i = 0; i < Legs.Length; i++)
        {
            old_extensions[i] = 1;
            extensions[i] = 1;
        }
    }
    public void UpdateLegs()
    {
        int i = 0;
        if (lerp == 0)
        {
            foreach (Transform leg in Legs)
            {
                old_extensions[i] = extensions[i];
                if (Physics.Raycast(leg.position, -leg.transform.up, out RaycastHit hitInfo, 10, ~(1 << 11 | 1 << 12)))
                    extensions[i] = hitInfo.distance * 0.9f;
                i++;
            }
        }

        lerp += Time.deltaTime;
        i = 0;
        foreach (Transform leg in Legs)
        {
            leg.localScale = new Vector3(leg.localScale.x, Mathf.Lerp(old_extensions[i], extensions[i], lerp), leg.localScale.z);
            i++;
        }
    }
}
