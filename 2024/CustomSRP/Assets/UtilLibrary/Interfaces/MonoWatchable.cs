using System;
using UnityEngine;

public abstract class MonoWatchable<T> : MonoBehaviour where T : struct, IEquatable<T>
{
    [SerializeField] private SceneWatcher<T> watcher;

    protected void OnChange()
    {
        if (watcher == null)
        {
            Debug.LogError("Error: watcher has not been assigned");
            return;
        }
        watcher.OnChange();
    }

    public abstract T GetData();

}