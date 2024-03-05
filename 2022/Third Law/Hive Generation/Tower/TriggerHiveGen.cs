using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerHiveGen : MonoBehaviour
{
    [SerializeField] private ShipDoorOpen trigger;
    [SerializeField] private GameObject hivePrefab;
    private bool comeFromHive = false;

    void Start()
    {
        CameraState.inHive = false;
    }

    void LateUpdate()
    {
        //SetPlanetActive(!CameraState.inHive);

        if (CameraState.inHive)
            return;

        //If the player is on the planet and the enterence door is closed
        if (!comeFromHive && trigger.lerp < 0)
        {
            CameraState.inHive = true;

            Instantiate(hivePrefab, transform.GetChild(3).position, transform.rotation, transform.root.GetChild(6));
            
            comeFromHive = true;
        }
        //If the player was in the hive but isn't now and hive has been generated
        else if (comeFromHive && transform.root.GetChild(6).childCount > 0)
        {
            Destroy(transform.root.GetChild(6).GetChild(0).gameObject);
        }
        //If the player was in the hive but isn't now and the enterence door is open
        else if (comeFromHive && trigger.lerp > 0)
            comeFromHive = false;
    }
}
