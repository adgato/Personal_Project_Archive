using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrassFollower : MonoBehaviour
{
   
    [SerializeField] private Planet planet;
    [SerializeField] private GameObject foilagePrefab;
    [SerializeField] private Material foilageMatPrefab;
    [Range(0, 1)]
    [SerializeField] private float grassDensity = 0.01f;
    [Range(0, 1)]
    [SerializeField] private float minSlope = 0.7f;
    [SerializeField] private int maxGrassCount = 500;
    [SerializeField] private float grassCubeSize = 225;
    [SerializeField] private float maxHeight;
    private int index;
    
    public bool debugLines;

    private float stepSize = 5;
    private GameObject[] surroundingGrass;
    public bool active = false;
    private Vector3 anchor;
    private Vector3 planetNormal;

    // Start is called before the first frame update
    void Start()
    {
        

        //grassCubeSize = planet.transform.GetChild(2).GetComponent<PlanetEffect>().fogRange * 2f;
        stepSize = grassCubeSize / 40;

        surroundingGrass = new GameObject[maxGrassCount];
        for (int i = 0; i < surroundingGrass.Length; i++)
        {
            surroundingGrass[i] = Instantiate(foilagePrefab, transform);
            surroundingGrass[i].name = foilagePrefab.name;

            //Set grass or stone material 
            if (surroundingGrass[i].name == "Grass")
            {
                surroundingGrass[i].transform.GetChild(0).GetChild(0).GetComponent<MeshRenderer>().sharedMaterial = new Material(foilageMatPrefab);
                surroundingGrass[i].transform.GetChild(0).GetChild(0).gameObject.layer = planet.gameObject.layer;
                for (int j = 1; j < surroundingGrass[i].transform.childCount; j++)
                {
                    surroundingGrass[i].transform.GetChild(j).GetChild(0).GetComponent<MeshRenderer>().sharedMaterial = surroundingGrass[i].transform.GetChild(0).GetChild(0).GetComponent<MeshRenderer>().sharedMaterial;
                    surroundingGrass[i].transform.GetChild(j).GetChild(0).gameObject.layer = planet.gameObject.layer;
                }
            }
            else if (surroundingGrass[i].name == "Stones")
            {
                surroundingGrass[i].transform.GetChild(0).GetComponent<MeshRenderer>().sharedMaterial = new Material(foilageMatPrefab);
                surroundingGrass[i].transform.GetChild(0).gameObject.layer = planet.gameObject.layer;
                for (int j = 1; j < surroundingGrass[i].transform.childCount; j++)
                {
                    surroundingGrass[i].transform.GetChild(j).GetComponent<MeshRenderer>().sharedMaterial = surroundingGrass[i].transform.GetChild(0).GetComponent<MeshRenderer>().sharedMaterial;
                    surroundingGrass[i].transform.GetChild(j).gameObject.layer = planet.gameObject.layer;
                }
            }
            else
                Debug.LogWarning("Warning: foilage type of NAME " + surroundingGrass[i].name + " not recognised.");


            //Reference the first child material to change them all
        }

        
    }

    private void Update()
    {
        maxHeight = planet.planetMesh.elevationData.Max;

        planetNormal = anchor - planet.transform.position;

        Vector3 localUp = planetNormal.normalized;
        Vector3 localRight = Vector3.Cross(localUp, Vector3.forward); //right is arbitrary
        Vector3 localForward = Vector3.Cross(localUp, localRight);

        localRight *= stepSize;
        localForward *= stepSize;

        //anchor in place because player position is always changing and we need an origin fixed on our grid of points
        for (int x = -1; x <= 1; x += 2)
        {
            for (int z = -1; z <= 1; z += 2)
            {
                Vector3 offset = localRight * x + localForward * z;

                Vector3 new_anchor = anchor + offset;
                if (debugLines)
                    Debug.DrawLine(anchor, new_anchor, Color.green);

                if ((new_anchor - Camera.main.transform.position).sqrMagnitude < (anchor - Camera.main.transform.position).sqrMagnitude)
                {
                    anchor = new_anchor;
                }
                    
            }
        }
    }

    // Update is called once per frame
    void UpdateGrass()
    {
        maxHeight = planet.planetMesh.elevationData.Max;

        if (!active && (planet.transform.position - Camera.main.transform.position).sqrMagnitude < maxHeight * maxHeight)
        {
            anchor = Camera.main.transform.position;
            active = true;
            foreach (GameObject grass in surroundingGrass)
            {
                grass.SetActive(true);
            }
        }
        else if (active && (planet.transform.position - Camera.main.transform.position).sqrMagnitude >= maxHeight * maxHeight)
        {
            active = false;
            foreach (GameObject grass in surroundingGrass)
            {
                grass.SetActive(false);
            }
        }

        if (!active)
            return;

        //demonstration here: https://www.geogebra.org/m/rty3tk8k
        Vector3 localUp = planetNormal.normalized;
        Vector3 localRight = new Vector3(1, 1, -(localUp.x + localUp.y) / localUp.z).normalized; //right is arbitrary
        Vector3 localForward = Vector3.Cross(localUp, localRight);

        localRight *= stepSize;
        localForward *= stepSize;

        if (debugLines)
            Debug.DrawLine(Camera.main.transform.position, anchor, Color.green, 0.5f);

        float scale = grassCubeSize / (2 * stepSize);


        for (float x = -scale; x < scale; x++)
        {
            for (float z = -scale; z < scale; z++)
            {

                Vector3 offset = localRight * x + localForward * z;

                Vector3 rayNormal = (planetNormal + offset).normalized;
                Vector3 v = rayNormal * 550;
                Vector3 clampNormal = Modulate(v, 7);
                rayNormal = clampNormal / 550;

                Physics.Raycast(planet.transform.position + rayNormal * maxHeight, -rayNormal, out RaycastHit hitInfo, planet.planetValues.radius);
                Vector3 grassPos = hitInfo.point;
                float slopeDot = Vector3.Dot(rayNormal, hitInfo.normal);

                if (debugLines)
                    Debug.DrawRay(planet.transform.position + rayNormal * maxHeight, -rayNormal * 10, Color.white, 0.5f);

                if (Random.value < grassDensity && slopeDot > minSlope && hitInfo.collider.gameObject.layer != 6) //the player
                {
                    surroundingGrass[index].transform.position = grassPos;
                    surroundingGrass[index].transform.rotation = Quaternion.LookRotation(hitInfo.normal, Vector3.Cross(hitInfo.normal, Random.onUnitSphere));

                    Material _grassMat = null;
                    if (surroundingGrass[index].name == "Grass")
                    {
                        surroundingGrass[index].transform.localScale = new Vector3(2, 2, Random.Range(1f, 2));

                        _grassMat = surroundingGrass[index].transform.GetChild(0).GetChild(0).GetComponent<MeshRenderer>().sharedMaterial;
                    }
                    else if (surroundingGrass[index].name == "Stones")
                    {
                        surroundingGrass[index].transform.localScale = Vector3.one * Random.Range(0.2f, 0.4f);

                        for (int i = 0; i < surroundingGrass[index].transform.childCount; i++)
                            surroundingGrass[index].transform.GetChild(i).gameObject.SetActive(Random.value < 0.5f);

                        _grassMat = surroundingGrass[index].transform.GetChild(0).GetComponent<MeshRenderer>().sharedMaterial;
                    }
                    else
                        Debug.LogWarning("Warning: foilage type of NAME " + surroundingGrass[index].name + " not recognised.");

                    _grassMat.SetFloat("_minElevation", planet.planetMesh.elevationData.Min);
                    _grassMat.SetFloat("_maxElevation", planet.planetMesh.elevationData.Max);
                    _grassMat.SetTexture("_planetTexture", planet.terrainMat.GetTexture("_planetTexture"));
                    _grassMat.SetVector("_position", grassPos - planet.transform.position);

                    index = (index + 1) % maxGrassCount;
                }
            }
        }
    }

    int SeedFromPosition(Vector3 position)
    {
        Vector3 planetNormal = (position - planet.transform.position).normalized;
        return Mathf.RoundToInt(10000 * planetNormal.x + 1000 * planetNormal.y + 100 * planetNormal.z);
    }

    Vector3 Modulate(Vector3 position, float clamp)
    {
        Vector3 p = position / clamp;
        return new Vector3(Mathf.Floor(p.x), Mathf.Floor(p.y), Mathf.Floor(p.z)) * clamp;
    }
}
