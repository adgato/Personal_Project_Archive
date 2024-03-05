using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SitDown : MonoBehaviour
{
    public float yRotation = 0;
    public float maxClamp = 90;

    public bool seated = false;
    private bool inputSeat = false;
    public GameObject player;
    private MeshRenderer playerMesh;
    private PlayerController playerController;

    public GameObject textDisplay;
    private Text toggleText;

    // Start is called before the first frame update
    void Start()
    {
        toggleText  = textDisplay.GetComponent<Text>();
        playerController = player.GetComponent<PlayerController>();
        playerMesh       = player.GetComponent<MeshRenderer>();
    }

    private void Update()
    {
        //User input captured here as it seems more reliable than in FixedUpdate()
        if (Input.GetKeyDown(KeyCode.E))
            inputSeat = true;
        else if (Input.GetKeyUp(KeyCode.E))
            inputSeat = false;
    }

    void FixedUpdate()
    {

        GameObject seat = FindSittable();
        toggleText.gameObject.SetActive(seat != null && !seated);

        if (inputSeat && seat != null)
        {
            inputSeat = false;

            if (!seated)
            {
                //Move the camera to the position and orientation of the seat
                transform.position = new Vector3(seat.transform.position.x, seat.transform.position.y + 1.5f, seat.transform.position.z);
                transform.rotation = Quaternion.Euler(0, seat.transform.eulerAngles.y, 0);

                yRotation = transform.localEulerAngles.y;
                maxClamp = 30;
            }
            else
            {
                //Return to original position
                transform.localPosition = new Vector3(0, 0.5f, 0);

                yRotation = 0;
                maxClamp = 90;
                playerController.yRotation = transform.eulerAngles.y; //yRotation cannot change in PlayerController.cs when it is reset here, so I won't run into any issues this time...
            }

            playerMesh.enabled = seated;
            seated = !seated;
        }

    }

    private GameObject FindSittable()
    {
        GameObject[] seats = GameObject.FindGameObjectsWithTag("Seat");
        //Return the GameObject of a seat that can be sat on, or null if there are none
        for (int i = 0; i < seats.Length; i++)
        {
            if (Sittable(seats[i].transform.position))
            {
                return seats[i];
            }
        }
        return null;
    }
    private bool Sittable(Vector3 benchPos)
    {
        //A seat can be sat on if the camera is pointing in the seat's general direction and is close to the seat
        Vector2 benchCam = Camera.main.WorldToViewportPoint(benchPos);
        return (Mathf.Round(benchCam.x * 4) == 2 && Vector3.Distance(benchPos, transform.position) < 4); //Issue where looking in opposite direction to seat also returns true
    }
}
