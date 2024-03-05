using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Inherit from SmoothMonoBehaviour for quick / invisible implementation of SmoothTransform
/// </summary>
[RequireComponent(typeof(SmoothWorldTransform))]
public class SmoothWorldMonoBehaviour : MonoBehaviour
{
    [HideInInspector] public new SmoothWorldTransform transform;
    public Transform baseTransform => base.transform;

    protected virtual void Awake()
    {
        transform = GetComponent<SmoothWorldTransform>();
    }

}
