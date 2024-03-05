using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

public class SearchBox : MonoBehaviour
{
    public static bool mouseOverAnyNode;

    [SerializeField] private Network network;
    [SerializeField] private EditOpen editOpen;
    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private RectTransform textBoxRectTransform;
    [SerializeField] private RectTransform selectBoxRectTransform;
    [SerializeField] private Text textRender;

    private bool active;
    private int selectOption;

    // Start is called before the first frame update
    void Start()
    {
        active = false;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (!active || Input.GetMouseButtonDown(1))
        {
            active = Input.GetMouseButtonDown(1) && !mouseOverAnyNode;
            rectTransform.anchoredPosition = Input.mousePosition;
        }
        else
            active = !Input.GetMouseButtonDown(0);

        mouseOverAnyNode = false;

        transform.GetChild(0).gameObject.SetActive(active);
        transform.GetChild(1).gameObject.SetActive(active);

        if (!active)
            return;

        string[] matches = NodeLoader.QueryNodeTypes(editOpen.text);
        textRender.text = string.Join("\n", matches);
        textBoxRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, matches.Length * 26);

        selectBoxRectTransform.gameObject.SetActive(matches.Length != 0);

        if (Input.GetKeyDown(KeyCode.UpArrow))
            selectOption--;
        else if (Input.GetKeyDown(KeyCode.DownArrow))
            selectOption++;
        else if (Input.GetKeyDown(KeyCode.Return))
        {
            network.AddNode(matches[selectOption]);
            active = false;
        }

        selectOption = Mathx.WrapMod(selectOption, Mathf.Max(1, matches.Length));

        selectBoxRectTransform.anchoredPosition = new Vector2(0, Mathf.Max(2.5f - selectOption * 26, 31 - matches.Length * 26));
    }
}
