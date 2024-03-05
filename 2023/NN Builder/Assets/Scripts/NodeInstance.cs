using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

public class NodeInstance : MonoBehaviour
{
    public Node node { get; private set; }

    public bool showData;
    private int prev_iteration;
    private RawImage matrixImage;
    private Text efficiencyText;

    [HideInInspector] public RectTransform rectTransform;
    private RectTransform upperEdgeRectTransform;
    private RectTransform lowerEdgeRectTransform;
    private bool dragging;
    private Vector2 dragOffset;

    public void Init1(Node node, string name)
    {
        if (node == null || node.instance == this)
            return;

        this.node = node;
        this.node.instance = this;

        if (node.GetType().IsSubclassOf(typeof(InputNode)) || node.GetType().IsSubclassOf(typeof(OutputNode)))
            showData = true;

        transform.GetChild(2).GetComponent<Text>().text = node.name;
        efficiencyText = transform.GetChild(5).GetComponent<Text>();

        rectTransform = GetComponent<RectTransform>();
        upperEdgeRectTransform = transform.GetChild(0).GetComponent<RectTransform>();
        lowerEdgeRectTransform = transform.GetChild(1).GetComponent<RectTransform>();

        rectTransform.GetComponent<RawImage>().color = node.colour;
        upperEdgeRectTransform.GetComponent<RawImage>().color = node.colour;
        lowerEdgeRectTransform.GetComponent<RawImage>().color = node.colour;

        matrixImage = lowerEdgeRectTransform.GetChild(0).GetComponent<RawImage>();
        matrixImage.texture = Texture2D.blackTexture;

        rectTransform.anchoredPosition = node.anchoredStartPos;

        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 10 + 25 * Mathf.Max(node.inputNo, node.outputNo));

        for (int i = 0; i < node.inputNo; i++)
        {
            if (i < transform.GetChild(3).childCount)
                continue;

            RectTransform new_port = Instantiate(Prefabs.Port, transform.GetChild(3)).GetComponent<RectTransform>();
            new_port.name = "Input " + i;
            new_port.pivot = new Vector2(0.5f, i);
        }
        for (int i = 0; i < node.outputNo; i++)
        {
            if (i < transform.GetChild(4).childCount)
                continue;

            RectTransform new_port = Instantiate(Prefabs.Port, transform.GetChild(4)).GetComponent<RectTransform>();
            new_port.name = "Output " + i;
            new_port.pivot = new Vector2(0.5f, i);
        }
    }
    private void LateUpdate()
    {
        if (Network.batchTime != 0 && Network.isTraining)
            efficiencyText.text = "=>" + Mathx.RoundPadded(100 * node.forwardTime / Network.batchTime, 1) + "%, <=" + Mathx.RoundPadded(100 * node.backTime / Network.batchTime, 1) + "%";
        else
            efficiencyText.text = "";

        if (showData && prev_iteration < node.iterations)
        {
            prev_iteration = node.iterations;
            matrixImage.enabled = true;

            Vector2Int shape = node.output[0].samples[0].shape;

            int channels;
            for (channels = 1; channels < Mathf.Min(3, node.output.Length) && node.output[channels].samples[0].shape == shape; channels++) { }

            Texture2D texture = new Texture2D(shape.x, shape.y);

            Color32[] colors32 = new Color32[shape.x * shape.y];

            for (int x = 0; x < shape.x; x++)
            {
                for (int y = 0; y < shape.y; y++)
                    colors32[x * shape.y + y] = 
                        channels == 1 ? Color.Lerp(Color.black, Color.white, 
                        node.output[0].samples[0].Get(x, y)) : new Color(node.output[0].samples[0].Get(x, y), node.output[1].samples[0].Get(x, y), channels == 3 ? node.output[2].samples[0].Get(x, y) : 0);
            }

            texture.SetPixels32(colors32);
            texture.Apply();

            matrixImage.texture = texture;
        }

        if (node == null || MakeArc())
            return;

        bool mouseOnNode = CanvasHelper.MouseInRect(rectTransform) || CanvasHelper.MouseInRect(upperEdgeRectTransform) || CanvasHelper.MouseInRect(lowerEdgeRectTransform);
        SearchBox.mouseOverAnyNode |= mouseOnNode;
        SearchBox.mouseOverAnyNode |= dragging;

        if (mouseOnNode && Input.GetMouseButtonUp(1))
            Destroy(gameObject);
        else if (mouseOnNode && Input.GetMouseButtonDown(0))
        {
            Vector2 mousePosition = Input.mousePosition;
            dragOffset = rectTransform.anchoredPosition - mousePosition;
            dragging = true;
        }
        else if (dragging && Input.GetMouseButtonUp(0))
        {
            dragging = false;
            InspectorBox.inspectNode = node;
            Selection.objects = new Object[] { gameObject };
        }

        if (dragging)
        {
            Vector2 mousePosition = Input.mousePosition;
            Vector2 p = mousePosition + dragOffset;
            Vector2 anchoredPosition = 10 * new Vector2(Mathf.Floor(p.x * 0.1f), Mathf.Floor(p.y * 0.1f));

            for (int i = 0; i < node.inputNo; i++)
            {
                if (node.arcsIn[i] == null || node.arcsIn[i].inputNode == null || node.arcsIn[i].inputNode.instance == null)
                    continue;

                anchoredPosition = new Vector2(Mathf.Max(anchoredPosition.x, 10 * Mathf.Floor(node.arcsIn[i].inputNode.instance.transform.position.x * 0.1f + 15)), anchoredPosition.y);
            }
            for (int i = 0; i < node.outputNo; i++)
            {
                if (node.arcsOut[i] == null || node.arcsOut[i].outputNode == null || node.arcsOut[i].outputNode.instance == null)
                    continue;

                anchoredPosition = new Vector2(Mathf.Min(anchoredPosition.x, 10 * Mathf.Floor(node.arcsOut[i].outputNode.instance.transform.position.x * 0.1f - 15)), anchoredPosition.y);
            }

            rectTransform.anchoredPosition = anchoredPosition;
        }
    }

    private void OnDestroy()
    {
        foreach (Arc arc in node.arcsIn)
        {
            if (arc != null)
                arc.outputNode = null;
        }
        foreach (Arc arc in node.arcsOut)
        {
            if (arc != null)
                arc.inputNode = null;
        }
        ArcInstance.temp_inputNode = null;
        ArcInstance.temp_outputNode = null;
    }

    private bool MakeArc()
    {
        if (Input.GetMouseButtonDown(0))
        {
            for (int i = 0; i < node.inputNo; i++)
            {
                if (node.arcsIn[i] != null || !CanvasHelper.MouseInRect(transform.GetChild(3).GetChild(i).GetComponent<RectTransform>()))
                    continue;
                ArcInstance arcInstance = Instantiate(Prefabs.Arc, transform.parent).GetComponent<ArcInstance>();
                arcInstance.InitNew(null, -1, node, i);
                return true;
            }
            for (int i = 0; i < node.outputNo; i++)
            {
                if (node.arcsOut[i] != null || !CanvasHelper.MouseInRect(transform.GetChild(4).GetChild(i).GetComponent<RectTransform>()))
                    continue;
                ArcInstance arcInstance = Instantiate(Prefabs.Arc, transform.parent).GetComponent<ArcInstance>();
                arcInstance.InitNew(node, i, null, -1);
                return true;
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            for (int i = 0; i < node.inputNo; i++)
            {
                if (node.arcsIn[i] != null || !CanvasHelper.MouseInRect(transform.GetChild(3).GetChild(i).GetComponent<RectTransform>()))
                    continue;
                ArcInstance.temp_outputNode = node;
                ArcInstance.temp_outputNodePort = i;
                return true;
            }
            for (int i = 0; i < node.outputNo; i++)
            {
                if (node.arcsOut[i] != null || !CanvasHelper.MouseInRect(transform.GetChild(4).GetChild(i).GetComponent<RectTransform>()))
                    continue;
                ArcInstance.temp_inputNode = node;
                ArcInstance.temp_inputNodePort = i;
                return true;
            }
        }
        return false;
    }
}
