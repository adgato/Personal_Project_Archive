using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(TextMesh))]
public class TextRotate : MonoBehaviour
{
    public GameObject cam;

    public float timePause = 1f;
    private float timeEnabled = 0f;

    [TextArea]
    public string[] dialogue = new string[]
    {
        "It's so peaceful here...", 
        "I almost don't want to leave...", 
        "I suppose I'm afraid if I did,\nI wouldn't be able to return",
        "But I know I must...",
        "Maybe someone will come to guide me?",
        "Until then, I'll wait..."
    };

    // Update is called once per frame
    void LateUpdate()
    {
        transform.rotation = Quaternion.Euler(0, cam.transform.rotation.eulerAngles.y, 0);

        int index = (int)(timeEnabled / timePause);
        GetComponent<MeshRenderer>().enabled = Vector3.Distance(cam.transform.position, transform.position) < 10 && index < dialogue.Length;
        if (GetComponent<MeshRenderer>().enabled)
        {
            timeEnabled += Time.deltaTime;
            GetComponent<TextMesh>().text = dialogue[index];
        }
        //If the player has seen all the dialogue and is out of the text range: allow the player to read all the dialogue again if they enter the text range
        else if ( !(Vector3.Distance(cam.transform.position, transform.position) < 10 || index < dialogue.Length) )
        {
            timeEnabled = 0f;
        }
    }
}
