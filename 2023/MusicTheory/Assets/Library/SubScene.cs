using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SingleBehaviour objects are created in the Project window, which is more suitable for singleton classes than the Hierarchy window.
/// </summary>
public abstract class SingleBehaviour : ScriptableObject
{
    public virtual void Start() { }
    public virtual void Update() { }
}


/// <summary>
/// The purpose of the class is to run Start and Update methods for selected SingleBehaviour objects.
/// </summary>
public class SubScene : MonoBehaviour
{
    [SerializeField] private SingleBehaviour[] executionOrder;

    private void Start()
    {
        foreach (SingleBehaviour singleBehaviour in executionOrder)
            singleBehaviour.Start();
    }

    private void Update()
    {
        foreach (SingleBehaviour singleBehaviour in executionOrder)
            singleBehaviour.Update();
    }
}