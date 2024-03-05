using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Rigidbody spider;
    [SerializeField] private float orbitRadius;
    [SerializeField] private Vector2 positionOffset;
    [SerializeField] private Vector2 pitchEulerAngleBounds;
    [SerializeField] private Vector2 additionalPitchRotation;

    [SerializeField] private Vector2 rotationSpeed;

    [Tooltip("X = horizontal, Y = vertical and rotational, Z = in/out")]
    [SerializeField] private Vector3 lerpSpeed;
    [Range(0, 1)]
    [SerializeField] private float horizontalCatchup;
    [Range(0, 1)]
    [SerializeField] private float normalUpCatchup;


    private Vector3 discCentre;

    private float currentOrbitRadius;
    private Vector3 localEulerAngles;
    private Vector3 localPosition;

    public Vector3 Forward { get; private set; }
    private Vector3 Up;
    public Vector3 Right { get; private set; }


    private Vector3 prevRobotPos;

    private Vector3 prevHorizontalMovement;

    public Camera mainCamera { get; private set; }

    private Vector3 last_position;
    private Quaternion last_rotation;
    private Vector3 position;
    private Quaternion rotation;


    // Start is called before the first frame update
    void Start()
    {
        mainCamera = GetComponent<Camera>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        UpdateRoamingCamera();
    }

    private void LateUpdate()
    {
        float t = (Time.time - Time.fixedTime) / Time.fixedDeltaTime;
        transform.SetPositionAndRotation(Vector3.Lerp(last_position, position, t), Quaternion.Slerp(last_rotation, rotation, t));
    }

    public static float Remap(float a, float b, float minValue, float maxValue, float value) => Mathf.Lerp(a, b, Mathf.InverseLerp(minValue, maxValue, value));

    private void UpdateRoamingCamera()
    {
        UpdateLocalEulerAngles();

        Vector3 robotPos = spider.position;
        Vector3 camPos = position;

        last_position = position;
        last_rotation = rotation;

        Up = Vector3.Slerp(Up, spider.transform.up, normalUpCatchup);

        Vector3 normalUp = Up;

        currentOrbitRadius = Mathf.Lerp(currentOrbitRadius, orbitRadius, lerpSpeed.z * Time.deltaTime);

        localPosition = Quaternion.Euler(localEulerAngles) * Vector3.forward * -currentOrbitRadius;

        Vector3 direction = spider.transform.TransformDirection(-localPosition);

        Vector3 targetPos = spider.transform.TransformPoint(localPosition) + Right * positionOffset.x + normalUp * positionOffset.y;
        Quaternion targetRot = Quaternion.LookRotation(direction, Vector3.ProjectOnPlane(normalUp, direction)) *
            Quaternion.Euler(Remap(additionalPitchRotation.x, additionalPitchRotation.y, pitchEulerAngleBounds.x, pitchEulerAngleBounds.y, localEulerAngles.x), 0, 0);

        Vector3 prevWorldDirOnNormal = Vector3.ProjectOnPlane(camPos - prevRobotPos, normalUp);
        Vector3 worldDirOnNormal = Vector3.ProjectOnPlane(targetPos - robotPos, normalUp);

        Vector3 prevWorldDisOnNormal = Vector3.ProjectOnPlane(camPos - prevWorldDirOnNormal, normalUp);
        Vector3 worldDisOnNormal = Vector3.ProjectOnPlane(targetPos - worldDirOnNormal, normalUp);

        Vector3 horizontalMovement = Vector3.Lerp(prevHorizontalMovement,
            Vector3.LerpUnclamped(prevWorldDisOnNormal, worldDisOnNormal, Mathf.Lerp(horizontalCatchup, 2 - horizontalCatchup, 0)),
            lerpSpeed.x * Time.deltaTime);

        //slerp all the rotational movement around the camera
        //lerp between behind and ahead of the rest of the horizontal movement according to the actions the player is performing
        //lerp the rest of the movement (which is vertical) 
        targetPos = Vector3.Slerp(prevWorldDirOnNormal, worldDirOnNormal, lerpSpeed.y * Time.deltaTime) +
            horizontalMovement +
            Vector3.Lerp(camPos - prevWorldDirOnNormal - prevWorldDisOnNormal, targetPos - worldDirOnNormal - worldDisOnNormal, lerpSpeed.y * Time.deltaTime);
        targetRot = Quaternion.Slerp(rotation, targetRot, lerpSpeed.y * Time.deltaTime);

        Vector3 targetDir = (targetPos - robotPos).normalized;
        if (Physics.Raycast(spider.position, targetDir, out RaycastHit hitInfo, orbitRadius, LayerMask.NameToLayer("Mirror")))
        {
            currentOrbitRadius = Mathf.Min(currentOrbitRadius, hitInfo.distance - 1.25f * mainCamera.nearClipPlane / Mathf.Max(0.01f, Vector3.Dot(hitInfo.normal, -targetDir)));
            targetPos = robotPos + targetDir * currentOrbitRadius;
        }
        else if ((targetPos - robotPos).sqrMagnitude > 4 * orbitRadius * orbitRadius)
            targetPos = robotPos + targetDir * (2 * orbitRadius);

        prevRobotPos = robotPos;
        prevHorizontalMovement = horizontalMovement;

        //transform.SetPositionAndRotation(targetPos, targetRot);
        position = targetPos;
        rotation = targetRot;
    }

    public void ReflectIn(Vector3 normal, Vector3 point)
    {
        position -= 2 * Vector3.Project(position - point, normal);
        Vector3 reflectFwd = Vector3.Reflect(rotation * Vector3.forward, normal);
        rotation = Quaternion.LookRotation(reflectFwd, Vector3.ProjectOnPlane(rotation * Vector3.up, reflectFwd));
        prevHorizontalMovement = Vector3.Reflect(prevHorizontalMovement, normal);

        Quaternion R = spider.rotation * Quaternion.Euler(localEulerAngles);
        Vector3 reflect2 = Vector3.Reflect(R * Vector3.forward, normal);
        Quaternion R1 = Quaternion.LookRotation(reflect2, Vector3.ProjectOnPlane(R * Vector3.up, reflect2));
        localEulerAngles = (Quaternion.Inverse(spider.rotation) * R1).eulerAngles;
    }

    void UpdateLocalEulerAngles()
    {
        localEulerAngles.x = Mathf.Clamp(localEulerAngles.x - Input.GetAxis("Mouse Y") * rotationSpeed.x * Time.deltaTime, pitchEulerAngleBounds.x, pitchEulerAngleBounds.y);
        localEulerAngles.y += Input.GetAxis("Mouse X") * SpiderMovement.inReflectedWorld * rotationSpeed.y * Time.deltaTime;
        localEulerAngles.y %= 360;
    }
}
