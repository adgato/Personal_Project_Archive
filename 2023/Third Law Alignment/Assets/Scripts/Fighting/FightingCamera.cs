using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FightingCamera : Umpire
{
    // Start is called before the first frame update
    void Start()
    {
        Init();
    }

    // Update is called once per frame
    void Update()
    {
        if (ControlSaver.GamePaused)
            return;
        UpdateInputs();
    }

    private void FixedUpdate()
    {
        if (ControlSaver.GamePaused)
            return;
        UpdateGame();
    }


    /// <summary>
    /// NOT OPTIMISED
    /// </summary>
    void DebugLogHitsBetween(FightingRobot A, FightingRobot B)
    {
        foreach ((FightingRobot c, FightingRobot d) in new (FightingRobot, FightingRobot)[] { (A, B), (B, A) })
        {
            CollisionWith collisionWith = CheckCollision(c, d);
            if (collisionWith == CollisionWith.HurtBox)
                Debug.Log(c.name + " hit " + d.name);
            else if (collisionWith == CollisionWith.Shield)
                Debug.Log(d.name + " blocked " + c.name);
        }
    }
}
