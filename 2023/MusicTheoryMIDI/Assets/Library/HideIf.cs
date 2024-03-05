using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HideWhen : PropertyAttribute
{
    public string conditionalSourceField;
    public bool matches;

    public HideWhen(string booleanFieldName, bool state)
    {
        conditionalSourceField = booleanFieldName;
        matches = state;
    }
}
