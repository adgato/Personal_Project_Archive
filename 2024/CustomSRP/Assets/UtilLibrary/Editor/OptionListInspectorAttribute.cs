using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(OptionList))]
public class OptionListDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        OptionList optionList = (OptionList)attribute;
        string path = property.propertyPath.Replace(property.name, optionList.optionsName);

        SerializedProperty optionsProperty = property.serializedObject.FindProperty(path);

        if (optionsProperty == null || !optionsProperty.isArray)
        {
            EditorGUI.PropertyField(position, property, label);
            return;
        }
        else if (optionsProperty.arraySize == 0)
            return;
        string[] options = new string[optionsProperty.arraySize];
        for (int i = 0; i < options.Length; i++)
            options[i] = i + ": " + optionsProperty.GetArrayElementAtIndex(i).stringValue;

        property.intValue = Mathf.Clamp(property.intValue, 0, options.Length);
        property.intValue = optionList.flags ? EditorGUI.MaskField(position, label, property.intValue, options) : EditorGUI.Popup(position, label.text, property.intValue, options);
    }
}