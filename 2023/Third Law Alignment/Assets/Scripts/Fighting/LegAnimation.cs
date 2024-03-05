using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LegAnimation
{
    private RobotBody r;

    [SerializeField] private float rotLerpSpeed;

    [SerializeField] private float HipWidth;
    private float HipHeightOffset = 0;
    private bool PositionIsLocked => r.arms.Lasering() || hardLockPos;
    private bool RotationIsLocked => hardLockRot;
    private bool hardLockPos;
    private bool hardLockRot;

    public bool IsCrouching => HipHeightOffset == -0.15f;

    private Vector3 GroundOffset = new Vector3(0, -1.5f, 0);
    private Vector3 localForward = Vector3.forward;
    private Vector3 localLegForward = Vector3.forward;

    private Vector3 localUp = Vector3.up;

    private float direction;

    private float v = 0.5f;
    private float animationSpeed;
    [ReadOnly] [SerializeField] private float bestAnimationSpeed = 1.75f;
    [ReadOnly] [SerializeField] private float minVelocityAnimation = 0.5f;
    [ReadOnly] [SerializeField] private float maxVelocityAnimation = 5;
    [ReadOnly] [SerializeField] private float maxAbsoluteVelocity = 5;

    [Tooltip("Should be one, but left here for fun...")]
    [SerializeField] private float legScale = 1;

    [Range(2, 3)]
    [SerializeField] private float footCastDistMultiplier = 2.75f;
    [Range(0, 1)]
    [SerializeField] private float fwdSpeed;
    [SerializeField] private float fwdSpeedMultiplier;

    [SerializeField] private int BodyIntertia = 10;
    private Queue<float> PrevHipYPos;
    private float dampedHipYPos;


    private readonly float D = 20.4f;
    private readonly float d = 1.42f;
    private float R;
    private float s;
    private float L;
    private float h;
    private float f1;
    private float T1;
    private float T2;
    private float T;

    private float hsPlus;
    private float hsMinus;
    private float smoothOffset;

    private float timeAirBorne;
    private float airBorneLerp;
    private float t0;
    private Vector3 localHipsRight;

    private Vector3 lastUpdatedInputDirection;
    private Vector3 movementVector;

    // Start is called before the first frame update
    public void Start(RobotBody robot)
    {
        r = robot;
        RefreshWalkParamaters(0, maxAbsoluteVelocity);
        ResetHipDamping();
    }

    public void Update()
    {
        CalculateMovementInput();
    }

    public Vector3 FixedUpdate()
    {
        if (r.IsGrounded())
        {
            timeAirBorne = 0;
            airBorneLerp = 0;
        }
        else
        {
            timeAirBorne += r.dt;
            airBorneLerp = 0.9f * (1 - Mathf.Exp(-4 * timeAirBorne));
        }

        if (!RotationIsLocked)
        {
            RefreshWalkParamaters(Mathf.Clamp01(movementVector.magnitude), maxAbsoluteVelocity);

            animationSpeed *= Mathf.Sign(animationSpeed) * direction;
        }


        float timeElapsed = r.dt * animationSpeed * (PositionIsLocked ? 0 : 1);
        t0 = (t0 + timeElapsed * (1 - airBorneLerp)) % T;

        localHipsRight = r.body.Hips.localRotation * Vector3.right;
        UpdateJoints();
        RotationUpdate();

        return r.transform.TransformDirection(timeElapsed * v * localForward);

        //Treadmill.sharedMaterial.SetVector("_Displacement", displacement);
    }

    public float GetHipHeight()
    {
        return HipHeightOffset;
    }

    public void ChangeHipHeight(float amount, float min, float max)
    {
        float newHipHeight = Mathf.Clamp(HipHeightOffset + amount, Mathf.Max(-0.15f, min), Mathf.Min(0.05f, max));
        if (newHipHeight != HipHeightOffset)
        {
            HipHeightOffset = newHipHeight;
            ResetHipDamping();
        }
    }
    private void ResetHipDamping()
    {
        PrevHipYPos = new Queue<float>();
        dampedHipYPos = 2 * R;
        for (int i = 0; i < BodyIntertia; i++)
            PrevHipYPos.Enqueue(2 * R);
    }

    private void CalculateMovementInput()
    {
        if (airBorneLerp == 0 || r.arms.Dashing() || r.arms.Attacking() && !r.physics.fixedVelocity)
            lastUpdatedInputDirection = r.inputDirection;

        Vector3 input = lastUpdatedInputDirection.Clamp01();

        input *= fwdSpeed;

        input.x /= fwdSpeedMultiplier;

        if (input.z < 0 || IsCrouching || r.arms.Shielding() || r.arms.Lasering())
            input.z /= fwdSpeedMultiplier;
        if (IsCrouching)
            input /= fwdSpeedMultiplier;

        movementVector = input;
        direction = RotationIsLocked ? direction : Mathf.Sign(movementVector.z);

        //selection here keeps legs in current position when not moving, no selection would mean they go back to default
        localForward = movementVector.sqrMagnitude == 0 || RotationIsLocked ? localForward : movementVector.normalized * direction;
        localLegForward = PositionIsLocked ? localLegForward : localForward;
    }


    //Linear interpolation between 0 and maxVelocity by v01, splitting this velocity into a desirable v and animation speed https://www.desmos.com/calculator/oqeuzizfcs
    private void RefreshWalkParamaters(float v01, float maxVelocity)
    {
        float velocity = v01 * maxVelocity;
        animationSpeed = Mathf.Clamp(bestAnimationSpeed, velocity / maxVelocityAnimation, velocity / minVelocityAnimation);
        v = animationSpeed == 0 ? minVelocityAnimation : Mathf.Min(velocity / animationSpeed, maxVelocityAnimation);

        if (r.IsGrounded() && Physics.Raycast(r.position, -r.normalUp, out RaycastHit hitInfo, footCastDistMultiplier * R))
        {
            float gradient = Vector3.Dot(r.transform.forward, hitInfo.normal) * 0.5f;
            animationSpeed *= 1 - gradient;
            v *= 1 + gradient;
        }

        h = v / 5;

        hsPlus = (h * h + s * s) / (2 * h);
        hsMinus = (h * h - s * s) / (2 * h);

        R = Mathf.Lerp(0.7f, 0.9f, Mathf.InverseLerp(minVelocityAnimation, maxVelocityAnimation, v)) * legScale; //Hopefully no-one notices the legs change length often
        s = Mathf.Sqrt(v) / Mathf.Lerp(2, 4, airBorneLerp);
        L = 2 * R - 0.2f / (1 + Mathf.Exp(-v + 4.5f));
        f1 = Mathf.Atan(hsMinus / -s);

        smoothOffset = hsPlus * Mathf.Cos(Mathf.PI - f1) / (1 + Mathf.Exp(-d));

        T1 = Mathf.Pow(v, -0.05f);
        T2 = (s - smoothOffset) / v;
        T = T1 + T2;
    }

    //https://www.desmos.com/calculator/aznrvmwsir
    void UpdateJoints()
    {
        bool stationary = animationSpeed == 0 || PositionIsLocked;

        float tl = Mathx.Mod(t0, T);
        float tr = Mathx.Mod(tl + T / 2, T);

        float wrapT = 2 * Mathx.Mod(tl - T1, T / 2) - T2;

        float hipYPos = Mathf.Lerp(Mathf.Sqrt(Mathf.Max(L * L - s * s * Mathx.Square(Mathf.Max(-wrapT / T2, wrapT / T1)), 0)), 1.5f, airBorneLerp);

        float fl = Mathf.Lerp(Mathf.PI - f1, f1, tl / T1);
        float fr = Mathf.Lerp(Mathf.PI - f1, f1, tr / T1);

        Vector3 leftOffset = GroundOffset - localHipsRight * HipWidth / 2;
        Vector3 rightOffset = GroundOffset + localHipsRight * HipWidth / 2;

        float leftFootXPos = tl < T1 ? hsPlus * Mathf.Cos(fl) / (1 + Mathf.Exp(D * (fl - Mathf.PI + f1) - d)) : Mathf.Lerp(s, smoothOffset, (tl - T1) / T2);
        float rightFootXPos = tr < T1 ? hsPlus * Mathf.Cos(fr) / (1 + Mathf.Exp(D * (fr - Mathf.PI + f1) - d)) : Mathf.Lerp(s, smoothOffset, (tr - T1) / T2);

        float leftFootEuler = 0;
        float leftFootYPos = 0;
        if (tl < T1 && !stationary)
        {
            leftFootYPos = hsMinus + hsPlus * Mathf.Sin(fl);
            leftFootEuler = Vector3.SignedAngle(Vector3.up, r.body.LeftLowerLeg.localRotation * Vector3.up, Vector3.right);
        }
        else
        {
            Vector3 leftRayCastOrigin = r.body.LeftAnkle.parent.TransformPoint(leftOffset + leftFootXPos * localLegForward + hipYPos * localUp);
            if (Physics.Raycast(leftRayCastOrigin, -r.normalUp, out RaycastHit hitInfo, footCastDistMultiplier * R, LayerMask.NameToLayer("ZeroWeight")))
            {
                leftFootYPos = hipYPos - hitInfo.distance;
                Vector3 localNormal = r.body.LeftAnkle.parent.InverseTransformDirection(hitInfo.normal);
                leftFootEuler = Quaternion.LookRotation(Vector3.ProjectOnPlane(Vector3.forward, localNormal), localNormal).eulerAngles.x;
            }
        }

        float rightFootEuler = 0;
        float rightFootYPos = 0;
        if (tr < T1 && !stationary)
        {
            rightFootYPos = hsMinus + hsPlus * Mathf.Sin(fr);
            rightFootEuler = Vector3.SignedAngle(Vector3.up, r.body.RightLowerLeg.localRotation * Vector3.up, Vector3.right);
        }
        else
        {
            Vector3 leftRayCastOrigin = r.body.RightAnkle.parent.TransformPoint(rightOffset + rightFootXPos * localLegForward + hipYPos * localUp);
            if (Physics.Raycast(leftRayCastOrigin, -r.normalUp, out RaycastHit hitInfo, footCastDistMultiplier * R, LayerMask.NameToLayer("ZeroWeight")))
            {
                rightFootYPos = hipYPos - hitInfo.distance;
                Vector3 localNormal = r.body.RightAnkle.parent.InverseTransformDirection(hitInfo.normal);
                rightFootEuler = Quaternion.LookRotation(Vector3.ProjectOnPlane(Vector3.forward, localNormal), localNormal).eulerAngles.x;
            }
        }

        float leftKneeBend = Mathf.Sqrt(Mathf.Max(0, R * R / (leftFootXPos * leftFootXPos + Mathx.Square(hipYPos + HipHeightOffset * 2 - leftFootYPos)) - 0.25f));

        float leftKneeXPos = leftFootXPos / 2 + (hipYPos - leftFootYPos) * leftKneeBend;
        float leftKneeYPos = (hipYPos + leftFootYPos) / 2 + leftFootXPos * leftKneeBend;

        float rightKneeBend = Mathf.Sqrt(Mathf.Max(0, R * R / (rightFootXPos * rightFootXPos + Mathx.Square(hipYPos + HipHeightOffset * 2 - rightFootYPos)) - 0.25f));

        float rightKneeXPos = rightFootXPos / 2 + (hipYPos - rightFootYPos) * rightKneeBend;
        float rightKneeYPos = (hipYPos + rightFootYPos) / 2 + rightFootXPos * rightKneeBend;

        dampedHipYPos += (hipYPos - PrevHipYPos.Dequeue()) / BodyIntertia;
        PrevHipYPos.Enqueue(hipYPos);

        r.body.UpperBody.localPosition = GroundOffset + (dampedHipYPos + HipHeightOffset * 4) * localUp;

        r.body.Torso.localPosition = GroundOffset + (hipYPos + HipHeightOffset * 2) * localUp;
        r.body.Hips.localPosition = r.body.Torso.localPosition;

        r.body.LeftKnee.localPosition = Vector3.MoveTowards(r.body.LeftKnee.localPosition, leftOffset + leftKneeXPos * localLegForward + leftKneeYPos * localUp, 0.2f);
        r.body.RightKnee.localPosition = Vector3.MoveTowards(r.body.RightKnee.localPosition, rightOffset + rightKneeXPos * localLegForward + rightKneeYPos * localUp, 0.2f);

        r.body.LeftAnkle.localPosition = Vector3.MoveTowards(r.body.LeftAnkle.localPosition, leftOffset + leftFootXPos * localLegForward + leftFootYPos * localUp, 0.2f);
        r.body.RightAnkle.localPosition = Vector3.MoveTowards(r.body.RightAnkle.localPosition, rightOffset + rightFootXPos * localLegForward + rightFootYPos * localUp, 0.2f);

        r.body.LeftAnkle.localRotation = Quaternion.Euler(Mathf.MoveTowardsAngle(r.body.LeftAnkle.localEulerAngles.x, leftFootEuler, stationary ? 25 : 5 * v), 0, 0);
        r.body.RightAnkle.localRotation = Quaternion.Euler(Mathf.MoveTowardsAngle(r.body.RightAnkle.localEulerAngles.x, rightFootEuler, stationary ? 25 : 5 * v), 0, 0);
    }

    void RotationUpdate()
    {
        float bend = Mathf.Clamp(v + r.physics.GetHorizontalSpeed() * 0.3f, 0, 2 * maxVelocityAnimation);

        float lean = Vector3.Dot(localForward, Vector3.forward) * -direction * 9 * (bend - 9 * HipHeightOffset);
        float side = Vector3.Dot(localForward, Vector3.right) * -direction * 9 * bend;

        r.body.UpperBody.localRotation = Quaternion.Lerp(r.body.UpperBody.localRotation, Quaternion.Euler(lean, 180, -side), rotLerpSpeed * r.dt);
        r.body.Torso.localRotation = Quaternion.Lerp(r.body.Torso.localRotation, Quaternion.Euler(-0.9f * lean, 0, 0.9f * side), rotLerpSpeed * r.dt);
        if (r.arms.Lasering())
            r.body.Head.localRotation = Quaternion.identity;
        else
        {
            float additionalLean = (r.arms.InHitStun() ? -30 : 0) + (r.arms.InLaserHitStun ? -30 : 0);
            r.body.Head.localRotation = Quaternion.Lerp(r.body.Head.localRotation, Quaternion.Euler(-0.5f * lean + additionalLean, 0, side), rotLerpSpeed * r.dt);
        }

        r.body.Hips.localRotation = Quaternion.Lerp(r.body.Hips.localRotation, Quaternion.Euler(0, Vector3.Dot(localLegForward, localHipsRight) * 25, 0), rotLerpSpeed * r.dt);
    }

    public void LookTowardsOpponent(float angleLimit, float t01)
    {
        Vector3 forward = r.Opponent == null ? r.transform.TransformDirection(r.inputDirection == Vector3.zero ? -Vector3.forward : -r.inputDirection) : r.body.Head.position - r.Opponent.body.Head.position;
        r.body.Head.rotation = Quaternion.Slerp(r.body.Head.rotation, 
            Quaternion.RotateTowards(r.body.Head.rotation, Quaternion.LookRotation(forward, Vector3.ProjectOnPlane(r.normalUp, forward)), angleLimit), 
            t01);
    }

    public void LockPosition(bool locked)
    {
        hardLockPos = locked;
    }
    public void LockRotation(bool locked)
    {
        hardLockRot = locked;
    }
}
