using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CannonAttack : AttackAction
{
    [SerializeField] private float startUpTime;
    [SerializeField] private float maxHoldTime = 0.24f;
    [SerializeField] private float explodeTime;

    [SerializeField] private float straightIniSpeed;
    [SerializeField] private float curveIniSpeed;
    [SerializeField] private float straightAcc;
    [SerializeField] private float curveAcc;
    [Range(0, 1)]
    [SerializeField] private float straightDrag01;
    [Range(0, 1)]
    [SerializeField] private float curveDrag01;
    [SerializeField] private float ballRadius;
    [SerializeField] private SphereCollider ballCollider;

    [SerializeField] private float armSwingRotationAngle = 30f;
    private float cannonArmPitchOffset;
    private float cannonArmYawOffset;

    private Vector3 ballVelocity;
    private Vector3 ballPosition;
    private Quaternion ballRotation;

    private float acceleration;
    private float drag01;

    private Transform Cannon;
    private Transform CannonBall;
    private Transform CannonTrail;

    private Material cannonMat;
    private Material cannonBallMat;
    private Material trailMat;

    private MeshRenderer hand;
    private MeshRenderer forearm;

    private Coroutine ContinueExpansion;

    public override void Init(RobotBody robot, bool isRightArm)
    {
        base.Init(robot, isRightArm);

        CannonBall = transform.GetChild(0);
        Cannon = transform.GetChild(1);
        CannonTrail = CannonBall.GetChild(0);

        cannonBallMat = CannonBall.GetComponent<MeshRenderer>().sharedMaterial = new Material(CannonBall.GetComponent<MeshRenderer>().sharedMaterial);
        cannonMat = Cannon.GetComponent<MeshRenderer>().sharedMaterial = new Material(Cannon.GetComponent<MeshRenderer>().sharedMaterial);
        trailMat = CannonTrail.GetComponent<MeshRenderer>().sharedMaterial = new Material(CannonTrail.GetComponent<MeshRenderer>().sharedMaterial);

        hand = (IsRightArm ? r.body.RightHand : r.body.LeftHand).GetComponent<MeshRenderer>();
        forearm = (IsRightArm ? r.body.RightLowerArm : r.body.LeftLowerArm).GetComponent<MeshRenderer>();
    }

    protected override IEnumerator AttackSequence()
    {
        if (ContinueExpansion != null)
        {
            StopCoroutine(ContinueExpansion);
            ContinueExpansion = null;
        }

        if (ballCollider.enabled) //wait while cancelling explosion here
            yield return new WaitWhile(() => ballCollider.enabled);

        CanJumpCrouch = false;

        cannonBallMat.SetFloat("_ExplosionRadius", 0);
        cannonBallMat.SetFloat("_Brightness", 1.5f);
        trailMat.SetFloat("_Brightness", 1);

        Cannon.gameObject.SetActive(true);
        CannonBall.gameObject.SetActive(true);
        CannonBall.parent = transform.root;

        hand.enabled = false;
        forearm.enabled = false;

        CannonBall.localScale = Vector3.one * 0.35f;
        CannonTrail.localScale = new Vector3(0.15f, 0.15f, 0);

        float timer = 0;
        while (timer < startUpTime)
        {
            float t01 = Mathf.Sqrt(timer / startUpTime);

            cannonMat.SetFloat("_PulseTime", t01);

            r.arms.Animate(IsRightArm ? "CannonOther" : "CannonShoot", IsRightArm ? "CannonShoot" : "CannonOther", Mathf.Lerp(0, 0.4f, t01));
            SetArmRotation();
            if (r.Opponent != null)
                r.arms.IdleRotateTowardsOpponent();

            CannonBall.SetPositionAndRotation(transform.position, transform.rotation * Quaternion.Euler(0, -90, 0));

            yield return r.WaitForUpdateRobot();
            timer += r.dt;

            Vector3 normalInput = r.inputDirection.normalized;
            cannonArmPitchOffset = Mathf.Clamp01(-normalInput.z);
            cannonArmYawOffset = normalInput.x;
        }
        r.arms.Animate(IsRightArm ? "CannonOther" : "CannonShoot", IsRightArm ? "CannonShoot" : "CannonOther", 0.4f);

        CannonBall.localScale = Vector3.one * ballRadius;

        LaunchCannon();

        CanJumpCrouch = true;

        float animationT01 = 0;
        timer = 0;
        while (timer < maxHoldTime && r.Controller.GetButton(IsRightArm ? Controller.Inputs.A : Controller.Inputs.Y))
        {
            cannonMat.SetFloat("_PulseTime", Mathf.Min(2, 1 + timer / startUpTime));

            r.arms.Animate(IsRightArm ? "CannonOther" : "CannonShoot", IsRightArm ? "CannonShoot" : "CannonOther", Mathf.Lerp(0.4f, 1, animationT01));
            r.body.Arms.localRotation = Quaternion.Slerp(r.body.Arms.localRotation, Quaternion.Euler(0, armSwingRotationAngle * (IsRightArm ? 1 : -1), 0), animationT01);
            SetArmRotation();

            r.arms.IdleRotateTowardsOpponent();

            if (timer != 0)
                UpdateCannon(true);
            else
                CannonBall.position = ballPosition;
            CannonTrail.localScale = new Vector3(0.15f, 0.15f, 1.5f * Mathf.Min(Mathf.Clamp01(ballVelocity.sqrMagnitude / 1000), timer / explodeTime));
            yield return r.WaitForUpdateRobot();
            timer += r.dt;
            animationT01 = Mathf.Min(0.8f * Mathf.InverseLerp(0.4f, 1, 0.5f), animationT01 + r.dt / explodeTime);
        }


        yield return Explode(animationT01, true);

        r.body.LeftArm.localRotation = Quaternion.identity;
        r.body.RightArm.localRotation = Quaternion.identity;

        hand.enabled = true;
        forearm.enabled = true;

        FinishedAttack = true;
    }

    private IEnumerator Explode(float animationT01, bool swingArms)
    {
        ballCollider.enabled = true;

        CannonBall.SetPositionAndRotation(ballPosition, ballRotation);

        if (r.Controller.GetButton(Controller.Inputs.RB))
            foreach (Collider bounceBox in r.GetHurtBoxes())
                if (animationT01 == 0 || Umpire.CollidesWith(bounceBox, ballCollider))
                {
                    if (r.IsGrounded())
                        r.physics.Launch(ballVelocity.normalized * 20 + 50 * r.normalUp);
                    else
                    {
                        r.physics.Launch(ballVelocity.normalized * 5);
                        r.physics.Jump(0);
                    }
                    break;
                }

        r.physics.Launch((r.inputDirection == Vector3.zero ? ballVelocity : r.transform.TransformDirection(r.inputDirection)).normalized * 4); //just for the leaning effect
        float pulseTime = cannonMat.GetFloat("_PulseTime");

        float timer = 0;
        while (timer < explodeTime)
        {
            float t01 = timer / explodeTime;


            if (swingArms)
            {
                if (animationT01 > 0.8f)
                {
                    float final01 = Mathf.InverseLerp(0.8f, 1, animationT01);
                    r.arms.Animate(IsRightArm ? null : "CannonShoot", IsRightArm ? "CannonShoot" : null, Mathf.Lerp(1, 0.6f, final01));
                    r.body.Arms.localRotation = Quaternion.Slerp(r.body.Arms.localRotation, Quaternion.identity, final01);
                    r.body.LeftArm.localRotation = Quaternion.Slerp(r.body.LeftArm.localRotation, Quaternion.identity, final01);
                    r.body.RightArm.localRotation = Quaternion.Slerp(r.body.RightArm.localRotation, Quaternion.identity, final01);
                }
                else
                {
                    r.arms.Animate(IsRightArm ? "CannonOther" : "CannonShoot", IsRightArm ? "CannonShoot" : "CannonOther", Mathf.Lerp(0.4f, 1, 1.25f * animationT01));
                    r.body.Arms.localRotation = Quaternion.Slerp(r.body.Arms.localRotation, Quaternion.Euler(0, -armSwingRotationAngle * (IsRightArm ? 1 : -1), 0), 1.25f * animationT01);
                    SetArmRotation();
                }
                animationT01 = Mathf.Clamp01(animationT01 + r.dt / explodeTime);
            }
            else if (!r.arms.InHitStun())
                break;

            cannonMat.SetFloat("_PulseTime", Mathf.Min(2, pulseTime + timer / startUpTime));

            CannonTrail.localScale = new Vector3(0.15f, 0.15f, Mathf.Min(CannonTrail.localScale.z, 1.5f * (1 - t01)));

            float pulse01 = Mathx.Square(Mathf.Sin(t01 * Mathf.PI / 2));
            cannonBallMat.SetFloat("_Brightness", Mathf.Lerp(1.5f, 5, pulse01));
            trailMat.SetFloat("_Brightness", Mathf.Lerp(1, 2.5f, pulse01));

            cannonBallMat.SetFloat("_ExplosionRadius", t01);
            ballCollider.radius = 0.5f + t01 * 2;

            UpdateCannon(false);
            yield return r.WaitForUpdateRobot();
            timer += r.dt;
        }

        ballCollider.enabled = false;
        Cannon.gameObject.SetActive(false);

        r.body.LeftArm.localRotation = Quaternion.identity;
        r.body.RightArm.localRotation = Quaternion.identity;
        r.body.Arms.localRotation = Quaternion.identity;

        CannonTrail.localScale = new Vector3(0.15f, 0.15f, 0);
        ContinueExpansion = StartCoroutine(ContinueBallExpansion());
    }

    private IEnumerator ContinueBallExpansion()
    {
        float timer = 0;
        while (timer < explodeTime)
        {
            float t01 = 1 + timer / explodeTime;
            float pulse01 = Mathx.Square(Mathf.Sin(t01 * Mathf.PI / 2));
            cannonBallMat.SetFloat("_Brightness", Mathf.Lerp(0, 5, pulse01));

            cannonBallMat.SetFloat("_ExplosionRadius", t01);

            UpdateCannon(false);
            yield return null;
            timer += Time.deltaTime; //just visual so this is preferred
        }
        CannonBall.parent = transform;
        CannonBall.gameObject.SetActive(false);
    }

    public override void CancelAttackSequence()
    {
        base.CancelAttackSequence();

        r.body.LeftArm.localRotation = Quaternion.identity;
        r.body.RightArm.localRotation = Quaternion.identity;
        r.body.Arms.localRotation = Quaternion.identity;
        Cannon.gameObject.SetActive(false);
        hand.enabled = true;
        forearm.enabled = true;

        StartCoroutine(Explode(1, false));
    }

    /// <returns>Nothing of significance, just a way for the base class method to communicate with the derived.</returns>
    public override bool HandleHit(CollisionWith collisionWith, out int fixedFrames)
    {
        if (base.HandleHit(collisionWith, out fixedFrames))
            return default;

        fixedFrames = 0;
        if (collisionWith == CollisionWith.HurtBox)
        {
            r.Opponent.physics.Launch(ballVelocity.normalized * 40 + r.normalUp * 50);
            r.Opponent.arms.EnterHitStun(new System.Func<bool>(() => r.Opponent.physics.GetHorizontalSpeed() < 2), 0.5f);
        }
        else
            r.Opponent.physics.Launch(ballVelocity.normalized * 40);

        return default;
    }


    void UpdateCannon(bool homeIn)
    {

        Vector3 prevPos = ballPosition;

        if (homeIn)
        {
            ballVelocity += acceleration * r.dt * (AttackCanAutoAim() && r.Opponent != null ? (r.Opponent.position - ballPosition).normalized : CannonBall.forward);
            ballRotation = Quaternion.LookRotation(ballVelocity, Quaternion.AngleAxis(ballVelocity.sqrMagnitude, CannonBall.forward) * CannonBall.up);
        }
        
        ballVelocity -= drag01 * r.dt * 60 * ballVelocity; //optimised when FixedPS is 60

        ballPosition += ballVelocity * r.dt;

        CannonBall.SetPositionAndRotation((ballPosition + prevPos) / 2, ballRotation);
        CannonBall.localScale = ballRadius * new Vector3(1, 1, 1 + (ballPosition - prevPos).sqrMagnitude);
    }

    void LaunchCannon()
    {
        float curve01 = r.inputDirection == Vector3.zero ? 0 : Mathx.Square(1 - Mathf.Abs(r.inputDirection.normalized.z));
        float speed = Mathf.Lerp(straightIniSpeed, curveIniSpeed, curve01);
        acceleration = Mathf.Lerp(straightAcc, curveAcc, curve01);
        drag01 = Mathf.Lerp(straightDrag01, curveDrag01, curve01);

        ballVelocity = r.physics.GetObservedVelocity() + r.transform.TransformDirection(new Vector3(r.inputDirection.x, 0.5f * cannonArmPitchOffset, 1 - 0.5f * cannonArmPitchOffset)).normalized * speed;
        ballPosition = transform.position;
        ballRotation = Quaternion.LookRotation(ballVelocity, Quaternion.AngleAxis(ballVelocity.sqrMagnitude, CannonBall.forward) * CannonBall.up);
    }

    void SetArmRotation()
    {
        r.body.LeftArm.localRotation = Quaternion.Euler(
            cannonArmPitchOffset * 20 + Mathf.MoveTowardsAngle(-r.body.UpperBody.localEulerAngles.x, 0, r.body.UpperBody.localEulerAngles.x < 180 || r.IsGrounded() ? 0 : 20), 0, 0);

        r.body.RightArm.localRotation = Quaternion.Euler(
            cannonArmPitchOffset * 20 + Mathf.MoveTowardsAngle(-r.body.UpperBody.localEulerAngles.x, 0, r.body.UpperBody.localEulerAngles.x < 180 || r.IsGrounded() ? 0 : 20), 0, 0);
    }
}