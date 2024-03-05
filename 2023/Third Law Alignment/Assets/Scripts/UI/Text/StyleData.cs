using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class StyleData
{
    private static Dictionary<string, StyleData> loadedStyles = new Dictionary<string, StyleData>();

    public Wave[] waves;
    [System.Serializable]
    public struct VertexOffset
    {
        public Vector2 amplidutde;
        public Vector2 speed;
    }
    public VertexOffset vertexOffset;

    public static StyleData Get(string style)
    {
        if (loadedStyles.ContainsKey(style))
            return loadedStyles[style];

        StyleData styleData = JsonSaver.LoadResource<StyleData>("Styles/" + style);
        loadedStyles.Add(style, styleData);
        return styleData;
    }
}
