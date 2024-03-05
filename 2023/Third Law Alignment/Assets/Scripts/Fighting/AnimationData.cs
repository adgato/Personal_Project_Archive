using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public struct AnimationData
{
    [System.Serializable]
    public struct LocalArmKeyPose
    {
        public float time01;
        public Vector3 elbow;
        public Vector3 hand;
        public Quaternion handRotation;
    }

    [Range(0, 1)]
    [Tooltip("How smooth the animation from start to end is.")]
    [SerializeField] private float smoothness;
    [Range(0, 1)]
    [Tooltip("Slerp parameter from the current arm position to the current animation position.")]
    [SerializeField] private float snapiness;
    [SerializeField] private LocalArmKeyPose[] keyPoses;//time = 0 to time = keyTime[0] is for interpolation from whatever into the first key pose

    public static AnimationData LoadAnimation(string filename)
    {
        return JsonSaver.LoadResource<AnimationData>(filename);
    }

    /// <summary>
    /// Convention: All saved animations are saved as left arm localPositions, NOT RIGHT!
    /// </summary>
    public void SaveAnimation(string filename)
    {
        keyPoses = keyPoses.OrderBy(pose => pose.time01).ToArray();
        JsonSaver.SaveData(filename, this);
    }

    public void Evaluate(float time01, bool leftArm, ref SmoothTransform elbow, ref SmoothTransform hand)
    {
        if (keyPoses == null || keyPoses.Length == 0)
            return;

        time01 = Mathf.Clamp01(time01);

        //Works since the keyPoses are ordered by time01
        int i;
        for (i = 0; i < keyPoses.Length - 1; i++)
            if (keyPoses[i].time01 > time01)
                break;

        float t01 = Mathf.InverseLerp(i == 0 ? 0 : keyPoses[i - 1].time01, keyPoses[i].time01, time01);
        Vector3 elbowLocalPosition = BezierBetween(i == 0 ? elbow.localPosition : keyPoses[i - 1].elbow, keyPoses[i].elbow, keyPoses[i == keyPoses.Length - 1 ? i : i + 1].elbow, t01);
        Vector3 handLocalPosition = BezierBetween(i == 0 ? hand.localPosition : keyPoses[i - 1].hand, keyPoses[i].hand, keyPoses[i == keyPoses.Length - 1 ? i : i + 1].hand, t01);
        Quaternion handLocalRotation = Quaternion.Slerp(i == 0 ? hand.localRotation : keyPoses[i - 1].handRotation, keyPoses[i].handRotation, t01);

        if (!leftArm)
        {
            elbow.localPosition = Vector3.Slerp(elbow.localPosition, Vector3.Scale(elbowLocalPosition, new Vector3(-1, 1, 1)), snapiness);
            hand.localPosition = Vector3.Slerp(hand.localPosition, Vector3.Scale(handLocalPosition, new Vector3(-1, 1, 1)), snapiness);
            hand.localRotation = Quaternion.Slerp(hand.localRotation, Quaternion.Euler(handLocalRotation.eulerAngles.x, -handLocalRotation.eulerAngles.y, handLocalRotation.eulerAngles.z), snapiness);
        }
        else
        {
            elbow.localPosition = Vector3.Slerp(elbow.localPosition, elbowLocalPosition, snapiness);
            hand.localPosition = Vector3.Slerp(hand.localPosition, handLocalPosition, snapiness);
            hand.localRotation = Quaternion.Slerp(hand.localRotation, handLocalRotation, snapiness);
        }
    }

    public bool FinishedAnimation(float time01)
    {
        return keyPoses == null || keyPoses.Length == 0 || time01 > keyPoses[keyPoses.Length - 1].time01;
    }

    private Vector3 BezierBetween(Vector3 start, Vector3 end, Vector3 next, float t)
    {
        if (smoothness == 0)
            return Vector3.Slerp(start, end, t);

        Vector3 p0 = start;
        Vector3 p1 = (end - start).normalized * smoothness + start;
        Vector3 p2 = (end - next).normalized * smoothness + end;
        Vector3 p3 = end;

        // Calculate the point on the Bezier curve using the formula: B(t) = (1 - t)^3 * p0 + 3 * (1 - t)^2 * t * p1 + 3 * (1 - t) * t^2 * p2 + t^3 * p3
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        float uuu = uu * u;
        float ttt = tt * t;

        Vector3 pointOnCurve = uuu * p0 + 3 * uu * t * p1 + 3 * u * tt * p2 + ttt * p3;

        return pointOnCurve;
    }

    public void AddKeyFrame(float keyTime, Vector3 localElbowKeyPos, Vector3 localHandKeyPos, Quaternion localHandKeyRot)
    {
        if (keyPoses == null)
            keyPoses = new LocalArmKeyPose[0] { };

        int index = keyPoses.ToList().FindIndex(pose => Mathf.Approximately(pose.time01, keyTime));

        if (index >= 0)
        {
            Debug.LogError("Error: keyPose already exists at this time");
            return;
        }

        LocalArmKeyPose newKeyPose = new LocalArmKeyPose
        {
            time01 = keyTime,
            elbow = localElbowKeyPos,
            hand = localHandKeyPos,
            handRotation = localHandKeyRot
        };

        keyPoses = keyPoses.Append(newKeyPose).ToArray();
    }
}

