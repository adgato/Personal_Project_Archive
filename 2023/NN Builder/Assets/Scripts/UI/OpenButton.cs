using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

public class OpenButton : MonoBehaviour
{
    [SerializeField] private Network network;
    [SerializeField] private EditTitle editTitle;
    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private RawImage openTextBox;
    [SerializeField] private Text nameTextRender;
    [SerializeField] private Text textRender;
    [SerializeField] private Color defaultBoxColour;
    [SerializeField] private Color selectBoxColour;
    [SerializeField] private Color defaultTextColour;
    [SerializeField] private Color selectTextColour;

    void Update()
    {
        nameTextRender.text = InspectorBox.inspectNode == null ? "" : InspectorBox.inspectNode.name;

        if (CanvasHelper.MouseInRect(rectTransform) && (Input.GetMouseButton(0) || Input.GetMouseButtonUp(0)))
        {
            openTextBox.color = selectBoxColour;
            textRender.color = selectTextColour;

            if (Input.GetMouseButtonUp(0))
            {
                network.OpenCustomNode(new CustomNode(InspectorBox.inspectNode.name));
                editTitle.SetText(InspectorBox.inspectNode.name);
            }
        }
        else
        {
            openTextBox.color = defaultBoxColour;
            textRender.color = defaultTextColour;
        }
    }
}
