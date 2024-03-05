using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StabAttack : AttackAction
{
    [SerializeField] private float startUpTime;
    [SerializeField] private float holdTime;
    [SerializeField] private float endLagTime;
    [SerializeField] private float maxAttackYaw = 5f;
    [SerializeField] private float maxAttackPitch = 20f;
    [SerializeField] private float armSwingRotationAngle = 20f;

    private Transform Sword;
    bool riseAttack;

    public override void Init(RobotBody robot, bool rightArm)
    {
        base.Init(robot, rightArm);

        Sword = transform.GetChild(0);
    }

    protected override IEnumerator AttackSequence()
    {
        CanJumpCrouch = true;

        Sword.gameObject.SetActive(true);
        Sword.GetComponent<BoxCollider>().enabled = false;

        float timer = 0;
        while (timer < startUpTime || r.Controller.GetButton(IsRightArm ? Controller.Inputs.A : Controller.Inputs.Y))
        {
            float t01 = timer / startUpTime;

            if (IsRightArm)
                r.arms.Animate("SwordOther", "SwordStab", Mathf.Clamp01(0.4f * (t01 - 1)) * 0.2f, Mathf.Clamp01(t01) * 0.2f);
            else
                r.arms.Animate("SwordStab", "SwordOther", Mathf.Clamp01(t01) * 0.2f, Mathf.Clamp01(0.4f * (t01 - 1)) * 0.2f);

            r.body.Arms.localRotation = Quaternion.Euler(0, armSwingRotationAngle * Mathf.InverseLerp(0.2f, 0.3f, t01) * (IsRightArm ? 1 : -1), 0);
            SetArmRotation();

            yield return r.WaitForUpdateRobot();
            timer += r.dt;

            if (r.Opponent != null)
                r.arms.IdleRotateTowardsOpponent();
        }

        CanJumpCrouch = false;

        r.legs.LockPosition(true);
        r.legs.LockRotation(true);
        Sword.GetComponent<BoxCollider>().enabled = true;

        Vector3 attackDir = r.inputDirection.normalized * Mathf.Sign(r.inputDirection.z);

        riseAttack = r.inputDirection.z < 0;
        string attackAnimation = riseAttack ? "SwordRise" : "SwordStab";

        r.physics.Dash(Mathf.Clamp01(r.inputDirection.z) * (r.arms.StanceGrounded() ? 1 : 0.5f), r.inputDirection == Vector3.zero ? Vector3.forward : attackDir);

        Quaternion prevRot = r.rotation;
        Quaternion newRot = Quaternion.LookRotation(r.transform.TransformDirection(Vector3.RotateTowards(Vector3.forward, attackDir, Mathf.Deg2Rad * maxAttackYaw, 0)), r.normalUp);

        timer = 0;

        while (timer < holdTime * (riseAttack ? 1.5f : 1))
        {
            float t01 = Mathf.Clamp01(0.2f + 0.8f * timer / holdTime);
            float stabLerp = Mathf.InverseLerp(0.2f, 0.5f, t01);

            r.arms.Animate(IsRightArm ? "SwordOther" : attackAnimation, IsRightArm ? attackAnimation : "SwordOther", t01);

            if (r.Opponent != null && (riseAttack || AttackCanAutoAim()))
                r.RotateTowardsOpponent(90);
            else
                r.Rotate(Quaternion.Slerp(prevRot, newRot, stabLerp));

            r.body.Arms.localRotation = Quaternion.Euler(0, stabLerp * armSwingRotationAngle * (IsRightArm ? -1 : 1), 0);
            SetArmRotation();

            yield return r.WaitForUpdateRobot();
            timer += r.dt;

            if (riseAttack && !r.physics.fixedVelocity && t01 > 0.6f)
            {
                r.physics.fixedVelocity = true;
                r.physics.Jump(r.IsGrounded() ? 0.75f : 0);
            }
        }
        r.physics.fixedVelocity = false;

        timer = 0;
        while (timer < endLagTime)
        {
            float t01 = timer / endLagTime;

            r.arms.Animate(IsRightArm ? null : "SwordStab", IsRightArm ? "SwordStab" : null, Mathf.InverseLerp(1, 0.2f, t01));

            r.body.Arms.localRotation = Quaternion.Euler(0, Mathf.InverseLerp(1, 0.8f, t01) * armSwingRotationAngle * (IsRightArm ? -1 : 1), 0);
            SetArmRotation();

            yield return r.WaitForUpdateRobot();
            timer += r.dt;
        }

        Sword.gameObject.SetActive(false);

        r.arms.Animate("Idle", r.arms.GetIdleAnimationLerp());

        r.body.LeftArm.localRotation = Quaternion.identity;
        r.body.RightArm.localRotation = Quaternion.identity;
        r.body.Arms.localRotation = Quaternion.identity;

        r.legs.LockPosition(false);
        r.legs.LockRotation(false);

        CanCancelAttack = false;
        FinishedAttack = true;
    }

    public override void CancelAttackSequence()
    {
        base.CancelAttackSequence();

        r.physics.fixedVelocity = false;

        Sword.gameObject.SetActive(false);

        r.body.LeftArm.localRotation = Quaternion.identity;
        r.body.RightArm.localRotation = Quaternion.identity;
        r.body.Arms.localRotation = Quaternion.identity;

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
            if (riseAttack)
                CanCancelAttack = true;

            r.Opponent.arms.EnterHitStun(new System.Func<bool>(() => r.Opponent.physics.GetHorizontalSpeed() < 2), riseAttack ? 0.5f : 1);
            
            r.Opponent.physics.Launch(transform.up * (riseAttack ? 25 : 50) + r.normalUp * (riseAttack ? 80 : 40));
        }
        else
            r.Opponent.physics.Launch(transform.up * 50);

        fixedFrames = 4;

        return default;
    }

    void SetArmRotation()
    {
        r.body.LeftArm.localRotation = Quaternion.Euler(
            Mathf.MoveTowardsAngle(-r.body.UpperBody.localEulerAngles.x, 0, r.body.UpperBody.localEulerAngles.x < 180 || r.IsGrounded() ? 0 : maxAttackPitch), 
            r.body.Arms.localEulerAngles.y > 180 ? 0 : -r.body.Arms.localEulerAngles.y, 
            0);

        r.body.RightArm.localRotation = Quaternion.Euler(
            Mathf.MoveTowardsAngle(-r.body.UpperBody.localEulerAngles.x, 0, r.body.UpperBody.localEulerAngles.x < 180 || r.IsGrounded() ? 0 : maxAttackPitch),
            r.body.Arms.localEulerAngles.y < 180 ? 0 : -r.body.Arms.localEulerAngles.y, 
            0);
    }
}
