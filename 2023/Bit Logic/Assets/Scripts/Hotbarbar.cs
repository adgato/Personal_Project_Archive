using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Hotbarbar : MonoBehaviour
{
    [SerializeField] private RectTransform hotbar;
    [SerializeField] private Color selectColour;
    [SerializeField] private Color pressColour;
    [SerializeField] private Color defaultColour;

    private KeyCode AlphaN = KeyCode.Alpha1;

    public readonly Dictionary<KeyCode, int> hotbarIndicies = new Dictionary<KeyCode, int>(10)
    {
        { KeyCode.Alpha1, 0 },
        { KeyCode.Alpha2, 1 },
        { KeyCode.Alpha3, 2 },
        { KeyCode.Alpha4, 3 },
        { KeyCode.Alpha5, 4 },
        { KeyCode.Alpha6, 5 },
        { KeyCode.Alpha7, 6 },
        { KeyCode.Alpha8, 7 },
        { KeyCode.Alpha9, 8 },
        { KeyCode.Alpha0, 9 }
    };

    private RawImage GetBox(KeyCode AlphaN) => hotbar.GetChild(hotbarIndicies[AlphaN]).GetComponent<RawImage>();
    private Text GetText(KeyCode AlphaN) => hotbar.GetChild(hotbarIndicies[AlphaN]).GetChild(0).GetComponent<Text>();

    public void Bind(KeyCode AlphaN, string nodename)
    {
        GetText(AlphaN).text = $"<b>{(hotbarIndicies[AlphaN] + 1) % 10}</b> <i>{nodename}</i>";
    }
    public void Select(KeyCode AlphaN)
    {
        this.AlphaN = AlphaN;
        foreach (KeyCode key in hotbarIndicies.Keys)
            GetBox(key).color = AlphaN == key ? pressColour : defaultColour;
    }
    public void ReSelect()
    {
        foreach (KeyCode key in hotbarIndicies.Keys)
            GetBox(key).color = AlphaN == key ? selectColour : defaultColour;
    }
    public void SelectAll()
    {
        foreach (KeyCode AlphaN in hotbarIndicies.Keys)
            GetBox(AlphaN).color = selectColour;
    }
    public void UnSelect(KeyCode AlphaN)
    {
        GetBox(AlphaN).color = selectColour;
    }
}
