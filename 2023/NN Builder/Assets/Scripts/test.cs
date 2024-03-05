using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class test : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Matrix a = new Matrix(new float[6] { 1, 2, 3, 4, 5, 6 }, new Vector2Int(3, 2));
        Matrix b = new Matrix(new float[6] { 6, 5, 4, 3, 2, 1 }, new Vector2Int(2, 3));
        a.Log();
        b.Log();
        Matrix.Dot(a, b).Log();
    }
}
