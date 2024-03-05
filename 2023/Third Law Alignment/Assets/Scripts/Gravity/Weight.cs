using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weight : SmoothWorldMonoBehaviour
{
    protected Gravity gravity;

    [Tooltip("Should be equal to the radius of the object, or greater, really.")]
    [Min(1)]
    [SerializeField] private float mass = 1;
    public Vector3 Position => transform.position;
    public Vector3 Velocity => velocity;
    public virtual float Radius => mass;
    public virtual float Mass => mass;
    public virtual bool IsZeroWeight => false;

    [SerializeField] protected Vector3 velocity;


    protected bool positionUpToDate;

    protected virtual void Start()
    {
        gravity = baseTransform.parent.GetComponent<Gravity>();
        gravity.AddWeight(this);
    }
    public void Accelerate(Vector3 acceleration) => velocity += acceleration * Time.deltaTime;

    public virtual void PreUpdate()
    {
        positionUpToDate = false; 
    }

    public virtual void PostUpdate() { }

    /// <summary>
    /// Updates the position of the weight, can only do this once per fixed update.
    /// </summary>
    /// <returns>The change in the position of the weight before and after the update.</returns>
    public virtual void UpdatePosition()
    {
        if (positionUpToDate)
            return;
        Interpolate(GetDisplacement());
        positionUpToDate = true;
    }
    public virtual Vector3 GetDisplacement() => velocity * Time.deltaTime;
    public virtual void UpdateColliders() 
    {
        if (!positionUpToDate)
            Debug.LogWarning("Warning: updating colliders when position is not up to date, continuing anyway.");
    }

    public void Interpolate(Vector3 displacement) => transform.position += displacement;
}