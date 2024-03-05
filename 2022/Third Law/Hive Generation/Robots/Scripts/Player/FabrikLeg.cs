using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FabrikLeg : MonoBehaviour
{
    [SerializeField] private AudioClip carpetSound;
    [SerializeField] private Transform Chest;
    private bool stepped = false;

    [SerializeField] private Transform[] Limbs;

    [SerializeField] private Transform[] Ligaments;

    [SerializeField] private float limbLength;
    private float usedLimbLength;

    [SerializeField] private bool inferior; //we all know right is inferior to left :)
    private bool usedInferior;
    [SerializeField] private FabrikLeg superior;

    public Vector3 prevTarget;
    public Vector3 midTarget;
    public Vector3 endTarget;

    private Vector3 lerpTarget;


    private readonly float syncCycle = 1;

    public float walkCycle { get; private set; }

    [SerializeField] private float lerpSpeed;

    private int limbNum;
    public Vector3 deltaPos;


    void Start()
    {
        usedInferior = inferior;
        usedLimbLength = limbLength;

        limbNum = Limbs.Length;

        endTarget = Limbs[0].position;
        midTarget = endTarget;
        prevTarget = endTarget;
        walkCycle = 0;
    }


    void FixedUpdate()
    {
        //prev,mid,end Target updated again in CollsionDetection.GetSafeMotion()

        //Re-synchronise inferior leg with superior leg when then superior leg is directly below the hip stepping forwards
        //Set the inferior leg to directly below the hip moving backwards as the player walks
        if (usedInferior && Mathf.Abs(superior.walkCycle - syncCycle) < 0.1f)
        {
            walkCycle = 2;
            Physics.Raycast(Limbs[limbNum - 1].position, -transform.up, out RaycastHit belowHip);

            endTarget = belowHip.point;
        }

        if (walkCycle > 2 && !stepped)
        {
            stepped = true;
            if (carpetSound != null)
                GetComponent<AudioSource>().PlayOneShot(carpetSound);
        }
        else if (walkCycle <= 2)
            stepped = false;
        
        TargetUpdate();

        walkCycle += Time.fixedDeltaTime * 100 / lerpSpeed;

        if (walkCycle < 1)
            lerpTarget = Vector3.Lerp(prevTarget, midTarget, walkCycle);
        else
            lerpTarget = Vector3.Lerp(midTarget, endTarget, walkCycle - 1);

        FabrikUpdate(ref Limbs, lerpTarget);
        LigamentUpdate();
    }

    void TargetUpdate()
    {
        Vector3 hipPos = Limbs[limbNum - 1].position;

        Physics.Raycast(hipPos, endTarget - hipPos, out RaycastHit newHit);
        endTarget = newHit.point;

        // If a ray cast downwards from the hip doesn't hit anything, reset the targets to below the hip so the robot has straight legs in the air
        if (!Physics.Raycast(hipPos, -transform.up, out RaycastHit belowHip, usedLimbLength * (limbNum - 1)))
        {
            midTarget = hipPos - transform.up * usedLimbLength * (limbNum - 1);
            endTarget = midTarget;
            usedInferior = false;
        }
        //If the end target is too far behind the hip, adjust the target to step forward
        else if ((endTarget - hipPos).sqrMagnitude > Mathf.Pow(usedLimbLength * (limbNum - 1) + deltaPos.magnitude, 2))
        {
            //Debug.DrawRay(hipPos, -transform.up, Color.white, 1);

            Vector3 idealTarget = endTarget + 2 * (belowHip.point - endTarget);

            RaycastHit targetInfo = new RaycastHit();

            bool flag = false;
            //Try different limb lengths until a valid target is found
            for (usedLimbLength = limbLength; usedLimbLength < limbLength * 1.25f; usedLimbLength += limbLength * 0.05f)
            {
                //Lerp between the ideal target and the hit point below the hip to find a target position
                for (int lerp = 0; lerp < 100; lerp++)
                {
                    Vector3 targetPos = Vector3.Lerp(idealTarget, belowHip.point, lerp / 100f);
                    //Create a target direction that points towards the target and is biased towards the forward-down direction of the character to decrease the distance to the floor
                    Vector3 targetDir = (targetPos - hipPos).normalized * 0.75f + (transform.forward - transform.up) * 0.25f;
                    if (Physics.Raycast(hipPos, targetDir, out targetInfo, usedLimbLength * (limbNum - 1)))
                    {
                        flag = true;
                        //Debug.Log(lerp);
                        //Debug.DrawRay(hipPos, targetDir, Color.red, 1);
                        break;
                    }
                }
                if (flag)
                    break;
            }

            prevTarget = endTarget;
            midTarget = (belowHip.point + hipPos) / 2;
            endTarget = targetInfo.point;

            Limbs[0].transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(Chest.forward, targetInfo.normal), targetInfo.normal);

            walkCycle = 0;
            usedInferior = inferior;
        }

    }

    void FabrikUpdate(ref Transform[] limbs, Vector3 target)
    {
        Vector3 hipPos = limbs[limbNum - 1].position;

        //Set the position of the end node to the target position
        limbs[0].transform.position = target;
        //Update all intermediate joints from end node to hip to move towards target
        for (int i = 1; i < limbNum; i++)
        {
            Vector3 u = (limbs[i].position - limbs[i - 1].position).normalized;
            limbs[i].position = limbs[i - 1].position + u * usedLimbLength;
        }
        //Set the position of the hip joint to its original position
        limbs[limbNum - 1].position = hipPos;
        //Update all intermediate joints from hip to end node to move back to hip
        for (int i = limbNum - 2; i >= 0; i--)
        {
            Vector3 u = (limbs[i].position - limbs[i + 1].position).normalized;

            limbs[i].position = limbs[i + 1].transform.position + u * usedLimbLength;
        }
    }

    void LigamentUpdate()
    {
        for (int i = 0; i < Ligaments.Length; i++)
        {
            Ligaments[i].position = (Limbs[i].position + Limbs[i + 1].position) / 2;
            Ligaments[i].rotation = Quaternion.LookRotation(Limbs[i].position - Limbs[i + 1].position) * Quaternion.Euler(90, 90, 90);
            Ligaments[i].localScale = new Vector3(Ligaments[i].localScale.x, usedLimbLength / 2, Ligaments[i].localScale.z);
        }
    }
}
