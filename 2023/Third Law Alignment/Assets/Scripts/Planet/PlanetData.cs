using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlanetData : MonoBehaviour
{
    public PlanetGen PlanetTerrain { get; private set; }
    public PlanetMaterial PlanetMaterial { get; private set; }
    public PlanetEffects PlanetEffects { get; private set; }
    public PlanetFoilage PlanetFoilage { get; private set; }

    [SerializeField] private Rand.Seed seed;
    [SerializeField] private bool ignoreSeedRandInit;
    private Rand rand;
    [SerializeField] private float maxPlanetRadius;
    [Range(0, 1)] [SerializeField] private float avgTemperature;

    public void Randomise()
    {
        seed = Rand.Seed.RandomSeed();
        Initialise();
    }
    public void Initialise()
    {
        if (ignoreSeedRandInit)
        {
            rand = new Rand(seed = Rand.Seed.RandomSeed());
            avgTemperature = rand.value;
        }
        else
            rand = new Rand(seed);

        PlanetTerrain = transform.GetChild(0).GetComponent<PlanetGen>();
        PlanetMaterial = transform.GetChild(0).GetComponent<PlanetMaterial>();
        PlanetEffects = transform.GetChild(1).GetComponent<PlanetEffects>();
        PlanetFoilage = transform.GetChild(2).GetComponent<PlanetFoilage>();
        
        PlanetTerrain.Initialise(rand.PsuedoNewSeed(), maxPlanetRadius);
        PlanetMaterial.Initialise(rand.PsuedoNewSeed(), PlanetTerrain.pathMaker.PlanetPathMeshes, maxPlanetRadius, avgTemperature);
        PlanetEffects.Initialise(rand.PsuedoNewSeed(), maxPlanetRadius, PlanetTerrain.OceanRadius, PlanetMaterial.biomeColour1, PlanetMaterial.biomeColour2);
        PlanetFoilage.Initialise(rand.PsuedoNewSeed(), PlanetTerrain.GetPlanetSubmeshes(), PlanetTerrain.Radius, PlanetTerrain.OceanRadius, PlanetMaterial.biomeColour1, PlanetMaterial.biomeColour2);
        
        PlanetEffects.SetPlanetPosition(transform.position);
    }

    public void UpdateColliders(IEnumerable<ZeroWeight> collidingObjects)
    {
        PlanetTerrain.UpdateColliders(collidingObjects);
    }

}
