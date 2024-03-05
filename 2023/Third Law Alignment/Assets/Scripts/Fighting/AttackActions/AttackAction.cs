using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class AttackAction : MonoBehaviour
{

    private bool handledHitThisAttack;

    [SerializeField] private float rangeToAutoAim;
    [SerializeField] private Collider[] permenantHitBoxes;

    private Coroutine attackSequence;

    protected RobotBody r { get; private set; }
    protected bool IsRightArm { get; private set; }
    public bool CanCancelAttack { get; protected set; } = false;
    public bool FinishedAttack { get; protected set; } = false;
    public bool CanJumpCrouch { get; protected set; } = false;


    //Here as a reminder to not implement it in derived classes
    protected void Start() { }

    public virtual void Init(RobotBody robot, bool rightArm)
    {
        r = robot;
        IsRightArm = rightArm;
    }

    public void Begin()
    {
        CanCancelAttack = false;
        FinishedAttack = false;
        handledHitThisAttack = false;
        if (attackSequence != null)
            StopCoroutine(attackSequence);
        attackSequence = StartCoroutine(AttackSequence());
    }

    protected abstract IEnumerator AttackSequence();

    public virtual void CancelAttackSequence()
    {
        CanCancelAttack = false;
        StopCoroutine(attackSequence);
    }

    /// <returns>True if already handled hit.</returns>
    public virtual bool HandleHit(CollisionWith collisionWith, out int fixedFrames)
    {
        fixedFrames = 0;
        if (r.Opponent == null || handledHitThisAttack)
            return true;
        handledHitThisAttack = true;
        return false;
    }

    /// <summary>Allow the attack to hit again</summary>
    protected void UnhandleHit()
    {
        handledHitThisAttack = false;
    }

    public virtual Collider[] GetHitBoxes()
    {
        return permenantHitBoxes;
    }

    protected bool AttackCanAutoAim()
    {
        if (r.Opponent == null)
            return true;
        Vector3 opponentRelativePos = r.Opponent.position - r.position;
        return opponentRelativePos.sqrMagnitude < Mathx.Square(rangeToAutoAim) && Vector3.Dot(opponentRelativePos, r.transform.forward) > 0;
    }
}
