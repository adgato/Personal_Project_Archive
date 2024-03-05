using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class ControlMenu : MonoBehaviour
{
    [SerializeField] private RectTransform HoverBox;
    [SerializeField] private RectTransform ControlBoxList;
    private ControlOption[] ControlOptionList;
    private RawImage HoverBoxImage;
    int SelectedOption;

    [SerializeField] private string[] ValidAxis;
    private float[] HeldAxisValues;
    private float HoldAxisTimer;

    // Start is called before the first frame update
    void Start()
    {
        ValidAxis = ValidAxis.Concat(Enumerable.Range(1, 28).Select(x => "Axis" + x)).ToArray();

        HoverBoxImage = HoverBox.GetComponent<RawImage>();
        SelectedOption = -1;

        ControlOptionList = new ControlOption[ControlBoxList.childCount];
        for (int i = 0; i < ControlBoxList.childCount; i++)
        {
            ControlOptionList[i] = ControlBoxList.GetChild(i).GetComponent<ControlOption>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!ControlSaver.GamePaused)
            return;

        Controller.InputType selectedControlType = SelectedOption == -1 ? Controller.InputType.None : ControlOptionList[SelectedOption - 1].ThisInputType;

        Vector2 mousePos = GetMousePosOnCanvas();
        int option = Mathf.FloorToInt(mousePos.y / 50);



        if (mousePos.y % 50 > 40 || mousePos.x < 567 || option < 1 || option > ControlOptionList.Length || (ControlOptionList[option - 1].ThisInputType == Controller.InputType.Axis && mousePos.x > 817 && mousePos.x < 867))
            option = -1;
        else
            CursorState.Select();

        if (Input.GetMouseButtonDown(0))
        {
            SelectedOption = option;
            HoverBox.anchoredPosition = new Vector2(HoverBox.anchoredPosition.x, option * -50);
            HeldAxisValues = new float[ValidAxis.Length];
            HoldAxisTimer = Time.realtimeSinceStartup;
        }

        else if (selectedControlType == Controller.InputType.Button && AnyKeyDown(out KeyCode keyCode))
        {
            ControlOptionList[SelectedOption - 1].Bind(keyCode);
            SelectedOption = -1;
        }
        else if (selectedControlType == Controller.InputType.Axis && AnyAxisHeld(out string axisHeld, out int axisInversion))
        {
            ControlOptionList[SelectedOption - 1].Bind(axisHeld, axisInversion);
            SelectedOption = -1;
        }

        if (SelectedOption != -1)
            HoverBoxImage.color = new Color(0.5f, 0.2f, 0);
        else
        {
            HoverBoxImage.color = Input.GetMouseButton(0) ? new Color(0.5f, 0.2f, 0) : new Color(0.31f, 0.31f, 0.31f);
            HoverBox.anchoredPosition = new Vector2(HoverBox.anchoredPosition.x, option * -50);
        }
    }

    Vector2 GetMousePosOnCanvas()
    {
        return new Vector2(Input.mousePosition.x / Screen.width * 1067, (1 - Input.mousePosition.y / Screen.height) * 600);
    }

    bool AnyKeyDown(out KeyCode keyDown)
    {
        foreach (KeyCode kcode in System.Enum.GetValues(typeof(KeyCode)))
        {
            if (Input.GetKeyDown(kcode))
            {
                keyDown = kcode;
                return true;
            }
        }
        keyDown = KeyCode.None;
        return false;
    }

    bool AnyAxisHeld(out string axisHeld, out int inversion)
    {
        if (Time.realtimeSinceStartup - HoldAxisTimer > 1)
        {
            float maxAxisValue = HeldAxisValues.Select(x => Mathf.Abs(x)).Max();
            int maxAxisIndex = System.Array.FindIndex(HeldAxisValues, x => Mathf.Abs(x) == maxAxisValue);

            axisHeld = ValidAxis[maxAxisIndex];
            inversion = (int)Mathf.Sign(HeldAxisValues[maxAxisIndex]);

            if (maxAxisValue < 1) //Not enough to be sure an axis was inputted
            {
                SelectedOption = -1;
                return false;
            }

            return true;
        }
        else
        {
            for (int i = 0; i < ValidAxis.Length; i++)
            {
                HeldAxisValues[i] += Input.GetAxis(ValidAxis[i]);
            }
        }
        axisHeld = null;
        inversion = 0;
        return false;
    }
}
