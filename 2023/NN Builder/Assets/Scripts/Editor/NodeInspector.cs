using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

public class NodeInspector : EditorWindow
{
    [MenuItem("Window/Node Inspector")]
    public static void ShowWindow()
    {
        GetWindow(typeof(NodeInspector));
    }

    public void OnInspectorUpdate()
    {
        Repaint();
    }

    void OnGUI()
    {
        DrawLayout();
    }

    public static void DrawLayout()
    {
        Node node = InspectorBox.inspectNode;
        if (node == null)
            return;

        string[] fields = node.GetType().GetFields().Where(field => field.IsPublic && !typeof(Node).GetFields().Select(field => field.Name).Contains(field.Name)).Select(field => field.Name).ToArray();
        object[] values = node.GetType().GetFields().Where(field => field.IsPublic && !typeof(Node).GetFields().Select(field => field.Name).Contains(field.Name)).Select(field => field.GetValue(node)).ToArray();

        EditorGUILayout.Space();
        for (int i = 0; i < fields.Length; i++)
        {
            GetFieldEditorGUI(node, fields[i], values[i]);
            EditorGUILayout.Space();
        }
    }

    public static void GetFieldEditorGUI(Node node, string field, object value)
    {
        string fieldName = InspectorBox.GetInspectorName(field);

        switch (value)
        {
            case bool _value:
                node.GetType().GetField(field).SetValue(node, EditorGUILayout.Toggle(fieldName, _value));
                return;

            case string _value:
                node.GetType().GetField(field).SetValue(node, EditorGUILayout.TextField(fieldName, _value));
                return;

            case System.Enum _value:
                node.GetType().GetField(field).SetValue(node, EditorGUILayout.EnumPopup(fieldName, _value));
                return;

            case int _value:
                node.GetType().GetField(field).SetValue(node, EditorGUILayout.IntField(fieldName, _value));
                return;

            case Vector2Int _value:
                node.GetType().GetField(field).SetValue(node, EditorGUILayout.Vector2IntField(fieldName, _value));
                return;

            case Object _value:
                node.GetType().GetField(field).SetValue(node, EditorGUILayout.ObjectField(fieldName, _value, value.GetType(), true));
                return;

            default:
                break;
        };
    }
}
