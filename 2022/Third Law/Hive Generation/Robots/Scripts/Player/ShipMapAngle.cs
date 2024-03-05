using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipMapAngle : MonoBehaviour
{
    [SerializeField] private ShipWeight shipWeight;
    private Material starMap;

    // Start is called before the first frame update
    void Start()
    {
        starMap = GetComponent<MeshRenderer>().sharedMaterial;
    }

    //Ship map should always rotate to the direction the ship is facing
    void Update()
    {
        Vector3 forward = Vector3.ProjectOnPlane(shipWeight.transform.forward, Vector3.up);

        starMap.SetFloat("_rotationRad", Mathf.Atan2(forward.z, forward.x));
    }
}
