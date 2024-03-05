using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SocialPlatforms;

public class SpiderLegs : MonoBehaviour
{
    [SerializeField] private Transform body;
    [SerializeField] private float bodyBounce;
    [SerializeField] private Transform head;
    [SerializeField] private float headBounce;

    [SerializeField] private float[] GroundOffset;
    public float CrouchOffset { get; set; }
    public bool IsGrounded { get; set; }

    private readonly float D = 20.4f;
    private readonly float d = 1.42f;
    [SerializeField] private float[] R;
    private float s;
    private float h;
    private float f1;
    private float T1;
    private float T2;
    private float T;

    [SerializeField] private float v = 0.5f;
    private float animationSpeed;


    private float timeAirBorne;
    private float airBorneLerp;
    private float t0;

    private float hsPlus;
    private float hsMinus;
    private float smoothOffset;

    private void Start()
    {
        PushParameterChanges();
    }

    private void OnValidate()
    {
        PushParameterChanges();
    }

    private void Update()
    {
        t0 += Time.deltaTime * (1 - airBorneLerp) * animationSpeed;

        if (IsGrounded)
        {
            timeAirBorne = 0;
            airBorneLerp = 0;
        }
        else
        {
            timeAirBorne += Time.deltaTime;
            airBorneLerp = 0.9f * (1 - Mathf.Exp(-4 * timeAirBorne));
        }


        UpdateJoints();
    }

    public void RefreshWalkParameters(float velocity)
    {
        animationSpeed = velocity / v;
    }
    private void PushParameterChanges()
    {
        h = v / 5;

        hsPlus = (h * h + s * s) / (2 * h);
        hsMinus = (h * h - s * s) / (2 * h);

        s = Mathf.Sqrt(v) / 2;// Mathf.Lerp(2, 4, airBorneLerp);
        f1 = Mathf.Atan(hsMinus / -s);

        smoothOffset = hsPlus * Mathf.Cos(Mathf.PI - f1) / (1 + Mathf.Exp(-d));

        T1 = Mathf.Pow(v, -0.05f);
        T2 = (s - smoothOffset) / v;
        T = T1 + T2;
    }

    float Mod(float x, float m) => (x % m + m) % m;


    //https://www.desmos.com/calculator/aznrvmwsir
    void UpdateJoints()
    {
        bool stationary = animationSpeed < 0.1f;

        float tl = Mod(t0, T);
        float tr = Mod(tl + T / 2, T);

        float bounce = 1 - 2 * Mathf.Abs(Mathf.Pow(Mathf.Cos(Mathf.PI * 0.5f * (tl + tr) / T), 3));
        body.localPosition = new Vector3(0, bodyBounce * bounce, body.localPosition.z);
        head.localPosition = new Vector3(0, headBounce * bounce, head.localPosition.z);

        float fl = Mathf.Lerp(Mathf.PI - f1, f1, tl / T1);
        float fr = Mathf.Lerp(Mathf.PI - f1, f1, tr / T1);

        float leftFootXPos = tl < T1 ? hsPlus * Mathf.Cos(fl) / (1 + Mathf.Exp(D * (fl - Mathf.PI + f1) - d)) : Mathf.Lerp(s, smoothOffset, (tl - T1) / T2);
        float rightFootXPos = tr < T1 ? hsPlus * Mathf.Cos(fr) / (1 + Mathf.Exp(D * (fr - Mathf.PI + f1) - d)) : Mathf.Lerp(s, smoothOffset, (tr - T1) / T2);


        float leftFootYPos = !stationary && tl < T1 ? hsMinus + hsPlus * Mathf.Sin(fl) : 0;
        float rightFootYPos = !stationary && tr < T1 ? hsMinus + hsPlus * Mathf.Sin(fr) : 0;

        for (int i = 0; i < 4; i++)
        {
            Transform legPair = transform.GetChild(i);

            Transform lHip = legPair.GetChild(0);
            Transform rHip = legPair.GetChild(1);

            float leftKneeBend = Mathf.Sqrt(Mathf.Max(0, R[i] * R[i] - 0.25f * (lHip.position - lHip.GetChild(1).position).sqrMagnitude));
            Vector3 leftMid = 0.5f * (lHip.position + lHip.GetChild(1).position);
            Vector3 leftNormal = Vector3.Cross(lHip.GetChild(1).forward, lHip.position - lHip.GetChild(1).position).normalized * (i % 2 == 0 ? 1 : -1);

            float rightKneeBend = Mathf.Sqrt(Mathf.Max(0, R[i] * R[i] - 0.25f * (rHip.position - rHip.GetChild(1).position).sqrMagnitude));
            Vector3 rightMid = 0.5f * (rHip.position + rHip.GetChild(1).position);
            Vector3 rightNormal = Vector3.Cross(rHip.GetChild(1).forward, rHip.position - rHip.GetChild(1).position).normalized * (i % 2 == 0 ? -1 : 1);

            lHip.GetChild(0).position = leftMid + leftNormal * leftKneeBend;
            rHip.GetChild(0).position = rightMid + rightNormal * rightKneeBend;

            lHip.GetChild(1).localPosition = Vector3.MoveTowards(lHip.GetChild(1).localPosition, leftFootXPos * Vector3.forward + (leftFootYPos - GroundOffset[i] + CrouchOffset) * Vector3.up, 0.2f);
            rHip.GetChild(1).localPosition = Vector3.MoveTowards(rHip.GetChild(1).localPosition, rightFootXPos * Vector3.forward + (rightFootYPos - GroundOffset[i] + CrouchOffset) * Vector3.up, 0.2f);

            DrawLine(lHip.position, lHip.GetChild(0).position, lHip.GetChild(2));
            DrawLine(lHip.GetChild(0).position, lHip.GetChild(1).position, lHip.GetChild(3));

            DrawLine(rHip.position, rHip.GetChild(0).position, rHip.GetChild(2));
            DrawLine(rHip.GetChild(0).position, rHip.GetChild(1).position, rHip.GetChild(3));
        }
    }

    public static void DrawLine(Vector3 from, Vector3 to, Transform line)
    {
        if (from == to)
            line.position = from;
        else
            line.SetPositionAndRotation((from + to) / 2, Quaternion.LookRotation(Vector3.Cross(from - to, line.right), from - to));
        line.localScale = new Vector3(0.1f, (from - to).magnitude / 2, 0.1f);
    }
}
