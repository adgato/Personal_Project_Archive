using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//This is the opening and closing sequence of the great trap door in the Conrol Tower
public class EvilBaseDoorOpen : MonoBehaviour
{
    [SerializeField] private FlipSwitch trigger;
    [SerializeField] private float lerpSpeed = 0.1f;
    private float lerp = 0;

    [SerializeField] private Transform[] safetyBarriers;

    [SerializeField] private Vector2 openCloseTDoorZ;
    [SerializeField] private Vector2 openCloseTDoorX;
    [SerializeField] private Vector2 topBottomStandY;
    [SerializeField] private Vector2 topBottomPillarY;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (trigger.switchState == FlipSwitch.State.top && lerp < 2.25f)
            lerp += lerpSpeed * Time.deltaTime;
        else if (trigger.switchState == FlipSwitch.State.bottom && lerp > 0)
            lerp -= lerpSpeed * Time.deltaTime;

        //start interpolates from 0 to 1 to move the lift down first, then gates interpolates from 0 to 1 to open the gates, then end interpolates from 0 to 1 to close the trap door
        //this process is reversable
        lerp = Mathf.Clamp(lerp, 0, 2.25f);
        float start = Mathx.EndsCommon(Mathf.Clamp01(lerp));
        float gates = Mathx.EndsCommon((Mathf.Clamp(lerp, 1, 1.25f) - 1) * 4);
        float end = Mathx.EndsCommon(Mathf.Clamp(lerp, 1.25f, 2.25f) - 1.25f);
        

        transform.GetChild(0).transform.localScale = new Vector3(1, Mathf.Lerp(topBottomPillarY.x, topBottomPillarY.y, start), 1);
        transform.GetChild(1).transform.localPosition = new Vector3(0, Mathf.Lerp(topBottomStandY.x, topBottomStandY.y, start), 0);

        for (int i = 0; i < safetyBarriers.Length; i++)
            safetyBarriers[i].localEulerAngles = new Vector3(90 * (i == 0 ? gates : 1 - gates), -90, i == 0 ? 90 : -90);

        transform.GetChild(2).GetChild(0).transform.localScale = new Vector3(Mathf.Lerp(openCloseTDoorX.x, openCloseTDoorX.y, end), 1, Mathf.Lerp(openCloseTDoorZ.x, openCloseTDoorZ.y, end));
        transform.GetChild(2).GetChild(1).transform.localScale = new Vector3(Mathf.Lerp(openCloseTDoorX.x, openCloseTDoorX.y, end), 1, -Mathf.Lerp(openCloseTDoorZ.x, openCloseTDoorZ.y, end));
    }
}
