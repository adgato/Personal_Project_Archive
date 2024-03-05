using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
[RequireComponent(typeof(FightingRobot))]
public class ArmAnimation
{
    private RobotBody r;

    private readonly List<string> animationNames = new List<string>()
    {
        "Shielding",
        "Idle",
        "Lasering",
        "SwordStab", "SwordOther", "SwordRise",
        "BlasterShoot", "BlasterOther",
        "CannonShoot", "CannonOther",
        "SwordSlash", "SwordUnwrapStart", "SwordWrapStart", "SlashOther"
    };
    private Dictionary<string, AnimationData> Animations;
    private float animationTime01;

    private enum ActionState
    {
        HitStun,
        Idle,
        ShieldUp, Shielding, ShieldDown,
        Dash,
        LaserOn, Lasering, LaserOff,
        LeftAttack,
        RightAttack
    }
    private enum StanceState
    {
        Standing,
        CrouchDown, Crouching, CrouchUp,
        Jumping, Projectile, Landing
    }

    private ActionState actionState = ActionState.Idle;
    private StanceState stanceState = StanceState.Standing;
    private bool finishedActionState;
    private bool finishedStanceState;

    [SerializeField] private float fullShieldWidth;
    [SerializeField] private float shieldDrainRate;
    [SerializeField] private float shieldChargeRate;

    [SerializeField] private float fullLaserLength;
    [SerializeField] private float laserDrainRate;
    [SerializeField] private float laserChargeRate;

    [SerializeField] private float laserOnSpeed;
    [SerializeField] private float laserOffSpeed;

    [SerializeField] private float dashChargeRate;

    [SerializeField] private float crouchSpeed;

    private System.Func<bool> CanBreakOutOfHitStun;
    private float TimeTillCanBreakOutOfHitStun = 0;
    public bool InLaserHitStun { get; private set; }

    private float maxShieldWidth01 = 1;
    private float shieldWidth01 = 0;
    private float shieldBrightness = 1;

    private float maxLaserLength01 = 1;
    private Vector2 laserActiveRange01 = Vector2.zero;

    [SerializeField] private float dashCharge01 = 0;
    [SerializeField] private float dashAgainTime = 0.5f;
    private float timeSinceDash = float.MaxValue;
    private float jumpCharge01 = 0;

    [SerializeField] private MeshRenderer shieldRenderer;
    public BoxCollider shieldHitBox { get; private set; }


    // Start is called before the first frame update
    public void Start(RobotBody robot)
    {
        r = robot;

        shieldRenderer.sharedMaterial = new Material(shieldRenderer.sharedMaterial);
        shieldRenderer.sharedMaterial.SetFloat("_MaxShieldWidth", fullShieldWidth);
        shieldHitBox = shieldRenderer.GetComponent<BoxCollider>();

        Animations = new Dictionary<string, AnimationData>();
        foreach (string key in animationNames)
            Animations.Add(key, JsonSaver.LoadResource<AnimationData>("Animations/" + key));
    }

    // Update is called once per frame
    public void Update()
    {
        UpdateState();
    }

    public void FixedUpdate()
    {
        ExecuteState();

        timeSinceDash += r.dt;
        if (r.Controller.GetButton(Controller.Inputs.RB))
            ChargeDash();
        else if (!InHitStun())
            dashCharge01 = 0;

        //Charge shield and laser
        if (!Shielding())
            UseShield(shieldChargeRate * r.dt, 0);
        if (!Lasering())
            UseLaser(laserChargeRate * r.dt, 0, 0);
    }

    void UpdateState()
    {
        switch (actionState)
        {
            case ActionState.HitStun:
                if (finishedActionState)
                {
                    actionState = ActionState.Idle;
                    animationTime01 = GetIdleAnimationLerp();
                }
                break;

            case ActionState.Idle:
                if (StanceGrounded() && r.Controller.GetButton(Controller.Inputs.LB))
                    actionState = ActionState.ShieldUp;
                else if (!Dashing() && (r.Controller.GetButtonUp(Controller.Inputs.RB) || dashCharge01 > 0 && !r.Controller.GetButton(Controller.Inputs.RB)))
                    actionState = ActionState.Dash;
                else if (maxLaserLength01 > 0.25f && r.Controller.GetButton(Controller.Inputs.B))
                    actionState = ActionState.LaserOn;
                else if (r.Controller.GetButton(Controller.Inputs.A))
                    actionState = ActionState.RightAttack;
                else if (r.Controller.GetButton(Controller.Inputs.Y))
                    actionState = ActionState.LeftAttack;
                else
                    break;
                if (r.rightAttackAction.CanCancelAttack)
                    r.rightAttackAction.CancelAttackSequence();
                else if (r.leftAttackAction.CanCancelAttack)
                    r.leftAttackAction.CancelAttackSequence();
                if (actionState == ActionState.RightAttack)
                    r.rightAttackAction.Begin();
                else if (actionState == ActionState.LeftAttack)
                    r.leftAttackAction.Begin();
                break;

            case ActionState.Dash:
                if (finishedActionState)
                {
                    actionState = ActionState.Idle;
                    animationTime01 = GetIdleAnimationLerp();
                }
                break;

            case ActionState.ShieldUp:
                if (!r.Controller.GetButton(Controller.Inputs.LB))
                    actionState = ActionState.ShieldDown;
                else if (finishedActionState)
                    actionState = ActionState.Shielding;
                break;

            case ActionState.Shielding:
                if (r.Controller.GetButtonUp(Controller.Inputs.LB) || finishedActionState)
                    actionState = ActionState.ShieldDown;
                break;

            case ActionState.ShieldDown:
                if (r.Controller.GetButton(Controller.Inputs.LB))
                    actionState = ActionState.ShieldUp;
                else if (finishedActionState)
                {
                    actionState = ActionState.Idle;
                    animationTime01 = GetIdleAnimationLerp();
                }
                break;

            case ActionState.LaserOn:
                if (finishedActionState)
                    actionState = ActionState.Lasering;
                break;

            case ActionState.Lasering:
                if (!r.Controller.GetButton(Controller.Inputs.B) || finishedActionState)
                    actionState = ActionState.LaserOff;
                break;

            case ActionState.LaserOff:
                if (finishedActionState)
                {
                    actionState = ActionState.Idle;
                    animationTime01 = GetIdleAnimationLerp();
                }
                break;

            case ActionState.LeftAttack:
                if (finishedActionState)
                {
                    actionState = ActionState.Idle;
                    animationTime01 = GetIdleAnimationLerp();
                }
                break;

            case ActionState.RightAttack:
                if (finishedActionState)
                {
                    actionState = ActionState.Idle;
                    animationTime01 = GetIdleAnimationLerp();
                }
                break;
        }

        finishedActionState = false;

        int rank = GetActionRank();
        bool bluePass = rank < 3;
        bool redPass = rank < 2;

        switch (stanceState)
        {
            case StanceState.Standing:
                //if dashing then crouch when finished if holding x, so dash cancelling crouching is easier
                if (bluePass && (r.Controller.GetButtonDown(Controller.Inputs.X) || !Dashing() && r.Controller.GetButton(Controller.Inputs.X)))
                    stanceState = StanceState.CrouchDown;
                break;

            case StanceState.CrouchDown:
                if (redPass && !r.Controller.GetButton(Controller.Inputs.X))
                {
                    jumpCharge01 = 0.75f; //short hop
                    stanceState = StanceState.Jumping;
                }
                else if (finishedStanceState)
                    stanceState = StanceState.Crouching;
                break;

            case StanceState.Crouching:
                if (!r.Controller.GetButton(Controller.Inputs.X))
                {
                    if (redPass)
                    {
                        jumpCharge01 = 1; //full hop
                        stanceState = StanceState.Jumping;
                    }
                    else if (bluePass)
                        stanceState = StanceState.CrouchUp;
                }
                else if (actionState == ActionState.Dash)
                    stanceState = StanceState.CrouchUp;
                break;

            case StanceState.CrouchUp:
                if (finishedStanceState)
                    stanceState = StanceState.Standing;
                break;

            case StanceState.Jumping:
                if (finishedStanceState)
                    stanceState = StanceState.Projectile;
                break;

            case StanceState.Projectile:
                if (r.IsGrounded())
                    stanceState = StanceState.Landing;
                break;

            case StanceState.Landing:
                if (redPass && r.Controller.GetButton(Controller.Inputs.X))
                    stanceState = StanceState.CrouchDown;
                else if (finishedStanceState)
                    stanceState = StanceState.CrouchUp;
                break;
        }

        finishedStanceState = false;
    }

    void ExecuteState()
    {
        switch (actionState)
        {
            case ActionState.HitStun:
                TimeTillCanBreakOutOfHitStun -= r.dt;
                if (CanBreakOutOfHitStun() || TimeTillCanBreakOutOfHitStun < 0)
                {
                    InLaserHitStun = false;
                    r.eyes.SetEyesEnabled(true);
                    r.legs.LockPosition(false);
                    r.legs.LockRotation(false);
                    finishedActionState = true;
                }
                break;

            case ActionState.Idle:
                if (r.leftAttackAction.CanCancelAttack || r.rightAttackAction.CanCancelAttack)
                    break;
                Animate("Idle", Mathf.MoveTowards(animationTime01, GetIdleAnimationLerp(), 0.1f));
                IdleRotateTowardsOpponent();
                break;

            case ActionState.Dash:
                r.physics.Dash(GetDashSpeed(), r.inputDirection.normalized);
                timeSinceDash = 0;
                finishedActionState = true;
                break;

            case ActionState.ShieldUp:
                UseShield(-shieldDrainRate * r.dt, 25 * r.dt);
                if (shieldWidth01 > 0)
                    TryRotateTowardsOpponent(180);
                Animate("Shielding", shieldWidth01);
                if (shieldWidth01 == maxShieldWidth01)
                    finishedActionState = true;
                break;

            case ActionState.Shielding:
                UseShield(-shieldDrainRate * r.dt, 0);
                r.legs.LookTowardsOpponent(40, 1);
                Animate("Shielding", shieldWidth01);
                if (maxShieldWidth01 == 0)
                    finishedActionState = true;
                break;

            case ActionState.ShieldDown:
                UseShield(-shieldDrainRate * r.dt, -12.5f * r.dt);
                Animate("Shielding", shieldWidth01);
                if (shieldWidth01 == 0)
                    finishedActionState = true;
                break;

            case ActionState.LaserOn:
                if (laserActiveRange01.y == 0)
                    r.eyes.SetLasersEnabled(true);
                UseLaser(-laserDrainRate * r.dt, 0, laserOnSpeed * r.dt);
                TryRotateTowardsOpponent(180);
                Animate("Lasering",  laserActiveRange01.y);
                if (laserActiveRange01.y == 1)
                    finishedActionState = true;
                break;

            case ActionState.Lasering:
                UseLaser(-laserDrainRate * r.dt, 0, 0);
                Animate("Lasering", 1);
                if (maxLaserLength01 == 0)
                    finishedActionState = true;
                break;

            case ActionState.LaserOff:
                UseLaser(-laserDrainRate * r.dt, laserOffSpeed * r.dt, 0);
                if (laserActiveRange01.x == 1)
                {
                    r.eyes.SetLasersEnabled(false);
                    laserActiveRange01 = Vector2.zero;
                    finishedActionState = true;
                }
                break;

            case ActionState.LeftAttack:
                if (r.leftAttackAction.FinishedAttack || r.leftAttackAction.CanCancelAttack)
                    finishedActionState = true;
                break;

            case ActionState.RightAttack:
                if (r.rightAttackAction.FinishedAttack || r.rightAttackAction.CanCancelAttack)
                    finishedActionState = true;
                break;
        }

        switch (stanceState)
        {
            case StanceState.Standing:
                break;

            case StanceState.CrouchDown:
                r.legs.ChangeHipHeight(-crouchSpeed * r.dt, -0.15f, 0);
                if (r.legs.GetHipHeight() == -0.15f)
                    finishedStanceState = true;
                break;

            case StanceState.Crouching:
                break;

            case StanceState.CrouchUp:
                r.legs.ChangeHipHeight(crouchSpeed * r.dt, -0.15f, 0);
                if (r.legs.GetHipHeight() == 0)
                    finishedStanceState = true;
                break;

            case StanceState.Jumping:
                r.legs.ChangeHipHeight(crouchSpeed * r.dt, -0.15f, 0.05f);
                if (jumpCharge01 != 0)
                    r.physics.Jump(jumpCharge01);
                jumpCharge01 = 0;
                if (r.legs.GetHipHeight() == 0.05f)
                    finishedStanceState = true;
                break;

            case StanceState.Projectile:
                r.legs.ChangeHipHeight(-crouchSpeed / 5 * r.dt, 0, 0.05f);
                break;

            case StanceState.Landing:
                r.legs.ChangeHipHeight(-crouchSpeed * r.dt, -0.15f, 0.05f);
                if (r.legs.GetHipHeight() == -0.15f)
                    finishedStanceState = true;
                break;
        }
    }

    /// <summary>
    /// Correctly put robot into HitStun from any action state
    /// </summary>
    /// <param name="until">Condition function to break out of being in HitStun</param>
    /// <param name="timeLimit">After timeLimit seconds, break out of being in HitStun</param>
    public void EnterHitStun(System.Func<bool> until, float timeLimit = float.MaxValue)
    {
        TimeTillCanBreakOutOfHitStun = Mathf.Max(TimeTillCanBreakOutOfHitStun, timeLimit);
        CanBreakOutOfHitStun = until;

        switch (actionState)
        {
            case ActionState.Idle:
                break;

            case ActionState.Dash:
                break;

            case ActionState.ShieldUp:
                UseShield(0, -1);
                break;

            case ActionState.Shielding:
                UseShield(0, -1);
                break;

            case ActionState.ShieldDown:
                UseShield(0, -1);
                break;

            case ActionState.LaserOn:
                r.eyes.SetLasersEnabled(false);
                laserActiveRange01 = Vector2.zero;
                break;

            case ActionState.Lasering:
                r.eyes.SetLasersEnabled(false);
                laserActiveRange01 = Vector2.zero;
                break;

            case ActionState.LaserOff:
                r.eyes.SetLasersEnabled(false);
                laserActiveRange01 = Vector2.zero;
                break;

            case ActionState.LeftAttack:
                r.leftAttackAction.CancelAttackSequence();
                break;

            case ActionState.RightAttack:
                r.rightAttackAction.CancelAttackSequence();
                break;
        }

        actionState = ActionState.HitStun;
        InLaserHitStun = r.Opponent != null && r.Opponent.arms.Lasering();

        r.eyes.SetEyesEnabled(false);
        r.legs.LockPosition(true);
        r.legs.LockRotation(true);
        r.arms.Animate("Idle", 0.5f);
    }

    public void Animate(string animation, float atTime)
    {
        Animate(animation, animation, atTime, atTime);
        animationTime01 = atTime;
    }
    public void Animate(string leftAnimation, string rightAnimation, float atTime)
    {
        Animate(leftAnimation, rightAnimation, atTime, atTime);
        animationTime01 = atTime;
    }
    public void Animate(string leftAnimation, string rightAnimation, float leftTime, float rightTime)
    {
        if (leftAnimation != null)
            Animations[leftAnimation].Evaluate(leftTime, true, ref r.body.LeftElbow, ref r.body.LeftHand);
        if (rightAnimation != null)
            Animations[rightAnimation].Evaluate(rightTime, false, ref r.body.RightElbow, ref r.body.RightHand);
    }

    public Collider[] GetCurrentHitBoxes()
    {
        if (Lasering())
            return r.eyes.GetLaserHitBox();
        else if (actionState == ActionState.LeftAttack || r.leftAttackAction.CanCancelAttack)
            return r.leftAttackAction.GetHitBoxes();
        else if (actionState == ActionState.RightAttack || r.rightAttackAction.CanCancelAttack)
            return r.rightAttackAction.GetHitBoxes();
        return new Collider[0] { };
    }

    public void HandleHit(CollisionWith collisionWith, out int fixedFrames)
    {
        fixedFrames = 0;

        if (r.Opponent == null)
        {
            Debug.LogError("Error: HandleHit should not be called if there is no opponent.");
            return;
        }

        if (collisionWith == CollisionWith.Nothing)
            return;
        else if (collisionWith == CollisionWith.Shield)
        {
            r.Opponent.arms.shieldBrightness = Lasering() ? 0 : 3;
            r.Opponent.arms.shieldRenderer.sharedMaterial.SetFloat("_Brightness", r.Opponent.arms.shieldBrightness);
        }

        if (Lasering())
            r.Opponent.arms.EnterHitStun(new System.Func<bool>(() => false), 1);
        else if (actionState == ActionState.LeftAttack || r.leftAttackAction.CanCancelAttack)
            r.leftAttackAction.HandleHit(collisionWith, out fixedFrames);
        else if (actionState == ActionState.RightAttack || r.rightAttackAction.CanCancelAttack)
            r.rightAttackAction.HandleHit(collisionWith, out fixedFrames);
    }

    void UseShield(float changeMaxWidth, float changeWidth)
    {
        maxShieldWidth01 = Mathf.Clamp01(maxShieldWidth01 + changeMaxWidth);
        shieldWidth01 = Mathf.Clamp(shieldWidth01 + changeWidth, 0, maxShieldWidth01);

        shieldRenderer.enabled = shieldWidth01 != 0;
        shieldHitBox.enabled = shieldWidth01 != 0;

        shieldBrightness = Mathf.Lerp(shieldBrightness, 1, 25 * r.dt);
        shieldRenderer.sharedMaterial.SetFloat("_Brightness", shieldBrightness);

        shieldRenderer.sharedMaterial.SetFloat("_ShieldWidth01", shieldWidth01);
        shieldHitBox.size = new Vector3(0.58f * shieldWidth01, shieldHitBox.size.y, shieldHitBox.size.z);
    }
    void UseLaser(float changeMaxLength, float changeNearActiveLength, float changeFarActiveLength)
    {
        maxLaserLength01 = Mathf.Clamp01(maxLaserLength01 + changeMaxLength);
        laserActiveRange01.x = Mathf.Clamp01(laserActiveRange01.x + changeNearActiveLength);
        laserActiveRange01.y = Mathf.Clamp01(laserActiveRange01.y + changeFarActiveLength);

        r.eyes.SetEyeBrightness(changeMaxLength > 0 ? 0.25f * (1 + 3 * Mathf.Sqrt(Mathf.Max(0, 1 - 16 / 9f * (1 - maxLaserLength01) * (1 - maxLaserLength01)))) : Mathf.Lerp(16, 1, laserActiveRange01.x));
        r.eyes.SetLaserScale(maxLaserLength01 * fullLaserLength);
        r.eyes.SetActiveDistanceRange(laserActiveRange01);
    }
    public float GetLaserRange()
    {
        return fullLaserLength * maxLaserLength01;
    }

    public bool InHitStun()
    {
        return actionState == ActionState.HitStun;
    }
    public bool Shielding()
    {
        return actionState == ActionState.Shielding || actionState == ActionState.ShieldUp || actionState == ActionState.ShieldDown;
    }
    public bool Lasering()
    {
        return actionState == ActionState.LaserOn || actionState == ActionState.Lasering || actionState == ActionState.LaserOff;
    }
    public bool Attacking()
    {
        return actionState == ActionState.LeftAttack || r.leftAttackAction.CanCancelAttack || actionState == ActionState.RightAttack || r.rightAttackAction.CanCancelAttack;
    }
    public bool Dashing()
    {
        return timeSinceDash < dashAgainTime && r.physics.GetHorizontalSpeed() > 0.3f;
    }

    public bool StanceGrounded()
    {
        return !(stanceState == StanceState.Jumping || stanceState == StanceState.Projectile || stanceState == StanceState.Landing);
    }

    void ChargeDash()
    {
        dashCharge01 = Mathf.Min(1, dashCharge01 + dashChargeRate * r.dt);
    }

    public float GetDashSpeed()
    {
        return Mathx.Square(Mathf.Sin(Mathf.PI * 0.5f * dashCharge01));
    }

    /// <returns>3 for lasering, 2 for shield, 1 for no action or attacks</returns>
    int GetActionRank()
    {
        return actionState == ActionState.Idle || actionState == ActionState.Dash || 
            actionState == ActionState.LeftAttack && r.leftAttackAction.CanJumpCrouch || 
            actionState == ActionState.RightAttack && r.rightAttackAction.CanJumpCrouch ? 1 :

            actionState == ActionState.ShieldDown || actionState == ActionState.Shielding || actionState == ActionState.ShieldUp ? 2 :

            3;
    }

    public float GetIdleAnimationLerp()
    {
        return stanceState == StanceState.CrouchDown || stanceState == StanceState.Crouching ? 0 :
            stanceState == StanceState.Standing || stanceState == StanceState.CrouchUp ? 0.5f : 
            1;
    }


    public void IdleRotateTowardsOpponent()
    {
        float t01 = Mathf.Min(r.inputDirection.sqrMagnitude, 1);
        r.legs.LookTowardsOpponent(50 * t01, 1);
        TryRotateTowardsOpponent(180 * t01);
    }
    /// <summary>
    /// RotateTowardsOpponent but only if in standing or crouching states
    /// </summary>
    public void TryRotateTowardsOpponent(float maxDegreesDelta)
    {
        if (r.IsGrounded())
            r.RotateTowardsOpponent(maxDegreesDelta);
    }
}
