using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipWeight : ZeroWeight
{
    private Vector3 normalUp = Vector3.up;
    private ShipRouter shipRouter;
    [SerializeField] private FlipSwitch wingOpen;
    [SerializeField] private Transform[] wingIndicators;
    private Vector3[] wingIndicatorsStartLocalPos;
    private Vector3[] wingIndicatorsEndLocalPos;

    public static bool xRotatesYaw;
    private float wingSwapLerp;
    [SerializeField] private float wingSwapLerpTime = 0.2f;
    float wingSwapCooldown;

    private float legCastTimer;
    private bool normalContacted;

    [SerializeField] private float lookSpeed = 2;

    private Quaternion alignRot;

    private float xRotation = 0;
    private float yRotation = 0;
    private float zRotation = 0;

    [SerializeField] private float thrustSpeed = 0.1f;
    public float maxVelocity;
    public float maxHyperVelocity;
    private float hyperLerp;
    public static bool hyperOn;

    public bool onHelipad;

    [SerializeField] private RobotWeight robotWeight;

    public override void Start()
    {
        alignRot = transform.rotation;
        shipRouter = transform.GetChild(0).GetComponent<ShipRouter>();
        isShip = true;
        legCastTimer = 0;

        wingSwapLerp = 1;
        xRotatesYaw = true;
        wingIndicatorsEndLocalPos = new Vector3[2] { wingIndicators[0].localPosition, wingIndicators[1].localPosition };
        wingIndicatorsStartLocalPos = new Vector3[2] { wingIndicators[0].localPosition - Vector3.right, wingIndicators[1].localPosition - Vector3.right };
        wingOpen.overRide = true;
        wingSwapCooldown = 0;

        base.Start();
    }
    private void Update()
    {
        wingSwapCooldown += Time.deltaTime;

        if (InventoryUI.shipEngineOn01 <= 0)
            return;

        if (CameraState.flyingShip && Input.GetKeyDown(KeyCode.R) && wingSwapCooldown > 1)
        {
            wingOpen.overRide = true;
            wingSwapLerp = 0;
            wingSwapCooldown = 0;
            xRotatesYaw ^= true;
            (wingIndicatorsStartLocalPos, wingIndicatorsEndLocalPos) = (wingIndicatorsEndLocalPos, wingIndicatorsStartLocalPos);
        }
        else if (!CameraState.flyingShip && wingOpen.switchState == FlipSwitch.State.bottom != xRotatesYaw)
        {
            wingSwapLerp = 0;
            wingSwapCooldown = 0;
            xRotatesYaw ^= true;
            (wingIndicatorsStartLocalPos, wingIndicatorsEndLocalPos) = (wingIndicatorsEndLocalPos, wingIndicatorsStartLocalPos);
        }
        if (Input.GetKeyDown(KeyCode.X))
            hyperOn ^= true;
        //Turn hyper mode off for when player returns to ship to launch
        if (!CameraState.flyingShip && sigWeight != null)
            hyperOn = false;
    }
    public override Vector3 MoveRelative(Vector3 displacement)
    {
        //If player is exploring hive or ship is grounded and player is exploring planet and is not on the helipad, don't move as this will result in clipping when the terrain collier is unloaded
        if (sigWeight != null && !CameraState.inShip && (CameraState.inHive || normalContacted && !onHelipad))
        {
            return Vector3.zero;
        }
        //Will be set to true by Helipad.cs if on helipad between MoveRelative calls
        onHelipad = false;

        transform.GetChild(1).gameObject.SetActive(sigWeight == null);

        //Cast legs when continuously normal contacted / grounded for a second
        float newTime = Mathf.Clamp01(legCastTimer + Time.fixedDeltaTime * (normalContacted ? 1 : -3));
        if (newTime == 1 && legCastTimer != 1)
            shipRouter.CastLegs();
        else if (newTime == 0 && legCastTimer != 0)
            shipRouter.ResetLegs();

        legCastTimer = newTime;

        if (!CameraState.flyingShip)
        {
            //Velocity streaks set to default rotation as otherwise they are visible inside the ship
            transform.GetChild(1).localRotation = Quaternion.Euler(90, 0, 0);

            RotateToGround();

            return base.MoveRelative(displacement);
        }
        //Rotate velocity streaks to direction of motion
        transform.GetChild(1).rotation = Quaternion.LookRotation(velocity) * Quaternion.Euler(90, 0, 0);

        hyperLerp = Mathf.Clamp01(hyperLerp + Time.fixedDeltaTime * (hyperOn ? 1 : -1));

        wingSwapLerp = Mathf.Clamp01(wingSwapLerp + Time.fixedDeltaTime / wingSwapLerpTime);

        wingIndicators[0].localPosition = Vector3.Lerp(wingIndicatorsStartLocalPos[0], wingIndicatorsEndLocalPos[0], wingSwapLerp);
        wingIndicators[1].localPosition = Vector3.Lerp(wingIndicatorsStartLocalPos[1], wingIndicatorsEndLocalPos[1], wingSwapLerp);

        if (!normalContacted)
        {
            //Start to ignore the planet's normal up
            normalUp = Vector3.RotateTowards(normalUp, transform.up, 6 * Mathf.Deg2Rad, 1);
        }
        if (normalContacted)
            RotateToGround();
        else if (!hyperOn)
            Rotate(GetRotationCommand());
        else if (hyperOn)
        {
            xRotation = 0;
            yRotation = 0;
            zRotation = 0;
        }

        velocity += GetMoveCommand();

        float maxCurrentVelocity = Mathf.Lerp(maxVelocity, maxHyperVelocity, hyperLerp);
        if (velocity.sqrMagnitude > maxCurrentVelocity * maxCurrentVelocity)
            velocity = velocity.normalized * maxCurrentVelocity;

        return base.MoveRelative(displacement);
    }
    public void ApplyNormal(Vector3 normalDir)
    {
        //Normals applied are filtered so that increasingly only those closer to the normalUp are selected over 1 second
        //The normalUp is then smoothly slerped towards the normal applied by 0% at t = 0, 1 and 10% at t = 0.5
        if (Vector3.Dot(normalUp, normalDir) > legCastTimer)
            normalUp = Vector3.Slerp(normalUp, normalDir, 0.1f * Mathf.Sin(legCastTimer * Mathf.PI));

        //Bounce the ship back if do not land near ship legs
        if ((sigWeight == null || legCastTimer < 1) && Vector3.Dot(transform.up, normalDir) < 0.5f)
            velocity += normalDir * 10;

        normalContacted = true;
    }

    private Vector3 GetMoveCommand()
    {
        Vector3 direction = transform.forward * Input.GetAxis("Vertical") + transform.right * Input.GetAxis("Horizontal") + transform.up * Input.GetAxis("Liftoff");

        bool matching = Input.GetKey(KeyCode.Space) && InventoryUI.shipTargetX != null;
        if (matching)
            direction += (InventoryUI.shipTargetX.velocity - velocity).normalized;

        return direction.normalized * thrustSpeed / Time.fixedDeltaTime * Mathf.Lerp(matching ? 2 : 1, 25, hyperLerp) * InventoryUI.shipEngineOn01;
    }

    private Quaternion GetRotationCommand()
    {
        if (InventoryUI.shipEngineOn01 > 0)
        {
            xRotation -= Mathf.Clamp(Input.GetAxis("Mouse Y"), -1, 1) * lookSpeed;
            if (xRotatesYaw)
                yRotation += Mathf.Clamp(Input.GetAxis("Mouse X"), -1, 1) * lookSpeed;
            else
                zRotation -= Mathf.Clamp(Input.GetAxis("Mouse X"), -1, 1) * lookSpeed;
        }

        xRotation = Mathf.Lerp(xRotation, 0, Time.fixedDeltaTime * 0.5f);
        yRotation = Mathf.Lerp(yRotation, 0, Time.fixedDeltaTime * 0.5f);
        zRotation = Mathf.Lerp(zRotation, 0, Time.fixedDeltaTime * 0.5f);

        Quaternion rotation = rb.rotation * Quaternion.Euler(xRotation, yRotation, zRotation);

        Vector3 up = sigWeight == null ? transform.up : (position - sigWeight.position).normalized;
        //If on sun or in planet atmosphere
        if (sigWeight != null && (sigWeight.planet == null || (position - sigWeight.position).sqrMagnitude < Mathf.Pow(sigWeight.transform.GetChild(2).GetComponent<PlanetEffect>().atmosRadius, 2)))
        {
            //Align rotation with local planet normal
            alignRot = Quaternion.RotateTowards(alignRot, Quaternion.LookRotation(Vector3.ProjectOnPlane(transform.forward, up), up), 0.5f);
            rotation = Quaternion.RotateTowards(rotation, alignRot, Mathf.Pow(Quaternion.Angle(rotation, alignRot), 0.6f));
        }
        else
            alignRot = Quaternion.LookRotation(Vector3.ProjectOnPlane(transform.forward, up), up);

        return Quaternion.Slerp(rb.rotation, rotation, Time.fixedDeltaTime);
    }
    private void RotateToGround()
    {
        normalContacted = false;

        Quaternion target = Quaternion.LookRotation(Vector3.ProjectOnPlane(transform.forward, normalUp), normalUp);
        alignRot = Quaternion.RotateTowards(rb.rotation, target, 6);
        Rotate(alignRot);
    }

    private void Rotate(Quaternion rotation)
    {
        //So that the robot does not clip out of the ship when the ship rotates
        if (!CameraState.InLockState(CameraState.LockState.unlocked))
            robotWeight.RotateAsChild(position, rotation * Quaternion.Inverse(rb.rotation));
        rb.MoveRotation(rotation);
    }
}
