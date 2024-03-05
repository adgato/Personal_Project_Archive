using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[System.Serializable]
public class Style
{
    public StyleData styleData;

    private TMP_TextInfo textInfo;
    private int letterIndex;

    private Vector2 vertexOffsetTime;

    public Style(StyleData styleData, TextMeshProUGUI textComponent, int letterIndex)
    {
        this.styleData = styleData;
        textInfo = textComponent.textInfo;
        this.letterIndex = letterIndex;
    }

    public void Stylise()
    {
        float displacement = 0;
        foreach (Wave wave in styleData.waves)
            displacement += wave.Displacement(textInfo.characterInfo[letterIndex].bottomLeft.x);

        vertexOffsetTime += styleData.vertexOffset.speed * Time.deltaTime;
        AddVertexOffset();
        AddYOffset(displacement);
    }

    private void SetLetterColour(Color colour)
    {
        SetLetterColour(colour, colour, colour, colour);
    }

    private void SetLetterColour(Color topLeft, Color bottomRight, bool horizontal)
    {
        SetLetterColour(topLeft, horizontal ? bottomRight : topLeft, horizontal ? topLeft : bottomRight, bottomRight);
    }

    private void SetLetterColour(Color topLeft, Color topRight, Color bottomLeft, Color bottomRight)
    {
        Color32[] vertexColours = textInfo.meshInfo[0].colors32;

        int i = textInfo.characterInfo[letterIndex].vertexIndex;

        if (i == 0 && letterIndex != 0)
            return;

        vertexColours[i + 0] = bottomLeft;
        vertexColours[i + 1] = topLeft;
        vertexColours[i + 2] = topRight;
        vertexColours[i + 3] = bottomRight;
    }

    private void AddYOffset(float y)
    {
        Vector3 displacement = Vector3.up * y;

        Vector3[] vertexPoints = textInfo.meshInfo[0].vertices;

        int i = textInfo.characterInfo[letterIndex].vertexIndex;

        if (i == 0 && letterIndex != 0)
            return;

        vertexPoints[i + 0] += displacement;
        vertexPoints[i + 1] += displacement;
        vertexPoints[i + 2] += displacement;
        vertexPoints[i + 3] += displacement;
    }

    private void AddVertexOffset()
    {
        Vector3[] vertexPoints = textInfo.meshInfo[0].vertices;

        int i = textInfo.characterInfo[letterIndex].vertexIndex;

        if (i == 0 && letterIndex != 0)
            return;

        float a = vertexPoints[i + 0].sqrMagnitude;
        float b = vertexPoints[i + 1].sqrMagnitude;
        float c = vertexPoints[i + 2].sqrMagnitude;
        float d = vertexPoints[i + 3].sqrMagnitude;

        vertexPoints[i + 0] += new Vector3(CosNoise(vertexOffsetTime.x, a), SinNoise(vertexOffsetTime.y, a), 0);
        vertexPoints[i + 1] += new Vector3(CosNoise(vertexOffsetTime.x, b), SinNoise(vertexOffsetTime.y, b), 0);
        vertexPoints[i + 2] += new Vector3(CosNoise(vertexOffsetTime.x, c), SinNoise(vertexOffsetTime.y, c), 0);
        vertexPoints[i + 3] += new Vector3(CosNoise(vertexOffsetTime.x, d), SinNoise(vertexOffsetTime.y, d), 0);
    }

    private float CosNoise(float t, float seed)
    {
        return styleData.vertexOffset.amplidutde.x * Mathf.PerlinNoise(t, seed) * Mathf.Cos(4 * Mathf.PI * Mathf.PerlinNoise(t, -seed));
    }
    private float SinNoise(float t, float seed)
    {
        return styleData.vertexOffset.amplidutde.y * Mathf.PerlinNoise(t, seed) * Mathf.Sin(4 * Mathf.PI * Mathf.PerlinNoise(t, -seed));
    }
}
