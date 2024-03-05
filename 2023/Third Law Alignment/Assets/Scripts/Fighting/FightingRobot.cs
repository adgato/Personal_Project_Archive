using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FightingRobot : RobotBody
{
    //private new Rigidbody rigidbody;

    [Min(0)]
    [SerializeField] private float gravity = 100;

    private Umpire umpire;


    private float side;

    public void Init(Umpire umpire)
    {
        this.umpire = umpire;
        Opponent = umpire.robot1 == this ? umpire.robot2 : umpire.robot1;

        //rigidbody = GetComponent<Rigidbody>();

        Init();
    }

    public void SetRLAgent(RLAgent agent)
    {
        if (IsHumanPlayer)
            Debug.LogWarning("Warning: this robot is a human player, why are you doing this?");
        AI.SetAgent(agent);
    }

    /// <summary>
    /// To be called in FightingCamera.FixedUpdate()
    /// </summary>
    public void UpdateRobot()
    {
        Vector3 displacement = GetFixedUpdateDisplacement();

        //collision detection
        if (position.y + displacement.y < 1.5f)
            displacement += new Vector3(0, 1.5f - position.y - displacement.y, 0);

        transform.position += displacement;
    }


    public override bool IsGrounded()
    {
        return position.y == 1.5f;
    }
    public override Vector3 GetGravitationalAcceleration()
    {
        return -normalUp * gravity;
    }

    protected override void UpdateFightingInputDirection()
    {
        float forward = Controller.GetAxis(Controller.Inputs.LH, 0.5f);
        float right = Controller.GetAxis(Controller.Inputs.LV, 0.5f);

        inputDirection = umpire.UseCameraDirection || arms.Shielding() ? 
            transform.InverseTransformDirection(umpire.transform.TransformDirection(new Vector3(forward, 0, right))) : new Vector3(right * -side, 0, forward * side);

    }

    public override void RotateTowardsOpponent(float maxDegreesDelta)
    {
        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(Vector3.ProjectOnPlane(Opponent.position - position, normalUp), normalUp), maxDegreesDelta);
        //only change the side when the robot tries to rotate, this avoids the issue of the side changeing as the camera catches up with a projectile robot
        side = Mathf.Sign(Vector3.Dot(umpire.transform.right, transform.forward));
    }
}
