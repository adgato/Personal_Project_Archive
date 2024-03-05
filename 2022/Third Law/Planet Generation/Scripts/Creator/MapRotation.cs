using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapRotation : MonoBehaviour
{
    [SerializeField] Transform player;
    [SerializeField] Transform planet;
    [SerializeField] Transform globe;
    // Update is called once per frame
    void Update()
    {
        transform.position = globe.position + (player.position - planet.position).normalized * 4;
        transform.rotation = Quaternion.LookRotation(globe.position - transform.position) * Quaternion.Euler(0, 0, -player.GetComponent<tempCam>().yRot);
    }
}
