using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Helipad : MonoBehaviour
{
    private ShipWeight shipWeight;

    // Start is called before the first frame update
    void Start()
    {
        shipWeight = FindObjectOfType<ShipWeight>();
    }

    // Update is called once per frame
    void Update()
    {
        //If this class has been instantiated then the ship is either in the air or on the helipad
        //If this is the case we want the ship to perform collision detection even if the player is not in the ship as the evilBase lift may move down beneath the ship
        shipWeight.onHelipad = true;
    }
}
