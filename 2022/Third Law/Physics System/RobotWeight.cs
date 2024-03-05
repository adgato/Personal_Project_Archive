using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class RobotWeight : ZeroWeight
{
    [SerializeField] private Transform Chest;

    [SerializeField] private float walkSpeed = 0.1f;
    [SerializeField] private float thrustSpeed = 0.1f;
    [SerializeField] private float defaultPowerSeconds = 0.2f;
    [Range(0, 5)]
    [SerializeField] private float jitterFix = 0;

    private float powerTimer = 0;

    public float forceMeter { get; set; }
    private float prevSqrSpeed;

    [SerializeField] private ShipWeight Ship;

    [SerializeField] private AudioSource walkSource;

    public void UpdateForceMeter()
    {
        float sqrSpeed = position.sqrMagnitude;
        forceMeter = Mathf.Abs(sqrSpeed - prevSqrSpeed);
        prevSqrSpeed = sqrSpeed;
    }

    public override Vector3 MoveRelative(Vector3 displacement)
    {
        walkSource.volume *= 0.9f;

        bool onPlanet = sigWeight != null;
        Vector3 normalUp = CameraState.inShip ? Ship.transform.up : onPlanet ? (position - sigWeight.position).normalized : transform.up;

        Quaternion target = Quaternion.LookRotation(Vector3.ProjectOnPlane(transform.forward, normalUp), normalUp);
        rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, target, 5));

        if (!CameraState.InLockState(CameraState.LockState.unlocked))
            return Vector3.zero;

        Vector3 moveBy = Vector3.zero;

        if (kinematicBody.isGrounded && onPlanet || CameraState.inShip)
        {
            moveBy = Vector3.ProjectOnPlane(Camera.main.transform.forward, Chest.up) * Input.GetAxis("Vertical") + Vector3.ProjectOnPlane(Camera.main.transform.right, Chest.up) * Input.GetAxis("Horizontal");

            if (moveBy.sqrMagnitude > 1)
                moveBy = moveBy.normalized;

            walkSource.volume = 0.02f * moveBy.sqrMagnitude;

            moveBy *= walkSpeed;
        }

        if (CameraState.inShip)
            displacement = -0.1f * normalUp;

        if (Input.GetButton("Jump"))
        {
            //Only allow the player to apply a jump force for defaultPowerSeconds
            if (powerTimer < defaultPowerSeconds)
            {
                //Maintain some lateral velocity during jump
                Vector3 lateral = Vector3.ProjectOnPlane(Camera.main.transform.forward, Chest.up) * Input.GetAxis("Vertical") + Vector3.ProjectOnPlane(Camera.main.transform.right, Chest.up) * Input.GetAxis("Horizontal");
                velocity += thrustSpeed * ((defaultPowerSeconds - powerTimer) / (Time.fixedDeltaTime * defaultPowerSeconds) * (normalUp + lateral.normalized * walkSpeed));
                powerTimer += Time.fixedDeltaTime;
            }
        }
        else if (kinematicBody.isGrounded)
        {
            powerTimer = Mathf.Max(0, powerTimer - Time.fixedDeltaTime);
            Vector3 intoGround = Vector3.Project(displacement + moveBy, normalUp);
            //Remove requested displacement into ground and apply smaller constant displacement down instead to fix the player jittering up and down
            if (intoGround.sqrMagnitude < jitterFix * jitterFix)
                moveBy += -normalUp * jitterFix - intoGround;
        }

        displacement += moveBy;

        return base.MoveRelative(displacement);
    }

    //Rotates the player the same way they would rotate were they a child of the parent that performed the rotation at their position
    public void RotateAsChild(Vector3 parentPosition, Quaternion rotation)
    {
        if (CameraState.inShip)
        {
            Vector3 normalUp = Ship.transform.up;

            Quaternion target = Quaternion.LookRotation(Vector3.ProjectOnPlane(transform.forward, normalUp), normalUp);
            rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, target, 5));

            Vector3 direction = position - parentPosition;
            Teleport(rotation * direction - direction);
        }
    }
}
