using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CanvasHelper
{
    public static bool MouseInRect(RectTransform rectTransform)
    {
        return rectTransform.rect.Contains(rectTransform.InverseTransformPoint(Input.mousePosition));
    }
}
