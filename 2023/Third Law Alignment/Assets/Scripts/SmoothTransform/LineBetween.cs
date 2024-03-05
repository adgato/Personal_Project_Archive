using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LineBetween : MonoBehaviour
{
    [SerializeField] private Transform a;
    [SerializeField] private Transform b;

    void LateUpdate()
    {
        transform.position = (a.position + b.position) / 2;
        transform.rotation = Quaternion.LookRotation(a.position - b.position) * Quaternion.Euler(90, 90, 90);
        transform.localScale = new Vector3(transform.localScale.x, (a.position - b.position).magnitude / 2, transform.localScale.z);
    }
}
