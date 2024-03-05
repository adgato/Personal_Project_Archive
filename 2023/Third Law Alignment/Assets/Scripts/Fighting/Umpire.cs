using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Umpire : MonoBehaviour
{

    public FightingRobot robot1;
    public FightingRobot robot2;

    [SerializeField] private float cameraAngularSpeed;
    [SerializeField] private float cameraZoomSpeed;
    [SerializeField] private float cameraLinearSpeed;
    [SerializeField] private float slowRotAtSeperation = 10;
    [SerializeField] private float stopRotAtSeperation = 1;
    [SerializeField] private float paddingFactor;
    [SerializeField] private float offsetHeight;
    [SerializeField] private float offsetZoom;

    private Camera mainCamera;
    private Transform cameraTransform;

    private Vector3 discCentre;

    public bool UseCameraDirection { get; private set; } = false;

    private int fixedFramesLeft;

    // Start is called before the first frame update
    public void Init()
    {
        cameraTransform = transform.parent;
        mainCamera = cameraTransform.GetComponent<Camera>();

        robot1.Init(this);
        robot2.Init(this);
    }

    public void UpdateInputs()
    {
        robot1.UpdateInputs();
        robot2.UpdateInputs();
    }

    public void UpdateGame()
    {
        if (fixedFramesLeft > 0)
        {
            fixedFramesLeft--;
            UpdateCamera();
            return;
        }

        //DebugLogHitsBetween(robot1, robot2);
        robot1.arms.HandleHit(CheckCollision(robot1, robot2), out int fixFrames1);
        robot2.arms.HandleHit(CheckCollision(robot2, robot1), out int fixFrames2);
        fixedFramesLeft = Mathf.Max(fixFrames1, fixFrames2);

        if (fixedFramesLeft > 0)
        {
            fixedFramesLeft--;
            UpdateCamera();
            return;
        }

        robot1.UpdateRobot();
        robot2.UpdateRobot();
        UpdateCamera();
    }

    void UpdateCamera()
    {
        Vector3 middle = Vector3.Lerp(robot1.position, robot2.position, 0.5f);
        Vector3 normalUp = Vector3.Slerp(robot1.transform.up, robot2.transform.up, 0.5f);

        Vector3 cameraDir = Vector3.Cross(Vector3.ProjectOnPlane(robot1.position - middle, normalUp), normalUp).normalized;

        float horizontalSeparation = Vector3.ProjectOnPlane(robot1.position - robot2.position, normalUp).magnitude;
        float verticalSeparation = Mathf.Sqrt(Mathf.Max(0, (robot1.position - robot2.position).sqrMagnitude - Mathx.Square(horizontalSeparation)));

        float horizontalOffset = offsetZoom - Mathf.Clamp(horizontalSeparation * 0.5f, 0, offsetZoom);
        float verticalOffset = offsetHeight - Mathf.Clamp(verticalSeparation * 0.5f, 0, offsetHeight);

        float cameraDist = Mathf.Max(
            horizontalOffset + paddingFactor * horizontalSeparation * 0.5f / (Mathf.Tan(mainCamera.fieldOfView * 0.5f * Mathf.Deg2Rad) * mainCamera.aspect), 
            verticalOffset + paddingFactor * verticalSeparation * 0.5f / Mathf.Tan(mainCamera.fieldOfView * 0.5f * Mathf.Deg2Rad)
            );

        float slerp = cameraAngularSpeed * Time.deltaTime * Mathf.InverseLerp(stopRotAtSeperation, slowRotAtSeperation, horizontalSeparation);

        if (discCentre == Vector3.zero)
            discCentre = normalUp * verticalOffset + middle;

        Vector3 discToCamera = cameraTransform.position - discCentre;

        discCentre = Vector3.Lerp(discCentre, normalUp * verticalOffset + middle, cameraLinearSpeed * Time.deltaTime);

        if (Vector3.Dot(cameraDir, discToCamera) < 0)
            cameraDir *= -1;

        UseCameraDirection = slerp == 0 || UseCameraDirection && Vector3.Angle(discToCamera, cameraDir) > 1;

        cameraTransform.SetPositionAndRotation(
            Vector3.Slerp(discToCamera.normalized, cameraDir, slerp) * Mathf.Lerp(discToCamera.magnitude, cameraDist + horizontalOffset, cameraZoomSpeed * Time.deltaTime) + discCentre,
            Quaternion.Slerp(cameraTransform.rotation, Quaternion.LookRotation(-cameraDir, normalUp), slerp)
            );

    }

    protected CollisionWith CheckCollision(FightingRobot predetor, FightingRobot prey)
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
