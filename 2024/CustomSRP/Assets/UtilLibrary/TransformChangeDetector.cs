using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[ExecuteAlways]
[DisallowMultipleComponent]
public class TransformChangeDetector : MonoBehaviour
{
    [SerializeField] private UnityEvent OnAnyChange;
    [SerializeField] private UnityEvent OnPositionChange;
    [SerializeField] private UnityEvent OnRotationChange;
    [SerializeField] private UnityEvent OnLocalScaleChange;
    private Vector3 position;
    private Quaternion rotation;
    private Vector3 localScale;

    // Update is called once per frame
    void LateUpdate()
    {
        if (!transform.hasChanged)
            return;
        transform.hasChanged = false;
        bool change = false;
        if (transform.position != position)
        {
            OnPositionChange.Invoke();
            position = transform.position;
            change = true;
        }
        if (transform.rotation != rotation)
        {
            OnRotationChange.Invoke();
            rotation = transform.rotation;
            change = true;
        }
        if (transform.localScale != localScale)
        {
            OnLocalScaleChange.Invoke();
            localScale = transform.localScale;
            change = true;
        }
        if (change)
            OnAnyChange.Invoke();
    }
}
