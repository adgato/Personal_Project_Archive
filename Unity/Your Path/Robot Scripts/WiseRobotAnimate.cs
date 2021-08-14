using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WiseRobotAnimate : MonoBehaviour
{
    //Yes I know you can handle animations without writing a script in unity, although I don't know much more than that... for now this seems the easier way

    public GameObject cam;
    public GameObject fire;

    private SitDown state;
    private Transform target;
    private int untilNext;
    private int dir = 1;

    private void Start()
    {
        state = cam.GetComponent<SitDown>();
        target = fire.transform;
        untilNext = Random.Range(1000, 3000);
    }

    // Update is called once per frame
    void Update()
    {
        if (!state.seated)
        {
            target = fire.transform;
        }
        else if (Time.frameCount % untilNext == 0)
        {
            untilNext = Random.Range(1000, 3000);
            
            target = (target == fire.transform) ? cam.transform : fire.transform;
        }
        //x_offset 125 when looking at camera is too creepy for my liking, I want wise robot to be kind, and 110 makes the robot look angrily at the fire
        int x_offset = (target == fire.transform) ? 125 : 110;

        //initially I did a look_left rotation as well but it makes the robot look a little cross-eyed
        Quaternion look_right = Quaternion.LookRotation((target.position - transform.Find("Chest Joint/Chest/Head/Right Eyesocket").position).normalized);

        transform.Find("Chest Joint/Chest/Head/Left Eyesocket").localRotation = Quaternion.Euler(look_right.eulerAngles.x - x_offset, look_right.eulerAngles.y - 10, look_right.eulerAngles.z);
        transform.Find("Chest Joint/Chest/Head/Right Eyesocket").localRotation = Quaternion.Euler(look_right.eulerAngles.x - x_offset, look_right.eulerAngles.y - 10, look_right.eulerAngles.z);

        if (transform.Find("Chest Joint/Chest/Head").eulerAngles.x < 325)
        {
            dir = 1;
        }
        else if (transform.Find("Chest Joint/Chest/Head").eulerAngles.x > 330)
        {
            dir = -1;
        }
        transform.Find("Chest Joint/Chest/Head").Rotate(dir * 2 * Time.deltaTime, 0, 0);

        //for fun I will leave a potential bug where the robot's eyes might visibly dislocate themselves if another seat spawns near enough to the robot the player can see it sat down
    }
}
