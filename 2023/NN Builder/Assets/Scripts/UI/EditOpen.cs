using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

public class EditOpen : MonoBehaviour
{
    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private RawImage textBox;
    [SerializeField] private Text textRender;
    [SerializeField] private Color defaultBoxColour;
    [SerializeField] private Color selectBoxColour;
    [SerializeField] private Color defaultTextColour;
    [SerializeField] private Color selectTextColour;

    public string text = "";

    private readonly string validLetters = "1234567890qwertyuiopasdfghjklzxcvbnmQWERTYUIOPASDFGHJKLZXCVBNM_- ";

    private bool interacting;
    private bool clearPotential;

    void OnEnable()
    {
        textBox.color = selectBoxColour;
        textRender.color = selectTextColour;
        interacting = true;
        clearPotential = true;
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

    void EditText()
    {
        if (Input.inputString.Length == 0)
            return;

        if ((KeyCode)Input.inputString[0] == KeyCode.Backspace)
        {
            text = clearPotential ? "" : text.Substring(0, Mathf.Max(0, text.Length - 1));
        }
        else if ((KeyCode)Input.inputString[0] == KeyCode.Return)
        {
            textBox.color = defaultBoxColour;
            textRender.color = defaultTextColour;
            interacting = false;
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
