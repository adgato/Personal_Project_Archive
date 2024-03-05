using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationBuilder : MonoBehaviour
{
    public RobotBody r;
    public string animationName = "filename";
    public AnimationData currentAnimation;
    public bool animatingLeftArm;
    [Range(0, 1)]
    public float time;



    private void OnValidate()
    {
        if (r == null || true)
            return;
        if (animatingLeftArm)
            currentAnimation.Evaluate(time, true, ref r.body.LeftElbow, ref r.body.LeftHand);
        else
            currentAnimation.Evaluate(time, false, ref r.body.RightElbow, ref r.body.RightHand);
    }

    public void ResetArms()
    {
        r.body.LeftShoulder.localPosition = Vector3.zero;
        r.body.LeftElbow.localPosition = new Vector3(0.1f, -2.6f, 0) * 0.3f;
        r.body.LeftHand.localPosition = new Vector3(0.1f, -4.4f, 0) * 0.3f;
        r.body.LeftHand.localRotation = Quaternion.identity;

        r.body.RightShoulder.localPosition = Vector3.zero;
        r.body.RightElbow.localPosition = new Vector3(-0.1f, -2.6f, 0) * 0.3f;
        r.body.RightHand.localPosition = new Vector3(-0.1f, -4.4f, 0) * 0.3f;
        r.body.RightHand.localRotation = Quaternion.identity;
    }
}
