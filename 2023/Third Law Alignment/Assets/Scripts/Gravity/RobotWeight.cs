using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RobotWeight : ZeroWeight
{
    public RoamingRobot roamingRobot;
    public RoamingCamera mainCamera;

    private Vector3 roamingRobotDisplacement;

    public override void PreUpdate()
    {
        base.PreUpdate();

        transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(transform.forward, NormalUp), NormalUp);

        UpdateRoamingRobot();
    }


    public override Vector3 GetDisplacement()
    {
        return base.GetDisplacement() + roamingRobotDisplacement;
    }

    public void FightingAccelerate(Vector3 acceleration)
    {
        roamingRobot.SetGravitationalAcceleration(acceleration);
    }

    protected override void Start()
    {
        base.Start();
        roamingRobot.Init(this);
    }

    protected void Update()
    {
        if (ControlSaver.GamePaused || roamingRobot == null)
            return;
        roamingRobot.UpdateInputs();
    }

    private void LateUpdate()
    {
        if (ControlSaver.GamePaused)
            return;
        if (Closest != null && Closest.GetType() == typeof(PlanetWeight))
            ((PlanetWeight)Closest).PlanetData.PlanetFoilage.SetPlayerPosition(baseTransform.position);
    }

    private void UpdateRoamingRobot()
    {
        if (mainCamera.fixedFrames > 0)
            roamingRobotDisplacement = Vector3.zero;
        else
        {
            mainCamera.HandleHit(roamingRobot);
            roamingRobotDisplacement = roamingRobot.GetFixedUpdateDisplacement();
        }
    }
}