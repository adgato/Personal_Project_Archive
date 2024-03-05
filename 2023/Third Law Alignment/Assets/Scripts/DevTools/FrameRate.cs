using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class FrameRate : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI fpsText;

    private float fps;

    [SerializeField] float volatility = 1;
    [SerializeField] private int targetUpdatesPerSecond = -1;
    [SerializeField] private float fixedUpdatesPerSecond = 70;
    [SerializeField] private bool dynamicFixedPS = false;
    int updateBetween = 0;
    private string warning = "";
    private float warningEndTime = 0;

    private void Start()
    {
        Application.targetFrameRate = targetUpdatesPerSecond < 0 ? int.MaxValue : targetUpdatesPerSecond;
        Time.fixedDeltaTime = 1 / fixedUpdatesPerSecond;
    }

    void Update()
    {
        UpdateFrameRate();
        UpdateDynamicFixedPS();
    }
    private void FixedUpdate()
    {
        if (warningEndTime < Time.timeSinceLevelLoad)
        {
            warning = "";
            fpsText.color = updateBetween == 0 ? Color.red : Color.white;
        }
        else
            fpsText.color = Color.yellow;


        updateBetween = 0;
        fpsText.text = "FPS: " + fps + warning + "\n";
    }

    void UpdateFrameRate()
    {
        //Press Ctrl+F to toggle frame rate view
        fpsText.enabled ^= (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.F)
            || (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl)) && Input.GetKey(KeyCode.F);

        updateBetween++;

        fps = Mathf.Lerp(fps, Time.deltaTime == 0 ? fps : 1 / Time.deltaTime, volatility * Time.deltaTime);

        fpsText.text += "#";
    }

    void UpdateDynamicFixedPS()
    {
        if (dynamicFixedPS && Time.timeSinceLevelLoad > 10 && (2 * fixedUpdatesPerSecond < fps || 1.25f * fixedUpdatesPerSecond > fps))
        {
            fixedUpdatesPerSecond = fps / 1.85f;
            Time.fixedDeltaTime = 1 / fixedUpdatesPerSecond;
            warning = "\nFixedPS: " + fixedUpdatesPerSecond;
            warningEndTime = Time.timeSinceLevelLoad + 1;
        }
    }
}