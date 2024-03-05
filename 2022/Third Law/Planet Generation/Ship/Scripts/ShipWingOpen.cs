using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipWingOpen : MonoBehaviour
{
    [SerializeField] private FlipSwitch trigger;
    [SerializeField] private float closedAngle = 300;
    [SerializeField] private float openAngle = 75;
    private Quaternion closedRot;
    private Quaternion openRot;

    private float lerp;
    [SerializeField] private float lerpSpeed = 1;

    private void Start()
    {
        lerp = 0;
        closedRot = Quaternion.Euler(closedAngle, transform.localEulerAngles.y, transform.localEulerAngles.z);
        openRot = Quaternion.Euler(openAngle, transform.localEulerAngles.y, transform.localEulerAngles.z);
    }

    private void Update()
    {
        if (trigger.switchState == FlipSwitch.State.top && lerp < 1)
            lerp += lerpSpeed * Time.deltaTime;
        else if (trigger.switchState == FlipSwitch.State.bottom && lerp > 0)
            lerp -= lerpSpeed * Time.deltaTime;

        transform.localRotation = Quaternion.Slerp(closedRot, openRot, lerp);
    }
}