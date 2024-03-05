using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

public class SaveButton : MonoBehaviour
{
    [SerializeField] private Network network;
    [SerializeField] private EditTitle editTitle;
    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private RawImage textBox;
    [SerializeField] private Text textRender;
    [SerializeField] private Color defaultBoxColour;
    [SerializeField] private Color selectBoxColour;
    [SerializeField] private Color defaultTextColour;
    [SerializeField] private Color selectTextColour;

    void Update()
    {
        if (CanvasHelper.MouseInRect(rectTransform) && (Input.GetMouseButton(0) || Input.GetMouseButtonUp(0)))
        {
            textBox.color = selectBoxColour;
            textRender.color = selectTextColour;

            if (Input.GetMouseButtonUp(0))
                network.SaveNetwork(editTitle.text);
        }
        else
        {
            textBox.color = defaultBoxColour;
            textRender.color = defaultTextColour;
        }
    }
}
