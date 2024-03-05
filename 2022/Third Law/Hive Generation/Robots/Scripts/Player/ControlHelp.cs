using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControlHelp : MonoBehaviour
{
    private bool showControls;

    private enum ControlMode { robot, ship, tetris, none };

    [SerializeField] private TMPro.TextMeshProUGUI robotText;
    [SerializeField] private TMPro.TextMeshProUGUI shipText;
    [SerializeField] private TMPro.TextMeshProUGUI tetrisText;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        showControls ^= PauseMenu.selectedPauseMenuOption == 3;

        ControlMode controlMode = ControlMode.none;
        if (CameraState.flyingShip)
            controlMode = ControlMode.ship;
        else if (CameraState.playingTetris)
            controlMode = ControlMode.tetris;
        else if (CameraState.InLockState(CameraState.LockState.unlocked))
            controlMode = ControlMode.robot;

        robotText.enabled = showControls && controlMode == ControlMode.robot;
        shipText.enabled = showControls && controlMode == ControlMode.ship;
        tetrisText.enabled = showControls && controlMode == ControlMode.tetris;
    }
}
