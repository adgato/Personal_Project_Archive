using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StaticTransform : SmoothTransform
{
    public override Vector3 position => transform.position;
    public override Vector3 localPosition => transform.localPosition;
    public override Quaternion rotation => transform.rotation; 
    public override Quaternion localRotation => transform.localRotation; 

    public override Vector3 TransformDirection(Vector3 localDirection) => transform.TransformDirection(localDirection);
    public override Vector3 TransformPoint(Vector3 localPoint) => transform.TransformPoint(localPoint);
    public override Vector3 TransformVector(Vector3 localVector) => transform.TransformVector(localVector);
    public override Vector3 InverseTransformDirection(Vector3 direction) => transform.InverseTransformDirection(direction);
    public override Vector3 InverseTransformPoint(Vector3 position) => transform.InverseTransformPoint(position);
    public override Vector3 InverseTransformVector(Vector3 vector) => transform.InverseTransformVector(vector);

    protected override void Awake()
    {

    }

    protected override void FixedUpdate()
    {

    }

    protected override void LateUpdate()
    {

    }
}
