using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class ControlSaver : MonoBehaviour
{
    public static bool GamePaused { get; private set; }
    private bool gameWasPaused;

    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform ControlBoxList;
    private ControlOption[] ControlOptionList;


    [SerializeField] private TextMeshProUGUI textRender;
    [SerializeField] private RawImage textBox;
    [SerializeField] private RectTransform textBoxRectTransform;

    [SerializeField] private RawImage SaveButton;
    [SerializeField] private RawImage LoadButton;
    [SerializeField] private RawImage NextButton;
    [SerializeField] private RectTransform SaveButtonRect;
    [SerializeField] private RectTransform LoadButtonRect;
    [SerializeField] private RectTransform NextButtonRect;

    [SerializeField] private TextMeshProUGUI LoadDisplayName;

    [SerializeField] private Color defaultBoxColour;
    [SerializeField] private Color selectBoxColour;
    [SerializeField] private Color defaultTextColour;
    [SerializeField] private Color selectTextColour;
    [SerializeField] private Color defaultButtonColour;
    [SerializeField] private Color selectButtonColour;

    public static Controller currentControls { get; private set; } = new Controller();

    private int MaxSaveNameChars = 20;

    private string text = "Type name here...";

    private readonly string validLetters = "1234567890qwertyuiopasdfghjklzxcvbnmQWERTYUIOPASDFGHJKLZXCVBNM_- ";

    private bool interacting;
    private bool clearPotential;

    private string[] LoadNames;
    private int LoadNamesDisplayPointer;

    private void Awake()
    {
        Controller.Player.LoadFromMostRecent();
        currentControls.LoadFromMostRecent();
    }

    // Start is called before the first frame update
    void Start()
    {
        ControlOptionList = new ControlOption[ControlBoxList.childCount];
        for (int i = 0; i < ControlBoxList.childCount; i++)
        {
            ControlOptionList[i] = ControlBoxList.GetChild(i).GetComponent<ControlOption>();
        }

        textBox.color = defaultBoxColour;
        textRender.color = defaultTextColour;
        interacting = false;
        clearPotential = false;
        
        //Controller.SaveAs("Default");
        LoadNames = Controller.GetSavedControls();
        LoadNamesDisplayPointer = 0;
        LoadDisplayName.text = LoadNames[LoadNamesDisplayPointer];
    }

    // Update is called once per frame
    void Update()
    {
        GamePaused ^= Input.GetKeyDown(KeyCode.Escape);

        if (gameWasPaused && !GamePaused)
            Controller.Player.LoadFromMostRecent();

        gameWasPaused = GamePaused;
        canvas.enabled = GamePaused;
        if (!GamePaused)
            return;

        textRender.text = text;

        if (Input.GetMouseButtonDown(0) && UIHelper.MouseInRect(textBoxRectTransform))
        {
            textBox.color = selectBoxColour;
            textRender.color = selectTextColour;
            interacting = true;
            clearPotential = true;
        }
        else if (Input.GetMouseButtonDown(0))
        {
            textBox.color = defaultBoxColour;
            textRender.color = defaultTextColour;
            interacting = false;
        }

        if (interacting)
            EditText();

        SaveButton.color = defaultButtonColour;
        LoadButton.color = defaultButtonColour;
        NextButton.color = defaultButtonColour;

        if (UIHelper.MouseInRect(SaveButtonRect))
        {
            CursorState.Select();
            if (Input.GetMouseButtonDown(0))
            {
                currentControls.SaveAs(text, true);
                LoadNames = Controller.GetSavedControls();
                LoadNamesDisplayPointer = 0;
                LoadDisplayName.text = LoadNames[LoadNamesDisplayPointer];
            }
            else if (!Input.GetMouseButton(0))
                SaveButton.color = selectButtonColour;
        }
        else if (UIHelper.MouseInRect(LoadButtonRect))
        {
            CursorState.Select();
            if (Input.GetMouseButtonDown(0))
            {
                currentControls.LoadFrom(LoadNames[LoadNamesDisplayPointer]);
                foreach (ControlOption controlOption in ControlOptionList)
                    controlOption.Refresh();
                text = LoadNames[LoadNamesDisplayPointer];
            }
            else if (!Input.GetMouseButton(0))
                LoadButton.color = selectButtonColour;
        }
        else if (UIHelper.MouseInRect(NextButtonRect))
        {
            CursorState.Select();
            if (Input.GetMouseButtonDown(0))
            {
                LoadNamesDisplayPointer = (LoadNamesDisplayPointer + 1) % LoadNames.Length;
                LoadDisplayName.text = LoadNames[LoadNamesDisplayPointer];
            }
            else if (!Input.GetMouseButton(0))
                NextButton.color = selectButtonColour;
        }
    }

    public void SetText(string text)
    {
        this.text = text;
    }

    void EditText()
    {
        if (clearPotential && Input.inputString.Length > 0 && !Input.GetMouseButtonDown(0))
        {
            text = "";
            clearPotential = false;
        }

        if (Input.inputString.Length == 0)
        {
            return;
        }
        else if ((KeyCode)Input.inputString[0] == KeyCode.Return)
        {
            textBox.color = defaultBoxColour;
            textRender.color = defaultTextColour;
            interacting = false;
        }
        else if ((KeyCode)Input.inputString[0] == KeyCode.Backspace)
        {
            text = text.Substring(0, Mathf.Max(0, text.Length - 1));
        }
        else
        {
            foreach (char letter in Input.inputString)
            {
                if (text.Length > MaxSaveNameChars)
                    break;
                if (validLetters.Contains(letter.ToString()))
                    text += letter;
            }
        }

        clearPotential = false;
    }
}
