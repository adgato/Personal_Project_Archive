using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

public class Controls : MonoBehaviour
{
    [SerializeField] private NodeInstance network;
    [SerializeField] private Text controlsText;

    private string controlsDefault =
        @"<color=#00ff66>Hold 1234567890</color>: Hold down to add component from hotbar to circuit
<color=#ff6600>Tab</color>: Lookup component by name

<color=#ff6600>Arrows Keys</color>: Move crosshair
<color=#ff6600>Space</color> / <color=#0066ff>Click Mouse</color>: Toggle input on crosshair
<color=#ff6600>Space + Arrow Keys</color> / <color=#0066ff>Drag Left Mouse Button</color>: Select components

<color=#0066ff>Drag Middle Mouse Button</color>: Move view
<color=#0066ff>Scroll Mouse</color>: Zoom view

<color=#ff6600>-Minus</color>: To remove the last added component
<color=#ff6600>+Plus</color>: To re-add the last removed component

<color=#ff6600>Any Letter</color>: To label the input / output on crosshair

<color=#0066ff>Click Save Bar</color> / <color=#ff6600>`Back Quote</color>: To enter a save name
<color=#ff6600>Escape</color>: Quit
<color=#ff6600>F1</color>: Hide these controls";

    private string controlsSelection =
        @"<color=#ff6600>Arrows Keys</color>: Move selected components
<color=#ff6600>Delete</color>: Remove selected components";

    private string savingCircuit =
        @"<color=#0066ff>Click Save</color> / <color=#ff6600>`Back Quote</color>: To save
<color=#0066ff>Click Off</color> / <color=#ff6600>Escape</color>: Stop saving";

    private string addComponent =
        @"<color=#00ff66>Release 1234567890</color>: Add component to circuit
<color=#ff6600>Tab</color>: Select which component input to add on crosshair
<color=#ff6600>Arrows Keys</color>: Orient component in direction";

    private string browseComponent =
        @"<color=#00ff66>Hold 1234567890</color>: Hold down to bind highlighted component to hotbar
<color=#ff6600>Tab</color>: Close lookup menu
<color=#ff6600>Up / Down Arrows Keys</color>: Browse components
<color=#ff6600>Left / Right Arrows Keys</color>: Select to open or delete a custom component
<color=#ff6600>Return</color>: Select highlighted option
<color=#ff0066>Delete + Return</color>: Delete the custom component's save";

    void Update()
    {
        controlsText.enabled ^= Input.GetKeyDown(KeyCode.F1);

        if (network.adding)
            controlsText.text = addComponent;
        else if (SearchBox.active)
            controlsText.text = browseComponent;
        else if (EditTitle.interacting || SaveButton.interacting)
            controlsText.text = savingCircuit;
        else if (network.AnySelected())
            controlsText.text = controlsSelection;
        else
        {
            controlsText.text = controlsDefault;
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                network.Save("AutoSave");
                Application.Quit();
                Debug.Log("Quit!");
            }
        }

    }
}
