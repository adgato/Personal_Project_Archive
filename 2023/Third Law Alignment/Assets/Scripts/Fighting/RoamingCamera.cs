using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CollisionWith { Nothing, HurtBox, Shield }

public class RoamingCamera : MonoBehaviour
{
    private Camera mainCamera;
    private SmoothWorldTransform camTransform;
    [SerializeField] private float orbitRadius;
    [SerializeField] private Vector2 positionOffset;
    [SerializeField] private Vector2 pitchEulerAngleBounds;
    [SerializeField] private Vector2 additionalPitchRotation;

    [SerializeField] private Vector2 rotationSpeed;

    [Tooltip("X = horizontal, Y = vertical and rotational, Z = in/out")]
    [SerializeField] private Vector3 lerpSpeed;
    [Range(0, 1)]
    [SerializeField] private float horizontalCatchup;

    [Header("Fighting Parameters")]
    [SerializeField] private float cameraAngularSpeed;
    [SerializeField] private float cameraZoomSpeed;
    [SerializeField] private float cameraLinearSpeed;
    [SerializeField] private float slowRotAtSeperation = 10;
    [SerializeField] private float stopRotAtSeperation = 1;
    [SerializeField] private float paddingFactor;
    [SerializeField] private float offsetHeight;
    [SerializeField] private float offsetZoom;

    private Vector3 discCentre;

    private float currentOrbitRadius;
    private Vector3 localEulerAngles;
    private Vector3 localPosition;

    public bool UseCameraDirection { get; private set; } = false;

    public Vector3 Forward { get; private set; }
    public Vector3 Right { get; private set; }

    private Controller Controller = new Controller();

    private Vector3 prevRobotPos;

    private Vector3 prevHorizontalMovement;

    private float calmTimer = 0;

    public int fixedFrames { get; private set; }

    void Start()
    {
        mainCamera = transform.parent.GetComponent<Camera>();
        camTransform = mainCamera.GetComponent<SmoothWorldTransform>();
        Controller = Controller.Player;
    }

    public void HandleHit(RoamingRobot roamingRobot)
    {
        if (roamingRobot.OpponentNull)
            return;
        roamingRobot.arms.HandleHit(CheckCollision(roamingRobot, roamingRobot.Opponent), out int fixFrames);
        fixedFrames = Mathf.Max(fixedFrames, fixFrames);
    }

    public void UpdateCamera()
    {
        fixedFrames = Mathf.Max(0, fixedFrames - 1);

        if (PlayerRobotWeight.Player.roamingRobot.OpponentNull)
            UpdateRoamingCamera();
        else
            UpdateFightingCamera();
    }

    private void UpdateRoamingCamera()
    {
        UpdateLocalEulerAngles();

        Vector3 robotPos = PlayerRobotWeight.Player.Position;
        Vector3 camPos = camTransform.position;

        Vector3 normalUp = PlayerRobotWeight.Player.NormalUp;

        currentOrbitRadius = Mathf.Lerp(currentOrbitRadius, orbitRadius, lerpSpeed.z * Time.deltaTime);

        localPosition = Quaternion.Euler(localEulerAngles) * Vector3.forward * -currentOrbitRadius;

        Vector3 direction = PlayerRobotWeight.Player.transform.TransformDirection(-localPosition);

        Vector3 targetPos = PlayerRobotWeight.Player.transform.TransformPoint(localPosition) + Right * positionOffset.x + normalUp * positionOffset.y;
        Quaternion targetRot = Quaternion.LookRotation(direction, Vector3.ProjectOnPlane(normalUp, direction)) * 
            Quaternion.Euler(Mathx.Remap(additionalPitchRotation.x, additionalPitchRotation.y, pitchEulerAngleBounds.x, pitchEulerAngleBounds.y, localEulerAngles.x), 0, 0);

        Vector3 prevWorldDirOnNormal = Vector3.ProjectOnPlane(camPos - prevRobotPos, normalUp);
        Vector3 worldDirOnNormal = Vector3.ProjectOnPlane(targetPos - robotPos, normalUp);

        Vector3 prevWorldDisOnNormal = Vector3.ProjectOnPlane(camPos - prevWorldDirOnNormal, normalUp);
        Vector3 worldDisOnNormal = Vector3.ProjectOnPlane(targetPos - worldDirOnNormal, normalUp);

        Vector3 horizontalMovement = Vector3.Lerp(prevHorizontalMovement, 
            Vector3.LerpUnclamped(prevWorldDisOnNormal, worldDisOnNormal, Mathf.Lerp(horizontalCatchup, 2 - horizontalCatchup, UpdateTimeRobotMovingSlow(PlayerRobotWeight.Player.roamingRobot))), 
            lerpSpeed.x * Time.deltaTime);

        //slerp all the rotational movement around the camera
        //lerp between behind and ahead of the rest of the horizontal movement according to the actions the player is performing
        //lerp the rest of the movement (which is vertical) 
        targetPos = Vector3.Slerp(prevWorldDirOnNormal, worldDirOnNormal, lerpSpeed.y * Time.deltaTime) +
            horizontalMovement +
            Vector3.Lerp(camPos - prevWorldDirOnNormal - prevWorldDisOnNormal, targetPos - worldDirOnNormal - worldDisOnNormal, lerpSpeed.y * Time.deltaTime);
        targetRot = Quaternion.Slerp(camTransform.rotation, targetRot, lerpSpeed.y * Time.deltaTime);

        Vector3 targetDir = (targetPos - robotPos).normalized;
        if (Physics.Raycast(PlayerRobotWeight.Player.Position, targetDir, out RaycastHit hitInfo, orbitRadius, LayerMask.NameToLayer("ZeroWeight")))
        {
            currentOrbitRadius = Mathf.Min(currentOrbitRadius, hitInfo.distance - 1.25f * mainCamera.nearClipPlane / Mathf.Max(0.01f, Vector3.Dot(hitInfo.normal, -targetDir)));
            targetPos = robotPos + targetDir * currentOrbitRadius;
        } 
        else if ((targetPos - robotPos).sqrMagnitude > Mathx.Square(2 * orbitRadius))
            targetPos = robotPos + targetDir * (2 * orbitRadius);

        UseCameraDirection = true;

        Forward = Vector3.ProjectOnPlane(targetRot * Vector3.forward, normalUp).normalized;
        Right = Vector3.ProjectOnPlane(targetRot * Vector3.right, normalUp).normalized;

        prevRobotPos = robotPos;
        prevHorizontalMovement = horizontalMovement;

        camTransform.position = targetPos;
        camTransform.rotation = targetRot;
    }

    private void UpdateFightingCamera()
    {
        RobotBody robot1 = PlayerRobotWeight.Player.roamingRobot;
        RobotBody robot2 = PlayerRobotWeight.Player.roamingRobot.Opponent;

        Vector3 middle = Vector3.Lerp(robot1.position, robot2.position, 0.5f);
        Vector3 normalUp = Vector3.Slerp(robot1.transform.up, robot2.transform.up, 0.5f);

        Vector3 cameraDir = Vector3.Cross(Vector3.ProjectOnPlane(robot1.position - middle, normalUp), normalUp).normalized;

        float horizontalSeparation = Vector3.Cross(robot1.position - robot2.position, normalUp).magnitude;
        float verticalSeparation = Mathf.Abs(Vector3.Dot(robot1.position - robot2.position, normalUp));

        float horizontalOffset = offsetZoom - Mathf.Clamp(horizontalSeparation * 0.5f, 0, offsetZoom);
        float verticalOffset = offsetHeight - Mathf.Clamp(verticalSeparation * 0.5f, 0, offsetHeight);

        float cameraDist = Mathf.Max(
            horizontalOffset + paddingFactor * horizontalSeparation * 0.5f / (Mathf.Tan(mainCamera.fieldOfView * 0.5f * Mathf.Deg2Rad) * mainCamera.aspect),
            verticalOffset + paddingFactor * verticalSeparation * 0.5f / Mathf.Tan(mainCamera.fieldOfView * 0.5f * Mathf.Deg2Rad)
            );

        float slerp = cameraAngularSpeed * Time.deltaTime * Mathf.InverseLerp(stopRotAtSeperation, slowRotAtSeperation, horizontalSeparation);

        if (discCentre == Vector3.zero)
            discCentre = normalUp * verticalOffset + middle;

        Vector3 discToCamera = camTransform.position - discCentre;

        discCentre = Vector3.Lerp(discCentre, normalUp * verticalOffset + middle, cameraLinearSpeed * Time.deltaTime);


        if (Vector3.Dot(cameraDir, discToCamera) < 0)
            cameraDir *= -1;

        UseCameraDirection = slerp == 0 || UseCameraDirection && Vector3.Angle(discToCamera, cameraDir) > 1;

        Quaternion targetRot = Quaternion.LookRotation(-cameraDir, normalUp);

        Forward = Vector3.ProjectOnPlane(targetRot * Vector3.forward, normalUp).normalized;
        Right = Vector3.ProjectOnPlane(targetRot * Vector3.right, normalUp).normalized;

        prevRobotPos = PlayerRobotWeight.Player.Position;

        camTransform.position = Vector3.Slerp(discToCamera.normalized, cameraDir, slerp) * Mathf.Lerp(discToCamera.magnitude, cameraDist + horizontalOffset, cameraZoomSpeed * Time.deltaTime) + discCentre;
        camTransform.rotation = Quaternion.Slerp(camTransform.rotation, targetRot, slerp);
    }

    private float UpdateTimeRobotMovingSlow(RoamingRobot robot)
    {
        if (robot.arms.StanceGrounded() && !(robot.arms.Dashing() || robot.arms.Attacking()))
            calmTimer += Time.deltaTime;
        else
            calmTimer -= Time.deltaTime;
        calmTimer = Mathf.Clamp01(calmTimer);
        return calmTimer;
    }

    void UpdateLocalEulerAngles()
    {
        localEulerAngles.x = Mathf.Clamp(localEulerAngles.x - Controller.GetAxis(Controller.Inputs.RV, 0.5f) * rotationSpeed.x * Time.deltaTime, pitchEulerAngleBounds.x, pitchEulerAngleBounds.y);
        localEulerAngles.y += Controller.GetAxis(Controller.Inputs.RH, 0.5f) * rotationSpeed.y * Time.deltaTime;
        localEulerAngles.y %= 360;
    }
    public void SetEulerAngles(Vector3 eulerAngles)
    {
        localEulerAngles = eulerAngles;
    }

    private CollisionWith CheckCollision(RobotBody predetor, RobotBody prey)
    {
        CollisionWith collisionDetected = CollisionWith.Nothing;

        foreach (Collider hitBox in predetor.GetHitBoxes())
        {
            if (CollidesWith(hitBox, prey.arms.shieldHitBox))
                return CollisionWith.Shield;

            if (collisionDetected == CollisionWith.HurtBox)
                continue;

            foreach (Collider hurtBox in prey.GetHurtBoxes())
            {
                if (CollidesWith(hitBox, hurtBox))
                {
                    collisionDetected = CollisionWith.HurtBox;
                    break;
                }
            }
        }
        return collisionDetected;
    }

    public static bool CollidesWith(Collider A, Collider B)
    {
        return A.enabled && B.enabled && Physics.ComputePenetration(A, A.transform.position, A.transform.rotation, B, B.transform.position, B.transform.rotation, out _, out _);
    }
}
