using UnityEngine;
using System.Collections.Generic;
using System.Linq;
/// <summary>
/// A dynamic enumeration attribute. Should be used on <see cref="int"/> fields.
/// </summary>
public class OptionList : PropertyAttribute
{
    /// <summary>
    /// The name of an array of strings.
    /// </summary>
    public string optionsName;
    public bool flags = false;
}