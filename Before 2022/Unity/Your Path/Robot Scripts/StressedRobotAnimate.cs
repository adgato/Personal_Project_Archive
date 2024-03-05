using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StressedRobotAnimate : MonoBehaviour
{
    //Yes I know you can handle animations without writing a script in unity, although I don't know much more than that... for now this seems the easier way

    private float dir = 60;

    // Update is called once per frame
    void Update()
    {
        if (transform.Find("Chest Joint/Chest/Head").eulerAngles.y < 231f)
        {
            dir += 1.5f;
        }
        else if (transform.Find("Chest Joint/Chest/Head").eulerAngles.y > 236f)
        {
            dir -= 1.5f;
        }
        dir = Mathf.Clamp(dir, -60, 60);
        transform.Find("Chest Joint/Chest/Head").Rotate(0, dir * 0.1f * Time.deltaTime, 0);
    }
}
