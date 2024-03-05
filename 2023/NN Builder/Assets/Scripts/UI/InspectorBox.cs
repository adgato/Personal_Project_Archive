using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using UnityEngine;

public class InspectorBox : MonoBehaviour
{
    public static Node inspectNode;
    [SerializeField] private RectTransform textBoxRectTransform;
    [SerializeField] private Text fieldsTextRender;
    [SerializeField] private Text valuesTextRender;


    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        EditFields();
        //inspectNode.GetType().GetField("test").SetValue(inspectNode, );
    }


    void EditFields()
    {
        if (inspectNode == null)
            return;

        string[] fields = inspectNode.GetType().GetFields().Where(field => field.IsPublic && !typeof(Node).GetFields().Select(field => field.Name).Contains(field.Name)).Select(field => GetInspectorName(field.Name)).ToArray();
        string[] values = inspectNode.GetType().GetFields().Where(field => field.IsPublic && !typeof(Node).GetFields().Select(field => field.Name).Contains(field.Name)).Select(field => field.GetValue(inspectNode).ToString())
            .Select(value => value.Substring(0, Mathf.Min(15, value.Length))).ToArray();

        fieldsTextRender.text = string.Join("\n", fields);
        valuesTextRender.text = string.Join("\n", values);

        textBoxRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, fields.Length * 26);
    }

    public static string GetInspectorName(string field)
    {
        string fieldName = string.Concat(field.Select(letter => letter.ToString() == letter.ToString().ToUpper() ? " " + letter.ToString() : letter.ToString()));
        return fieldName[0].ToString().ToUpper() + fieldName.Substring(1, fieldName.Length - 1);
    }
}
