using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

public class SearchBox : MonoBehaviour
{
    [SerializeField] private NodeInstance network;
    [SerializeField] private EditOpen editOpen;
    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private RectTransform textBoxRectTransform;
    [SerializeField] private RectTransform selectBoxRectTransform;
    [SerializeField] private RawImage optionSelectBox;
    [SerializeField] private RawImage openSelectBox;
    [SerializeField] private Color openColour;
    [SerializeField] private Color deleteColour;
    [SerializeField] private Text openSelectText;
    [SerializeField] private GameObject openBox;
    [SerializeField] private Text textRender;

    public static bool active { get; private set; }
    private int selectOption;
    private bool openSelected;

    // Start is called before the first frame update
    void Start()
    {
        active = false;
    }

    bool InputDownToOpen() => Input.GetKeyDown(KeyCode.Tab);
    bool InputDownToClose() => Input.GetKeyDown(KeyCode.Tab);

    // Update is called once per frame
    void LateUpdate()
    {
        if (!active)
        {
            active = InputDownToOpen();
            if (active)
                network.hotbarbar.SelectAll();
        }
        else
        {
            active = !InputDownToClose();
            if (!active)
                network.hotbarbar.ReSelect();
        }

        if (network.adding)
        {
            if (active)
                network.hotbarbar.ReSelect();
            active = false;
        }

        transform.GetChild(0).gameObject.SetActive(active);
        transform.GetChild(1).gameObject.SetActive(active);

        if (!active || EditTitle.interacting)
            return;

        rectTransform.anchoredPosition = network.mainCamera.WorldToScreenPoint(new Vector3(network.cursorCoord.x, network.cursorCoord.y, 0)) / canvas.scaleFactor + new Vector3(-1, 1, 0) * canvas.scaleFactor;

        string[] matches = CustomNodeSaver.QueryNodeTypes(editOpen.text);
        textRender.text = string.Join("\n", matches);
        textBoxRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, matches.Length * 26);

        selectBoxRectTransform.gameObject.SetActive(matches.Length != 0);

        if (Input.GetKeyDown(KeyCode.UpArrow))
            selectOption--;
        else if (Input.GetKeyDown(KeyCode.DownArrow))
            selectOption++;

        int m = Mathf.Max(1, matches.Length);
        selectOption = (selectOption % m + m) % m;

        selectBoxRectTransform.anchoredPosition = new Vector2(0, Mathf.Max(2.5f - selectOption * 26, 31 - matches.Length * 26));

        if (matches.Length == 0)
            return;

        bool selectOptionCustom = CustomNodeSaver.IsCustomNode(matches[selectOption]);

        openSelected ^= Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow);
        openSelected &= selectOptionCustom;

        selectBoxRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, selectOptionCustom ? 200 : 290);
        openSelectBox.enabled = openSelected;
        optionSelectBox.enabled = !openSelected;
        openBox.SetActive(selectOptionCustom);
        openSelectText.text = Input.GetKey(KeyCode.Delete) ? "Delete" : "Open";
        openSelectBox.color = Input.GetKey(KeyCode.Delete) ? deleteColour : openColour;

        if (openSelected)
        {
            if (Input.GetKey(KeyCode.Delete) && Input.GetKeyDown(KeyCode.Return))
            {
                network.Delete(matches[selectOption]);
            }
            else if (Input.GetKeyDown(KeyCode.Return))
            {
                network.Load(matches[selectOption]);
                network.hotbarbar.ReSelect();
                active = false;
            }
        }
        else
            foreach (KeyCode AlphaN in network.Hotbar.Keys)
                if (Input.GetKeyDown(AlphaN))
                {
                    network.BindNode(AlphaN, matches[selectOption]);
                    network.CheckNewNode();
                    network.hotbarbar.ReSelect();
                    active = false;
                    break;
                }
    }
}
