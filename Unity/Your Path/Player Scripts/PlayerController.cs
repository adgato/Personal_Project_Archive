using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{

    private CharacterController controller;
    private Vector3 fallVelocity;
    public bool jumpAllowed = true;
    public float yRotation  = 0f;

    public float jumpHeight = 1f;
    public float gravityValue = -15f;
    public float groundSpeed = 5f;
    public float sprintBonus = 1.5f; //sprintSpeed = groundSpeed * sprintBonus
    public float lookSpeed = 3f;

    public GameObject sit;
    private SitDown state;

    // Start is called before the first frame update
    void Start()
    {
        controller = GetComponent<CharacterController>();
        state = sit.GetComponent<SitDown>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!state.seated)
        {
            yRotation += Input.GetAxis("Mouse X") * lookSpeed;
            transform.rotation = Quaternion.Euler(0f, yRotation, 0f);

            if (Input.GetKeyDown(KeyCode.LeftShift)) 
                groundSpeed *= sprintBonus;
            else if (Input.GetKeyUp(KeyCode.LeftShift)) 
                groundSpeed /= sprintBonus;

            Vector3 moveVelocity = (transform.right * Input.GetAxis("Horizontal") + transform.forward * Input.GetAxis("Vertical")) * groundSpeed;

            if (Input.GetKeyDown(KeyCode.Space) && jumpAllowed)
            {
                fallVelocity.y = Mathf.Sqrt(jumpHeight * -3.0f * gravityValue);
                jumpAllowed = false;
            }

            if (jumpAllowed) 
                fallVelocity.y = -5;
            else 
                fallVelocity.y += gravityValue * Time.deltaTime;

            if ((transform.position + moveVelocity * Time.deltaTime).x < -200 || (transform.position + moveVelocity * Time.deltaTime).x > 200)
                moveVelocity = new Vector3(0, 0, moveVelocity.z);
            if ((transform.position + moveVelocity * Time.deltaTime).z < -200 || (transform.position + moveVelocity * Time.deltaTime).z > 200)
                moveVelocity = new Vector3(moveVelocity.x, 0, 0);

            controller.Move((moveVelocity + fallVelocity) * Time.deltaTime);

            if (controller.isGrounded) 
                jumpAllowed = true;
            else 
                jumpAllowed = false;
        }
    }

}
