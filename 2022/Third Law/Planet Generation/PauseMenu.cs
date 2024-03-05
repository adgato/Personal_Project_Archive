using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private Text fpsText;
    [SerializeField] private TMPro.TextMeshProUGUI pauseText;
    [SerializeField] private RoboVision roboVision;
    public static int selectedPauseMenuOption;

    bool pauseTextShow;
    string defaultPauseMessage = "-New Game-\n\n-Self Destruct-\n\n-Continue-\n\n-Controls-\n\n-Quit-";
    string pauseMessage = "";
    float pauseLerp;

    int frameCount = 0;
    float dt = 0.0f;
    float fps = 0.0f;
    float updateRate = 4.0f;  // 4 updates per sec.

    private void Start()
    {

    }

    void Update()
    {
        UpdateFrameRate();

        if (selectedPauseMenuOption == 4)
        {
            Application.Quit();
            Debug.Log("Quit registered");
        }

        if (pauseTextShow != CameraState.isPaused)
        {
            if (CameraState.isPaused)
                pauseLerp = 0;
            pauseTextShow = CameraState.isPaused;
        }
        pauseLerp += 4 * Time.deltaTime;
        pauseText.enabled = CameraState.isPaused && roboVision.cameraIsPausedLerpDirection == -1;

        selectedPauseMenuOption = -1;

        float b = 0.05f * Mathf.Abs(Mathf.Sin(Time.realtimeSinceStartup * 5));
        pauseText.fontSharedMaterial.SetColor("_bgColour", new Color(RoboVision.visionColour.r * b, RoboVision.visionColour.g * b, RoboVision.visionColour.b * b, 1));

        if (!CameraState.isPaused)
            return;

        string[] splits = defaultPauseMessage.Split('\n');
        List<string> options = new List<string>();
        foreach (string split in splits)
        {
            if (split != "")
                options.Add(split);
        }

        //Get and highlight currently selected pause menu option
        int i = Mathf.Clamp(options.Count - Mathf.CeilToInt(Input.mousePosition.y / Screen.height * options.Count), 0, options.Count - 1);
        options[i] = (Input.GetKey(KeyCode.Mouse0) ? "+" : "[") + options[i].Substring(1, options[i].Length - 2) + (Input.GetKey(KeyCode.Mouse0) ? "+" : "]");
        //Set this option when the mouse is clicked
        if (Input.GetKeyUp(KeyCode.Mouse0))
            selectedPauseMenuOption = i;

        string newPauseMessage = string.Join("\n\n", options);

        if (newPauseMessage != pauseMessage)
        {
            pauseMessage = newPauseMessage;
        }

        //Write the option text letter by letter when the menu appears
        pauseText.text = pauseMessage.Substring(0, Mathf.Min(Mathf.FloorToInt(Mathf.Max(0, pauseLerp - 2) * 20), pauseMessage.Length));
    }
    void UpdateFrameRate()
    {
        //Press Ctrl+F to toggle frame rate view
        fpsText.enabled ^= (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.F)
            || (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl)) && Input.GetKey(KeyCode.F);

        frameCount++;
        dt += Time.deltaTime;
        if (dt > 1.0 / updateRate)
        {
            fps = frameCount / dt;
            frameCount = 0;
            dt -= 1.0f / updateRate;
        }
        fpsText.text = "FPS: " + fps;
        fpsText.color = fpsText.color == Color.white ? Color.black : Color.white;
    }
}
