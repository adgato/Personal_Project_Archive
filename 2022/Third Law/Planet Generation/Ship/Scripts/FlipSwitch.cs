using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlipSwitch : MonoBehaviour
{
    private AudioSource liftSource;
    public enum State { top, bottom, rising, falling }
    private enum Type { innerDoor, outerDoor, wing, light, seat }
    public State switchState { get; private set; }
    private float lerp = 1;
    [SerializeField] State initialSwitchState = State.bottom;
    [SerializeField] private Material ShipLightMat;
    [SerializeField] private Light[] lights;
    [SerializeField] private ParticleSystem[] smokeEmittors;
    [SerializeField] private AudioSource smokeSource;
    [SerializeField] private FlipSwitch innerSwitch;
    [SerializeField] private float lerpSpeed = 1;
    [SerializeField] private Type switchType;
    
    [HideInInspector] public bool overRide = false;
    [HideInInspector] public float doorLerpOffset;
    private float smokeTimer = 6;
    private float softlockTimer = 0;
    private bool softLocked = false;

    // Start is called before the first frame update
    public void Start()
    {
        liftSource = GetComponent<AudioSource>();

        lerp = 0;
        switchState = initialSwitchState;
        transform.localRotation = switchState == State.bottom ? Quaternion.Euler(Vector3.zero) : Quaternion.Euler(Vector3.up * -180);

        if (switchType == Type.light)
        {
            for (int i = 0; i < lights.Length; i++)
            {
                lights[i].color = switchState == State.bottom ? Color.white : Color.black;
                ShipLightMat.SetFloat("_brightness", switchState == State.bottom ? 1 : 0);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (CameraState.isPaused)
            return;

        if (!liftSource.isPlaying && lerp < 1 && lerp > 0)
            PlayLift();

        if (switchType == Type.outerDoor)
        {
            smokeTimer += Time.deltaTime;
            smokeSource.volume = (7 - smokeTimer) / 14;
            if (smokeTimer > 6)
            {
                if (smokeEmittors[0].isPlaying)
                {
                    smokeEmittors[0].Stop();
                    smokeEmittors[1].Stop();
                }
            }
            else if (smokeTimer > 3)
            {
                if (smokeEmittors[0].isStopped)
                {
                    smokeEmittors[0].Play();
                    smokeEmittors[1].Play();
                    smokeSource.Play();
                }
            }
        }

        if (lerp < 1 && (switchState == State.rising || switchState == State.falling))
        {
            transform.localRotation = Quaternion.Euler(Vector3.Slerp(Vector3.zero, Vector3.up * -180, switchState == State.rising ? lerp : (1 - lerp)));
            lerp += Time.deltaTime * lerpSpeed;
        }
        //If switch has finished moving, set its state to top or bottom
        else
        {
            lerp = 1;
            if (switchState == State.rising)
                switchState = State.top;
            else if (switchState == State.falling)
                switchState = State.bottom;
        }

        //If seat is in the way of the switch, extra range is needed
        int maxDist = switchType == Type.seat && switchState == State.top ? 3 : 2;

        //If player is not inside the ship and switch is in bottom state, check if softlock timer has exceeded 1 second, if so set softLocked flag to true
        if (!CameraState.withinShip && switchState == State.bottom)
        {
            if (Time.realtimeSinceStartup > softlockTimer + 1)
                softLocked = true;
        }
        else
            softlockTimer = Time.realtimeSinceStartup;

        if (switchType != Type.outerDoor && CameraState.CamIsInteractingW(transform.position, -transform.parent.right, maxDist, 60) || overRide)
        {
            //If the switch is Type wing then it is to be synchonised with another switch, override this other switch so both are in the same state
            if (switchType == Type.wing && !overRide)
            {
                innerSwitch.overRide = true;
            }

            overRide = false;
            if (switchState == State.bottom)
            {
                lerp = 0;
                switchState = State.rising;
            }
            else if (switchState == State.top)
            {
                lerp = 0;
                switchState = State.falling;
            }
        }
        else if (switchType == Type.outerDoor && (CameraState.CamIsInteractingW(transform.position, -transform.parent.right, 2, 30) || softLocked))
        {
            overRide = true;

            //No smoke if softlocked, since its visible through walls and you're trapped outside
            smokeTimer = softLocked ? 6 : 0; 
            softLocked = false;

            //Make sure the inner switch for the inner door is in the opposite state to the outer switch for the outer door, so at most one door is open (otherwise there is no air lock)
            if (switchState == State.bottom)
            {
                if (innerSwitch.switchState == State.top)
                    innerSwitch.overRide = true;

                //Wait 5 seconds before beginning to open outer door, start to close inner door
                doorLerpOffset = -5;
                innerSwitch.doorLerpOffset = 0;
            }
            else if (switchState == State.top)
            {
                if (innerSwitch.switchState == State.bottom)
                    innerSwitch.overRide = true;

                //Wait 5 seconds before beginning to open inner door, start to close outer door
                doorLerpOffset = 0;
                innerSwitch.doorLerpOffset = -5;
            }
        }

        //If the switch is a light type, update the light color and brightness based on the fuel remaining and the switch state
        if (switchType == Type.light)
        {
            for (int i = 0; i < lights.Length; i++)
            {
                if (InventoryUI.shipFuelRemaining == 0)
                {
                    lights[i].color = Color.black;
                    ShipLightMat.SetFloat("_brightness", 0);
                }
                else if (switchState == State.falling)
                {
                    lights[i].color = Color.Lerp(Color.black, Color.white, lerp);
                    ShipLightMat.SetFloat("_brightness", lerp);
                    InventoryUI.shipEngineOn01 = lerp;
                }
                else if (switchState == State.rising)
                {
                    lights[i].color = Color.Lerp(Color.white, Color.black, lerp);
                    ShipLightMat.SetFloat("_brightness", 1 - lerp);
                    InventoryUI.shipEngineOn01 = 1 - lerp;
                }   
            }
        }
    }

    void PlayLift()
    {
        StartCoroutine(CoPlayLift());
    }
    IEnumerator CoPlayLift()
    {
        float startVolume = liftSource.volume;
        float startPitch = liftSource.pitch;

        liftSource.volume = 0;
        liftSource.pitch = startPitch + (lerp > 0.5f ? 0.1f : -0.1f);
        liftSource.Play();

        while (liftSource.volume < startVolume)
        {
            liftSource.volume += startVolume * Time.deltaTime * 5;
            yield return new WaitForEndOfFrame();
        }

        //Wait until switch has finished rotating before turning "lift" noise off
        yield return new WaitUntil(new System.Func<bool>(() => lerp <= 0 || lerp >= 1));

        while (liftSource.volume > 0)
        {
            liftSource.volume -= startVolume * Time.deltaTime * 5;
            yield return new WaitForEndOfFrame();
        }

        liftSource.Stop();
        liftSource.volume = startVolume;
        liftSource.pitch = startPitch;
    }
}
