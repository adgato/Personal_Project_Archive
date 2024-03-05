using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerRobotWeight : RobotWeight
{
    public static PlayerRobotWeight Player { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        if (Player != null)
            Debug.LogError("Error: only one Player is allowed per scene");
        Player = this;
    }

    public override void PostUpdate()
    {
        base.PostUpdate();
        mainCamera.UpdateCamera();
    }
}
