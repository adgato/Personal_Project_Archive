using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public static class GameObjectEx
{
    /// <summary>
    /// Replaces the child at index with an empty gameobject. Otherwise adds a new empty child.
    /// </summary>
    /// <param name="gameObject">The parent</param>
    /// <param name="index">The index at which to replace</param>
    /// <param name="name">The name of the new empty child</param>
    /// <returns>The transform of the new empty child</returns>
    public static Transform ReplaceChild(this GameObject gameObject, int index, string name)
    {
        Transform child = new GameObject(Mathf.Min(index, gameObject.transform.childCount) + ") " + name).transform;
        child.parent = gameObject.transform;
        child.gameObject.layer = gameObject.layer;
        child.SetSiblingIndex(Mathf.Min(index, gameObject.transform.childCount - 1));
        child.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

        if (index < gameObject.transform.childCount - 1)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying || EditorApplication.isPaused)
                Object.DestroyImmediate(gameObject.transform.GetChild(index + 1).gameObject);
            else
#endif
                Object.Destroy(gameObject.transform.GetChild(index + 1).gameObject);
        }

        return child;
    }
}
