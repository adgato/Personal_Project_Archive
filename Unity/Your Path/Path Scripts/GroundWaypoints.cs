using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GroundWaypoints : MonoBehaviour
{
    public bool autoUpdate;
    public float globalOffsetHeight;

    public void Ground()
    {
        int n_waypoints = transform.childCount;

        for (int i = 0; i < n_waypoints; i++)
        {
            Physics.Raycast(new Vector3(transform.GetChild(i).position.x, 100, transform.GetChild(i).position.z), -Vector3.up, out RaycastHit hit);

            transform.GetChild(i).position = new Vector3(
                transform.GetChild(i).position.x, 
                100 - hit.distance + globalOffsetHeight + transform.GetChild(i).gameObject.GetComponent<PointOffset>().offsetHeight, 
                transform.GetChild(i).position.z);
        }
    }
    private void Start()
    {
        int n_waypoints = transform.childCount;

        for (int i = 0; i < n_waypoints; i++)
        {
            transform.GetChild(i).gameObject.SetActive(false);
        }
    }
}
