using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// If this object has a SmoothTransform parent, consider using the base class for correct movement, otherwise, this is more efficient.
/// </summary>
public class SmoothWorldTransform : MonoBehaviour
{
    public Vector3 position
    {
        get => _position;
        set => _position = value;
    }
    public Vector3 localPosition
    {
        get => transform.parent.InverseTransformPoint(_position); 
        set => _position = transform.parent.TransformPoint(value);
    }
    public Quaternion rotation
    {
        get => _rotation; 
        set => _rotation = value;
    }
    public Quaternion localRotation
    {
        get => Quaternion.Inverse(transform.parent.rotation) * rotation; 
        set => _rotation = transform.parent.rotation * value;
    }

    public Vector3 right => rotation * Vector3.right;
    public Vector3 up => rotation * Vector3.up;
    public Vector3 forward => rotation * Vector3.forward;
    public Vector3 eulerAngles => rotation.eulerAngles;
    public Vector3 localEulerAngles => localRotation.eulerAngles;

    private Vector3 _position;
    private Quaternion _rotation;

    private Rigidbody rb;

    private void Awake()
    {
        rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        SyncToTransform();
    }

    /// <summary>
    /// After Default Time.
    /// </summary>
    private void FixedUpdate()
    {
        rb.MovePosition(position);
        rb.MoveRotation(rotation);
    }

    private void SyncToTransform()
    {
        _position = transform.position;
        _rotation = transform.rotation;
    }

    public Vector3 TransformDirection(Vector3 localDirection) => rotation * localDirection;
    public Vector3 TransformPoint(Vector3 localPoint) => Matrix4x4.TRS(position, rotation, transform.lossyScale).MultiplyPoint(localPoint);
    public Vector3 TransformVector(Vector3 localVector) => Matrix4x4.TRS(Vector3.zero, rotation, transform.lossyScale).MultiplyPoint(localVector);
    public Vector3 InverseTransformDirection(Vector3 direction) => Quaternion.Inverse(rotation) * direction;
    public Vector3 InverseTransformPoint(Vector3 position) => Matrix4x4.TRS(position, rotation, transform.lossyScale).inverse.MultiplyPoint(position);
    public Vector3 InverseTransformVector(Vector3 vector) => Matrix4x4.TRS(Vector3.zero, rotation, transform.lossyScale).inverse.MultiplyPoint(vector);
}
