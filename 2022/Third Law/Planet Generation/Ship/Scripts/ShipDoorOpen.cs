using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipDoorOpen : MonoBehaviour
{
    private AudioSource liftSource;
    private BoxCollider boxCollider;
    [SerializeField] private FlipSwitch trigger;
    [SerializeField] private float closedLength = 3.514f;
    [SerializeField] private float openLength = 1;
    public float lerp { get; private set; } = 0.5f;
    [SerializeField] private bool initiallyClosed = true;
    [SerializeField] private float lerpSpeed = 1;

    public void Start()
    {
        boxCollider = GetComponent<BoxCollider>();
        liftSource = GetComponent<AudioSource>();
        lerp = initiallyClosed ? -0.1f : 1.1f;
    }

    private void Update()
    {
        //If the switch is in the "top" state and the door is not fully open, increase the lerp value
        if (trigger.switchState == FlipSwitch.State.top && lerp < 1)
            lerp += lerpSpeed * Time.deltaTime;
        //If the switch is in the "bottom" state and the door is not fully closed, decrease the lerp value
        else if (trigger.switchState == FlipSwitch.State.bottom && lerp > 0)
            lerp -= lerpSpeed * Time.deltaTime;

        //If there's a doorLerpOffset set in the switch component, set the lerp value to it and reset the offset, this is used to delay the opening of the door (for example the outer door opens after the smoke effect)
        if (trigger.doorLerpOffset != 0)
        {
            lerp = trigger.doorLerpOffset;
            trigger.doorLerpOffset = 0;
        }

        if (!liftSource.isPlaying && lerp < 1 && lerp > 0)
            PlayLift();

        //If the door is fully open, disable the collider component so that the player can pass through
        if (lerp > 1 && trigger.switchState != FlipSwitch.State.falling)
        {
            if (boxCollider.enabled == true)
                boxCollider.enabled = false;
        }
        else if (boxCollider.enabled == false)
            boxCollider.enabled = true;

        transform.GetChild(0).localScale = new Vector3(0, 1, 1) + Vector3.right * Mathf.Lerp(closedLength, openLength, lerp);
        transform.GetChild(1).localScale = new Vector3(0, 1, 1) - Vector3.right * Mathf.Lerp(closedLength, openLength, lerp);
    }

    void PlayLift()
    {
        StartCoroutine(CoPlayLift());
    }
    IEnumerator CoPlayLift()
    {
        float startVolume = liftSource.volume;

        liftSource.volume = 0;
        liftSource.Play();

        while (liftSource.volume < startVolume)
        {
            liftSource.volume += startVolume * Time.deltaTime * 5;
            yield return new WaitForEndOfFrame();
        }

        yield return new WaitUntil(new System.Func<bool>(() => lerp <= 0 || lerp >= 1));

        while (liftSource.volume > 0)
        {
            liftSource.volume -= startVolume * Time.deltaTime * 5;
            yield return new WaitForEndOfFrame();
        }

        liftSource.Stop();
        liftSource.volume = startVolume;
    }
}
