using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Console", menuName = "Single Behaviour/Console")]
public class Console : SingleBehaviour
{
    [SerializeField] private GameObject console;
    private TMPro.TextMeshProUGUI textMesh;

    private static string consoleText = "";

    public override void Start()
    {
        textMesh = Instantiate(console).transform.GetChild(0).GetChild(0).GetComponent<TMPro.TextMeshProUGUI>();
    }
    public override void Update()
    {
        textMesh.text = consoleText;
    }

    public static void WriteLine()
    {
        WriteLine("");
    }
    public static void WriteLine(string input)
    {
        Write(input + "\n");
    }
    public static void Write(string input)
    {
        consoleText += input;
    }
}

