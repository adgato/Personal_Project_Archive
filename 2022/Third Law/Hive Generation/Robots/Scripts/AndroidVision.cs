using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AndroidVision : MonoBehaviour
{
    [SerializeField] private Transform Arms;
    [SerializeField] private Transform Chest;
    [SerializeField] private Transform Head;

    public Transform[] Eyes;

    [SerializeField] private float inertia = 100;

    private Vector3 up;

    private void Start()
    {
        up = FindObjectOfType<HiveGen>().transform.up;
        
    }

    public void LookTowards(Vector3 target)
    {
        //Calculate the rotation to face the target position.
        Quaternion rotation = Quaternion.LookRotation(-(target - transform.position).normalized, up);
        Quaternion flatRotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(-(target - transform.position).normalized, up), up);

        Chest.rotation = Quaternion.Slerp(Chest.rotation, flatRotation, Time.deltaTime / inertia);

        Arms.rotation = Quaternion.Slerp(Arms.rotation, Chest.rotation, Time.deltaTime / inertia);

        Head.rotation = Quaternion.Slerp(Head.rotation, rotation, Time.deltaTime / inertia);

        Head.localRotation = Quaternion.Euler(Mathf.LerpAngle(0, Head.localEulerAngles.x, 0.1f), Head.localEulerAngles.y, Head.localEulerAngles.z);

        //Eyes[0].rotation = rotation * Quaternion.Euler(90, 0, 0);
        //Eyes[1].rotation = rotation * Quaternion.Euler(90, 0, 0);
    }
}
