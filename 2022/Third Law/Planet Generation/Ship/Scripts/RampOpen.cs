using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RampOpen : MonoBehaviour
{
    private ShipWeight shipWeight;
    [SerializeField] private ShipDoorOpen doorToFollow;
    private readonly float closedLength = 0.1f;
    private float openLength = 3f;
    private bool calculated;

    private void Start()
    {
        shipWeight = FindObjectOfType<ShipWeight>();
    }

    private void LateUpdate()
    {
        if (doorToFollow.lerp > 0 && doorToFollow.lerp < 1)
            CalculateRampEnd();
        else
            calculated = false;

        if (shipWeight.onHelipad)
            openLength = 2;

        transform.GetChild(0).localScale = new Vector3(transform.GetChild(0).localScale.x, Mathf.Lerp(closedLength, openLength, doorToFollow.lerp), transform.GetChild(0).localScale.z);
    }

    void CalculateRampEnd()
    {
        if (calculated)
            return;

        calculated = true;

        transform.localRotation = Quaternion.Euler(90, 0, 90);
        bool flag = false;
        float angle = 0;

        //Get shortest length and flattest angle required by the ramp to hit the terrain
        for (openLength = 10; openLength < 20; openLength++)
        {
            for (angle = -30; angle > -50; angle--)
            {
                Vector3 fwd = Quaternion.Euler(angle, 0, 0) * -transform.right;

                if (Physics.Raycast(transform.position, fwd, out RaycastHit hitInfo, openLength, ~(1 << 11 | 1 << 12)))
                {
                    openLength = hitInfo.distance * 0.7f;
                    flag = true;
                    break;
                }
            }
            if (flag)
                break;
        }

        transform.localRotation = Quaternion.Euler(90 + angle, 0, 90);
    }
}
