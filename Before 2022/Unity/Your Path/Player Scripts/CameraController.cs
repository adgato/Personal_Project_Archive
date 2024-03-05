using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float lookSpeed = 3f;
    private float xRotation = 0f;
    private SitDown seated;

    private void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        seated = GetComponent<SitDown>();
    }

    // Update is called once per frame
    void Update()
    {
        //Already implemented X-Axis rotation in PlayerController.cs
        xRotation -= Input.GetAxis("Mouse Y") * lookSpeed;
        xRotation = Mathf.Clamp(xRotation, -90f, seated.maxClamp);
        transform.localRotation = Quaternion.Euler(xRotation, seated.yRotation, 0f);
    }
}
