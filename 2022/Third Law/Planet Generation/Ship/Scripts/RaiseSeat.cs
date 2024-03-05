using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RaiseSeat : MonoBehaviour
{
    private AudioSource liftSource;
    [SerializeField] private ShipWeight shipWeight;
    [SerializeField] private FlipSwitch trigger;
    [SerializeField] private float topHeight = 1.5f;
    [SerializeField] private float bottomHeight = 0.3f;
    [SerializeField] private Vector3 ogLocalPos;
    [SerializeField] private Vector3 prevSpeed;
    [SerializeField] private Vector3 deltaSpeed;

    private float lerp;
    [SerializeField] private float lerpSpeed = 1;

    [SerializeField] private bool interacting = false;

    private void Start()
    {
        liftSource = GetComponent<AudioSource>();
        ogLocalPos = transform.localPosition;
        CameraState.flyingShip = false;
        lerp = 0;
    }

    private void Update()
    {
        if (trigger.switchState == FlipSwitch.State.top && lerp < 1)
            lerp += lerpSpeed * Time.deltaTime;
        else if (trigger.switchState == FlipSwitch.State.bottom && lerp > 0)
            lerp -= lerpSpeed * Time.deltaTime;

        transform.GetChild(0).localScale = new Vector3(transform.GetChild(0).localScale.x , transform.GetChild(0).localScale.y, Mathf.Lerp(bottomHeight, topHeight, lerp));

        if (!liftSource.isPlaying && lerp < 1 && lerp > 0)
            PlayLift();

        if (!interacting && CameraState.InLockState(CameraState.LockState.unlocked) && (transform.position - Camera.main.transform.position).sqrMagnitude < 1 && trigger.switchState == FlipSwitch.State.top && lerp >= 1)
        {
            interacting = true;
            CameraState.LockCamera(transform);
            CameraState.flyingShip = true;
        }
        else if (interacting && CameraState.InLockState(CameraState.LockState.locked) && Input.GetKeyDown(KeyCode.E))
        {
            CameraState.UnlockCamera();
            CameraState.flyingShip = false;
            //but still interacting
        }
        else if (interacting && (transform.position - Camera.main.transform.position).sqrMagnitude > 1)
        {
            interacting = false;
            if (trigger.switchState == FlipSwitch.State.top)
                trigger.overRide = true;
        }

        transform.localPosition = ogLocalPos;
        //If flying ship, move the target position of the camera based on the delta speed so the player leans backward while accelerating forward for instance
        if (CameraState.flyingShip)
        {
            Vector3 newSpeed = shipWeight.velocity;
            Vector3 newDelta = newSpeed - prevSpeed;
            if (newDelta.sqrMagnitude > 16)
                newDelta = 4 * newDelta.normalized;

            deltaSpeed = Vector3.Lerp(deltaSpeed, 0.025f * newDelta, 0.05f);

            transform.position -= deltaSpeed;
            prevSpeed = newSpeed;
        }

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
