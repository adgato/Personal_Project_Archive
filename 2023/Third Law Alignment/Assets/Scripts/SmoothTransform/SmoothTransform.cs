using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The aim of this class is to allow for updating transforms immediately and in a heirarchy during fixed update, whilst interpolating their rendered positions during update 
/// </summary>
public class SmoothTransform : MonoBehaviour 
{
    public virtual Vector3 position
    {
        get => parent.TransformPoint(_localPosition);
        set => _localPosition = parent.InverseTransformPoint(value);
    }
    public virtual Vector3 localPosition
    {
        get => _localPosition; 
        set => _localPosition = value;
    }
    public virtual Quaternion rotation
    {
        get => parent.rotation * _localRotation; 
        set => _localRotation = Quaternion.Inverse(parent.rotation) * value;
    }
    public virtual Quaternion localRotation
    {
        get => _localRotation; 
        set => _localRotation = value;
    }

    public Vector3 _localPosition;
    public Quaternion _localRotation;
    private Vector3 last_localPosition;
    private Quaternion last_localRotation;

    public Vector3 right => rotation * Vector3.right;
    public Vector3 up => rotation * Vector3.up;
    public Vector3 forward => rotation * Vector3.forward;
    public Vector3 eulerAngles => rotation.eulerAngles;
    public Vector3 localEulerAngles => localRotation.eulerAngles;


    [HideInInspector] public SmoothTransform parent = null;

    protected virtual void Awake()
    {
        if (!transform.parent.TryGetComponent(out parent))
            parent = transform.parent.gameObject.AddComponent<StaticTransform>();

        SyncToTransform();
    }


    /// <summary>
    /// Before Default Time.
    /// </summary>
    protected virtual void FixedUpdate()
    {
        last_localPosition = _localPosition;
        last_localRotation = _localRotation;
    }

    protected virtual void LateUpdate()
    {
        float t = (Time.time - Time.fixedTime) / Time.fixedDeltaTime;
        transform.SetLocalPositionAndRotation(Vector3.Lerp(last_localPosition, _localPosition, t), Quaternion.Slerp(last_localRotation, _localRotation, t));
    }

    private void SyncToTransform()
    {
        last_localPosition = _localPosition = transform.localPosition;
        last_localRotation = _localRotation = transform.localRotation;
    }

    public virtual Vector3 TransformDirection(Vector3 localDirection) => rotation * localDirection;
    public virtual Vector3 TransformPoint(Vector3 localPoint) => Matrix4x4.TRS(position, rotation, transform.lossyScale).MultiplyPoint3x4(localPoint);
    public virtual Vector3 TransformVector(Vector3 localVector) => Matrix4x4.TRS(Vector3.zero, rotation, transform.lossyScale).MultiplyPoint3x4(localVector);
    public virtual Vector3 InverseTransformDirection(Vector3 direction) => Quaternion.Inverse(rotation) * direction;
    public virtual Vector3 InverseTransformPoint(Vector3 position) => Matrix4x4.TRS(position, rotation, transform.lossyScale).inverse.MultiplyPoint3x4(position);
    public virtual Vector3 InverseTransformVector(Vector3 vector) => Matrix4x4.TRS(Vector3.zero, rotation, transform.lossyScale).inverse.MultiplyPoint3x4(vector);
}

