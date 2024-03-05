using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArcInstance : MonoBehaviour
{
    private Arc arc;

    public static Node temp_inputNode;
    public static Node temp_outputNode;
    public static int temp_inputNodePort;
    public static int temp_outputNodePort;
    private Vector3 mouseOffset;

    private bool dragSplit;
    private float dragOffset;
    public RectTransform rectTransform;
    private RectTransform verticalRect;
    private RectTransform lowerSplitRect;
    private RectTransform upperSplitRect;
    private RectTransform horizontalLowerRect;
    private RectTransform horizontalUpperRect;

    // Start is called before the first frame update
    public void InitNew(Node inputNode, int inputNodePort, Node outputNode, int outputNodePort)
    {
        Init2(new Arc(inputNode, inputNodePort, outputNode, outputNodePort));
    }

    public void Init2(Arc arc)
    {
        this.arc = arc;

        temp_inputNode = null;
        temp_outputNode = null;

        verticalRect = transform.GetChild(1).GetComponent<RectTransform>();
        lowerSplitRect = transform.GetChild(0).GetChild(0).GetComponent<RectTransform>();
        upperSplitRect = transform.GetChild(1).GetChild(0).GetComponent<RectTransform>();
        horizontalLowerRect = transform.GetChild(0).GetComponent<RectTransform>();
        horizontalUpperRect = transform.GetChild(2).GetComponent<RectTransform>();

        if (arc.inputNode == null)
        {
            arc.outputNode.arcsIn[arc.outputNodePort] = arc;
            if (arc.outputNode.instance != null)
                mouseOffset = GetPivotInWorldSpace(arc.outputNode.instance.transform.GetChild(3).GetChild(arc.outputNodePort).GetComponent<RectTransform>()) - Input.mousePosition;
        }
        else if (arc.outputNode == null)
        {
            arc.inputNode.arcsOut[arc.inputNodePort] = arc;
            if (arc.inputNode.instance != null)
                mouseOffset = GetPivotInWorldSpace(arc.inputNode.instance.transform.GetChild(4).GetChild(arc.inputNodePort).GetComponent<RectTransform>()) - Input.mousePosition;
        }
    }
    private void Update()
    {
        if (arc.inputNode == null && temp_inputNode != null)
        {
            arc.inputNode = temp_inputNode;
            arc.inputNodePort = temp_inputNodePort;
            arc.inputNode.arcsOut[arc.inputNodePort] = arc;
        }
        if (arc.outputNode == null && temp_outputNode != null)
        {
            arc.outputNode = temp_outputNode;
            arc.outputNodePort = temp_outputNodePort;
            arc.outputNode.arcsIn[arc.outputNodePort] = arc;
        }

        if (!Input.GetMouseButton(0) && !Input.GetMouseButtonUp(0) && (arc.inputNode == null || arc.outputNode == null))
            Destroy(gameObject);


        Vector2 mousePosition = Input.mousePosition;

        Vector3 startPos = arc.inputNode == null || arc.inputNode.instance == null ? Input.mousePosition + mouseOffset : GetPivotInWorldSpace(arc.inputNode.instance.transform.GetChild(4).GetChild(arc.inputNodePort).GetComponent<RectTransform>());
        Vector3 endPos = arc.outputNode == null || arc.outputNode.instance == null ? Input.mousePosition + mouseOffset : GetPivotInWorldSpace(arc.outputNode.instance.transform.GetChild(3).GetChild(arc.outputNodePort).GetComponent<RectTransform>());

        float deltaHorizontal = Mathf.Max(0, endPos.x - startPos.x);
        float deltaVertical = endPos.y - startPos.y;

        if (Mathf.Approximately(deltaVertical, 0))
        {
            upperSplitRect.gameObject.SetActive(false);
            lowerSplitRect.gameObject.SetActive(false);
            deltaVertical = 0.001f;
        }
        else if (deltaVertical != 0.001f)
        {
            upperSplitRect.gameObject.SetActive(true);
            lowerSplitRect.gameObject.SetActive(true);
        }

        bool mouseOnArc = CanvasHelper.MouseInRect(lowerSplitRect) || CanvasHelper.MouseInRect(upperSplitRect);
        SearchBox.mouseOverAnyNode |= mouseOnArc;
        SearchBox.mouseOverAnyNode |= dragSplit;

        if (mouseOnArc && Input.GetMouseButtonDown(0))
        {
            dragSplit = true;
            dragOffset = arc.splitRatio01 * deltaHorizontal + rectTransform.anchoredPosition.x - mousePosition.x;
        }
        else if (mouseOnArc && Input.GetMouseButtonUp(1))
            Destroy(gameObject);
        else if (dragSplit && Input.GetMouseButtonUp(0))
            dragSplit = false;

        if (dragSplit)
            arc.splitRatio01 = (dragOffset + mousePosition.x - rectTransform.anchoredPosition.x) / deltaHorizontal;
        arc.splitRatio01 = Mathf.Clamp(arc.splitRatio01, 0.1f, 0.9f);

        transform.position = startPos;

        horizontalLowerRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, deltaHorizontal * arc.splitRatio01);

        horizontalUpperRect.anchoredPosition = new Vector2(deltaHorizontal * arc.splitRatio01, deltaVertical);
        horizontalUpperRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, deltaHorizontal * (1 - arc.splitRatio01));

        verticalRect.anchoredPosition = new Vector2(deltaHorizontal * arc.splitRatio01, 0);
        transform.GetChild(1).localScale = new Vector3(1, deltaVertical / verticalRect.rect.height, 1);
        transform.GetChild(1).GetChild(0).localScale = 0.05f * new Vector3(1, Mathf.Pow(transform.GetChild(1).localScale.y, -1), 1);

        transform.GetChild(0).gameObject.SetActive(true);
        transform.GetChild(1).gameObject.SetActive(true);
        transform.GetChild(2).gameObject.SetActive(true);
    }

    private void OnDestroy()
    {
        if (arc.inputNode != null)
            arc.inputNode.arcsOut[arc.inputNodePort] = null;
        if (arc.outputNode != null)
            arc.outputNode.arcsIn[arc.outputNodePort] = null;
    }

    private static Vector3 GetPivotInWorldSpace(RectTransform source)
    {
        // Rewrite Rect.NormalizedToPoint without any clamping.
        Vector2 pivot = new Vector2(
            -source.pivot.x * source.rect.width,
            -source.pivot.y * source.rect.height);
        // Apply scaling and rotations.
        return source.TransformPoint(new Vector3(pivot.x, pivot.y, 0f));
    }
}
