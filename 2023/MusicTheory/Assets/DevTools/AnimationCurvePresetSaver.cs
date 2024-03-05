using UnityEngine;
using UnityEditor;
using System.Linq;

public class AnimationCurvePresetSaver : MonoBehaviour
{
    public AnimationCurve curve; // Reference to your animation curve

    private void Start()
    {
        curve = new AnimationCurve(Enumerable.Range(0, 11).Select(i => new Keyframe(i / 10f, Mathf.Sin(0.2f * Mathf.PI * i), 2 * Mathf.PI * Mathf.Cos(0.2f * Mathf.PI * i), 2 * Mathf.PI * Mathf.Cos(0.2f * Mathf.PI * i))).ToArray());
    }

}