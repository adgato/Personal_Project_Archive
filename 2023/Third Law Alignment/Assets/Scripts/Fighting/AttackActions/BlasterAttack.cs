using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlasterAttack : AttackAction
{
    [SerializeField] private float startUpTime;
    [SerializeField] private float shootTime;
    [SerializeField] private float endLagTime;
    [SerializeField] private float aimTime;
    [SerializeField] private float bulletSpeed;
    [SerializeField] private Vector2 bulletBrightnessMinMax;

    [SerializeField] private float armSwingRotationAngle = 20f;

    private Transform Blaster;
    private Transform Bullet;
    private Material bulletMat;
    private Vector3 bulletVelocity;
    private Vector3 bulletPosition;
    private Quaternion bulletRotation;
    private float bulletTimer;

    public override void Init(RobotBody robot, bool isRightArm)
    {
        base.Init(robot, isRightArm);

        Blaster = transform.GetChild(0);
        Bullet = transform.GetChild(1);

        MeshRenderer bulletRenderer = Bullet.GetChild(0).GetComponent<MeshRenderer>();
        bulletMat = new Material(bulletRenderer.sharedMaterial);
        bulletRenderer.sharedMaterial = bulletMat;
    }

    protected override IEnumerator AttackSequence()
    {
        Blaster.gameObject.SetActive(true);

        bool firstShot = true;

        float timer;

        bool pressingShoot = true;
        while (pressingShoot)
        {
            CanCancelAttack = false;
            CanJumpCrouch = false;
            Bullet.parent = transform;
            Bullet.gameObject.SetActive(false);

            if (!firstShot)
                r.body.Arms.localRotation = Quaternion.Euler(0, armSwingRotationAngle * (IsRightArm ? -1 : 1), 0);

            float aim01 = 0;
            timer = 0;
            while (timer < startUpTime || pressingShoot)
            {
                float t01 = Mathf.Clamp01(timer / shootTime);

                r.arms.IdleRotateTowardsOpponent();

                if (firstShot)
                    r.body.Arms.localRotation = Quaternion.Euler(0, t01 * armSwingRotationAngle * (IsRightArm ? -1 : 1), 0);

                r.arms.Animate(IsRightArm ? "BlasterOther" : "BlasterShoot", IsRightArm ? "BlasterShoot" : "BlasterOther", 0);

                yield return r.WaitForUpdateRobot();
                timer += r.dt;

                pressingShoot = r.Controller.GetButton(IsRightArm ? Controller.Inputs.A : Controller.Inputs.Y);

                aim01 = Mathf.Clamp01(aim01 + r.dt / aimTime * (AttackCanAutoAim() ? 1 : -1));
                AimBlaster(aim01);
            }

            CanCancelAttack = !firstShot;

            Quaternion armRotation = IsRightArm ? r.body.RightArm.localRotation : r.body.LeftArm.localRotation;

            if (r.Controller.GetButton(Controller.Inputs.RB))
                r.physics.Dash(0, r.inputDirection.normalized);

            UnhandleHit();
            ShootBlaster();

            timer = 0;
            while (timer < shootTime || pressingShoot && timer < shootTime + endLagTime)
            {
                float t01 = Mathf.Clamp01(timer / (shootTime + endLagTime));

                r.arms.IdleRotateTowardsOpponent();
                r.body.Arms.localRotation = Quaternion.Euler(0, Mathf.Lerp(1, 0.9f, t01) * armSwingRotationAngle * (IsRightArm ? -1 : 1), 0);

                r.arms.Animate(IsRightArm ? "BlasterOther" : "BlasterShoot", IsRightArm ? "BlasterShoot" : "BlasterOther", Mathf.Sqrt(t01));

                if (IsRightArm)
                    r.body.RightArm.localRotation = armRotation;
                else
                    r.body.LeftArm.localRotation = armRotation;

                UpdateBullet();
                yield return r.WaitForUpdateRobot();
                timer += r.dt;

                pressingShoot |= r.Controller.GetButton(IsRightArm ? Controller.Inputs.A : Controller.Inputs.Y);
            }
            firstShot = false;
        }

        timer = 0;
        while (timer < endLagTime)
        {
            r.arms.IdleRotateTowardsOpponent();
            r.body.Arms.localRotation = Quaternion.Euler(0, Mathf.Lerp(0.9f, 0, timer / endLagTime) * armSwingRotationAngle * (IsRightArm ? -1 : 1), 0);

            r.arms.Animate("Idle", r.arms.GetIdleAnimationLerp());
            UpdateBullet();
            yield return r.WaitForUpdateRobot();
            timer += r.dt;
        }

        Bullet.parent = transform;
        Bullet.gameObject.SetActive(false);
        Blaster.gameObject.SetActive(false);

        r.body.LeftArm.localRotation = Quaternion.identity;
        r.body.RightArm.localRotation = Quaternion.identity;
        r.body.Arms.localRotation = Quaternion.identity;

        CanCancelAttack = false;
        FinishedAttack = true;
    }

    public override void CancelAttackSequence()
    {
        base.CancelAttackSequence();

        Bullet.parent = transform;
        Bullet.gameObject.SetActive(false);
        Blaster.gameObject.SetActive(false);

        r.body.LeftArm.localRotation = Quaternion.identity;
        r.body.RightArm.localRotation = Quaternion.identity;
        r.body.Arms.localRotation = Quaternion.identity;
    }

    /// <returns>Nothing of significance, just a way for the base class method to communicate with the derived.</returns>
    public override bool HandleHit(CollisionWith collisionWith, out int fixedFrames)
    {
        if (base.HandleHit(collisionWith, out fixedFrames))
            return default;

        if (collisionWith == CollisionWith.HurtBox)
        {
            r.Opponent.physics.Jump(0);
            r.Opponent.arms.EnterHitStun(new System.Func<bool>(() => false), startUpTime * 2);
        }
        
        fixedFrames = 0;

        Bullet.parent = transform;
        Bullet.gameObject.SetActive(false);


        return default;
    }

    void AimBlaster(float t01)
    {
        if (IsRightArm)
        {
            Vector3 aimDir = r.Opponent == null ? -r.transform.forward : r.body.RightArm.position - r.Opponent.body.Head.position;
            r.body.RightArm.rotation = Quaternion.Slerp(r.body.RightArm.transform.parent.rotation, Quaternion.LookRotation(aimDir, Vector3.ProjectOnPlane(r.normalUp, aimDir)), t01);
        }
        else
        {
            Vector3 aimDir = r.Opponent == null ? -r.transform.forward : r.body.LeftArm.position - r.Opponent.body.Head.position;
            r.body.LeftArm.rotation = Quaternion.Slerp(r.body.LeftArm.transform.parent.rotation, Quaternion.LookRotation(aimDir, Vector3.ProjectOnPlane(r.normalUp, aimDir)), t01);
        }

        r.legs.LookTowardsOpponent(45, t01);
    }

    void UpdateBullet()
    {
        if (bulletTimer == -1)
            return;
        else if (bulletTimer > shootTime + endLagTime)
        {
            bulletTimer = -1;
            Bullet.parent = transform;
            Bullet.gameObject.SetActive(false);
            return;
        }


        bulletPosition += bulletVelocity * r.dt;
        Bullet.SetPositionAndRotation(bulletPosition, bulletRotation);
        Bullet.GetChild(0).localScale = new Vector3(0.5f, 0.5f, Mathf.Lerp(5, 25, Mathx.Square(Mathf.Sin(bulletTimer / (shootTime + endLagTime) * Mathf.PI))));
        bulletMat.SetFloat("_Brightness", bulletTimer == 0 ? bulletBrightnessMinMax.y : bulletBrightnessMinMax.x);
        bulletTimer += r.dt;
    }

    void ShootBlaster()
    {
        Bullet.gameObject.SetActive(true);
        Bullet.parent = transform.root;
        Bullet.GetChild(0).localScale = new Vector3(0.5f, 0.5f, 5);
        bulletRotation = transform.rotation;
        bulletVelocity = bulletSpeed * -transform.up;
        bulletPosition = transform.position - bulletVelocity * r.dt; //so the first frame is on the blaster
        bulletTimer = 0;
    }
}
