using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

public class EditTitle : MonoBehaviour
{
    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private RawImage textBox;
    [SerializeField] private Text textRender;
    [SerializeField] private Color defaultBoxColour;
    [SerializeField] private Color selectBoxColour;
    [SerializeField] private Color defaultTextColour;
    [SerializeField] private Color selectTextColour;

    public string text { get; private set; }

    private readonly string validLetters = "1234567890qwertyuiopasdfghjklzxcvbnmQWERTYUIOPASDFGHJKLZXCVBNM_- ";

    private bool escaped;
    public static bool interacting { get; private set; }
    private bool clearPotential;

    bool GetMouseButton() => CanvasHelper.MouseInRect(rectTransform) && Input.GetMouseButton(0);
    bool GetMouseButtonDown() => CanvasHelper.MouseInRect(rectTransform) && Input.GetMouseButtonDown(0);
    bool GetKey() => Input.GetKey(KeyCode.BackQuote) && !escaped;
    bool GetKeyUp() => Input.GetKeyUp(KeyCode.BackQuote) && !escaped;
    bool GetSelect() => GetMouseButtonDown() && !GetKey() || !GetMouseButton() && GetKeyUp();

    void Start()
    {
        textBox.color = defaultBoxColour;
        textRender.color = defaultTextColour;
        interacting = false;
        clearPotential = false;
        text = "AutoSave";
    }

    // Update is called once per frame
    void Update()
    {
        textRender.text = text;

        if (Input.GetKeyDown(KeyCode.BackQuote))
            escaped = false;

        if (GetSelect())
        {
            textBox.color = selectBoxColour;
            textRender.color = selectTextColour;
            interacting = true;
            clearPotential = true;
        }
        else if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Escape))
        {
            escaped = true;
            textBox.color = defaultBoxColour;
            textRender.color = defaultTextColour;
            interacting = false;
        }

        if (interacting)
            EditText();
    }

    public void SetText(string text)
    {
        this.text = text;
    }

    void EditText()
    {
        if (clearPotential && Input.anyKeyDown && !Input.GetMouseButtonDown(0) && !Input.GetKeyDown(KeyCode.BackQuote))
        {
            text = Input.inputString.Length == 0 ? text : "";
            clearPotential = false;
        }

        if (Input.inputString.Length == 0)
            return;
        else if ((KeyCode)Input.inputString[0] == KeyCode.Backspace)
            text = text.Substring(0, Mathf.Max(0, text.Length - 1));
        else
            foreach (char letter in Input.inputString)
                if (validLetters.Contains(letter.ToString()))
                    text += letter;

        clearPotential = false;
    }
}
