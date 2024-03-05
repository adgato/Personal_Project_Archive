using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(Rigidbody))]
public class SpiderMovement : MonoBehaviour
{
    enum State
    {
        Locked,
        Running,
        Jumping,
        Falling,
        Webbing,
        Landing
    }

    [SerializeField] private CollisionDetection collisionDetection = new CollisionDetection();
    
    [SerializeField] private float stickGravity;
    [SerializeField] private float jumpForce;
    [SerializeField] private float moveAcceleration;
    [SerializeField] private float webAcceleration;
    [SerializeField] private float moveDrag;
    [SerializeField] private float physicsDrag;
    [SerializeField] private float maxMoveSpeed;

    [SerializeField] private CameraController cameraController;
    [SerializeField] private Transform spiderBody;
    [SerializeField] private Transform spiderWeb;
    [SerializeField] private SpiderLegs spiderLegs;
    [SerializeField] private float bodyRotateSpeed;
    private Rigidbody rb;
    [SerializeField] [ReadOnly] private Vector3 velocity;
    [SerializeField] [ReadOnly] private Vector3 moveVelocity;
    [SerializeField] private State physicsState;
    [SerializeField] [ReadOnly] private Vector3 prevAxisInput;
    [SerializeField] [ReadOnly] private Vector3 axisInput;
    [SerializeField] private float crouchAmount;

    [SerializeField] private Material flipV;


    /// <summary>
    /// oh how i love game jams and not caring about security!
    /// </summary>
    public static int inReflectedWorld = 1;
    public static int flipVfix = 1;

    private Vector3 webStart;
    private Vector3 webEnd;
    private float webTime01;

    private float crouch01 = 0;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        collisionDetection.Init(rb);
        flipV.SetFloat("_FlipH", inReflectedWorld);
        flipV.SetFloat("_FlipV", flipVfix);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.V))
        {
            flipVfix *= -1;
            flipV.SetFloat("_FlipV", flipVfix);
        }
    }

    private void ReflectIn(Vector3 normal, Vector3 point)
    {
        ResetReflect();


        cameraController.ReflectIn(normal, point);
        if (physicsState != State.Falling)
        {
            physicsState = State.Running;
            spiderWeb.gameObject.SetActive(false);
        }
        Vector3 reflectFwd = Vector3.Reflect(spiderBody.rotation * Vector3.forward, normal);

        spiderBody.rotation = Quaternion.LookRotation(reflectFwd, Vector3.ProjectOnPlane(spiderBody.rotation * Vector3.up, reflectFwd));

        velocity = Vector3.Reflect(velocity, normal);
        moveVelocity = Vector3.Reflect(moveVelocity, normal);
        collisionDetection.hitMirror = false;
    }

    public void ResetReflect()
    {
        inReflectedWorld *= -1;
        flipV.SetFloat("_FlipH", inReflectedWorld);
    }

    private void FixedUpdate()
    {
        UpdateGravity();
        UpdateWeb();
        UpdateMove();


        if (physicsState == State.Landing)
        {
            crouch01 -= Time.deltaTime * 6;
            crouch01 = Mathf.Clamp01(crouch01);
            if (crouch01 == 0)
                physicsState = State.Running;
        }
        else if (Input.GetKey(KeyCode.Space) && physicsState == State.Running)
        {
            crouch01 += Time.deltaTime * 6;
            crouch01 = Mathf.Clamp01(crouch01);
        }
        else if (crouch01 > 0 && !Input.GetKey(KeyCode.Space) && physicsState == State.Running)
        {
            physicsState = State.Jumping;
            velocity += jumpForce * Mathf.Max(0.25f, crouch01) * transform.up;
        }
        else if (collisionDetection.isGrounded && physicsState == State.Falling)
        {
            physicsState = State.Landing;
            crouch01 = 1;
        }
        else
            crouch01 = 0;

        spiderBody.localPosition = new Vector3(0, -spiderLegs.CrouchOffset, 0);

        spiderLegs.IsGrounded = collisionDetection.isGrounded;
        spiderLegs.CrouchOffset = crouch01 * crouchAmount;
        spiderLegs.RefreshWalkParameters(moveVelocity.magnitude);

        if (physicsState == State.Jumping)
            physicsState = State.Falling;

        if (collisionDetection.isGrounded)
            velocity += -transform.up * 10 + Mathf.Max(0, Vector3.Dot(-transform.up, velocity)) * transform.up;

        collisionDetection.GetSafeMotion(velocity + moveVelocity, out Vector3 displacement, out _);
        //rb.position += displacement;

        if (collisionDetection.hitMirror)
            ReflectIn(collisionDetection.mirrorNormal, collisionDetection.mirrorPoint);

        rb.MovePosition(rb.position + displacement);
    }

    private void UpdateMove()
    {
        

        axisInput = physicsState == State.Falling ? prevAxisInput : new Vector3(Input.GetAxis("Horizontal") * inReflectedWorld, 0, Input.GetAxis("Vertical"));
        prevAxisInput = axisInput;

        Vector3 moveDir = Vector3.ProjectOnPlane(cameraController.transform.TransformDirection(axisInput), transform.up).normalized * Mathf.Min(1, axisInput.magnitude);
        moveVelocity += moveAcceleration * Time.deltaTime * moveDir;
        moveVelocity -= moveDrag * Time.deltaTime * moveVelocity;
        if (moveVelocity.sqrMagnitude > maxMoveSpeed * maxMoveSpeed)
        {
            moveVelocity = moveVelocity.normalized * maxMoveSpeed;
        }


        if (moveDir.sqrMagnitude > 0.0001f)
        {
            spiderBody.rotation = Quaternion.RotateTowards(spiderBody.rotation, Quaternion.LookRotation(Vector3.ProjectOnPlane(moveDir, transform.up), transform.up), bodyRotateSpeed * Time.deltaTime);
        }
    }
    private void UpdateWeb()
    {
        if (physicsState == State.Webbing && (!Input.GetKey(KeyCode.LeftShift) || collisionDetection.isGrounded && webTime01 > 2))
        {
            physicsState = State.Falling;
            spiderWeb.gameObject.SetActive(false);
            return;
        }
        else if (physicsState == State.Webbing)
        {
            webTime01 += Time.deltaTime * 6;
            SpiderLegs.DrawLine(rb.position, Vector3.Lerp(webStart, webEnd, webTime01), spiderWeb);
            if (webTime01 > 1)
                velocity += (webEnd - rb.position) * webAcceleration * Time.deltaTime;
        }

        if (physicsState != State.Running && physicsState != State.Falling)
            return;

        if (Input.GetKey(KeyCode.LeftShift))
        {
            Ray ray = cameraController.mainCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2));
            if (Physics.Raycast(ray, out RaycastHit hitInfo) && Vector3.Dot(hitInfo.point - rb.position, ray.direction) > 0)
            {
                webStart = rb.position;
                webEnd = hitInfo.point;
                webTime01 = 0;
                SpiderLegs.DrawLine(rb.position, Vector3.Lerp(webStart, webEnd, webTime01), spiderWeb);
                spiderWeb.gameObject.SetActive(true);
                physicsState = State.Webbing;
            }
        }
    }
    private void UpdateGravity()
    {
        Vector3 gravity = collisionDetection.isGrounded ? transform.up * -stickGravity : Vector3.up * -stickGravity;

        velocity += gravity * Time.deltaTime - physicsDrag * Time.deltaTime * velocity;
    }



    //http://web.archive.org/web/20221209225802/https://github.com/marmitoTH/Unity-Kinematic-Body
    [System.Serializable]
    private class CollisionDetection
    {
        private enum CapsuleAxis { xAxis, yAxis, zAxis }

        [SerializeField] private float slopeLimit = 70;
        [SerializeField] private float stepOffset = 0.3f;
        [SerializeField] private float skinWidth = 0.02f;

        [SerializeField] private Vector3 center = Vector3.zero;
        [SerializeField] private float radius = 0.7f;
        [SerializeField] private float height = 3.23f;
        [SerializeField] private CapsuleAxis axis;

        public bool hitMirror;
        public Vector3 mirrorNormal;
        public Vector3 mirrorPoint;

        private Vector3 m_position;
        private Vector3 m_upDirection;
        private Vector3 velocity;

        private Rigidbody body;
        private CapsuleCollider collider;

        private readonly Collider[] m_overlaps = new Collider[5];
        private readonly List<RaycastHit> m_contacts = new List<RaycastHit>();

        private const int MaxSweepSteps = 5;
        private const float MinMoveDistance = 0f;
        private const float MinCeilingAngle = 145;
        [ReadOnly] public bool isGrounded;

        public void Init(Rigidbody zeroWeight)
        {
            body = zeroWeight;
            if (!body.TryGetComponent(out collider))
                collider = body.gameObject.AddComponent<CapsuleCollider>();

            collider.center = center;
            collider.height = height;
            collider.radius = radius + Physics.defaultContactOffset;
            collider.direction = (int)axis;
        }


        public void GetSafeMotion(Vector3 motion, out Vector3 safeDisplacement, out Vector3 advisedImpulse)
        {
            m_position = body.position;

            switch (axis)
            {
                case CapsuleAxis.xAxis: m_upDirection = Vector3.ProjectOnPlane(body.transform.right, body.transform.up).normalized; break;
                case CapsuleAxis.yAxis: m_upDirection = body.transform.up; break;
                case CapsuleAxis.zAxis: m_upDirection = Vector3.ProjectOnPlane(body.transform.forward, body.transform.up).normalized; break;
            }

            velocity = motion;

            m_contacts.Clear();
            isGrounded = false;

            //Calculate a safe displacement to move by
            if (velocity.sqrMagnitude > MinMoveDistance)
            {

                Vector3 localVelocity = body.transform.InverseTransformDirection(velocity);
                Vector3 lateralVelocity = new Vector3(localVelocity.x, 0, localVelocity.z);
                Vector3 verticalVelocity = new Vector3(0, localVelocity.y, 0);

                lateralVelocity = body.transform.TransformDirection(lateralVelocity) * Time.deltaTime;
                verticalVelocity = body.transform.TransformDirection(verticalVelocity) * Time.deltaTime;

                CapsuleSweep(lateralVelocity.normalized, lateralVelocity.magnitude, stepOffset, MinCeilingAngle);
                CapsuleSweep(verticalVelocity.normalized, verticalVelocity.magnitude, 0, 0, slopeLimit);

            }

            //Adjust velocity here as a whole vector
            if (m_contacts.Count > 0)
            {
                foreach (RaycastHit contact in m_contacts)
                {
                    float angle = Vector3.Angle(m_upDirection, contact.normal);

                    if (angle <= slopeLimit)
                        isGrounded = true;

                    if (contact.transform.name == "OneMirror")
                    {
                        hitMirror = true;
                        mirrorNormal = contact.normal;
                        mirrorPoint = contact.point;
                    }

                    velocity -= Vector3.Project(velocity, contact.normal);
                }
                if (!isGrounded && m_contacts.Count == 1)
                {
                    body.MoveRotation(Quaternion.FromToRotation(body.transform.up, m_contacts[0].normal) * body.rotation);
                    isGrounded = true;
                } 
            }

            //Fix any overlaps (these cannot be fixed by CapsuleSweep)

            float capsuleOffset = height * 0.5f - radius;
            Vector3 top = m_position + m_upDirection * capsuleOffset;
            Vector3 bottom = m_position - m_upDirection * capsuleOffset;
            int overlapsNum = Physics.OverlapCapsuleNonAlloc(top, bottom, collider.radius, m_overlaps);
            if (overlapsNum > 0)
            {
                for (int i = 0; i < overlapsNum; i++)
                {
                    bool mask = m_overlaps[i].transform == body.transform || m_overlaps[i].gameObject.layer == 2 || m_overlaps[i].transform.IsChildOf(body.transform);
                    if (!mask &&
                        Physics.ComputePenetration(collider, m_position, body.transform.rotation, m_overlaps[i], m_overlaps[i].transform.position, m_overlaps[i].transform.rotation, out Vector3 direction, out float distance))
                    {
                        m_position += direction * (distance + skinWidth);
                        velocity -= Vector3.Project(velocity, -direction);
                    }
                }
            }

            safeDisplacement = m_position - body.position;
            advisedImpulse = velocity - motion;
        }

        private void CapsuleSweep(Vector3 direction, float distance, float stepOffset, float minSlideAngle = 0, float maxSlideAngle = 360)
        {
            float capsuleOffset = height * 0.5f - radius;

            //int layerMask = isShip ? ~(1 << 6 | 1 << 11 | 1 << 12) : ~(1 << 3); //IF SHIP 1...1011111 ELSE 1...1111011

            for (int i = 0; i < MaxSweepSteps; i++)
            {
                Vector3 world_centre = body.transform.TransformDirection(center.normalized) * center.magnitude;
                Vector3 origin = m_position + world_centre - direction * radius;
                Vector3 bottom = origin - m_upDirection * (capsuleOffset - stepOffset);
                Vector3 top = origin + m_upDirection * capsuleOffset;

                if (Physics.CapsuleCast(top, bottom, radius, direction, out RaycastHit hitInfo, distance + radius))
                {
                    float slideAngle = Vector3.Angle(m_upDirection, hitInfo.normal);
                    float safeDistance = hitInfo.distance - radius - skinWidth;
                    m_position += direction * safeDistance;
                    m_contacts.Add(hitInfo);

                    if ((slideAngle >= minSlideAngle) && (slideAngle <= maxSlideAngle))
                        break;

                    direction = Vector3.ProjectOnPlane(direction, hitInfo.normal);
                    distance -= safeDistance;
                }
                else
                {
                    m_position += direction * distance;
                    break;
                }
            }
        }
    }
}
