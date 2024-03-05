using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class CustomScreenEffects : MonoBehaviour
{
    [SerializeField] private Shader effectsShader;
    [SerializeField] private Texture3D simplexNoise;
    public VolumeProfile CameraVolume;
    private BlitMaterial blitMaterial;
    [SerializeField] private Vector3 sunDir = Vector3.right;
    public float temperature;
    [SerializeField] private bool tonemappingEnabled;
    [Min(1)]
    [SerializeField] private float tonemappingStrength = 4.5f;

    // Start is called before the first frame update
    void Start()
    {
        if (blitMaterial != null)
            blitMaterial.Dispose();

        blitMaterial = new BlitMaterial(new Material(effectsShader), -1);
        blitMaterial.Material.SetTexture("SimplexNoise", simplexNoise);
        blitMaterial.Enable();
    }

    private void OnDestroy()
    {
        if (blitMaterial != null)
            blitMaterial.Dispose();
    }
    // Update is called once per frame
    void Update()
    {
        if (PlayerRobotWeight.Player.Closest != null)
            blitMaterial.Material.SetFloat("darkness01", Mathf.InverseLerp(0, -0.25f, Vector3.Dot((PlayerRobotWeight.Player.Position - PlayerRobotWeight.Player.Closest.Position).normalized, sunDir)));
        blitMaterial.Material.SetFloat("temperature01", temperature);
        blitMaterial.Material.SetFloat("tonemappingValue", tonemappingStrength);
        blitMaterial.Material.SetInt("TonemappingEnabled", tonemappingEnabled.ToInt());
    }
}
