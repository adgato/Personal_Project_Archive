using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(HideWhen))]
public class HideIfDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        bool enabled = !Hide(property);

        bool wasEnabled = GUI.enabled;
        GUI.enabled = enabled;

        if (enabled)
            EditorGUI.PropertyField(position, property, label, true);

        GUI.enabled = wasEnabled;
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => !Hide(property) ? EditorGUI.GetPropertyHeight(property, label) : 0;

    private bool Hide(SerializedProperty property)
    {
        HideWhen hideWhen = (HideWhen)attribute;
        SerializedProperty sourcePropertyValue = property.serializedObject.FindProperty(hideWhen.conditionalSourceField);
        return sourcePropertyValue == null ? true : sourcePropertyValue.boolValue == hideWhen.matches;
    }
}
