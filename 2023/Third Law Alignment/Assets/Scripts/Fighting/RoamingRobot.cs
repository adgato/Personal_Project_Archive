using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoamingRobot : RobotBody
{

    private RoamingCamera umpire;
    private RobotWeight robotWeight;
    private Vector3 roamingRobotGravity;
    [SerializeField] private float rotFwdLerpSpeed;
    [SerializeField] private float stopStrength = 0.02f;
    [Range(0, 1)]
    [SerializeField] private float stopInputDot = 0.9f;

    private float side;

    Vector3 unsafeInputDirection;

    public void Init(RobotWeight robotWeight)
    {
        this.robotWeight = robotWeight;
        umpire = robotWeight.mainCamera;
        Init();
    }

    protected override void UpdateFightingInputDirection()
    {
        normalUp = robotWeight.NormalUp;

        float forward = Controller.GetAxis(Controller.Inputs.LV, 0.5f);
        float right = Controller.GetAxis(Controller.Inputs.LH, 0.5f);

        if (umpire.UseCameraDirection || arms.Shielding())
        {
            Vector3 worldDir = Vector3.ProjectOnPlane(umpire.Right * right + umpire.Forward * forward, normalUp).normalized * new Vector2(right, forward).magnitude;
            inputDirection = transform.InverseTransformDirection(worldDir);
        }
        else
            inputDirection = new Vector3(forward * -side, 0, right * side);


        if (!IsGrounded())
            unsafeInputDirection = Vector3.zero;
        else if (robotWeight.HorizontalUnsafeDisplacement.sqrMagnitude > stopStrength)
        {
            unsafeInputDirection = inputDirection;
            inputDirection = Vector3.zero;
        }
        else if (Vector3.Dot(unsafeInputDirection, inputDirection) > stopInputDot)
            inputDirection = Vector3.zero;
        else
            unsafeInputDirection = Vector3.zero;
    }

    public override void RotateTowardsOpponent(float maxDegreesDelta)
    {


        if (OpponentNull)
        {
            if (arms.Shielding())
            {
                umpire.SetEulerAngles(transform.localEulerAngles);
                return;
            }

            Quaternion targetRot = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(inputDirection == Vector3.zero ? transform.forward : transform.TransformDirection(inputDirection), normalUp), maxDegreesDelta);
            transform.rotation = Quaternion.Lerp(rotation, targetRot, rotFwdLerpSpeed * dt);
        }
        else
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(Vector3.ProjectOnPlane(Opponent.position - position, normalUp), normalUp), maxDegreesDelta);
            //only change the side when the robot tries to rotate, this avoids the issue of the side changeing as the camera catches up with a projectile robot
            side = Mathf.Sign(Vector3.Dot(umpire.transform.right, transform.forward));

        }
    }

    public override bool IsGrounded() => robotWeight.IsGrounded;

    public override Vector3 GetGravitationalAcceleration() => roamingRobotGravity;
    public void SetGravitationalAcceleration(Vector3 acceleration)
    {
        roamingRobotGravity = acceleration;
    }
}
