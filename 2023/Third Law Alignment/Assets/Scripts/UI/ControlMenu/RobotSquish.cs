using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//What even....
public class RobotSquish : MonoBehaviour
{
    [SerializeField] private float stiffness = 1;
    [SerializeField] private float inputAcceleration = 1;
    [SerializeField] private float damping = 1;
    private float equilibrium = 2;
    private float velocity = 0;

    // Update is called once per frame
    void Update()
    {
        float acceleration = stiffness * (equilibrium - transform.localScale.y) - velocity * damping - (ControlSaver.currentControls.AnyButtonDown() ? inputAcceleration : 0);

        velocity += acceleration * Time.deltaTime;
        transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y + velocity * Time.deltaTime, transform.localScale.z);
    }
}
