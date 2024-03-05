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

    private readonly string validLetters = "qwertyuiopasdfghjklzxcvbnmQWERTYUIOPASDFGHJKLZXCVBNM_- ";
    private readonly string alphaKeys = "01234567890";

    private bool clearPotential;

    void OnEnable()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
            clearPotential = true;

        if (EditTitle.interacting)
        {
            textBox.color = defaultBoxColour;
            textRender.color = defaultTextColour;
        }
        else
        {
            textBox.color = selectBoxColour;
            textRender.color = selectTextColour;
        }

        textRender.text = text;
        if (SearchBox.active && !EditTitle.interacting)
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
        else
            foreach (char letter in Input.inputString)
                if (validLetters.Contains(letter.ToString()))
                    text += letter;
                else if (alphaKeys.Contains(letter.ToString()))
                    break;

        clearPotential = false;
    }
}
