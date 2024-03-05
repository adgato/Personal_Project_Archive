using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FightingAIController : AIController
{
    private RobotBody r;
    private RLAgent a;


    public void Start(RobotBody robot)
    {
        r = robot;
    }

    public void SetAgent(RLAgent agent)
    {
        a = agent;
    }


    public override void FixedUpdate()
    {
        base.FixedUpdate();

        //Construct input for agent here... probably only want to construct an input once every >5 frames, more than this is unnecessary

        //Pass input to agent here and get buttons to press...

        //Press appropriate buttons here...

        //For now:
        PlayRandomly();
    }

    private void PlayRandomly()
    {
        float perlinOffset = GetHashCode() * 100;
        bool canLaser = !r.OpponentNull && (r.position - r.Opponent.position).magnitude * 0.5f - 0.35f < r.arms.GetLaserRange();

        SetAxis(Controller.Inputs.LV, Mathf.Lerp(-1, 1, Mathf.PerlinNoise(Time.realtimeSinceStartup, 0.1f + perlinOffset)));
        SetAxis(Controller.Inputs.LH, Mathf.Lerp(-1, 1, Mathf.PerlinNoise(0.1f + perlinOffset, Time.realtimeSinceStartup)));
        SetButton(Controller.Inputs.RB, !canLaser && Mathf.PerlinNoise(1.1f + perlinOffset, Time.realtimeSinceStartup) > 0.5f);
        SetButton(Controller.Inputs.X, !canLaser && Mathf.PerlinNoise(2.1f + perlinOffset, Time.realtimeSinceStartup) > 0.5f);
        SetButton(Controller.Inputs.A, !canLaser && Mathf.PerlinNoise(3.1f + perlinOffset, Time.realtimeSinceStartup) > 0.5f);
        SetButton(Controller.Inputs.Y, !canLaser && Mathf.PerlinNoise(4.1f + perlinOffset, 2 * Time.realtimeSinceStartup) > 0.5f);
        SetButton(Controller.Inputs.LB, !canLaser && !r.OpponentNull && r.Opponent.arms.Attacking() && Mathf.PerlinNoise(5.1f + perlinOffset, Time.realtimeSinceStartup) > 0.5f);
        SetButton(Controller.Inputs.B, canLaser);
    }
}
