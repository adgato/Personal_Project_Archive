using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Prefabs : MonoBehaviour
{
    [SerializeField] private GameObject portPrefab;
    [SerializeField] private GameObject arcPrefab;
    [SerializeField] private GameObject nodePrefab;

    public static GameObject Port;
    public static GameObject Arc;
    public static GameObject Node;

    private void Awake()
    {
        Port = portPrefab;
        Arc = arcPrefab;
        Node = nodePrefab;
    }
}
