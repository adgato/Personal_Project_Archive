using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeFix : MonoBehaviour
{


    // Start is called before the first frame update
    void Start()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        meshFilter.mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 20);
    }


}
