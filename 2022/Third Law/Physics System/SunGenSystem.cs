using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SunGenSystem : MonoBehaviour
{
    [SerializeField] private TMPro.TextMeshProUGUI systemText;
    public int numPlanets;
    [SerializeField] private int numMoons;
    public Planet[] celestialBodies;
    public GameObject Grass;
    public GameObject Stone;
    public Vector2 minMaxDist;
    public int lordSeed;
    private System.Random masterPrng;


    private void Start()
    {
        //transform.position = Random.insideUnitCircle * 10000000000;
        //lordSeed = Random.Range(0, 999999);
        Segment.InstantiateFoilage(this);
    }
    public void Generate(int seed, Vector3 position, float radius, Color sunColour)
    {
        transform.GetChild(1).GetComponent<MeshRenderer>().sharedMaterial.SetColor("_sunColour", sunColour);
        transform.GetChild(1).GetComponent<MeshRenderer>().sharedMaterial.SetColor("_sunColour", sunColour);
        Generate(seed, position, radius);
    }
    public void Generate(int seed, Vector3 position, float radius)
    {
        PhysicsUpdate.AddWeight(GetComponent<Weight>());

        lordSeed = seed;
        Random.InitState(lordSeed);
        masterPrng = new System.Random(lordSeed);

        numPlanets = Random.Range(0, 5);

        transform.position = position;
        transform.GetChild(1).localScale = 2 * radius * Vector3.one;

        for (int i = 0; i < celestialBodies.Length; i++)
        {
            if (i < numPlanets)
                StartCoroutine(Init(i));
            else
                celestialBodies[i].gameObject.SetActive(false);
        }
    }

    IEnumerator Init(int i)
    {
        systemText.enabled = true;
        yield return new WaitForSeconds(Time.deltaTime);
        Planet planet = celestialBodies[i];
        planet.gameObject.SetActive(true);

        Random.InitState(masterPrng.Next(-9999, 9999));

        float speed = Mathf.Sqrt(GetComponent<Weight>().mass * Weight.gConst);
        Vector3 orbitNormal = Vector3.Slerp(Vector3.up, Random.onUnitSphere, 0.2f);
        Vector3 startDir = Vector3.Cross(orbitNormal, Random.onUnitSphere).normalized;

        //Such that the planet orbits the sun in an approximate circle on a plane similar to the galaxy's
        Vector3 initialVelocity = Vector3.Cross(orbitNormal, startDir).normalized * speed;

        planet.Create(transform.position + startDir * Random.Range(minMaxDist.x, minMaxDist.y), initialVelocity, masterPrng);
        PhysicsUpdate.AddWeight(planet.GetComponent<Weight>());

        systemText.enabled = false;
    }
}
