using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class SceneWatcher<T> : ScriptableObject where T : struct, IEquatable<T>
{
    private T[] GetData() => FindObjectsOfType<MonoWatchable<T>>().Select(x => x.GetData()).ToArray();

    public void OnChange() => PushChanges(GetData());

    protected abstract void PushChanges(T[] data);
}