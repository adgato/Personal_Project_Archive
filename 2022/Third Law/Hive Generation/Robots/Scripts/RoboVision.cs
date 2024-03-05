using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RoboVision : MonoBehaviour
{

    //Structure which assignes a score to a target requested to be highlighted
    public struct TargetBounds
    {
        public int ID;
        public Transform[] bounds;
        public float golfScore;
        public float timeTargeted;

        public TargetBounds(Transform[] bounds)
        {
            ID = bounds[0].GetInstanceID();
            this.bounds = bounds;

            Vector3 toBounds = bounds[0].position - Camera.main.transform.position;
            float distance = toBounds.magnitude;
            bool valid = Vector3.Dot(toBounds / distance, Camera.main.transform.forward) > 0.5 && distance > 10;

            golfScore = valid ? distance : float.MaxValue;
            timeTargeted = Time.realtimeSinceStartup;
        }
        public TargetBounds(Transform planetBounds)
        {
            bounds = new Transform[planetBounds.childCount + 1];
            bounds[0] = planetBounds;
            ID = bounds[0].GetInstanceID();

            for (int i = 0; i < planetBounds.childCount; i++)
                bounds[i + 1] = planetBounds.GetChild(i);

            golfScore = float.MaxValue;
            timeTargeted = Time.realtimeSinceStartup;
        }
        public TargetBounds(float golfScoreToBeat)
        {
            ID = -1;
            bounds = null;
            golfScore = golfScoreToBeat;
            timeTargeted = Application.isPlaying ? Time.realtimeSinceStartup : float.MaxValue;
        }
    }

    public static TargetBounds highlightBounds;
    private IEnumerator updateScreenEffects;

    [SerializeField] private Material screenEffectsMat;

    [SerializeField] private Color eyeColour = Color.cyan;
    [SerializeField] private Color dangerColour = Color.red;
    public static Color visionColour;

    [SerializeField] private Slider fuel;
    [SerializeField] private Slider oxygen;
    [Range(0, 1)]
    public float blur01 = 1;
    private float danger01 = -1;

    public CameraState.LockState isLocked { get; private set; }
    private bool locking;
    private Vector3 ogLocalPos;

    private Vector3 startPos;
    private Vector3 endPos;
    private Quaternion startRot;
    private Quaternion endRot;
    private Transform endTransform;
    private float lerp = 1;
    public float lockSpeed = 2;

    [SerializeField] private RobotWeight robotWeight;
    [SerializeField] private Transform Body;
    [SerializeField] private Transform Arms;
    [SerializeField] private Transform Chest;
    [SerializeField] private Transform Head;

    public Transform[] Eyes;

    [SerializeField] private float lookSpeed = 2;
    [SerializeField] private float inertia = 100;
    [SerializeField] private float lookHoldTime = 0.5f;

    [SerializeField] private Vector2 minMaxLookClamps;
    [SerializeField] private float maxHeadRot;
    [SerializeField] private float maxEyeRot;
    private float bodyRot = 0;

    private float xRotation = 0;
    private float yRotation = 0;

    private float nearClipPlaneLerp;
    private float cameraIsPausedLerp = 1.1f;
    public int cameraIsPausedLerpDirection { get; private set; }

    private float deadBlurLerp = 2;

    private float yRotStillCount = 0;

    private bool prevOutRange = false;
    private bool prevGrounded = false;
    private bool justLanded = false;

    private void Start()
    {
        CameraState.isPaused = false;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        highlightBounds = new TargetBounds(float.MaxValue);

        isLocked = CameraState.LockState.unlocked;

        ogLocalPos = transform.localPosition;
        endPos = transform.parent.position + ogLocalPos;

        updateScreenEffects = UpdateScreenEffects(0.5f);
        StartCoroutine(updateScreenEffects);
    }
    private void OnValidate()
    {
        if (screenEffectsMat != null && !Application.isPlaying)
            screenEffectsMat.SetFloat("blur01", 0);
    }
    private void Update()
    {
        //Kill player if they go into the ocean
        CameraState.isDead |= robotWeight.sigWeight != null && (Camera.main.transform.position - robotWeight.sigWeight.position).sqrMagnitude < (robotWeight.sigWeight.planet.planetValues.radius - 10) * (robotWeight.sigWeight.planet.planetValues.radius - 10);
        //Kill player if they go into empty space
        CameraState.isDead |= robotWeight.sigWeight == null && !CameraState.inShip;

        nearClipPlaneLerp = Mathf.Clamp01(nearClipPlaneLerp + Time.deltaTime * (CameraState.flyingShip ? 1 : -1));
        Camera.main.nearClipPlane = Mathf.Lerp(0.4f, 1.5f, nearClipPlaneLerp);
        fuel.GetComponentInParent<Canvas>().planeDistance = Camera.main.nearClipPlane + 0.01f;

        EnableEyeRays(!CameraState.inShip || InventoryUI.shipEngineOn01 <= 0, !CameraState.inShip);

        deadBlurLerp += Time.smoothDeltaTime;
        //Pixalate screen to black if dead
        if (deadBlurLerp < 2)
        {
            PixalateBlack(Mathf.Pow(Mathf.Max(deadBlurLerp, 1) - 1, 4));
            screenEffectsMat.SetFloat("temperature01", CameraState.inShip || CameraState.inHive ? 0.5f : InventoryUI.robotTemperature);
        }
        else if (!CameraState.isPaused)
        {
            //Pixalate screen based on forceMeter

            float blur01 = robotWeight.forceMeter;

            if (CameraState.flyingShip && robotWeight.sigWeight == null)
                blur01 /= 1000;
            else if (CameraState.flyingShip)
                blur01 /= 10;
            else
                blur01 /= 0.5f;

            blur01 = Mathf.Floor(blur01 * 20) / 20;

           
            screenEffectsMat.SetFloat("blur01", Mathf.Clamp(1 - blur01, 0.05f, 1));
            screenEffectsMat.SetFloat("temperature01", CameraState.inShip || CameraState.inHive ? 0.5f : InventoryUI.robotTemperature);
        }
    }


    private IEnumerator UpdateScreenEffects(float time)
    {
        while (true)
        {
            yield return new WaitForSeconds(time);

            if (fuel == null || oxygen == null)
                continue;

            bool nl = highlightBounds.bounds == null;
            screenEffectsMat.SetVector("target", nl ? Vector3.zero : highlightBounds.bounds[0].position);
            screenEffectsMat.SetVector("b1", nl ? Vector3.zero : highlightBounds.bounds[1].position);
            screenEffectsMat.SetVector("b2", nl ? Vector3.zero : highlightBounds.bounds[2].position);
            screenEffectsMat.SetVector("b3", nl ? Vector3.zero : highlightBounds.bounds[3].position);
            screenEffectsMat.SetVector("b4", nl ? Vector3.zero : highlightBounds.bounds[4].position);
            screenEffectsMat.SetVector("b5", nl ? Vector3.zero : highlightBounds.bounds[5].position);
            screenEffectsMat.SetVector("b6", nl ? Vector3.zero : highlightBounds.bounds[6].position);
            screenEffectsMat.SetVector("b7", nl ? Vector3.zero : highlightBounds.bounds[7].position);
            screenEffectsMat.SetVector("b8", nl ? Vector3.zero : highlightBounds.bounds[8].position);

            if (Time.realtimeSinceStartup > highlightBounds.timeTargeted + 5)
                highlightBounds = new TargetBounds(float.MaxValue);

            float new_danger01 = 1 - fuel.value / fuel.maxValue;
            if (danger01 == new_danger01)
                continue;

            danger01 = new_danger01;

            visionColour = Color.Lerp(eyeColour, dangerColour, danger01);

            screenEffectsMat.SetColor("linesCol", visionColour);
            screenEffectsMat.SetFloat("danger01", danger01);

            foreach (Light light in transform.GetComponentsInChildren<Light>())
                light.color = visionColour;
        }

    }

    //Fix camera position and rotation
    public void Lock(Transform cameraPos)
    {
        locking = true;
        lerp = 0;

        startPos = transform.position;
        startRot = transform.rotation;

        endTransform = cameraPos;
        endPos = Vector3.zero;

        EnableEyeRays(false, false);
    }
    //Allow the player to rotate the camera again
    public void Unlock()
    {
        locking = false;
        lerp = 0;

        startPos = transform.position;
        endPos = transform.parent.position + ogLocalPos;
        startRot = transform.rotation;
        endRot = Body.rotation * Quaternion.Euler(-xRotation, yRotation, 0);

        EnableEyeRays(true, true);
    }

    private void EnableEyeRays(bool eyesOnOff, bool bloomerOnOff)
    {
        transform.GetChild(0).GetComponent<Light>().enabled = eyesOnOff;
        transform.GetChild(1).GetComponent<Light>().enabled = eyesOnOff;
        transform.GetChild(2).GetComponent<Light>().enabled = bloomerOnOff;
    }

    public void PixalateBlack(float x)
    {
        screenEffectsMat.SetFloat("blur01", x);
        screenEffectsMat.SetFloat("dark01", x);
    }
    public void KillTransition()
    {
        PixalateBlack(0);
        deadBlurLerp = 0;
    }


    void LateUpdate()
    {
        if (Input.GetKeyDown(KeyCode.Escape) || PauseMenu.selectedPauseMenuOption == 0 || PauseMenu.selectedPauseMenuOption == 1 || PauseMenu.selectedPauseMenuOption == 2 || PauseMenu.selectedPauseMenuOption == 3)
        {
            Cursor.visible ^= true;
            Cursor.lockState = Cursor.lockState == CursorLockMode.Locked ? CursorLockMode.None : CursorLockMode.Locked;

            if (CameraState.isPaused)
            {
                cameraIsPausedLerp = Mathf.Max(0, cameraIsPausedLerp);
                cameraIsPausedLerpDirection = 1;
            }
            else
            {
                CameraState.isPaused = true;
                cameraIsPausedLerp = Mathf.Min(1, cameraIsPausedLerp);
                cameraIsPausedLerpDirection = -1;
            }
        }

        //If the camera is paused or pausing, return after updating cameraIsPausedLerp, apply pixlation transition to pause menu if the death transition is not in effect
        if (cameraIsPausedLerp >= 0 && cameraIsPausedLerp <= 1)
        {
            cameraIsPausedLerp += 2 * Time.deltaTime * cameraIsPausedLerpDirection;
            if (deadBlurLerp > 2)
                PixalateBlack(Mathf.Pow(cameraIsPausedLerp, 4));
            return;
        }
        else if (CameraState.isPaused)
        {
            CameraState.isPaused = cameraIsPausedLerpDirection == -1;
            if (deadBlurLerp > 2)
                PixalateBlack(Mathf.Clamp01(cameraIsPausedLerp));
            return;
        }
        if (deadBlurLerp > 2)
        {
            screenEffectsMat.SetFloat("dark01", 1);
        }


        //If the camera is not locked, set the target position to the original camera position
        if (!locking)
            endPos = transform.parent.position + ogLocalPos;

        isLocked = locking ? CameraState.LockState.locked : CameraState.LockState.unlocked;

        //If the camera has finished lerping, set the camera's position and rotation to the final values
        if (lerp >= 1)
        {
            transform.position = endPos == Vector3.zero ? endTransform.position : endPos;
            transform.rotation = endPos == Vector3.zero ? endTransform.rotation : endRot;
        }
        //If the camera is still lerping, set the camera's position and rotation to the interpolated values based on the current lerp value
        if (lerp < 1)
        {
            isLocked = CameraState.LockState.changing;
            transform.position = Vector3.Slerp(startPos, endPos == Vector3.zero ? endTransform.position : endPos, lerp);
            transform.rotation = Quaternion.Slerp(startRot, endPos == Vector3.zero ? endTransform.rotation : endRot, lerp);
            lerp += lockSpeed * Time.smoothDeltaTime;
        }
        //If the camera is not locked and the lerp is finished, handle camera rotation based on user input
        else if (!locking)
        {
            justLanded |= !prevGrounded && robotWeight.kinematicBody.isGrounded;

            if (justLanded && xRotation < minMaxLookClamps.x)
                xRotation = Mathf.MoveTowards(xRotation, minMaxLookClamps.x, Time.deltaTime * 1000);
            else if (justLanded && xRotation > minMaxLookClamps.y)
                xRotation = Mathf.MoveTowards(xRotation, minMaxLookClamps.y, Time.deltaTime * 1000);
            else
            {
                xRotation += Input.GetAxis("Mouse Y") * lookSpeed;
                justLanded = false;
            }
            yRotation += Input.GetAxis("Mouse X") * lookSpeed; 

            LookTowards(ref xRotation, yRotation);

            transform.rotation = Body.rotation * Quaternion.Euler(-xRotation, yRotation, 0);
        }
    }

    void LookTowards(ref float xRotation, float yRotation)
    {
        prevGrounded = robotWeight.kinematicBody.isGrounded;

        //If in air (or just landed) the player rotates their body (Chest) only and can rotate it freely
        if (!prevGrounded || justLanded)
        {
            Chest.localRotation = Quaternion.Euler(xRotation, yRotation, 0);
            Head.localRotation = Quaternion.Euler(0, 0, 0);

            bodyRot = 0;
            yRotStillCount = lookHoldTime;
            prevOutRange = true;

            //Check if the player's pitch angle is out of range, and flip it if needed
            float flip = xRotation > 0 ? xRotation - 360 : xRotation + 360;
            xRotation = Mathf.Abs(xRotation) < Mathf.Abs(flip) ? xRotation : flip;
        }
        //Otherwise, the player rotates their head and eyes first and their body and arms follows with delay and their pitch view angle is clamped
        else
        {
            if (Input.GetAxis("Mouse X") == 0)
                yRotStillCount += Time.deltaTime;
            else
                yRotStillCount = 0;

            if (yRotStillCount > lookHoldTime || (prevOutRange != Mathf.Abs(yRotation - bodyRot) > maxHeadRot && !(Mathf.Abs(yRotation - bodyRot) > maxHeadRot)))
            {
                bodyRot = yRotation;
                Chest.localRotation = Quaternion.Euler(0, yRotation, 0);
            }

            xRotation = Mathf.Clamp(xRotation, minMaxLookClamps.x, minMaxLookClamps.y);

            if (Mathf.Abs(yRotation - bodyRot) > maxHeadRot)
            {
                Chest.localRotation = Quaternion.Euler(0, yRotation, 0);
                Head.localRotation = Quaternion.Euler(xRotation, 0, 0);
            }
            else if (Quaternion.Angle(Head.rotation, Quaternion.Euler(xRotation, yRotation + 180, 0f)) > maxEyeRot)
                Head.localRotation = Quaternion.Euler(xRotation, yRotation - bodyRot, 0);

            prevOutRange = Mathf.Abs(yRotation - bodyRot) > maxHeadRot;
        }

        Eyes[0].rotation = Body.rotation * Quaternion.Euler(90 - xRotation, yRotation, 0);
        Eyes[1].rotation = Body.rotation * Eyes[0].rotation;

        Arms.rotation = Quaternion.Slerp(Arms.rotation, Chest.rotation, Time.deltaTime / inertia);
    }
}
