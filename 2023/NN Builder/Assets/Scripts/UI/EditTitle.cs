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

    private bool interacting;
    private bool clearPotential;

    void Start()
    {
        textBox.color = defaultBoxColour;
        textRender.color = defaultTextColour;
        interacting = false;
        clearPotential = false;
        text = "";
    }

    // Update is called once per frame
    void Update()
    {
        textRender.text = text;

        if (Input.GetMouseButtonDown(0) && CanvasHelper.MouseInRect(rectTransform))
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
    }

    public void SetText(string text)
    {
        this.text = text;
    }

    void EditText()
    {
        if (clearPotential && Input.anyKeyDown && !Input.GetMouseButtonDown(0))
        {
            text = Input.inputString.Length == 0 ? text : "";
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
                if (validLetters.Contains(letter.ToString()))
                    text += letter;
            }
        }

        clearPotential = false;
    }
}
