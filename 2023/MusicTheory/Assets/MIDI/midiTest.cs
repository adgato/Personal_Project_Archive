using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TobiasErichsen.teVirtualMIDI;

public class midiTest : MonoBehaviour
{
    TeVirtualMIDI port;
    byte[] on = new byte[] { 0x90, 60, 100 };
    byte[] off = new byte[] { 0x80, 60, 100 };

    // Start is called before the first frame update
    void Start()
    {
        port = new TeVirtualMIDI("C# loopback");
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
            port.sendCommand(on);
        if (Input.GetKeyUp(KeyCode.E))
            port.sendCommand(off);
    }
}
