using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class tempSun : MonoBehaviour
{
    public GameObject planet;

    // Update is called once per frame
    void Update()
    {
        transform.rotation = Quaternion.LookRotation(planet.transform.position - transform.position);
    }
}
