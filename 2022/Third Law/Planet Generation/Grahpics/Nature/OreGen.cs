using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OreGen : MonoBehaviour
{
    private float sqrMaxNatureDist;
    private int oreCount = 0;
    [SerializeField] private Transform[] bounds;

    private AudioSource pickSource;
    [SerializeField] private AudioClip pickSound;

    public void Init(int _layer, int seed)
    {
        Random.InitState(seed);

        //50% chance no ore is generated on the rock
        bool justRock = Random.value < 0.5f;

        pickSource = GetComponent<AudioSource>();

        gameObject.layer = _layer;
        for (int i = 0; i < transform.childCount - 1; i++)
        {
            transform.GetChild(i).gameObject.layer = _layer;
            //Each ore looks different
            transform.GetChild(i).GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_seed", Random.Range(-9999, 9999));
            bool addRock = justRock && Random.value < 0.5f;
            transform.GetChild(i).gameObject.SetActive(addRock);
            if (addRock)
                oreCount++;
        }
        //The base stone is the last child, we want it to be active but also on the planet's layer
        transform.GetChild(transform.childCount - 1).gameObject.layer = _layer; 

        Random.InitState(System.Environment.TickCount);
    }

    public void SetMaterials(Planet planet, Vector3 relativePosition)
    {
        sqrMaxNatureDist = planet.maxNatureDist * planet.maxNatureDist;

        for (int i = 0; i < transform.childCount; i++)
        {
            Material oreMat = new Material(transform.GetChild(i).GetComponent<MeshRenderer>().sharedMaterial);
            transform.GetChild(i).GetComponent<MeshRenderer>().sharedMaterial = oreMat;

            oreMat.SetFloat("_minElevation", planet.planetMesh.elevationData.Min);
            oreMat.SetFloat("_maxElevation", planet.planetMesh.elevationData.Max);
            oreMat.SetTexture("_planetTexture", planet.terrainMat.GetTexture("_planetTexture"));
            oreMat.SetVector("_position", relativePosition);
        }
    }

    private IEnumerator RenderAtLOD(float time)
    {

        while (true)
        {
            yield return new WaitForSeconds(time);

            float LOD = Mathf.InverseLerp(0, sqrMaxNatureDist, (transform.position - Camera.main.transform.position).sqrMagnitude);

            //If the player is looking at the ore and there is sufficient ore for its proximity to warrent its highlighting on the UI
            if (LOD < 0.1f * oreCount && Physics.Raycast(transform.position, Camera.main.transform.position - transform.position, out RaycastHit hitInfo) && LayerMask.NameToLayer("Player") == hitInfo.collider.gameObject.layer)
            {
                RequestBounds();
            }

        }
    }
    void RequestBounds()
    {
        RoboVision.TargetBounds targetBounds = new RoboVision.TargetBounds(bounds);
        if (RoboVision.highlightBounds.ID == transform.GetInstanceID() && targetBounds.golfScore == float.MaxValue)
            //Forget about highlighting this ore if it is too close or too far
            RoboVision.highlightBounds = new RoboVision.TargetBounds(float.MaxValue);
        else if (RoboVision.highlightBounds.ID == transform.GetInstanceID() || targetBounds.golfScore < RoboVision.highlightBounds.golfScore)
            //Update the bounds for this highlighted ore
            RoboVision.highlightBounds = targetBounds;
    }

    void Start()
    {
        StartCoroutine(RenderAtLOD(0.5f));
    }

    private void Update()
    {
        if (oreCount == 0 || !CameraState.CamIsInteractingW(transform.position, 15))
            return;

        for (int i = 0; i < 3; i++)
        {
            if (transform.GetChild(i).gameObject.activeSelf && CameraState.CamIsInteractingW(transform.GetChild(i).position, 7))
            {
                //Collect the ore
                InventoryUI.fuelRemaining += InventoryUI.player.Stat("robotFuelPerOre");
                InventoryUI.robotMetalCount += 4 + Random.Range(0, 3 - i);
                transform.GetChild(i).gameObject.SetActive(false);
                oreCount--;
                pickSource.PlayOneShot(pickSound);
                break;
            }
        }
    }
}
