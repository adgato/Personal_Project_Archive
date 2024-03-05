using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlashLight : MonoBehaviour
{
    public GameObject campFire;
    public GameObject campFire2;
    public GameObject sit;
    private SitDown state;
    private Light toggleLight;

    // Start is called before the first frame update
    void Start()
    {
        state = sit.GetComponent<SitDown>();
        toggleLight = GetComponent<Light>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Vector3.Distance(campFire.transform.position, transform.position) < 15f || Vector3.Distance(campFire2.transform.position, transform.position) < 10f) 
        { 
            toggleLight.enabled = false; 
        }
        else                                                                                            
        { 
            toggleLight.enabled = true; 
        }

        if  (state.seated) { transform.localPosition = new Vector3(0, -1, 0);  }
        else                 { transform.localPosition = new Vector3(0, -1, -1); }
    }
}
