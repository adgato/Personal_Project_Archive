using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(TextMeshProUGUI))]
public class TextAnimator : MonoBehaviour
{
    private TextMeshProUGUI textComponent;
    [SerializeField] private List<Style> letters = new List<Style>();
    [SerializeField] private string InitialStyle = "default";
    private string CurrentStyle;

    private void Awake()
    {
        textComponent = GetComponent<TextMeshProUGUI>();
    }

    void Start()
    {
        UpdateStyle(InitialStyle);
        //textComponent.ForceMeshUpdate();
        //SetLetterColour(0, Color.red, Color.green, Color.blue, Color.yellow);
        //textComponent.UpdateVertexData();
    }

    public void UpdateStyle(string newStyle)
    {
        CurrentStyle = newStyle;
        letters.Clear();
        StyleData styleData = StyleData.Get(newStyle);
        for (int i = 0; i < textComponent.text.Length; i++)
        {
            letters.Add(new Style(styleData, textComponent, i));
        }
    }

    void Update()
    {
        textComponent.ForceMeshUpdate();
        if (letters.Count != textComponent.text.Length)
            UpdateStyle(CurrentStyle);
        for (int i = 0; i < letters.Count; i++)
        {
            letters[i].Stylise();
        }
        textComponent.UpdateVertexData();
    }


}
