using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlashAttack : AttackAction
{
    [SerializeField] private CustomLineRenderer Trail;

    [SerializeField] private float decisionTime = 0.021f;
    [SerializeField] private float startUpTime = 0.06f;
    [SerializeField] private float holdTime = 0.18f;
    [SerializeField] private float endLagTime = 0.12f;
    [SerializeField] private float maxAttackYaw = 5f;
    [SerializeField] private float maxAttackPitch = 30f;

    [SerializeField] private float armSwingRotationAngle = 40f;

    float direction = 1;
    float unWrapAttack = 1;

    Transform Sword;


    public override void Init(RobotBody robot, bool isRightArm)
    {
        base.Init(robot, isRightArm);
        Sword = transform.GetChild(0);
        Trail.SetMaterial(new Material(Trail.sharedMaterial));
    }

    protected override IEnumerator AttackSequence()
    {
        CanJumpCrouch = true;

        float lastDirection = direction;
        float timer = 0;
        while (timer < decisionTime)
        {
            yield return r.WaitForUpdateRobot();
            timer += r.dt;
            direction = Mathx.Sign(r.inputDirection.x, -lastDirection);
        }

        Sword.gameObject.SetActive(true);
        Sword.localRotation = Quaternion.Euler(0, direction == 1 ? 0 : 180, 180);

        unWrapAttack = IsRightArm ? -direction : direction;
        string startAnimation = unWrapAttack == 1 ? "SwordUnwrapStart" : "SwordWrapStart";

        timer = 0;
        while (timer < startUpTime || r.Controller.GetButton(IsRightArm ? Controller.Inputs.A : Controller.Inputs.Y))
        {
            float t01 = timer / startUpTime;

            r.arms.Animate(IsRightArm ? "SlashOther" : startAnimation, IsRightArm ? startAnimation : "SlashOther", t01);

            r.body.Arms.localRotation = Quaternion.Euler(0, armSwingRotationAngle * Mathf.Lerp(0, direction, t01), 0);
            SetArmRotation(0);

            r.arms.IdleRotateTowardsOpponent();

            yield return r.WaitForUpdateRobot();
            timer += r.dt;
        }
        r.physics.Launch(r.transform.TransformDirection(r.inputDirection == Vector3.zero ? Vector3.forward : r.inputDirection).normalized * Mathf.Max(4, 2 * r.physics.GetHorizontalSpeed())); //just for the leaning effect?

        r.legs.LockPosition(true);
        r.legs.LockRotation(true);
        Sword.GetComponent<BoxCollider>().enabled = true;

        Vector3 attackDir = r.inputDirection.normalized * Mathf.Sign(r.inputDirection.z);

        Quaternion prevRot = r.rotation;
        Quaternion newRot = Quaternion.LookRotation(r.transform.TransformDirection(Vector3.RotateTowards(Vector3.forward, attackDir, Mathf.Deg2Rad * maxAttackYaw, 0)), r.normalUp);

        Trail.sharedMaterial.SetFloat("_Alpha", 0.1f);
        Trail.sharedMaterial.SetFloat("_AlphaFade", 2);

        Vector3 prevPos = r.position;
        Vector3 prevSwordPos = transform.position;

        timer = 0;
        while (timer < holdTime)
        {
            float t01 = Mathx.Square(Mathf.Sin(Mathf.PI * 0.5f * Mathf.Lerp(0, 0.9f, timer / holdTime))); //Mathx.Square(Mathf.Lerp(0, 0.9f, timer / holdTime));

            r.arms.Animate(IsRightArm ? null : "SwordSlash", IsRightArm ? "SwordSlash" : null, unWrapAttack == 1 ? t01 : 1 - t01);

            r.body.Arms.localRotation = Quaternion.Euler(0, armSwingRotationAngle * Mathf.Lerp(direction, -direction, t01), 0);
            SetArmRotation(t01);

            if (AttackCanAutoAim())
                r.RotateTowardsOpponent(90);
            else
                r.Rotate(Quaternion.Slerp(prevRot, newRot, t01));


            if (t01 > 0.1f && prevSwordPos != transform.position)
            {
                transform.rotation = Quaternion.LookRotation(Vector3.Cross(transform.position - prevSwordPos, transform.up), transform.up);
                Sword.localRotation = Quaternion.Euler(0, 0, 180);
            }

            Trail.Displace(r.position - prevPos);
            Trail.AddPosition(Trail.transform.position - Trail.transform.up * 0.5f, Trail.transform.position + Trail.transform.up * 0.5f);
            Trail.UpdateMesh();
            prevPos = r.position;
            prevSwordPos = transform.position;
            yield return r.WaitForUpdateRobot();
            timer += r.dt;
        }

        transform.localRotation = Quaternion.identity;
        Sword.localRotation = Quaternion.Euler(0, direction == 1 ? 0 : 180, 180);


        Sword.GetComponent<BoxCollider>().enabled = false;

        timer = 0;
        while (timer < endLagTime)
        {
            float t01 = timer / endLagTime;
            
            r.arms.Animate(IsRightArm ? null : "SwordSlash", IsRightArm ? "SwordSlash" : null, Mathx.Square(Mathf.Sin(Mathf.PI * 0.5f * (unWrapAttack == 1 ? Mathf.Lerp(0.9f, 1, t01) : Mathf.Lerp(0.1f, 0, t01)))));

            r.body.Arms.localRotation = Quaternion.Euler(0, armSwingRotationAngle * Mathx.Remap(-direction, 0, 0.8f, 1, t01), 0);
            SetArmRotation(1);

            if (AttackCanAutoAim())
                r.RotateTowardsOpponent(90);
            else
                r.arms.IdleRotateTowardsOpponent();

            Trail.sharedMaterial.SetFloat("_Alpha", 0.1f * Mathf.InverseLerp(1, 0.5f, t01));
            Trail.sharedMaterial.SetFloat("_AlphaFade", Mathf.Lerp(2, 3, t01));

            Trail.Displace(r.position - prevPos);
            Trail.AddPosition(Trail.transform.position - Trail.transform.up * 0.5f, Trail.transform.position + Trail.transform.up * 0.5f);
            Trail.UpdateMesh();
            prevPos = r.position;
            yield return r.WaitForUpdateRobot();
            timer += r.dt;
        }

        Trail.Clear();

        r.legs.LockPosition(false);
        r.legs.LockRotation(false);
        Sword.gameObject.SetActive(false);

        r.arms.Animate("Idle", r.arms.GetIdleAnimationLerp());

        r.body.LeftArm.localRotation = Quaternion.identity;
        r.body.RightArm.localRotation = Quaternion.identity;
        r.body.Arms.localRotation = Quaternion.identity;

        FinishedAttack = true;
        CanCancelAttack = false;
    }

    public override void CancelAttackSequence()
    {
        base.CancelAttackSequence();

        Sword.GetComponent<BoxCollider>().enabled = false;
        Sword.gameObject.SetActive(false);

        r.body.LeftArm.localRotation = Quaternion.identity;
        r.body.RightArm.localRotation = Quaternion.identity;
        r.body.Arms.localRotation = Quaternion.identity;

        Trail.Clear();

        r.legs.LockPosition(false);
        r.legs.LockRotation(false);
    }

    /// <returns>Nothing of significance, just a way for the base class method to communicate with the derived.</returns>
    public override bool HandleHit(CollisionWith collisionWith, out int fixedFrames)
    {
        if (base.HandleHit(collisionWith, out fixedFrames))
            return default;

        if (collisionWith == CollisionWith.HurtBox)
        {
            CanCancelAttack = true;
            r.Opponent.arms.EnterHitStun(new System.Func<bool>(() => r.Opponent.physics.GetHorizontalSpeed() < 2), 0.75f);

            r.Opponent.physics.Launch(-Sword.right * 50 + r.normalUp * 40);
        }
        else
        {
            unWrapAttack *= -1;
            direction *= -1;
            r.Opponent.physics.Launch(-Sword.right * 40);
        }

        fixedFrames = 4;

        return default;
    }

    void SetArmRotation(float t01)
    {
        float rawAngle = Mathf.MoveTowardsAngle(-r.body.UpperBody.localEulerAngles.x, 0, r.body.UpperBody.localEulerAngles.x > 180 && r.IsGrounded() ? 0 : maxAttackPitch);

        float angle = Mathf.LerpAngle(0, 2 * rawAngle, unWrapAttack == 1 ? t01 : 1 - t01);

        r.body.LeftArm.localRotation = Quaternion.Euler(angle, 0, 0);
        r.body.RightArm.localRotation = Quaternion.Euler(angle, 0, 0);
    }
}

