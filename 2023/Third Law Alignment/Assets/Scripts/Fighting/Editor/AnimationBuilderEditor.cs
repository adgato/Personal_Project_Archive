using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(AnimationBuilder))]
public class AnimationBuilderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        AnimationBuilder fa = (AnimationBuilder)target;

        if (GUILayout.Button("Default Arms"))
        {
            fa.ResetArms();
        }

        if (GUILayout.Button("Add"))
        {
            if (fa.animatingLeftArm)
                fa.currentAnimation.AddKeyFrame(fa.time, fa.r.body.LeftElbow.localPosition, fa.r.body.LeftHand.localPosition, fa.r.body.LeftHand.localRotation);
            else
                fa.currentAnimation.AddKeyFrame(fa.time, 
                    Vector3.Scale(fa.r.body.RightElbow.localPosition, new Vector3(-1, 1, 1)),
                    Vector3.Scale(fa.r.body.RightHand.localPosition, new Vector3(-1, 1, 1)),
                    Quaternion.Euler(fa.r.body.RightHand.localEulerAngles.x, -fa.r.body.RightHand.localEulerAngles.y, fa.r.body.RightHand.localEulerAngles.z)
                    );
        }

        if (GUILayout.Button("Save"))
            fa.currentAnimation.SaveAnimation(fa.animationName);

        if (GUILayout.Button("Load"))
            fa.currentAnimation = JsonSaver.LoadResource<AnimationData>("Animations/" + fa.animationName);

        DrawDefaultInspector();
    }
}
