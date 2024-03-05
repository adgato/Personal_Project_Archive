using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SupriseAndroid : MonoBehaviour
{
    [Range(0, 1)]
    [SerializeField] private float lerp = 0;
    private float lerpTime = 1.5f;

    private List<Transform> sittingParts;
    private List<Transform> movingParts;
    private List<Transform> lerpingParts;

    // Start is called before the first frame update
    void Start()
    {
        sittingParts = new List<Transform>();
        movingParts = new List<Transform>();
        lerpingParts = new List<Transform>();

        //The rig of the robot when sitting
        foreach (Transform part in transform.GetChild(0).GetComponentsInChildren<Transform>(true))
            sittingParts.Add(part);
        //The rig of the robot when standing
        foreach (Transform part in transform.GetChild(1).GetComponentsInChildren<Transform>(true))
            movingParts.Add(part);
        //The rig of the robot that will interpolate between sitting and standing
        foreach (Transform part in transform.GetChild(2).GetComponentsInChildren<Transform>(true))
            lerpingParts.Add(part);

        lerp = 0;
    }
    private void OnValidate()
    {
        Start();
        LerpBetween();
    }
    // Update is called once per frame
    void Update()
    {
        if (lerp == 1)
            return;

        if ((Camera.main.transform.position - transform.position).sqrMagnitude < 25)
            lerp += Time.deltaTime / lerpTime;
        else
            lerp -= Time.deltaTime / lerpTime;

        lerp = Mathf.Clamp01(lerp);

        transform.GetChild(0).gameObject.SetActive(lerp == 0);
        transform.GetChild(1).gameObject.SetActive(lerp == 1);
        transform.GetChild(2).gameObject.SetActive(lerp > 0 && lerp < 1);

        if (lerp > 0 && lerp < 1)
            LerpBetween();
    }
    void LerpBetween()
    {
        for (int i = 0; i < lerpingParts.Count; i++)
        {
            lerpingParts[i].position = Vector3.Slerp(sittingParts[i].position, movingParts[i].position, lerp);
            lerpingParts[i].rotation = Quaternion.Slerp(sittingParts[i].rotation, movingParts[i].rotation, lerp);
            lerpingParts[i].localScale = Vector3.Slerp(sittingParts[i].localScale, movingParts[i].localScale, lerp);
        }
    }
}
