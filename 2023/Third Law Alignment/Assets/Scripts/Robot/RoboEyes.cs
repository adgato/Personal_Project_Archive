using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RoboEyes
{
    private RobotBody r;

    private Material leftEyeMat;
    private Material rightEyeMat;
    private Material laserMat;

    private MeshRenderer leftEyeRenderer;
    private MeshRenderer rightEyeRenderer;
    private MeshRenderer leftLaserRenderer;
    private MeshRenderer rightLaserRenderer;

    private BoxCollider[] laserHitBox;

    [SerializeField] private Material eyeMatPrefab;
    [SerializeField] private Material laserMatPrefab;

    [SerializeField] private Color eyeColour;
    [SerializeField] private float eyeBrightness;

    private bool eyesEnabled = true;

    [SerializeField] private Vector2 eyeRotation01;
    [SerializeField] private Vector2 eyeSlitWidth01;

    public Vector2 activeDistanceRange = Vector2.zero;

    private Color laserColour1;
    private Color laserColour2;

    // Start is called before the first frame update
    public void Start(RobotBody robot)
    {
        r = robot;

        leftEyeRenderer = r.body.LeftEye.GetChild(0).GetComponent<MeshRenderer>();
        rightEyeRenderer = r.body.RightEye.GetChild(0).GetComponent<MeshRenderer>();
        leftLaserRenderer = r.body.LeftEye.GetChild(1).GetChild(0).GetComponent<MeshRenderer>();
        rightLaserRenderer = r.body.RightEye.GetChild(1).GetChild(0).GetComponent<MeshRenderer>();

        laserHitBox = new BoxCollider[1] { leftLaserRenderer.GetComponent<BoxCollider>() };

        leftEyeMat = new Material(eyeMatPrefab);
        rightEyeMat = new Material(eyeMatPrefab);
        laserMat = new Material(laserMatPrefab);

        leftEyeRenderer.sharedMaterial = leftEyeMat;
        rightEyeRenderer.sharedMaterial = rightEyeMat;
        leftLaserRenderer.sharedMaterial = laserMat;
        rightLaserRenderer.sharedMaterial = laserMat;

        Initialise();
    }

    // Update is called once per frame
    public void Update()
    {

    }

    /// <param name="brightness">Brightness as a scale factor of default brightness</param>
    public void SetEyeBrightness(float brightness)
    {
        if (eyesEnabled)
            brightness *= eyeBrightness;
        else
            brightness = 0;

        leftEyeMat.SetFloat("_Brightness", brightness);
        rightEyeMat.SetFloat("_Brightness", brightness);
    }
    public void SetLaserScale(float length)
    {
        leftLaserRenderer.transform.localScale = new Vector3(1, 1, length);
        rightLaserRenderer.transform.localScale = new Vector3(1, 1, length);
    }

    public void SetActiveDistanceRange(Vector2 activeDistanceRange)
    {
        laserMat.SetVector("_ActiveDistanceRange", activeDistanceRange);
        laserHitBox[0].center = new Vector3(-4.1f, 0, activeDistanceRange.y);
        laserHitBox[0].size = new Vector3(10, 2, 2 * activeDistanceRange.y);
    }

    public void SetLasersEnabled(bool enabled)
    {
        leftLaserRenderer.enabled = enabled;
        rightLaserRenderer.enabled = enabled;
        laserHitBox[0].enabled = enabled;
    }

    /// <summary>
    /// Disabling the eyes prevents changing the brightness of the eyes from turning them back on
    /// </summary>
    public void SetEyesEnabled(bool enabled)
    {
        eyesEnabled = enabled;
        SetEyeBrightness(1);
    }

    public BoxCollider[] GetLaserHitBox()
    {
        return laserHitBox;
    }

    private void Initialise()
    {
        laserMat.SetVector("_ActiveDistanceRange", Vector2.zero);

        leftEyeMat.SetColor("_EyeColour", eyeColour);
        rightEyeMat.SetColor("_EyeColour", eyeColour);

        leftEyeMat.SetFloat("_Brightness", eyeBrightness);
        rightEyeMat.SetFloat("_Brightness", eyeBrightness);

        leftEyeMat.SetFloat("_Rotation01", eyeRotation01.x);
        rightEyeMat.SetFloat("_Rotation01", eyeRotation01.y);

        leftEyeMat.SetFloat("_SlitWidth", eyeSlitWidth01.x);
        rightEyeMat.SetFloat("_SlitWidth", eyeSlitWidth01.y);

        Color.RGBToHSV(eyeColour, out float H, out _, out _);
        laserColour1 = Color.HSVToRGB(H, 0.95f, 1);
        laserColour2 = Color.HSVToRGB(H, 0.90f, 1);

        laserMat.SetColor("_Colour1", laserColour1);
        laserMat.SetColor("_Colour2", laserColour2);
    }
}
