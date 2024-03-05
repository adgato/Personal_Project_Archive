using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HideWhen : PropertyAttribute
{
    public enum Boolean { True, False }

    /// <summary>
    /// The enumeration should not have custom values.
    /// </summary>
    public string enumFieldName;
    public int matches;
}
public class ShowWhen : PropertyAttribute
{
    public enum Boolean { True, False }

    /// <summary>
    /// The enumeration should not have custom values.
    /// </summary>
    public string enumFieldName;
    public int matches;
}

