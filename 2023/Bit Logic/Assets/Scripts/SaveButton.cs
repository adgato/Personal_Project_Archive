using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

public class SaveButton : MonoBehaviour
{
    [SerializeField] private NodeInstance network;
    [SerializeField] private EditTitle editTitle;
    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private RawImage textBox;
    [SerializeField] private Text textRender;
    [SerializeField] private Color defaultBoxColour;
    [SerializeField] private Color selectBoxColour;
    [SerializeField] private Color defaultTextColour;
    [SerializeField] private Color selectTextColour;

    bool GetMouseButton() => CanvasHelper.MouseInRect(rectTransform) && Input.GetMouseButton(0);
    bool GetMouseButtonUp() => CanvasHelper.MouseInRect(rectTransform) && Input.GetMouseButtonUp(0);
    bool GetKey() => EditTitle.interacting && Input.GetKey(KeyCode.BackQuote);
    bool GetKeyUp() => EditTitle.interacting && Input.GetKeyUp(KeyCode.BackQuote);
    bool GetSelect() => GetKey() || GetMouseButton();
    bool GetSelectUp() => GetMouseButtonUp() && !GetKey() || !GetMouseButton() && GetKeyUp();

    public static bool interacting { get; private set; }

    void Update()
    {
        if (GetSelect())
        {
            textBox.color = selectBoxColour;
            textRender.color = selectTextColour;
            interacting = true;
            return;
        }
        else if (GetSelectUp() && interacting)
            network.Save(editTitle.text);
        textBox.color = defaultBoxColour;
        textRender.color = defaultTextColour;
        interacting = false;
    }
}
