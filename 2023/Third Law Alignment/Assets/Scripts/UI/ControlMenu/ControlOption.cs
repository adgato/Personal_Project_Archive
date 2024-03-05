using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ControlOption : MonoBehaviour
{
    [SerializeField] private Controller.Inputs InputToBindTo;
    public Controller.InputType ThisInputType { get; private set; }

    [SerializeField] private RawImage PressHighlight;
    [SerializeField] private TextMeshProUGUI InputValueTextBox;
    [SerializeField] private RectTransform InvertBoxRect;
    [SerializeField] private TextMeshProUGUI InvertTextBox;
    private TextAnimator InputValueTextAnimator;

    private float prevAxisAlpha = 0;

    public void Bind(KeyCode value)
    {
        if (ThisInputType == Controller.InputType.Button)
        {
            ControlSaver.currentControls.Bind(InputToBindTo, value);
            InputValueTextBox.text = value.ToString();
        }
        else
            Debug.LogError("Error: not a button");
    }
    public void Bind(string value, int inversion)
    {
        if (ThisInputType == Controller.InputType.Axis)
        {
            ControlSaver.currentControls.Bind(InputToBindTo, value);
            ControlSaver.currentControls.SetAxisInvert(InputToBindTo, inversion);
            InputValueTextBox.text = value;
            InvertTextBox.text = inversion == 1 ? "N" : "Y";
        }
        else
            Debug.LogError("Error: not an axis");
    }
    public void Refresh()
    {
        if (ThisInputType == Controller.InputType.Button)
            InputValueTextBox.text = ControlSaver.currentControls.Button(InputToBindTo).ToString();
        else if (ThisInputType == Controller.InputType.Axis)
        {
            if (ControlSaver.currentControls.Axis(InputToBindTo) == "")
                InputValueTextBox.text = "None";
            else
                InputValueTextBox.text = ControlSaver.currentControls.Axis(InputToBindTo);
            InvertTextBox.text = ControlSaver.currentControls.GetAxisInvert(InputToBindTo) == 1 ? "N" : "Y";
        }
    }

    private void Start()
    {
        InputValueTextAnimator = InputValueTextBox.GetComponent<TextAnimator>();
        ThisInputType = Controller.GetInputType(InputToBindTo);

        if (ThisInputType == Controller.InputType.Axis)
            InvertBoxRect.gameObject.SetActive(true);
    }
    private void Update()
    {
        if (!ControlSaver.GamePaused)
            return;

        if  (ThisInputType == Controller.InputType.Axis && UIHelper.MouseInRect(InvertBoxRect))
        {
            CursorState.Select();
            if (Input.GetMouseButtonDown(0))
            {
                int inversion = ControlSaver.currentControls.GetAxisInvert(InputToBindTo) * -1;
                InvertTextBox.text = inversion == 1 ? "N" : "Y";
                ControlSaver.currentControls.SetAxisInvert(InputToBindTo, inversion);
            }
        }

        if (ThisInputType == Controller.InputType.Button && ControlSaver.currentControls.GetButton(InputToBindTo))
            PressHighlight.color = new Color(1, 0.4f, 0, 1);
        else if (ThisInputType == Controller.InputType.Axis)
        {
            float a = ControlSaver.currentControls.GetAxis(InputToBindTo);
            PressHighlight.color = a > 0 ? new Color(1, 0.4f, 0, a) : new Color(0, 0.4f, 1, -a);

            if (a != 0)
            {
                if (prevAxisAlpha == 0)
                    InputValueTextAnimator.UpdateStyle("waves");
            }
            else if (prevAxisAlpha != 0)
                InputValueTextAnimator.UpdateStyle("default");
            prevAxisAlpha = a;
        }
        else
            PressHighlight.color = new Color(1, 0.4f, 0, 0);

        if (ThisInputType == Controller.InputType.Button)
        {
            if (ControlSaver.currentControls.GetButtonDown(InputToBindTo))
                InputValueTextAnimator.UpdateStyle("waves");
            else if (ControlSaver.currentControls.GetButtonUp(InputToBindTo))
                InputValueTextAnimator.UpdateStyle("default");
        }
    }
}
