using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShowWorld : MonoBehaviour
{
    public GameObject player;
    public int distance;

    // Update is called once per frame
    void FixedUpdate()
    {
        for (int q = 0; q < transform.childCount; q++) {
            for (int i = 0; i < transform.GetChild(q).childCount; i++)
            {
                transform.GetChild(q).GetChild(i).gameObject.SetActive(Vector3.Distance(player.transform.position, transform.GetChild(q).GetChild(i).position) < distance);
            }
        }
    }
}
