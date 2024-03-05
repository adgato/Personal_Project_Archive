using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class tempCam : MonoBehaviour
{
    public float moveSpeed;
    public float lookSpeed;
    public float gravityStrength;
    private float xRot;
    public float yRot { get; private set; }
    private CharacterController Controller;
    private float gravity;
    private void Start()
    {
        Controller = GetComponent<CharacterController>();
        gravity = 0;
    }
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftShift))
            moveSpeed *= 3;
        else if (Input.GetKeyUp(KeyCode.LeftShift))
            moveSpeed /= 3;

        yRot += Input.GetAxis("Mouse X") * lookSpeed;
        xRot -= Input.GetAxis("Mouse Y") * lookSpeed;
        xRot = Mathf.Clamp(xRot, -90, 90);

        if (Controller.isGrounded)
            gravity = 0;

        gravity -= gravityStrength * Time.deltaTime;

        Controller.Move(Time.deltaTime * moveSpeed * (transform.forward * Input.GetAxis("Vertical") + transform.right * Input.GetAxis("Horizontal")
            + transform.position.normalized * (Input.GetKey(KeyCode.E) ? 1 : Input.GetKey(KeyCode.Q) ? -1 : gravity)));
    }
    void LateUpdate()
    {
        
        transform.rotation = Quaternion.Euler(Quaternion.LookRotation(-transform.position).eulerAngles - Vector3.right * 90) * Quaternion.Euler(0, yRot, 0);
        transform.GetChild(0).localRotation = Quaternion.Euler(xRot, 0, 0);
    }
}
