using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AfraidRobotAnimate : MonoBehaviour
{
    //Yes I know you can handle animations without writing a script in unity, although I don't know much more than that... for now this seems the easier way

    private int untilNext;
    private int dir = 1;

    private void Start()
    {
        untilNext = Random.Range(1000, 3000);
    }

    // Update is called once per frame
    void Update()
    {
        if (Time.frameCount % untilNext == 0)
        {
            untilNext = Random.Range(1000, 3000);
            float yRotation = Random.Range(-20f, 20f);
            float xRotation = Random.Range(80f, 105f);
            transform.Find("Chest Joint/Chest/Head/Left Eyesocket").localRotation = Quaternion.Euler(xRotation, yRotation, 0);
            transform.Find("Chest Joint/Chest/Head/Right Eyesocket").localRotation = Quaternion.Euler(xRotation, yRotation, 0);
        }

        if (transform.Find("Chest Joint/Chest/Head").eulerAngles.x < 325)
        {
            dir = 1;
        }
        else if (transform.Find("Chest Joint/Chest/Head").eulerAngles.x > 330)
        {
            dir = -1;
        }
        transform.Find("Chest Joint/Chest/Head").Rotate(dir * 2 * Time.deltaTime, 0, 0);


    }
}
