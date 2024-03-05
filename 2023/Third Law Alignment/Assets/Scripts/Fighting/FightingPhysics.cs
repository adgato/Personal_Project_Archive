using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
[RequireComponent(typeof(FightingRobot))]
public class FightingPhysics
{
    private RobotBody r;

    private Vector3 velocity;
    [SerializeField] private float G = 10;
    [Range(0, 1)]
    [SerializeField] private float drag01 = 0.05f;
    [Range(0, 1)]
    [SerializeField] private float friction01 = 0.1f;
    [SerializeField] private float crouchFrictionMultiplier = 2;
    [SerializeField] private float shieldFrictionMultiplier = 2;
    [SerializeField] private float jumpSpeed;
    [SerializeField] private float fallSpeed;
    [SerializeField] private float minDashSpeed;
    [SerializeField] private float maxDashSpeed;


    private float horizontalSpeed;

    [HideInInspector] public bool fixedVelocity;


    public void Start(RobotBody robot)
    {
        r = robot;
    }

    public void Update()
    {

    }

    public Vector3 FixedUpdate()
    {
        if (!fixedVelocity)
            Accelerate();

        return velocity * r.dt;
    }


    private void Accelerate()
    {
        float resistance01 = !r.IsGrounded() ? drag01 : friction01;
        if (r.arms.Shielding())
            resistance01 *= shieldFrictionMultiplier;
        if (r.legs.IsCrouching)
            resistance01 *= crouchFrictionMultiplier;

        velocity -= Mathf.Clamp01(resistance01) * 50 * r.dt * velocity;
        UpdateHorizontalSpeed();

        if (Vector3.Dot(velocity, r.normalUp) < -fallSpeed)
            velocity += (-fallSpeed - Vector3.Dot(velocity, r.normalUp)) * r.normalUp;
        else if (Vector3.Dot(velocity, r.normalUp) > jumpSpeed)
            velocity += (jumpSpeed - Vector3.Dot(velocity, r.normalUp)) * r.normalUp;
        else
            velocity += G * r.GetGravitationalAcceleration() * r.dt;
    }

    public float GetHorizontalSpeed()
    {
        return horizontalSpeed;
    }
    public Vector3 GetObservedVelocity()
    {
        return r.IsGrounded() ? velocity - Vector3.Dot(velocity, r.normalUp) * r.normalUp : velocity;
    }
    void UpdateHorizontalSpeed()
    {
        horizontalSpeed = (velocity - Vector3.Dot(velocity, r.normalUp) * r.normalUp).magnitude;
    }

    public void Jump(float speed01)
    {
        velocity += (jumpSpeed * speed01 - Vector3.Dot(velocity, r.normalUp)) * r.normalUp;
    }


    public Vector3 Dash(float speed01, Vector3 localDashDirection)
    {
        Vector3 input = r.transform.TransformDirection(localDashDirection);

        Vector3 velocity01 = r.IsGrounded() ? Vector3.zero : velocity.Clamp01();

        velocity += (input + velocity01).normalized * Mathf.Lerp(minDashSpeed, maxDashSpeed, speed01);
        UpdateHorizontalSpeed();

        return velocity;
    }

    public void Launch(Vector3 globalLaunchVector)
    {
        velocity += globalLaunchVector;
        UpdateHorizontalSpeed();
    }
}
