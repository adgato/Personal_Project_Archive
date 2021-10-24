using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToggleMusic : MonoBehaviour
{
    public Sprite Music;
    public Sprite Effects;
    public Sprite Mute;
    private int cycle = 1;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            cycle = HandleAudio(cycle);
        }
    }

    void OnMouseDown()
    {
        cycle = HandleAudio(cycle);
    }

    int HandleAudio(int cycle)
    {
        if (cycle == 0)
        {
            GetComponent<SpriteRenderer>().sprite = Music;
            GetComponent<AudioSource>().mute = false;
            AudioListener.volume = 1;
        }
        else if (cycle == 1)
        {
            GetComponent<SpriteRenderer>().sprite = Effects;
            GetComponent<AudioSource>().mute = true;
        }
        else
        {
            GetComponent<SpriteRenderer>().sprite = Mute;
            AudioListener.volume = 0;
        }
            

        return (cycle + 1) % 3;
    }
}
