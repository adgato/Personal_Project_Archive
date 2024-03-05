using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public static class ShaderPropertyCheck
{
    public static bool HasProperty(this Shader shader, string propertyName, ShaderPropertyType ofType)
    {
        int index = shader.FindPropertyIndex(propertyName);
        if (index == -1)
            return false;
        return shader.GetPropertyType(index) == ofType;
    }
}
