using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlobeRotation : MonoBehaviour
{
    [SerializeField] Transform player;
    [SerializeField] Transform planet;

    // Update is called once per frame
    void Update()
    {
        transform.rotation = Quaternion.Euler(Quaternion.LookRotation(player.position - planet.position).eulerAngles - Vector3.up * 90);
    }
}
